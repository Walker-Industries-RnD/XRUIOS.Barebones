using Grpc.Net.Client;
using MagicOnion;
using MagicOnion.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Security;
using System.Security.Cryptography;
using System.Text;
using static Pariah_Cybersecurity.EasyPQC;
using static Secure_Store.Storage;
using static EclipseProject.Security;
using EclipseLCL;
using Microsoft.AspNetCore.Mvc.ModelBinding;


namespace EclipseProject
{
    public class Test
    {
        [SeaOfDirac("AddNumbers", null, typeof(int), typeof(int), typeof(int))]
        public static int AddNumbers(int a, int b)
        {
            return a + b;
        }

        [SeaOfDirac("SubtractNumbers", null, typeof(int), typeof(int), typeof(int))]
        public static int SubtractNumbers(int a, int b)
        {
            return a - b;
        }
    }
    public class MainTest
    {

        public static async Task Main()
        {
            EclipseServer.RunServer("Eclipse Server");

            try
            {

                await EclipseClient.Initialize();

                int? c = await EclipseClient.InvokeAsync<int>("AddNumbers", ("a", 1), ("b", 2));
                Console.WriteLine($"Response received.\nCONTENT: {c}");

                EclipseClient.FinishAsync();

            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
            }
        }
    }
}