using IWshRuntimeLibrary;
using Microsoft.Maui.ApplicationModel;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using static Pariah_Cybersecurity.DataHandler;
using static XRUIOS.Barebones.SoundEQClass;
using static XRUIOS.Barebones.XRUIOS;
using static XRUIOS.Barebones.XRUIOS.Application;
using static XRUIOS.Barebones.XRUIOS.Yuuko.App;
using System.Drawing;
using File = System.IO.File;


namespace XRUIOS.Barebones.Functions
{
    public static class AppClass
    {

        public record XRUIOSAppManifest
        {
            public string AppId;
            public string Name;
            public string Description;
            public string Author;
            public string Version;

            public FileRecord? YuukoAppInfo;

            public string EntryPoint;
            public ulong Hash;
            public List<string> SupportedPlatforms;

            public XRUIOSAppManifest() { }

            public XRUIOSAppManifest(
                string name,
                string description,
                string author,
                string version,
                FileRecord? yuukoAppInfo,
                string entryPoint,
                ulong hash,
                List<string> supportedPlatforms)
            {
                Name = name;
                Description = description;
                Author = author;
                Version = version;
                YuukoAppInfo = yuukoAppInfo;
                EntryPoint = entryPoint;
                Hash = hash;
                SupportedPlatforms = supportedPlatforms;
            }
        }





        public static async Task SyncComputerPrograms()
        {

            var directoryPath = Path.Combine(DataPath, "Apps");

            var FileWithPrograms = await JSONDataHandler.LoadJsonFile(directoryPath, "Apps");
            var SteamAppsFile = (List<XRUIOSAppManifest>)await JSONDataHandler.GetVariable<List<XRUIOSAppManifest>>(FileWithPrograms, "SteamApps", encryptionKey);
            var WindowsAppsFile = (List<XRUIOSAppManifest>)await JSONDataHandler.GetVariable<List<XRUIOSAppManifest>>(FileWithPrograms, "WindowsApps", encryptionKey);


            //Get all apps

            // Path to the Start Menu directory
            string startMenuPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs");

            var windowsApps = Directory.EnumerateFiles(startMenuPath, "*.*", SearchOption.AllDirectories)
              .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}Steam{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase));

            string steamMenuPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs", "Steam");


            var steamApps = Directory.GetFiles(startMenuPath, "*.lnk", SearchOption.AllDirectories);

            foreach (var item in windowsApps)
            {
                FileInfo fileInfo = new FileInfo(item);

                string targetPath = fileInfo.LinkTarget;
                if (WindowsAppsFile.Any(d => d.EntryPoint == targetPath))
                {
                    //App exists, let's make sure nothing was updated

                    //Just check version, image and author

                    FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(item);
                    string fileVersion = versionInfo.FileVersion;

                    




                }
            }

        }
    }

    public static class ShortcutIconHelper
    {
        public static Bitmap GetAppIcon(string path, int size = 256)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("File not found", path);

            // Resolve .lnk shortcuts
            if (Path.GetExtension(path).ToLower() == ".lnk")
            {
                var shell = new WshShell();
                var link = (IWshShortcut)shell.CreateShortcut(path);
                path = link.TargetPath;
            }

            if (!File.Exists(path))
                throw new FileNotFoundException("Resolved target not found", path);

            // Use Shell32 COM to get large icon
            return GetShellIcon(path, size);
        }

        private static Bitmap GetShellIcon(string path, int size)
        {
            SHFILEINFO shinfo = new SHFILEINFO();
            IntPtr hImg = SHGetFileInfo(path, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo),
                SHGFI_ICON | SHGFI_LARGEICON);

            if (hImg == IntPtr.Zero)
                return null;

            Icon icon = (Icon)Icon.FromHandle(shinfo.hIcon).Clone();
            DestroyIcon(shinfo.hIcon);

            // Resize to requested size (up to 256x256)
            return new Bitmap(icon.ToBitmap(), new Size(size, size));
        }

        #region Win32 Imports & Flags
        [StructLayout(LayoutKind.Sequential)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public IntPtr iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        };

        private const uint SHGFI_ICON = 0x100;
        private const uint SHGFI_LARGEICON = 0x0; // 32x32, Windows default

        [DllImport("shell32.dll")]
        private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes,
            ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);
        #endregion
    }

}