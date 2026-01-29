using System;
using System.Collections.Generic;
using System.Text;
using static Secure_Store.Storage;

namespace XRUIOS.Barebones
{

    public class ClipboardClass
    {
        public class BaseClipboard
        {
            internal static Dictionary<string, string> Clipboard = new Dictionary<string, string>();

            //R
            public Dictionary<string, string> LoadClipboard()
            {
                return Clipboard;
            }

            public async Task<byte[]> GetClipboardItem(string key)
            {
                var value = SecureStore.Get<byte[]>("last_session");
                return value;
            }

            //U
            public void AddToClipboard(byte[] item, string itemName)
            {
                SecureStore.Set(itemName, item);
            }

            //D
            public void RemoveFromClipboard(string itemName)
            {
                string path = GetPath(itemName);

                File.Delete(path);
            }


            private static string BasePath
            {
                get
                {
                    // Use per-session temp directory on all platforms
                    string sessionDir = Path.Combine(Path.GetTempPath(), "SECURE_STORE" + Environment.UserName);
                    Directory.CreateDirectory(sessionDir);
                    return sessionDir;
                }
            }

            private static string GetPath(string key) =>
    Path.Combine(BasePath, $"secstr_{key}.dat");

        }

        public class ClipboardGroups
        {
            private static Dictionary<string, Dictionary<string, byte[]>> ClipboardGroup = new();

            // R
            public Dictionary<string, byte[]> LoadClipboard(string groupName)
            {
                if (!ClipboardGroup.ContainsKey(groupName))
                    ClipboardGroup[groupName] = new Dictionary<string, byte[]>();

                return ClipboardGroup[groupName];
            }

            public async Task<byte[]> GetClipboardItem(string groupName, string key)
            {
                var group = LoadClipboard(groupName);
                if (group.TryGetValue(key, out var value))
                    return value;

                // Fallback: load from file if exists
                string path = GetPath(groupName, key);
                if (File.Exists(path))
                    return await File.ReadAllBytesAsync(path);

                return null;
            }

            // U
            public void AddToClipboard(string groupName, byte[] item, string key)
            {
                var group = LoadClipboard(groupName);
                group[key] = item;

                // Save to disk
                string path = GetPath(groupName, key);
                File.WriteAllBytes(path, item);
            }

            // D
            public void RemoveFromClipboard(string groupName, string key)
            {
                var group = LoadClipboard(groupName);
                if (group.ContainsKey(key))
                    group.Remove(key);

                string path = GetPath(groupName, key);
                if (File.Exists(path))
                    File.Delete(path);
            }


            private static string BasePath
            {
                get
                {
                    string sessionDir = Path.Combine(Path.GetTempPath(), "SECURE_STORE_" + Environment.UserName);
                    Directory.CreateDirectory(sessionDir);
                    return sessionDir;
                }
            }

            private static string GetPath(string groupName, string key)
            {
                string groupDir = Path.Combine(BasePath, groupName);
                Directory.CreateDirectory(groupDir);
                return Path.Combine(groupDir, $"secstr_{key}.dat");
            }
        }

    }


}
