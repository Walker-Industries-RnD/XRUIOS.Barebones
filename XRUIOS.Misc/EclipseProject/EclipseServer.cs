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
using MessagePack.Resolvers;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace EclipseProject
{
    public static class EclipseServer
    {
        internal static Ocean? ocean {get; set;}
        internal static (Dictionary<string, byte[]> Public, Dictionary<string, byte[]> Private) ServerKyberKeys {get; set;}
        internal static bool enabled = false;
        public static async void RunServer(string? serverName = "Test Eclipse Server", System.Reflection.Assembly? assembly = null, string[]? args = null, int port = 5000, bool useNamedPipesLater = false)
        {
            Console.WriteLine("Starting Eclipse Ocean gRPC Server...");
            assembly ??= Assembly.GetExecutingAssembly();

            ServerKyberKeys = EasyPQC.Keys.Initiate();

            var builder = WebApplication.CreateBuilder();

            builder.Services.AddSingleton<SessionStore>();
            builder.Services.AddMagicOnion();

            // Auto-register all [SeaOfDirac] functions at startup
            ocean = new Ocean();
            ocean.FloodTheSea(assembly);

            // Kestrel config — localhost TCP for now
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.Listen(System.Net.IPAddress.Loopback, 0, o => o.Protocols = HttpProtocols.Http2);
            });


            var app = builder.Build();

            // Map MagicOnion to gRPC endpoint
            app.MapMagicOnionService();

            enabled = true;

            // Start the server without awaiting (so we can grab the addresses)
            var serverTask = app.StartAsync();

            // Get the actual bound URL after Kestrel starts
            var server = app.Services.GetRequiredService<IServer>();
            var addressesFeature = server.Features.Get<IServerAddressesFeature>();

            if (addressesFeature == null)
                throw new Exception("Server initialization failed.");

            // Wait until Kestrel actually binds
            while (addressesFeature.Addresses.Count == 0)
            {
                await Task.Delay(50);
            }

            Console.WriteLine("Server running on: " + string.Join(", ", addressesFeature.Addresses));

            // Save the bound address
            SecureStore.Set(serverName!, addressesFeature.Addresses);

            // Keep the server running
            await serverTask;
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
            EncryptedEnvelope env = MessagePackSerializer.Deserialize<EncryptedEnvelope>(serializedEnv, ContractlessStandardResolver.Options);

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

            Dictionary<string, object?> parameters = s.FromClient.DecryptAndUnpack<Dictionary<string, object?>>(env);

            DiracResponse response = await ocean.HandleRequestAsync(env.Method, parameters, s);
            
            return MessagePackSerializer.Serialize<DiracResponse>(response, ContractlessStandardResolver.Options);
        }

        public async UnaryResult<bool> FinishAsync(byte[] serializedEnv)
        {
            EncryptedEnvelope env = MessagePackSerializer.Deserialize<EncryptedEnvelope>(serializedEnv, ContractlessStandardResolver.Options);

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

            return MessagePackSerializer.Serialize(ocean.ListFunctionDetails(), ContractlessStandardResolver.Options);
        }
    }
}
