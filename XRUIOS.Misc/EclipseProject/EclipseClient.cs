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

namespace EclipseProject
{
    public class EclipseClient
    {
        private static AeadChannel? clientChannel {get; set;}
        private static AeadChannel? serverChannel {get; set;}
        private static IDiracService? api {get; set;}
        public static async void Initialize()
        {
            using var channel = GrpcChannel.ForAddress("http://127.0.0.1:5000");
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
        public static async Task<DiracResponse> InvokeAsync(string methodName, IDictionary payload)
        {
            if (clientChannel == null || serverChannel == null || api == null)
            {
                throw new Exception("Handshake protocol failed.");
            }

            byte[] serializedEnv = clientChannel.PackAndEncrypt(methodName, payload);
            byte[] serializedResp = await api.InvokeAsync(serializedEnv);
            DiracResponse finalResults = serverChannel.UnpackResponse<DiracResponse>(serializedResp);

            Console.WriteLine($"Response received.\nCONTENT: {finalResults}");

            return finalResults;
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

        public static async Task<IEnumerable<object>> ListFunctions()
        {
            byte[] serializedFuncs = await api.ListFunctions();
            IEnumerable<object> funcs = MessagePackSerializer.Deserialize<IEnumerable<Object>>(serializedFuncs, MessagePack.Resolvers.ContractlessStandardResolver.Options);

            return funcs;
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