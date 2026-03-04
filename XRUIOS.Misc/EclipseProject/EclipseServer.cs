using Grpc.Core;
using MagicOnion;
using MagicOnion.Server;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Paddings;
using Org.BouncyCastle.Pqc.Crypto.Crystals.Kyber;
using Org.BouncyCastle.Security;
using Pariah_Cybersecurity;
using System;
using System.Buffers.Binary;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static Secure_Store.Storage;
using static EclipseProject.Ocean;
using static EclipseProject.Security;
using EclipseLCL;
using MessagePack;
using System.Reflection;

namespace EclipseProject
{
    public static class EclipseServer
    {
        internal static Ocean? ocean {get; set;}
        internal static (Dictionary<string, byte[]> Public, Dictionary<string, byte[]> Private) ServerKyberKeys {get; set;}
        internal static bool enabled = false;
        public static async void RunServer(System.Reflection.Assembly assembly, string[]? args = null, int port = 5000, bool useNamedPipesLater = false)
        {
            Console.WriteLine("Starting Eclipse Ocean gRPC Server...");
            assembly ??= Assembly.GetExecutingAssembly();

            ServerKyberKeys = EasyPQC.Keys.Initiate();

            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddSingleton<SessionStore>();

            // Register MagicOnion services
            builder.Services.AddMagicOnion();

            // Auto-register all [SeaOfDirac] functions at startup
            ocean = new Ocean();
            ocean.FloodTheSea(assembly);

            // Kestrel config — localhost TCP for now
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.ListenLocalhost(5000, o => o.Protocols = HttpProtocols.Http2);
            });

            var app = builder.Build();

            // Map MagicOnion to gRPC endpoint
            app.MapMagicOnionService();

            enabled = true;

            await app.RunAsync();
        }
    }
    public class DiracService : ServiceBase<IDiracService>, IDiracService
    {
        private readonly SessionStore _sessions;
        public DiracService(SessionStore sessions)
        {
            _sessions = sessions;
        }
        public async UnaryResult<Dictionary<string, byte[]>> EnrollAsync(string clientName, string clientId)
        {
            if (SecureStore.Get<string>(clientName) != null) // already existing clients
            {
                throw new Exception($"Client with name {clientName} is already registered.");
            }

            if (SecureStore.Get<byte[]>(clientId) == null)
            {
                throw new Exception($"Client with name {clientName} did not prepare for enrollment.");
            }

            return EclipseServer.ServerKyberKeys.Public;
        }

        public async UnaryResult<(byte[] nonceS, byte[] sessionId, uint epoch)> BeginHandshakeAsync(string clientId, byte[] cipher, byte[] nonceC)
        {
            SecureRandom rand = new SecureRandom();

            byte[] nonceS = new byte[16];
            byte[] sessionId = new byte[8];

            rand.NextBytes(nonceS);
            rand.NextBytes(sessionId);
            byte[]? PSK = SecureStore.Get<byte[]>(clientId);
            uint epoch = 1;

            if (PSK == null)
            {
                throw new Exception("Failed to retrieve necessary client data.");
            }

            byte[] sharedSecret = EasyPQC.Keys.CreateSecretTwo(EclipseServer.ServerKyberKeys.Private, cipher);

            var keys = PrepareKeys(PSK, nonceC, nonceS, sharedSecret);

            byte[] transcriptHash = SHA256.HashData(ByteArrayExtensions.Combine(Encoding.UTF8.GetBytes(clientId), cipher, nonceC, nonceS, sessionId, BitConverter.GetBytes(epoch)));

            _sessions.Upsert(
                new SessionState(
                    clientId, sessionId, epoch, 
                    new AeadChannel(keys.k_c2s, sessionId, clientId, 1, new Transcript(transcriptHash, "client-finished")),
                    new AeadChannel(keys.k_s2c, sessionId, clientId, 1, new Transcript(transcriptHash, "server-finished"))
                )
            );


            return (nonceS, sessionId, epoch);
        }

        public async UnaryResult<byte[]> FinishHandshakeAsync(string clientId, byte[] clientTranscriptRaw)
        {

            if (!_sessions.TryGet(clientId, out SessionState s))
            {
                throw new Exception("Session not found.");
            }

            Transcript clientTranscript = s.FromClient.transcript;
            if (clientTranscript.proof == null)
            {
                throw new Exception("Connection refused: Transcript has no proof");
            }

            Transcript serverTranscript = s.ToClient.transcript;
            if (serverTranscript.proof == null)
            {
                throw new Exception("Connection refused: Transcript has no proof");
            }

            if (!CryptographicOperations.FixedTimeEquals(clientTranscript.proof, clientTranscriptRaw))
            {
                throw new Exception("Connection refused: Incorrect transcript HMAC");
            }

            s.HandshakeComplete = true;

            return serverTranscript.proof;
        }

        public async UnaryResult<byte[]> InvokeAsync(byte[] serializedEnv)
        {
            EncryptedEnvelope env = MessagePackSerializer.Deserialize<EncryptedEnvelope>(serializedEnv, MessagePack.Resolvers.ContractlessStandardResolver.Options);

            if (!_sessions.TryGet(env.ClientId, out SessionState s))
            {
                throw new Exception("Session not found.");
            }

            if (!s.HandshakeComplete)
            {
                throw new Exception("Handshake not complete.");
            }

            Ocean? ocean = EclipseServer.ocean;
            if (ocean == null)
            {
                throw new Exception("Ocean not found.");
            }

            DiracRequest request = s.FromClient.DecryptAndUnpack<DiracRequest>(env);

            DiracResponse response = await ocean.HandleRequestAsync(request, s.ToClient);

            return MessagePackSerializer.Serialize<DiracResponse>(response, MessagePack.Resolvers.ContractlessStandardResolver.Options);
        }

        public async UnaryResult<bool> FinishAsync(byte[] serializedEnv)
        {
            EncryptedEnvelope env = MessagePackSerializer.Deserialize<EncryptedEnvelope>(serializedEnv);

            if (!_sessions.TryGet(env.ClientId, out SessionState s))
            {
                throw new Exception("Session not found.");
            }

            if (!s.HandshakeComplete)
            {
                throw new Exception("Handshake not complete.");
            }

            // Authenticate the teardown request by decrypting with the established c2s key.
            // A successful decrypt proves this was sent by the legitimate holder of the session key.
            s.FromClient.Decrypt(env);

            _sessions.Remove(env.ClientId);

            return true;
        }
        public async UnaryResult<byte[]> ListFunctions()
        {
            Ocean? ocean = EclipseServer.ocean;
            if (ocean == null)
            {
                throw new Exception("Ocean not found.");
            }

            return MessagePackSerializer.Serialize<IEnumerable<object>>(ocean.ListFunctionDetails());
        }
    }
}
