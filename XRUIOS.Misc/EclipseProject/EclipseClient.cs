using EclipseProject;
using MagicOnion;
using MagicOnion.Server;
using Org.BouncyCastle.Asn1.Cms;
using Pariah_Cybersecurity;
using System.Security.Cryptography;
using static EclipseProject.Security;
using Grpc.Net.Client;
using MagicOnion.Client;
using Org.BouncyCastle.Security;
using System.Text;
using static Pariah_Cybersecurity.EasyPQC;
using static Secure_Store.Storage;
using EclipseLCL;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Net.Http.Headers;
using System.Collections;
using System.Runtime;
using System.Threading.Tasks;
using MessagePack;
using MessagePack.Resolvers;

namespace EclipseProject
{
    public class EclipseClient
    {
        internal static AeadChannel? clientChannel {get; set;}
        internal static AeadChannel? serverChannel {get; set;}
        internal static IDiracService? api {get; set;}
        internal static GrpcChannel? Channel {get; set;}
        public static async Task Initialize()
        {
            ICollection<string>? addresses = SecureStore.Get<ICollection<string>>("Eclipse Server");
            if (addresses == null || addresses.Count == 0)
            {
                throw new Exception("No server addresses found in SecureStore.");
            }

            string serverAddr = addresses.First();
            var channel = GrpcChannel.ForAddress(serverAddr);

            api = MagicOnionClient.Create<IDiracService>(channel);

                // enrollment, create clientId/PSK and SecureStore them. server will have access if it's with the same user
            SecureRandom rand = new SecureRandom();
            byte[] PSK = new byte[32];
            string clientId = Guid.NewGuid().ToString();

            rand.NextBytes(PSK);
            SecureStore.Set(clientId, PSK);

            Dictionary<string, byte[]> pubKey = await api.EnrollAsync("demo", clientId);

            // handshake begin
            var secretResult = Keys.CreateSecret(pubKey);
            var sharedSecret = secretResult.key;
            var cipher = secretResult.text;
            byte[] nonceC = new byte[16];

            rand.NextBytes(nonceC);
            var serverResp = await api.BeginHandshakeAsync(clientId, cipher, nonceC);

            var keys = PrepareKeys(PSK, nonceC, serverResp.nonceS, sharedSecret);

            byte[] transcriptHash = SHA256.HashData(ByteArrayExtensions.Combine(Encoding.UTF8.GetBytes(clientId), cipher, nonceC, serverResp.nonceS, serverResp.sessionId, BitConverter.GetBytes(serverResp.epoch)));

            clientChannel = new AeadChannel(keys.k_c2s, serverResp.sessionId, clientId, 1, new Transcript(transcriptHash, "client-finished"));
            serverChannel = new AeadChannel(keys.k_s2c, serverResp.sessionId, clientId, 1, new Transcript(transcriptHash, "server-finished"));

            if (clientChannel.transcript.proof == null || serverChannel.transcript.proof == null)
            {
                throw new Exception("Invalid HMAC proof");
            }

            byte[] serverTranscriptRaw = await api.FinishHandshakeAsync(clientId, clientChannel.transcript.proof);

            for (int i = 0; i < serverTranscriptRaw.Length; i++)
            {
                if (serverTranscriptRaw[i] != serverChannel.transcript.proof[i])
                {
                    throw new Exception("Incorrect transcript HMAC");
                }
            }
        }
        public static async Task<T?> InvokeAsync<T>(string methodName, params (string key, object? value)[] args)
        {
            if (clientChannel == null || serverChannel == null || api == null)
            {
                throw new Exception("Handshake protocol failed.");
            }

            Dictionary<string, object?> parameters = args.ToDictionary(a => a.key, a => a.value);
            byte[] serializedEnv = clientChannel.PackAndEncrypt(methodName, parameters);
            byte[] serializedResp = await api.InvokeAsync(serializedEnv);
            T? result = serverChannel.UnpackResponse<T>(serializedResp);

            Console.WriteLine($"Response received.\nCONTENT: {result}");

            return result;
        }

        public static async void FinishAsync()
        {
            if (clientChannel == null || serverChannel == null || api == null)
            {
                throw new Exception("Handshake protocol failed.");
            }

            bool success = await api.FinishAsync(clientChannel.PackAndEncrypt<bool>("terminate", true));
            Console.WriteLine($"Terminating connection. Success: {success}");
        }

        public static async Task<IEnumerable<DiracFunction>> ListFunctions()
        {
            byte[] serializedFuncs = await api.ListFunctions();
            return MessagePackSerializer.Deserialize<IEnumerable<DiracFunction>>(serializedFuncs, ContractlessStandardResolver.Options);
        }
    }
}
public interface IDiracService : IService<IDiracService>
{
    UnaryResult<Dictionary<string, byte[]>> EnrollAsync(string clientName, string clientId);
    UnaryResult<(byte[] nonceS, byte[] sessionId, uint epoch)> BeginHandshakeAsync(string clientId, byte[] cipher, byte[] nonceC);
    UnaryResult<byte[]> FinishHandshakeAsync(string clientId, byte[] clientTranscript);
    UnaryResult<byte[]> InvokeAsync(byte[] serializedEnv);
    UnaryResult<bool> FinishAsync(byte[] serializedEnv);
    UnaryResult<byte[]> ListFunctions();
}