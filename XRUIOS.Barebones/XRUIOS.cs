using Pariah_Cybersecurity;
using Standart.Hash.xxHash;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json.Nodes;
using WISecureData;

namespace XRUIOS.Barebones
{
    public class XRUIOS
    {


        public static string DataPath { get; private set; } = Path.Combine(Directory.GetCurrentDirectory(), "XRUIOS");
        public static string PublicDataPath { get; private set; } = Path.Combine(Directory.GetCurrentDirectory(), "XRUIOSPublic");
        public static SecureData encryptionKey = "Test".ToSecureData();


    }

}
