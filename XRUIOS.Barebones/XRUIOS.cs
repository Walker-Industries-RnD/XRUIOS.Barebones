using ATL;
using CsvHelper;
using Hangfire;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using KeeperOfTomes;
using Microsoft.VisualBasic;
using Microsoft.Win32;
using Newtonsoft.Json;
using Org.BouncyCastle.Asn1.Mozilla;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Pqc.Crypto.Lms;
using Org.BouncyCastle.Utilities;
using Pariah_Cybersecurity;
using Secure_Store;
using Standart.Hash.xxHash;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net.Mail;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices.Marshalling;
using System.Security.AccessControl;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using WISecureData;
using static Org.BouncyCastle.Crypto.Engines.SM2Engine;
using static Pariah_Cybersecurity.DataHandler;
using static Pariah_Cybersecurity.DataHandler.DataRequest;
using static Secure_Store.Storage;
using static System.Net.Mime.MediaTypeNames;
using static Walker.Crypto.SimpleAESEncryption;
using static XRUIOS.Barebones.XRUIOS;
using static XRUIOS.Barebones.XRUIOS.AlarmClass;
using static XRUIOS.Barebones.XRUIOS.ChronoClass.Times;
using static XRUIOS.Barebones.XRUIOS.CreatorClass;
using static XRUIOS.Barebones.XRUIOS.Songs;
using static XRUIOS.Barebones.XRUIOS.Yuuko.Bindings;


namespace XRUIOS.Barebones
{
    public class XRUIOS
    {

        public static string DataPath { get; private set; } = Path.Combine(Directory.GetCurrentDirectory(), "XRUIOS");
        public static string PublicDataPath { get; private set; } = Path.Combine(Directory.GetCurrentDirectory(), "XRUIOSPublic");


        ObservableProperty<string> MainSong;
        ObservableCollection<string> MainSongQueue;

        public static string XRUIOSPath;
        public static SecureData encryptionKey = "Test".ToSecureData();


        public record DirectoryRecord
        {
            public string UUID { get; set; }
            public string PathName { get; set; }
            public string Path { get; set; }

            public DirectoryRecord() { }


            public DirectoryRecord(string uuid, string pathName, string path)
            {
                UUID = uuid;
                PathName = pathName;
                Path = path;
            }
        }

        public record FileRecord
        {
            public string UUID { get; set; }
            public string File { get; set; }

            public FileRecord() { }


            public FileRecord(string uuid, string file)
            {
                UUID = uuid;
                File = file;
            }
        }


        public class Yuuko
        {

            public class Bindings
            {

                //Lowkenuinely need to put comments here or I WILL forget how it works
                public sealed class DirectoryManager
                {
                    private readonly string _bindingsFolder;
                    private readonly Dictionary<string, DirectoryBinding> _bindings = new();

                    //Gets a directory by the ID, this is how you should get it
                    public async Task<string?> GetDirectoryById(
                    string directoryId,
                    string? defaultFolder = null,
                    CancellationToken ct = default)
                    {
                        // 1. Look up the binding
                        if (!_bindings.TryGetValue(directoryId, out var binding))
                            return null; // Unknown directory ID

                        // 2. Fast path: if the original path exists, use it
                        if (binding.Ref.OriginalDevice == ThisDeviceId &&
                            Directory.Exists(binding.Ref.FullPath))
                        {
                            return binding.Ref.FullPath;
                        }


                        // 3. If a verified resolution exists and is valid, use it
                        if (binding.Resolution?.Verified == true &&
                            Directory.Exists(binding.Resolution.ResolvedPath))
                        {
                            return binding.Resolution.ResolvedPath;
                        }

                        // 4. If no default folder is provided, DO NOT create anything
                        if (defaultFolder == null)
                            return null;

                        // 5. Only now do we allow resolution + creation
                        var resolvedPath = ResolveDirectory(binding, defaultFolder);

                        // 6. Persist the updated binding
                        string bindingPath = Path.Combine(_bindingsFolder, directoryId + ".binding");
                        var data = await BinaryConverter.NCObjectToByteArrayAsync(
                            binding,
                            cancellationToken: ct
                        );
                        await File.WriteAllBytesAsync(bindingPath, data, ct);

                        return resolvedPath;
                    }



                    public DirectoryManager(string bindingsFolder)
                    {
                        _bindingsFolder = Path.GetFullPath(bindingsFolder);
                        Directory.CreateDirectory(_bindingsFolder);
                    }

                    internal string ThisDeviceId => Environment.MachineName;

                    //Safely get or create a folder
                    public async Task<(string Uuid, string ResolvedPath)> GetOrCreateDirectory(string fullPath, string localOverride = null, CancellationToken ct = default)
                    {
                        // 1. Create a reference for this folder on this device
                        var directoryRef = new DirectoryRef(fullPath, ThisDeviceId);

                        // 2. Get existing binding or create a new one
                        if (!_bindings.TryGetValue(directoryRef.DirectoryId, out var binding))
                        {
                            binding = new DirectoryBinding(directoryRef);
                            _bindings[directoryRef.DirectoryId] = binding;
                        }

                        // 3. Resolve the folder (make sure it exists)
                        var resolvedPath = ResolveDirectory(binding, localOverride);

                        // 4. Persist the binding to disk
                        string bindingPath = Path.Combine(_bindingsFolder, directoryRef.DirectoryId + ".binding");
                        byte[] data = await BinaryConverter.NCObjectToByteArrayAsync(binding, cancellationToken: ct);
                        await File.WriteAllBytesAsync(bindingPath, data, ct);

                        // 5. Return both the UUID and the resolved path
                        return (directoryRef.DirectoryId, resolvedPath);
                    }

                    //Force a folder path on this machine


                    public string ResolveDirectory(DirectoryBinding binding, string defaultFolder = null)
                    {
                        if (binding.Ref.OriginalDevice == ThisDeviceId && Directory.Exists(binding.Ref.FullPath))
                            return binding.Ref.FullPath;

                        if (binding.Resolution != null && binding.Resolution.Verified && Directory.Exists(binding.Resolution.ResolvedPath))
                            return binding.Resolution.ResolvedPath;

                        if (Directory.Exists(binding.Ref.FullPath))
                        {
                            binding.SetResolution(new DirectoryResolution(binding.Ref.FullPath, verified: true));
                            return binding.Ref.FullPath;
                        }

                        var pathToUse = defaultFolder ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "XRUIOS", binding.Ref.DirectoryId);

                        if (defaultFolder == null && !Directory.Exists(binding.Ref.FullPath))
                            throw new InvalidOperationException("ResolveDirectory called without defaultFolder");

                        Directory.CreateDirectory(pathToUse);
                        binding.SetResolution(new DirectoryResolution(pathToUse, verified: true));

                        return pathToUse;
                    }

                    public async Task<bool> DeleteBinding(string directoryId, CancellationToken ct = default)
                    {
                        var bindingPath = Path.Combine(
                            Path.GetFullPath(_bindingsFolder),
                            directoryId + ".binding"
                        );

                        if (!File.Exists(bindingPath))
                            return false;

                        File.Delete(bindingPath);

                        return true;
                    }

                    public async Task<bool> UpdateBinding(string directoryId, DirectoryResolution newResolution, CancellationToken ct = default)
                    {
                        // 1. Check if binding exists
                        if (!_bindings.TryGetValue(directoryId, out var binding))
                            return false;

                        // 2. Update resolution
                        binding.SetResolution(newResolution);

                        // 3. Persist to disk
                        string bindingPath = Path.Combine(_bindingsFolder, directoryId + ".binding");
                        var data = await BinaryConverter.NCObjectToByteArrayAsync(binding, cancellationToken: ct);
                        await File.WriteAllBytesAsync(bindingPath, data, ct);

                        return true;
                    }


                    //Load all saved folder info from disk
                    public async Task LoadBindings(CancellationToken ct = default)
                    {
                        _bindings.Clear();
                        if (!Directory.Exists(_bindingsFolder)) return;

                        foreach (var file in Directory.EnumerateFiles(_bindingsFolder, "*.binding"))
                        {
                            try
                            {
                                var bytes = await File.ReadAllBytesAsync(file, ct);
                                var binding = await BinaryConverter.NCByteArrayToObjectAsync<DirectoryBinding>(bytes, cancellationToken: ct);
                                if (binding != null)
                                    _bindings[binding.Ref.DirectoryId] = binding;
                            }
                            catch { }
                        }
                    }

                    //Gets all the bindings in memory
                    public IEnumerable<DirectoryBinding> GetAllBindings() => _bindings.Values;

                    public DirectoryBinding? GetBindingById(string uuid)
                    {
                        _bindings.TryGetValue(uuid, out var binding);
                        return binding;
                    }



                }


                //SetResolution will set the DirectoryResolution for this file
                //ClearResolution forgets it
                public sealed class DirectoryBinding
                {
                    public DirectoryRef Ref { get; }
                    public DirectoryResolution? Resolution { get; private set; }

                    public DirectoryBinding(DirectoryRef directoryRef) => Ref = directoryRef;
                    public DirectoryBinding(DirectoryRef directoryRef, DirectoryResolution? resolution)
                    {
                        Ref = directoryRef;
                        Resolution = resolution;
                    }

                    public void SetResolution(DirectoryResolution resolution) => Resolution = resolution;


                    public void ClearResolution() => Resolution = null;
                }



                //DirectoryRef asks for
                //A: the original folder path and
                //B: the device where this path originally existed
                public readonly struct DirectoryRef
                {
                    public string FullPath { get; }
                    public string OriginalDevice { get; }
                    public string DirectoryId { get; }

                    private const ulong HashSeed = 0x59554B4F; // "YUUKO" 

                    public DirectoryRef(string fullPath, string originalDevice)
                    {
                        FullPath = fullPath ?? throw new ArgumentNullException(nameof(fullPath));
                        OriginalDevice = originalDevice ?? throw new ArgumentNullException(nameof(originalDevice));

                        DirectoryId = ComputeDirectoryId(fullPath, originalDevice).GetAwaiter().GetResult();
                    }

                    private static async Task<string> ComputeDirectoryId(string path, string device)
                    {
                        // Canonical string format (DO NOT CHANGE lightly)
                        string canonical = $"{device}|{Path.GetFullPath(path)}";

                        await using var stream = new MemoryStream(
                            Encoding.UTF8.GetBytes(canonical)
                        );

                        ulong hash = await xxHash64.ComputeHashAsync(
                            stream,
                            bufferSize: 81920,
                            seed: HashSeed
                        );

                        // Hex string is protocol-friendly
                        return hash.ToString("X16");
                    }
                }


                //DirectoryResolution asks for
                //A: the local path used if the original doesn’t exist and
                //B: if we’ve checked that path exists on this side
                public sealed class DirectoryResolution
                {
                    public string ResolvedPath { get; set; }
                    public DateTime LastVerifiedUtc { get; set; }
                    public bool Verified { get; set; }


                    public DirectoryResolution(string resolvedPath, bool verified = false)
                    {
                        ResolvedPath = resolvedPath ?? throw new ArgumentNullException(nameof(resolvedPath));
                        Verified = verified;
                        LastVerifiedUtc = DateTime.UtcNow;
                    }
                }



            }

            public class App
            {
                //Platform can be { Windows, Linux, YuukOS, Android, MacOS, iPhone}

                public record Overview
                {
                    public string YuukoAppName;
                    public string MinimumYuukoVersion;
                    public string DeveloperID;
                    public Dictionary<string, byte[]> PublicKey;
                    public string UUID;
                    public string AppName;
                    public string Description;
                    public string PrimaryPlatform;

                    public Overview() { }

                    public Overview(string yuukoAppName, string minimumYuukoVersion, string developerID, Dictionary<string, byte[]> publicKey, string uuid, string appName, string description, string primaryPlatform)
                    {
                        YuukoAppName = yuukoAppName;
                        MinimumYuukoVersion = minimumYuukoVersion;
                        DeveloperID = developerID;
                        PublicKey = publicKey;
                        UUID = uuid;
                        AppName = appName;
                        Description = description;
                        PrimaryPlatform = primaryPlatform;
                    }
                }

                public record Parameter
                {
                    public string Name;
                    public Type ParamType;
                    public bool Required;
                    public object? Default;
                    public List<object>? Choices;

                    public Parameter()
                    {
                        Name = string.Empty;
                        ParamType = typeof(object);
                        Required = false;
                        Default = null;
                        Choices = null;
                    }

                    public Parameter(string name, Type paramType, bool required, object? defaultValue, List<object>? choices)
                    {
                        Name = name;
                        ParamType = paramType;
                        Required = required;
                        Default = defaultValue;
                        Choices = choices;
                    }
                }

                public record Event
                {
                    public string Name;
                    public string Description;
                    public List<Parameter>? Parameters;
                    public string? Command;

                    public Event()
                    {
                        Name = string.Empty;
                        Description = string.Empty;
                        Parameters = null;
                        Command = null;
                    }

                    public Event(string name, string description, List<Parameter>? parameters, string? command)
                    {
                        Name = name;
                        Description = description;
                        Parameters = parameters;
                        Command = command;
                    }
                }

                public record OSSpecificApp
                {
                    public string OS;
                    public string Version;
                    public string Download;
                    public List<Panel> PanelInfo;

                    public OSSpecificApp()
                    {
                        OS = string.Empty;
                        Version = string.Empty;
                        Download = string.Empty;
                        PanelInfo = new List<Panel>();
                    }

                    public OSSpecificApp(string os, string version, string download, List<Panel> panelInfo)
                    {
                        OS = os;
                        Version = version;
                        Download = download;
                        PanelInfo = panelInfo;
                    }
                }

                //Modes can be { Full, Panel, Max, Minimized, CLI}

                //Views can be 2D, 3D or CMD

                public record Panel
                {
                    public string Description;
                    public List<RenderStyle> Renders;

                    //All formatted to a shared interface that the runtime can call

                    public Panel()
                    {
                        Description = string.Empty;
                        Renders = new List<RenderStyle>();
                    }

                    public Panel(string description, List<RenderStyle> renders)
                    {
                        Description = description;
                        Renders = renders;
                    }
                }

                public record RenderStyle
                {
                    public string Mode;
                    public string View;

                    public RenderStyle()
                    {
                        Mode = string.Empty;
                        View = string.Empty;
                    }

                    public RenderStyle(string mode, string view)
                    {
                        Mode = mode;
                        View = view;
                    }
                }

                public record PreferredPlatformMapping
                {
                    public string? Mode2D { get; init; }
                    public string? Mode3D { get; init; }
                    public string? ModeCMD { get; init; }

                    public PreferredPlatformMapping() { }

                    public PreferredPlatformMapping(string? mode2D, string? mode3D, string? modeCMD)
                    {
                        Mode2D = mode2D;
                        Mode3D = mode3D;
                        ModeCMD = modeCMD;
                    }
                }

                public record YuukoApp
                {
                    public Overview YuukoInfo;
                    public Dictionary<string, PreferredPlatformMapping> Defaults;
                    public List<OSSpecificApp> OSSet;
                    public Dictionary<string, List<Event>> OSSpecificEntrypoints;
                    public List<Event> SharedEntrypoints;

                    public YuukoApp(){}

                    public YuukoApp(
                        Overview yuukoInfo,
                        Dictionary<string, PreferredPlatformMapping> defaults,
                        List<OSSpecificApp> osSet,
                        Dictionary<string, List<Event>> osSpecificEntrypoints,
                        List<Event> sharedEntrypoints)
                    {
                        YuukoInfo = yuukoInfo;
                        Defaults = defaults;
                        OSSet = osSet;
                        OSSpecificEntrypoints = osSpecificEntrypoints;
                        SharedEntrypoints = sharedEntrypoints;
                    }
                }

                //The way a YuukoApp looks pretty simple but it's actually VERY complicated
                //There are a number of checks we do to ensure things work properly
                //The goal is to
                    //1. Expose a public API for each project
                    //2. Figure out the download we should be looking at
                    //3. Tell all the devices on the network to download the versions we say to and
                    //4. Handle mode switches for UIs

                //Atop this, we check the information by getting the sign and ensuring it's right


                public static async Task CreateSign()
                {

                }




            }




        }


       
        public sealed record ResolvedMedia
        {
            public string FullPath { get; init; }
            public string FileName { get; init; }
            public string DirectoryUuid { get; init; }
            public long SizeBytes { get; init; }
            public DateTime LastModifiedUtc { get; init; }


            public ResolvedMedia() { }

        }


        public static class Media
        {
            //This is harmless so we give access to all files; it only gives basic information. However never expose any info beyond basic metadata
            public static async Task<ResolvedMedia> GetFile(
                string directoryUuid,
                string fileName,
                CancellationToken ct = default)
            {
                if (string.IsNullOrWhiteSpace(directoryUuid))
                    throw new ArgumentException("Invalid directory UUID.", nameof(directoryUuid));

                if (string.IsNullOrWhiteSpace(fileName))
                    throw new ArgumentException("Invalid file name.", nameof(fileName));

                // Prevent path traversal or absolute paths
                if (Path.IsPathRooted(fileName) ||
                    fileName.Contains(Path.DirectorySeparatorChar) ||
                    fileName.Contains(Path.AltDirectorySeparatorChar))
                {
                    throw new InvalidOperationException("Invalid media file name.");
                }

                var bindingsPath = Path.Combine(DataPath, "Generic");
                var manager = new Yuuko.Bindings.DirectoryManager(bindingsPath);

                await manager.LoadBindings(ct);

                ct.ThrowIfCancellationRequested();

                var directory = await manager.GetDirectoryById(directoryUuid, ct: ct);
                if (directory == null)
                    throw new DirectoryNotFoundException("Media directory could not be resolved.");

                var fullPath = Path.Combine(directory, fileName);
                var info = new FileInfo(fullPath);

                if (!info.Exists)
                    throw new FileNotFoundException("Media file not found.", fileName);

                return new ResolvedMedia
                {
                    FullPath = info.FullName,
                    FileName = info.Name,
                    DirectoryUuid = directoryUuid,
                    SizeBytes = info.Length,
                    LastModifiedUtc = info.LastWriteTimeUtc
                };
            }


            public static async Task<DirectoryRecord?> GetOrCreateDirectory(string fullFilePath, string? directory, string? directoryName, CancellationToken ct = default)
            {

                var Directories = await GetGenericDirectories();

                var check1 = Directories.Item1.Any(b => b.Path == directory);
                var check2 = Directories.Item2.Any(b => b.Path == directory);

                DirectoryRecord returnVal = null;

                if (check1)
                {
                    returnVal = Directories.Item1.FirstOrDefault (b => b.Path == directory);
                }

                if (check2)
                {
                    returnVal = Directories.Item2.FirstOrDefault(b => b.Path == directory);

                }

                else
                {
                    await AddGenericDirectory(directory, directoryName);
                }

                return returnVal;

            }




            //C
            public static async Task AddGenericDirectory(string directory, string directoryName)
            {
                var directoryPath = Path.Combine(DataPath, "Generic");

                var manager = new Yuuko.Bindings.DirectoryManager(directoryPath);

                await manager.LoadBindings();

                var directoryFile = await DataHandler.JSONDataHandler.LoadJsonFile("GenericBank", directoryPath);

                var directories = (List<DirectoryRecord>)await DataHandler.JSONDataHandler.GetVariable<List<DirectoryRecord>>(directoryFile, "Data", encryptionKey);
                //UUID, path name, path 

                var directoryDataInManagerReturn = await manager.GetOrCreateDirectory(directory);

                var directoryUUID = directoryDataInManagerReturn.Uuid;

                if (!directories.Any(d => d.UUID == directoryUUID))
                {
                    //Create new record

                    var record = new DirectoryRecord(directoryUUID, directoryName, directory);

                    directories.Add(record);
                }

                else
                {
                    throw new InvalidOperationException($"Cannot add directory directory.");
                }


                var editedJSON = await DataHandler.JSONDataHandler.UpdateJson<List<DirectoryRecord>>(directoryFile, "Data", directories, encryptionKey);

                await DataHandler.JSONDataHandler.SaveJson(editedJSON);

            }
            //R
            public static async Task<(List<DirectoryRecord>, List<DirectoryRecord>)> GetGenericDirectories()
            {

                var directoryPath = Path.Combine(DataPath, "Generic");

                var manager = new Yuuko.Bindings.DirectoryManager(directoryPath);

                var directoryFile = await DataHandler.JSONDataHandler.LoadJsonFile("GenericBank", directoryPath);

                var directories = (List<DirectoryRecord>)await DataHandler.JSONDataHandler.GetVariable<List<DirectoryRecord>>(directoryFile, "Data", encryptionKey);
                //UUID, path name, path 

                //What bindings exist? Let's go through each and see

                List<DirectoryRecord> resolvedFiles = new List<DirectoryRecord>();
                List<DirectoryRecord> unresolvedFiles = new List<DirectoryRecord>();
                //UUID, Path Name, Path

                foreach (var directory in directories)
                {
                    string? foundDirectoryPath = await manager.GetDirectoryById(directory.UUID);

                    if (foundDirectoryPath == null)
                    {
                        unresolvedFiles.Add(directory);
                    }

                    else
                    {
                        resolvedFiles.Add(directory);

                    }
                }

                return (resolvedFiles, unresolvedFiles);

            }

            public static async Task<List<string>> GetGenericDirectories(bool onlyResolved = true)
            {
                var (resolved, unresolved) = await GetGenericDirectories();
                return onlyResolved ? resolved.Select(r => r.Path).ToList() : resolved.Concat(unresolved).Select(r => r.Path).ToList();
            }

            //U
            public static async Task UpdateGenericDirectory(string uuid, string newDirectory, string newDirectoryName)
            {
                var directoryPath = Path.Combine(DataPath, "Generic");
                var manager = new Yuuko.Bindings.DirectoryManager(directoryPath);

                await manager.LoadBindings();

                var directoryFile = await DataHandler.JSONDataHandler.LoadJsonFile("GenericBank", directoryPath);
                var directories = (List<DirectoryRecord>)await DataHandler.JSONDataHandler.GetVariable<List<DirectoryRecord>>(directoryFile, "Data", encryptionKey);

                // Get the binding by UUID
                var binding = manager.GetBindingById(uuid);

                if (binding != null)
                {
                    // Get the file record
                    var fileRecordSelected = directories.FirstOrDefault(d => d.UUID == uuid);

                    if (fileRecordSelected != null)
                    {
                        // 1. Update the FileRecord in memory
                        fileRecordSelected.Path = newDirectory;
                        fileRecordSelected.PathName = newDirectoryName;
                    }
                    else
                    {
                        Console.WriteLine($"Warning: No FileRecord found with UUID {uuid}. JSON update skipped.");
                    }

                    // 2. Update the binding in the manager
                    var newResolution = new Yuuko.Bindings.DirectoryResolution(newDirectory, verified: true);
                    bool updated = await manager.UpdateBinding(uuid, newResolution);

                    if (!updated)
                    {
                        Console.WriteLine($"Warning: Binding exists but could not be updated for UUID {uuid}.");
                    }

                    // 3. Save updated JSON
                    var editedJSON = await DataHandler.JSONDataHandler.UpdateJson<List<DirectoryRecord>>(directoryFile, "Data", directories, encryptionKey);
                    await DataHandler.JSONDataHandler.SaveJson(editedJSON);
                }
                else
                {
                    Console.WriteLine($"Warning: No binding found with UUID {uuid}. Update skipped.");
                }
            }
            //D
            public static async Task RemoveGenericDirectory(string uuid, bool deleteDirectory = false)
            {
                var directoryPath = Path.Combine(DataPath, "Generic");

                var manager = new Yuuko.Bindings.DirectoryManager(directoryPath);

                await manager.LoadBindings();

                var directoryFile = await DataHandler.JSONDataHandler.LoadJsonFile("GenericBank", directoryPath);

                var directories = (List<DirectoryRecord>)await DataHandler.JSONDataHandler.GetVariable<List<DirectoryRecord>>(directoryFile, "Data", encryptionKey);
                //UUID, path name, path 

                var directoryDataInManagerReturn = await manager.GetDirectoryById(uuid);

                if (directories.Any(d => d.UUID == uuid))
                {
                    //Delete
                    await manager.DeleteBinding(uuid);

                    if (deleteDirectory)
                    {
                        Directory.Delete(directoryPath, true);
                        throw new InvalidOperationException($"Cannot remove directory directory: UUID '{uuid}' not found.");
                    }
                }

                else
                {
                    //throw warning and stop

                }

                var editedJSON = await DataHandler.JSONDataHandler.UpdateJson<List<DirectoryRecord>>(directoryFile, "Data", directories, encryptionKey);

                await DataHandler.JSONDataHandler.SaveJson(editedJSON);

            }


        }

        //Do Albums Later

        //Do Artists Later



        //Order for later, first do albums then playlists
        //Then artists
        //Them music player
        //Then solve .yuukoApp
        //Then data slots
        // then audio management
        //then alarm
        //Then keyboard
        //Then previews

        //You can do this!





  





        









        public static class SystemInfoDisplayWindows
        {


            public struct SystemSpecs
            {
                public string OSInfo;
                public string CPUInfo;
                public string MemoryInfo;
                public string DiskInfo;
                public string GPUInfo;
                public string NetworkInfo;
                public string UptimeInfo;
                public string VRHeadsetStatus;
            }


            static SystemSpecs GenerateSpecs()
            {
                SystemSpecs specs = new SystemSpecs
                {
                    OSInfo = GetOSInfo(),
                    CPUInfo = GetCPUInfo(),
                    MemoryInfo = GetMemoryInfo(),
                    DiskInfo = GetDiskInfo(),
                    GPUInfo = GetGPUInfo(),
                    NetworkInfo = GetNetworkInfo(),
                    UptimeInfo = GetUptimeInfo(),
                    VRHeadsetStatus = CheckVRHeadset() ? "Coming Soon!" : "No VR Headset Connected"
                };

                return specs;
            }


            static string GetOSInfo()
            {
                return Environment.OSVersion.ToString();
            }

            static string GetCPUInfo()
            {
                string cpuInfo = "";
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("select * from Win32_Processor");
                foreach (ManagementObject obj in searcher.Get())
                {
                    cpuInfo += $"{obj["Name"]}, Cores: {obj["NumberOfCores"]}\n";
                }
                return cpuInfo;
            }

            static string GetMemoryInfo()
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("select * from Win32_OperatingSystem");
                foreach (ManagementObject obj in searcher.Get())
                {
                    return $"Total: {Math.Round(Convert.ToDouble(obj["TotalVisibleMemorySize"]) / 1024 / 1024, 2)} GB, Free: {Math.Round(Convert.ToDouble(obj["FreePhysicalMemory"]) / 1024 / 1024, 2)} GB";
                }
                return "N/A";
            }

            static string GetDiskInfo()
            {
                string diskInfo = "";
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("select * from Win32_LogicalDisk");
                foreach (ManagementObject obj in searcher.Get())
                {
                    diskInfo += $"{obj["DeviceID"]}: Free: {Math.Round(Convert.ToDouble(obj["FreeSpace"]) / 1024 / 1024 / 1024, 2)} GB, Total: {Math.Round(Convert.ToDouble(obj["Size"]) / 1024 / 1024 / 1024, 2)} GB\n";
                }
                return diskInfo;
            }

            static string GetGPUInfo()
            {
                string gpuInfo = "";
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("select * from Win32_VideoController");
                foreach (ManagementObject obj in searcher.Get())
                {
                    gpuInfo += $"{obj["Name"]}, RAM: {Math.Round(Convert.ToDouble(obj["AdapterRAM"]) / 1024 / 1024 / 1024, 2)} GB\n";
                }
                return gpuInfo;
            }

            static string GetNetworkInfo()
            {
                string networkInfo = "";
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("select * from Win32_NetworkAdapterConfiguration where IPEnabled = 'TRUE'");
                foreach (ManagementObject obj in searcher.Get())
                {
                    networkInfo += $"{obj["Description"]}: IP: {string.Join(", ", (string[])obj["IPAddress"])}\n";
                }

                var networkItems = new List<JsonObject>();

                foreach (ManagementObject obj in searcher.Get)
                {
                    var newNetworkItem = new JsonObject();

                    newNetworkItem.Add("Description", obj["Description"].ToString());
                    newNetworkItem.Add("IPAddress", string.Join(", ", (string[])obj["IPAddress"]));
                }


                return networkInfo;
            }

            static string GetUptimeInfo()
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("select * from Win32_OperatingSystem");
                foreach (ManagementObject obj in searcher.Get())
                {
                    DateTime lastBootUpTime = ManagementDateTimeConverter.ToDateTime(obj["LastBootUpTime"].ToString());
                    TimeSpan uptime = DateTime.Now - lastBootUpTime;

                    var returnPack = new JsonObject();
                    returnPack.Add("LastBootUpTime", lastBootUpTime.ToString());
                    returnPack.Add("UptimeDays", uptime.Days);
                    returnPack.Add("UptimeHours", uptime.Hours);
                    returnPack.Add("UptimeMinutes", uptime.Minutes);

                    //Guys please be smart; you don't need to run this on an update function to know the uptime; take a recording of the time as is and use this to get a reference point
                    //THEN you should add to the uptime based on the difference of time between when the call was done and now; this CAN be an update but you'd best make it async too


                    return $"{uptime.Days} days, {uptime.Hours} hours, {uptime.Minutes} minutes";
                }
                return "N/A";
            }

            static bool CheckVRHeadset()
            {
                //Coming soon
                return true;
            }

            //Okay what the HELL was younger me doing, we can still use this and it's good but let's format it as a JsonObject Instead
        }


        public class SceneData
        {

            #region structs
            public struct Session
            {
                public XRUIOS.Security.CultistKey SavedSession; //The actual save data for our session, this can be a gameobj holding an entire scene or a single 3D object
                public AESEncryptedText SaveSessionType; //Our type of Save Session, this is for organization and the user can create their own, in realtime we just check the parent object and decide accordingly!
                public XRUIOS.Security.EncryptedBoolData IsFavorite; //If this is favorited
                public AESEncryptedText DateAndTime; //The date and time
                public AESEncryptedText Title; //Title
                public AESEncryptedText Description; //Description



                public Session(DecryptedSession session, string UserPassword)

                {
                    this.SavedSession = new XRUIOS.Security.CultistKey(session.SavedSession, UserPassword);
                    this.SaveSessionType = Encrypt(session.SaveSessionType, UserPassword);
                    this.IsFavorite = XRUIOS.Security.EncryptBool(session.IsFavorite, UserPassword);
                    this.DateAndTime = Encrypt(session.DateAndTime, UserPassword);
                    this.Title = Encrypt(session.Title, UserPassword);
                    this.Description = Encrypt(session.Description, UserPassword);



                }
            }

            public struct DecryptedSession
            {
                public GameObject SavedSession; //The actual save data for our session, this can be a gameobj holding an entire scene or a single 3D object
                public string SaveSessionType; //Our type of Save Session, this is for organization and the user can create their own, in realtime we just check the parent object and decide accordingly!
                public bool IsFavorite; //If this is favorited
                public string DateAndTime; //The date and time
                public string Title; //Title
                public string Description; //Description



                public DecryptedSession(GameObject SavedSession, string SaveSessionType, bool IsFavorite, string DateTimeVar,
                    string Title, string Description, string UserPassword)

                {
                    this.SavedSession = SavedSession;
                    this.SaveSessionType = SaveSessionType;
                    this.IsFavorite = IsFavorite;
                    var timenow = DateTime.Now;
                    string temptimenowasstring = default;

                    //If we are using DecryptSession we just give the value decrypted, however if this is a new one we will set the datetime
                    if (DateTimeVar == default)
                    {
                        temptimenowasstring = timenow.ToString("yyyy-MM-dd//THH:mm:ss//Z");
                    }
                    else
                    {
                        temptimenowasstring = DateTimeVar;
                    }
                    var timenowasstring = temptimenowasstring;
                    this.DateAndTime = timenowasstring;
                    this.Title = Title;
                    this.Description = Description;



                }
            }

            public static DecryptedSession DecryptSession(Session session)
            {
                var tempsavedsession = (GameObject)XRUIOS.Security.DecryptCultistKey(session.SavedSession, UserPassword).Item;
                var tempsavesessiontype = Decrypt(session.SaveSessionType, UserPassword);
                var tempisfavorite = (Boolean)XRUIOS.Security.DecryptBool(session.IsFavorite, UserPassword);
                var tempdateandtime = Decrypt(session.DateAndTime, UserPassword);
                var temptitle = Decrypt(session.Title, UserPassword);
                var tempdescription = Decrypt(session.Description, UserPassword);



                DecryptedSession tempsess = new DecryptedSession(tempsavedsession, tempsavesessiontype, tempisfavorite, tempdateandtime, temptitle, tempdescription, UserPassword);

                return tempsess;
            }




            public struct DataSlot
            {
                public XRUIOS.Security.EncryptedBoolData IsFavorite; //If this is favorited
                public AESEncryptedText DateAndTime; //The date and time it was made
                public AESEncryptedText Title; //Title
                public AESEncryptedText Description; //Description
                public AESEncryptedText ImgPath; //The path to the img icon
                public AESEncryptedText TextureFolder; //2.5D images for previewing, for v2
                public List<WorldPoint> WorldPoints;

                public DataSlot(DecryptedDataSlot session, string UserPassword)

                {
                    this.IsFavorite = XRUIOS.Security.EncryptBool(session.IsFavorite, UserPassword);
                    this.DateAndTime = Encrypt(session.DateAndTime, UserPassword);
                    this.Title = Encrypt(session.Title, UserPassword);
                    this.Description = Encrypt(session.Description, UserPassword);
                    this.ImgPath = Encrypt(session.ImgPath, UserPassword);
                    this.TextureFolder = default;

                    this.WorldPoints = new List<WorldPoint>();
                    foreach (DecryptedWorldPoint item in session.WorldPoints)
                    {
                        WorldPoints.Add(new WorldPoint(item, UserPassword));
                    }
                }
            }


            public struct DecryptedDataSlot
            {
                public bool IsFavorite; //If this is favorited
                public string DateAndTime; //The date and time it was made
                public string Title; //Title
                public string Description; //Description
                public string ImgPath; //The path to the img icon
                public string TextureFolder; //2.5D images for previewing, for v2
                public List<DecryptedWorldPoint> WorldPoints;

                public DecryptedDataSlot(bool IsFavorite, string DateTimeVar,
                        string Title, string Description, string ImgPath, string UserPassword, List<string> ObjectsAndTransforms, string TextureFolder, Dictionary<string, Color> Categories,
                        List<DecryptedWorldPoint> structWorldPoints)

                {
                    this.IsFavorite = IsFavorite;
                    var timenow = DateTime.Now;
                    string temptimenowasstring = default;

                    //If we are using DecryptSession we just give the value decrypted, however if this is a new one we will set the datetime
                    if (DateTimeVar == default)
                    {
                        temptimenowasstring = timenow.ToString("yyyy-MM-dd//THH:mm:ss//Z");
                    }
                    else
                    {
                        temptimenowasstring = DateTimeVar;
                    }
                    var timenowasstring = temptimenowasstring;
                    this.DateAndTime = timenowasstring;
                    this.Title = Title;
                    this.Description = Description;
                    this.ImgPath = ImgPath;




                    this.TextureFolder = default; //I'm a dumbass i'll make it later I hate how stupid I am I bet someone else could do this in one sitting, maybe 5 minutes

                    this.WorldPoints = structWorldPoints;


                }
            }


            public static DecryptedDataSlot DecryptDataSlot(DataSlot item, string UserPassword)
            {
                var decryptedDataSlot = new DecryptedDataSlot();
                decryptedDataSlot.IsFavorite = XRUIOS.Security.DecryptBool(item.IsFavorite, UserPassword);
                decryptedDataSlot.DateAndTime = Decrypt(item.DateAndTime, UserPassword);
                decryptedDataSlot.Title = Decrypt(item.Title, UserPassword);
                decryptedDataSlot.Description = Decrypt(item.Description, UserPassword);
                decryptedDataSlot.ImgPath = Decrypt(item.ImgPath, UserPassword);
                decryptedDataSlot.TextureFolder = default; // You need to implement decryption for TextureFolder

                decryptedDataSlot.WorldPoints = new List<DecryptedWorldPoint>();
                foreach (WorldPoint objecct in item.WorldPoints)
                {
                    decryptedDataSlot.WorldPoints.Add(DecryptWorldPoint(objecct, UserPassword));
                }

                return decryptedDataSlot;
            }



            public struct DecryptedWorldPoint
            {
                public DesktopMirrors.Apps.RenderingMode RenderingMode;
                public object PointData; // Point data
                public string PointName;
                public string PointDescription;
                public string PointImagePath;
                public bool UserCentric; // Is this a point which is at a fixed point relative to the user
                public List<Objects.DecryptedStaticObject> StaticObjs;
                public List<Objects.DecryptedApp> AppObjs;
                public List<Objects.DecryptedDesktopScreen> DesktopScreenObjs;
                public List<Objects.DecryptedStaciaItems> StaciaObjs;

                public DecryptedWorldPoint(DesktopMirrors.Apps.RenderingMode renderingMode, object pointData, string pointName, string pointDescription, string pointImagePath,
                    bool userCentric, List<Objects.DecryptedStaticObject> staticObjs, List<Objects.DecryptedApp> appObjs,
                    List<Objects.DecryptedDesktopScreen> desktopScreenObjs, List<Objects.DecryptedStaciaItems> staciaObjs)
                {
                    RenderingMode = renderingMode;
                    PointData = pointData;
                    PointName = pointName;
                    PointDescription = pointDescription;
                    PointImagePath = pointImagePath;
                    UserCentric = userCentric;
                    StaticObjs = staticObjs;
                    AppObjs = appObjs;
                    DesktopScreenObjs = desktopScreenObjs;
                    StaciaObjs = staciaObjs;
                }
            }

            public struct WorldPoint
            {
                public XRUIOS.Security.CultistKey RenderingMode;
                public XRUIOS.Security.CultistKey PointData; // Point data
                public AESEncryptedText PointName;
                public AESEncryptedText PointDescription;
                public AESEncryptedText PointImagePath;
                public XRUIOS.Security.EncryptedBoolData UserCentric; // Is this a point which is at a fixed point relative to the user
                public List<Objects.StaticObject> StaticObjs;
                public List<Objects.App> AppObjs;
                public List<Objects.DesktopScreen> DesktopScreenObjs;
                public List<Objects.StaciaItems> StaciaObjs;


                public WorldPoint(DecryptedWorldPoint item, string userPassword)
                {
                    RenderingMode = new XRUIOS.Security.CultistKey(item.RenderingMode, userPassword);
                    PointData = new XRUIOS.Security.CultistKey(item.PointData, userPassword);
                    PointName = Encrypt(item.PointName, userPassword);
                    PointDescription = Encrypt(item.PointDescription, userPassword);
                    PointImagePath = Encrypt(item.PointImagePath, userPassword);
                    UserCentric = XRUIOS.Security.EncryptBool(item.UserCentric, userPassword);

                    StaticObjs = new List<Objects.StaticObject>();
                    foreach (var staticObj in item.StaticObjs)
                    {
                        StaticObjs.Add(new Objects.StaticObject(staticObj, userPassword));
                    }


                    AppObjs = new List<Objects.App>();
                    foreach (var appObj in item.AppObjs)
                    {
                        AppObjs.Add(new Objects.App(appObj, userPassword));
                    }

                    DesktopScreenObjs = new List<Objects.DesktopScreen>();
                    foreach (var desktopScreenObj in item.DesktopScreenObjs)
                    {
                        DesktopScreenObjs.Add(new Objects.DesktopScreen(desktopScreenObj, userPassword));
                    }

                    StaciaObjs = new List<Objects.StaciaItems>();
                    foreach (var staciaObj in item.StaciaObjs)
                    {
                        StaciaObjs.Add(new Objects.StaciaItems(staciaObj, userPassword));
                    }
                }

            }

            public static DecryptedWorldPoint DecryptWorldPoint(WorldPoint item, string userPassword)
            {
                DesktopMirrors.Apps.RenderingMode tempRenderingMode = (DesktopMirrors.Apps.RenderingMode)XRUIOS.Security.DecryptCultistKey(item.RenderingMode, userPassword).Item;
                object tempPointData = XRUIOS.Security.DecryptCultistKey(item.PointData, userPassword).Item;
                string tempPointName = Decrypt(item.PointName, userPassword);
                string tempPointDescription = Decrypt(item.PointDescription, userPassword);
                string tempPointImagePath = Decrypt(item.PointImagePath, userPassword);
                bool tempUserCentric = XRUIOS.Security.DecryptBool(item.UserCentric, userPassword);

                List<Objects.DecryptedStaticObject> tempStaticObjs = new List<Objects.DecryptedStaticObject>();
                foreach (var staticObj in item.StaticObjs)
                {
                    tempStaticObjs.Add(Objects.DecryptStaticObject(staticObj));
                }

                List<Objects.DecryptedApp> tempAppObjs = new List<Objects.DecryptedApp>();
                foreach (var appObj in item.AppObjs)
                {
                    tempAppObjs.Add(Objects.DecryptApp(appObj));
                }

                List<Objects.DecryptedDesktopScreen> tempDesktopScreenObjs = new List<Objects.DecryptedDesktopScreen>();
                foreach (var desktopScreenObj in item.DesktopScreenObjs)
                {
                    tempDesktopScreenObjs.Add(Objects.DecryptDesktopScreen(desktopScreenObj));
                }

                List<Objects.DecryptedStaciaItems> tempStaciaObjs = new List<Objects.DecryptedStaciaItems>();
                foreach (var staciaObj in item.StaciaObjs)
                {
                    tempStaciaObjs.Add(Objects.DecryptStaciaItems(staciaObj));
                }

                var decryptedWorldPoint = new DecryptedWorldPoint(
                    tempRenderingMode, tempPointData, tempPointName, tempPointDescription, tempPointImagePath,
                    tempUserCentric, tempStaticObjs, tempAppObjs, tempDesktopScreenObjs, tempStaciaObjs
                );

                return decryptedWorldPoint;
            }


            #endregion

            public class DataStore
            {
                public static string DataSlotDirectoryPath = "caca";



                public string LoadSessionIntoScene(string appname, string sessionname) //This loads an app or script item by using the SavedSessions system. You can hold multiple at once
                                                                                       //To use this, take Dictionary<string, XRUIOS.Security.CultistKey> from the App Object and find the session you want! Do that with the "GetSession"
                {
                    string status = default;


                    //Let's get our gameobject
                    var targetapp = Application.Apps.GetAppByName(appname);
                    //Now let's find the session we want
                    var sessiondictionary = targetapp.SavedSessions;

                    //Now let's decrypt everything in this dictionary and make a new, decrypted dictionary

                    Dictionary<string, Session> tempss = default;

                    foreach (KeyValuePair<AESEncryptedText, XRUIOS.Security.CultistKey> objects in sessiondictionary)
                    {
                        var item1 = Decrypt(objects.Key, UserPassword);
                        var item2 = (Session)XRUIOS.Security.DecryptCultistKey(objects.Value, UserPassword).Item;
                        tempss.Add(item1, item2);
                    }

                    //We now can get our our session object
                    var targetsession = (Session)tempss[sessionname];

                    //We now can get our decrypted session

                    var decryptedsession = DecryptSession(targetsession);



                    //Now let's get our gameobject                 
                    var obj1 = decryptedsession.SavedSession;
                    //Now let's get the topmost object in session. We use this to broadcast later
                    var parent = obj1.transform.root;

                    //Now let's load our object into the scene. For the sake of ease, we will simply check our mode and load it into the 5th main parent object, which is ALWAYS the scene obj holder.
                    //Keep this in mind when making save sessions!


                    return status;
                }

                //REPLACE APPINFO WITH DECRYPTEDAPPINFO
                public string SaveSession(string appname, DecryptedSession tempsession, string sessionname) //Add a session to an app object
                {
                    string status = "Failed";


                    //Let's get our app
                    var targetapp = Application.Apps.GetAppByName(appname);

                    //Now we turn it to the session
                    var newsession = new Session(tempsession, UserPassword);

                    //And we encrypt it
                    var encryptedsession = new XRUIOS.Security.CultistKey(newsession, UserPassword);

                    //As well as the title of the session name
                    var encryptedsessionname = Encrypt(sessionname, UserPassword);

                    //And we add to the list
                    targetapp.SavedSessions.Add(encryptedsessionname, encryptedsession);

                    //And we decrypt our app list so we can update it (probably better way and order to do this but screw it)
                    var finalapp = Application.DecryptAppInfo(targetapp);

                    //And now let's finally our applist with this session
                    Application.Apps.UpdateAppInVault(appname, finalapp);


                    status = "Finished";

                    return status;

                }


                public DecryptedSession GetSavedSessions(string appname, string sessionname)
                {

                    //Let's get our app
                    var targetapp = Application.Apps.GetAppByName(appname);

                    //Now let's get the saved sessions
                    var sessionEncrypted = targetapp.SavedSessions;

                    Dictionary<string, Session> tempss = default;

                    //Now let's decrypt everything in here
                    foreach (KeyValuePair<AESEncryptedText, XRUIOS.Security.CultistKey> objects in sessionEncrypted)
                    {
                        var item1 = Decrypt(objects.Key, UserPassword);
                        var item2 = (Session)XRUIOS.Security.DecryptCultistKey(objects.Value, UserPassword).Item;
                        tempss.Add(item1, item2);
                    }

                    var tempsession = tempss[sessionname];

                    var finalsession = DecryptSession(tempsession);

                    return finalsession;

                }

                public string DeleteSavedSessions(string appname, string sessionname)
                {

                    string status = "Failed";

                    //Let's get our app
                    var targetapp = Application.Apps.GetAppByName(appname);

                    //Now let's get the saved sessions
                    var sessionEncrypted = targetapp.SavedSessions;

                    Dictionary<string, Session> tempss = default;

                    //Now let's decrypt everything in here
                    foreach (KeyValuePair<AESEncryptedText, XRUIOS.Security.CultistKey> objects in sessionEncrypted)
                    {
                        var item1 = Decrypt(objects.Key, UserPassword);
                        var item2 = (Session)XRUIOS.Security.DecryptCultistKey(objects.Value, UserPassword).Item;
                        tempss.Add(item1, item2);
                    }

                    var tempsession = tempss.Remove(sessionname);

                    //And we decrypt our app list so we can update it (probably better way and order to do this but screw it)
                    var finalapp = Application.DecryptAppInfo(targetapp);


                    //And now let's finally our applist with this session
                    Application.Apps.UpdateAppInVault(appname, finalapp);

                    status = "Finished";

                    return status;


                }
            }

            public class DataSlots
            {

                public static List<DecryptedDataSlot> GetDataSlotDirectory()
                {

                    //Get the JSON File holding the MusicDirectory object for the user
                    var FileWithDataDirectory = UniversalSave.Load(DataStore.DataSlotDirectoryPath, DataFormat.JSON);

                    //This file has a lot of things in it, so we are getting the variable called MusicDirectory, aka the object we are looking for
                    List<DataSlot> CurrentDataSlotDirectory = (List<DataSlot>)FileWithDataDirectory.Get("DataSlots");

                    List<DecryptedDataSlot> finallist = new List<DecryptedDataSlot>();

                    foreach (DataSlot item in CurrentDataSlotDirectory)
                    {
                        finallist.Add(DecryptDataSlot(item, UserPassword));
                    }

                    return finallist;
                }

                public static void SaveDataSlot(DecryptedDataSlot slot, string DataSlotTitle)
                {
                    var listofslots = GetDataSlotDirectory();
                    listofslots.Add(slot);

                    List<DataSlot> final = new List<DataSlot>();

                    foreach (DecryptedDataSlot item in listofslots)
                    {
                        final.Add(new DataSlot(item, UserPassword));
                    }

                    var newsave = new UniversalSave();

                    newsave.Set("DataSlots", listofslots);

                    UniversalSave.Save(DataStore.DataSlotDirectoryPath, newsave);

                }

                public static DecryptedDataSlot LoadDataSlot(string DataSlotTitle)
                {
                    var listofslots = DataSlots.GetDataSlotDirectory();
                    DecryptedDataSlot final = default;
                    foreach (DecryptedDataSlot item in listofslots)
                    {
                        if (DataSlotTitle == item.Title)
                        {
                            final = item;
                            break;
                        }
                    }
                    return final;
                }

                public static void UpdateDataSlot(DecryptedDataSlot slot, string DataSlotTitle)
                {
                    var listofslots = GetDataSlotDirectory();

                    foreach (DecryptedDataSlot item in listofslots)
                    {
                        if (DataSlotTitle == item.Title)
                        {
                            var indexnumber = listofslots.IndexOf(item);
                            listofslots.RemoveAt(indexnumber);
                            listofslots.Insert(indexnumber, slot);
                            break;
                        }
                    }

                    List<DataSlot> final = new List<DataSlot>();

                    foreach (DecryptedDataSlot item in listofslots)
                    {
                        final.Add(new DataSlot(item, UserPassword));
                    }

                    var newsave = new UniversalSave();

                    newsave.Set("DataSlots", listofslots);

                    UniversalSave.Save(DataStore.DataSlotDirectoryPath, newsave);

                }

                public static void DeleteDataSlot(string DataSlotTitle)
                {
                    var listofslots = GetDataSlotDirectory();

                    foreach (DecryptedDataSlot item in listofslots)
                    {
                        if (DataSlotTitle == item.Title)
                        {
                            listofslots.Remove(item);
                            break;
                        }
                    }

                    List<DataSlot> final = new List<DataSlot>();

                    foreach (DecryptedDataSlot item in listofslots)
                    {
                        final.Add(new DataSlot(item, UserPassword));
                    }

                    var newsave = new UniversalSave();

                    newsave.Set("DataSlots", listofslots);

                    UniversalSave.Save(DataStore.DataSlotDirectoryPath, newsave);

                }

                public static List<DecryptedDataSlot> GetAllDataSlots()
                {
                    var listofslots = GetDataSlotDirectory();
                    return listofslots;
                }

                public void UnpackDataSlot()
                {

                }



            }


            public class WorldPoints
            {



                public void SaveWorldPoint(DecryptedWorldPoint point, string DataSlotTitle) //Dataslot must exist first
                {
                    DecryptedDataSlot targetdataslot = DataSlots.LoadDataSlot(DataSlotTitle);

                    targetdataslot.WorldPoints.Add(point);

                    List<DataSlot> final = new List<DataSlot>();

                    DataSlots.UpdateDataSlot(targetdataslot, DataSlotTitle);

                }

                public List<DecryptedWorldPoint> LoadWorldPoints(string pointname, string DataSlotTitle)
                {
                    DecryptedDataSlot targetdataslot = DataSlots.LoadDataSlot(DataSlotTitle);

                    List<DecryptedWorldPoint> points = targetdataslot.WorldPoints;

                    return points;


                }

                public DecryptedWorldPoint LoadSpecificWorldPoint(string pointname, string DataSlotTitle)
                {
                    DecryptedDataSlot targetdataslot = DataSlots.LoadDataSlot(DataSlotTitle);

                    DecryptedWorldPoint target = default;

                    foreach (DecryptedWorldPoint item in targetdataslot.WorldPoints)
                    {
                        if (item.PointName == pointname)
                        {
                            target = item;
                            break;
                        }
                    }

                    return target;


                }

                public void UpdateWorldPoint(DecryptedWorldPoint point, string DataSlotTitle, string pointname)
                {
                    DecryptedDataSlot targetdataslot = DataSlots.LoadDataSlot(DataSlotTitle);

                    foreach (DecryptedWorldPoint item in targetdataslot.WorldPoints)
                    {
                        if (item.PointName == pointname)
                        {
                            var indexnumber = targetdataslot.WorldPoints.IndexOf(item);
                            targetdataslot.WorldPoints.Remove(item);
                            targetdataslot.WorldPoints.Insert(indexnumber, item);
                            break;

                        }
                    }

                    DataSlots.UpdateDataSlot(targetdataslot, DataSlotTitle);

                }

                public void DeleteWorldPoint(string DataSlotTitle, string pointname)
                {
                    DecryptedDataSlot targetdataslot = DataSlots.LoadDataSlot(DataSlotTitle);

                    foreach (DecryptedWorldPoint item in targetdataslot.WorldPoints)
                    {
                        if (item.PointName == pointname)
                        {

                            targetdataslot.WorldPoints.Remove(item);

                            break;

                        }
                    }

                    DataSlots.UpdateDataSlot(targetdataslot, DataSlotTitle);

                }
            }


            public class Objects //Exception to struct rule, this is a chunk of structs
            {
                public enum PositionalTrackingMode { Follow, Anchored, FollowingExternal }
                public enum RotationalTrackingMode { Static, LAM }
                public enum ObjectOSLabel { Default, Software, Objects, Voice, Music, Alerts, Ui, Other }

                //For referncing objects, they are imported using modtool but are at a different list. 

                //Naming = Name.AR/VR


                public struct DecryptedStaticObject
                {
                    public PositionalTrackingMode PTrackingType;
                    public RotationalTrackingMode RTrackingType;
                    public string Name; // Path to the object
                    public Vector3 SpatialData;
                    public ObjectOSLabel ObjectLabel;

                    public DecryptedStaticObject(PositionalTrackingMode pTrackingType, RotationalTrackingMode rTrackingType, string name, Vector3 spatialData, ObjectOSLabel objectLabel)
                    {
                        PTrackingType = pTrackingType;
                        RTrackingType = rTrackingType;
                        Name = name;
                        SpatialData = spatialData;
                        ObjectLabel = objectLabel;
                    }
                }

                public struct DecryptedApp
                {
                    public PositionalTrackingMode PTrackingType;
                    public RotationalTrackingMode RTrackingType;
                    public Application.DecryptedAppInfo WindowsAppInfo;
                    public DesktopMirrors.Apps.DecryptedApp MainAppData;
                    public Vector3 SpatialData;
                    public ObjectOSLabel ObjectLabel;

                    public DecryptedApp(PositionalTrackingMode pTrackingType, RotationalTrackingMode rTrackingType, Application.DecryptedAppInfo windowsAppInfo, DesktopMirrors.Apps.DecryptedApp mainAppData, Vector3 spatialData, ObjectOSLabel objectLabel)
                    {
                        PTrackingType = pTrackingType;
                        RTrackingType = rTrackingType;
                        WindowsAppInfo = windowsAppInfo;
                        MainAppData = mainAppData;
                        SpatialData = spatialData;
                        ObjectLabel = objectLabel;
                    }
                }

                public struct DecryptedDesktopScreen
                {
                    public PositionalTrackingMode PTrackingType;
                    public RotationalTrackingMode RTrackingType;
                    public DesktopMirrors.Monitors.DecryptedMonitor DesktopData;
                    public Vector3 SpatialData;
                    public ObjectOSLabel ObjectLabel;

                    public DecryptedDesktopScreen(PositionalTrackingMode pTrackingType, RotationalTrackingMode rTrackingType, DesktopMirrors.Monitors.DecryptedMonitor desktopData, Vector3 spatialData, ObjectOSLabel objectLabel)
                    {
                        PTrackingType = pTrackingType;
                        RTrackingType = rTrackingType;
                        DesktopData = desktopData;
                        SpatialData = spatialData;
                        ObjectLabel = objectLabel;
                    }
                }

                public struct DecryptedStaciaItems
                {
                    public PositionalTrackingMode PTrackingType;
                    public RotationalTrackingMode RTrackingType;
                    public string BinaryData;
                    public Vector3 SpatialData;
                    public ObjectOSLabel ObjectLabel;

                    public DecryptedStaciaItems(PositionalTrackingMode pTrackingType, RotationalTrackingMode rTrackingType, string binaryData, Vector3 spatialData, ObjectOSLabel objectLabel)
                    {
                        PTrackingType = pTrackingType;
                        RTrackingType = rTrackingType;
                        BinaryData = binaryData;
                        SpatialData = spatialData;
                        ObjectLabel = objectLabel;
                    }
                }





                public struct StaticObject
                {
                    public XRUIOS.Security.CultistKey PTrackingType;
                    public XRUIOS.Security.CultistKey RTrackingType;
                    public AESEncryptedText Name;
                    public XRUIOS.Security.CultistKey SpatialData;
                    public XRUIOS.Security.CultistKey ObjectLabel;

                    public StaticObject(DecryptedStaticObject decryptedObject, string userPassword)
                    {
                        PTrackingType = new XRUIOS.Security.CultistKey(decryptedObject.PTrackingType, userPassword);
                        RTrackingType = new XRUIOS.Security.CultistKey(decryptedObject.RTrackingType, userPassword);
                        Name = Encrypt(decryptedObject.Name, userPassword);
                        SpatialData = new XRUIOS.Security.CultistKey(decryptedObject.SpatialData, userPassword);
                        ObjectLabel = new XRUIOS.Security.CultistKey(decryptedObject.ObjectLabel, userPassword);
                    }
                }

                public struct App
                {
                    public XRUIOS.Security.CultistKey PTrackingType;
                    public XRUIOS.Security.CultistKey RTrackingType;
                    public Application.AppInfo WindowsAppInfo;
                    public DesktopMirrors.Apps.App MainAppData;
                    public XRUIOS.Security.CultistKey SpatialData;
                    public XRUIOS.Security.CultistKey ObjectLabel;

                    public App(DecryptedApp decryptedApp, string userPassword)
                    {
                        PTrackingType = new XRUIOS.Security.CultistKey(decryptedApp.PTrackingType, userPassword);
                        RTrackingType = new XRUIOS.Security.CultistKey(decryptedApp.RTrackingType, userPassword);

                        WindowsAppInfo = new Application.AppInfo(decryptedApp.WindowsAppInfo, UserPassword); // Assuming AppInfo is not encrypted
                        MainAppData = new DesktopMirrors.Apps.App(decryptedApp.MainAppData);

                        SpatialData = new XRUIOS.Security.CultistKey(decryptedApp.SpatialData, userPassword);
                        ObjectLabel = new XRUIOS.Security.CultistKey(decryptedApp.ObjectLabel, userPassword);
                    }
                }

                public struct DesktopScreen
                {
                    public XRUIOS.Security.CultistKey PTrackingType;
                    public XRUIOS.Security.CultistKey RTrackingType;
                    public DesktopMirrors.Monitors.Monitor DesktopData;
                    public XRUIOS.Security.CultistKey SpatialData;
                    public XRUIOS.Security.CultistKey ObjectLabel;

                    public DesktopScreen(DecryptedDesktopScreen decryptedScreen, string userPassword)
                    {
                        PTrackingType = new XRUIOS.Security.CultistKey(decryptedScreen.PTrackingType, userPassword);
                        RTrackingType = new XRUIOS.Security.CultistKey(decryptedScreen.RTrackingType, userPassword);
                        DesktopData = new DesktopMirrors.Monitors.Monitor(decryptedScreen.DesktopData);
                        SpatialData = new XRUIOS.Security.CultistKey(decryptedScreen.SpatialData, userPassword);
                        ObjectLabel = new XRUIOS.Security.CultistKey(decryptedScreen.ObjectLabel, userPassword);
                    }
                }

                public struct StaciaItems
                {
                    public XRUIOS.Security.CultistKey PTrackingType;
                    public XRUIOS.Security.CultistKey RTrackingType;
                    public AESEncryptedText BinaryData; // Updated to encrypted text
                    public XRUIOS.Security.CultistKey SpatialData;
                    public XRUIOS.Security.CultistKey ObjectLabel;

                    public StaciaItems(DecryptedStaciaItems decryptedItems, string userPassword)
                    {
                        PTrackingType = new XRUIOS.Security.CultistKey(decryptedItems.PTrackingType, userPassword);
                        RTrackingType = new XRUIOS.Security.CultistKey(decryptedItems.RTrackingType, userPassword);
                        BinaryData = Encrypt(decryptedItems.BinaryData, userPassword);
                        SpatialData = new XRUIOS.Security.CultistKey(decryptedItems.SpatialData, userPassword);
                        ObjectLabel = new XRUIOS.Security.CultistKey(decryptedItems.ObjectLabel, userPassword);
                    }
                }





                public static DecryptedStaticObject DecryptStaticObject(StaticObject item)
                {
                    PositionalTrackingMode tempt = (PositionalTrackingMode)XRUIOS.Security.DecryptCultistKey(item.PTrackingType, UserPassword).Item;
                    RotationalTrackingMode tempr = (RotationalTrackingMode)XRUIOS.Security.DecryptCultistKey(item.RTrackingType, UserPassword).Item;
                    string name = Decrypt(item.Name, UserPassword);
                    Vector3 tempsd = (Vector3)XRUIOS.Security.DecryptCultistKey(item.SpatialData, UserPassword).Item;
                    ObjectOSLabel tempol = (ObjectOSLabel)XRUIOS.Security.DecryptCultistKey(item.ObjectLabel, UserPassword).Item;

                    var newso = new DecryptedStaticObject(tempt, tempr, name, tempsd, tempol);

                    return newso;
                }

                public static DecryptedApp DecryptApp(App item)
                {
                    PositionalTrackingMode tempt = (PositionalTrackingMode)XRUIOS.Security.DecryptCultistKey(item.PTrackingType, UserPassword).Item;
                    RotationalTrackingMode tempr = (RotationalTrackingMode)XRUIOS.Security.DecryptCultistKey(item.RTrackingType, UserPassword).Item;
                    var winAppInfo = Application.DecryptAppInfo(item.WindowsAppInfo); // Assuming AppInfo is not encrypted
                    var MainApp = DesktopMirrors.Apps.DecryptApp(item.MainAppData, UserPassword);
                    Vector3 tempsd = (Vector3)XRUIOS.Security.DecryptCultistKey(item.SpatialData, UserPassword).Item;
                    ObjectOSLabel tempol = (ObjectOSLabel)XRUIOS.Security.DecryptCultistKey(item.ObjectLabel, UserPassword).Item;

                    var newApp = new DecryptedApp(tempt, tempr, winAppInfo, MainApp, tempsd, tempol);

                    return newApp;
                }

                public static DecryptedDesktopScreen DecryptDesktopScreen(DesktopScreen item)
                {
                    PositionalTrackingMode tempt = (PositionalTrackingMode)XRUIOS.Security.DecryptCultistKey(item.PTrackingType, UserPassword).Item;
                    RotationalTrackingMode tempr = (RotationalTrackingMode)XRUIOS.Security.DecryptCultistKey(item.RTrackingType, UserPassword).Item;
                    DesktopMirrors.Monitors.DecryptedMonitor desktopdata = DesktopMirrors.Monitors.DecryptMonitor(item.DesktopData, UserPassword);
                    Vector3 tempsd = (Vector3)XRUIOS.Security.DecryptCultistKey(item.SpatialData, UserPassword).Item;
                    ObjectOSLabel tempol = (ObjectOSLabel)XRUIOS.Security.DecryptCultistKey(item.ObjectLabel, UserPassword).Item;

                    var newScreen = new DecryptedDesktopScreen(tempt, tempr, desktopdata, tempsd, tempol);

                    return newScreen;
                }

                public static DecryptedStaciaItems DecryptStaciaItems(StaciaItems item)
                {
                    PositionalTrackingMode tempt = (PositionalTrackingMode)XRUIOS.Security.DecryptCultistKey(item.PTrackingType, UserPassword).Item;
                    RotationalTrackingMode tempr = (RotationalTrackingMode)XRUIOS.Security.DecryptCultistKey(item.RTrackingType, UserPassword).Item;
                    string binaryData = Decrypt(item.BinaryData, UserPassword);
                    Vector3 tempsd = (Vector3)XRUIOS.Security.DecryptCultistKey(item.SpatialData, UserPassword).Item;
                    ObjectOSLabel tempol = (ObjectOSLabel)XRUIOS.Security.DecryptCultistKey(item.ObjectLabel, UserPassword).Item;

                    var newItems = new DecryptedStaciaItems(tempt, tempr, binaryData, tempsd, tempol);

                    return newItems;
                }

            }


        }



        public static class Notes
        {

            public static class NoteData
            {



                public struct Note
                {

                    // New fields
                    public string Title;
                    public string Category;
                    public DateTime Created;
                    public DateTime LastUpdate;
                    public string SavedID;
                    public string MiniDescription;
                    public string UserDescription;
                    public string AIDescription;

                    // Existing fields
                    public AESEncryptedText NoteID;
                    public AESEncryptedText XRUIOSNoteID;
                    public AESEncryptedText Text;
                    public List<XRUIOS.Security.CultistKey> Images;


                    public Note(DecryptedNote item, string userPassword)
                    {
                        // Assign new fields
                        Title = string.Empty;
                        Category = string.Empty;
                        Created = DateTime.UtcNow;
                        LastUpdate = Created;
                        SavedID = Guid.NewGuid().ToString(); // Randomly generated ID
                        MiniDescription = string.Empty;
                        UserDescription = string.Empty;
                        AIDescription = string.Empty;

                        // Assign existing fields
                        NoteID = Encrypt(item.NoteID, userPassword);
                        XRUIOSNoteID = Encrypt(item.XRUIOSNoteID, userPassword);
                        Text = Encrypt(item.Text, userPassword);
                        Images = new List<XRUIOS.Security.CultistKey>();

                        foreach (Texture2D picture in item.Images)
                        {
                            Images.Add(new XRUIOS.Security.CultistKey(picture, userPassword));
                        }
                    }
                }

                public struct DecryptedNote
                {
                    // New fields
                    public string Title;
                    public string Category;
                    public DateTime Created;
                    public DateTime LastUpdate;
                    public string SavedID;
                    public string MiniDescription;
                    public string UserDescription;
                    public string AIDescription;

                    // Existing fields
                    public string NoteID;
                    public string XRUIOSNoteID;
                    public string Text;
                    public List<Texture2D> Images;

                    // Constructor
                    public DecryptedNote(string title, string category, DateTime created, DateTime lastUpdate, string savedID, string miniDescription, string userDescription, string aiDescription, string noteID, string xrUiOSNoteID, string text, List<Texture2D> images)
                    {
                        // Assign new fields
                        Title = title;
                        Category = category;
                        Created = created;
                        LastUpdate = lastUpdate;
                        SavedID = savedID;
                        MiniDescription = miniDescription;
                        UserDescription = userDescription;
                        AIDescription = aiDescription;

                        // Assign existing fields
                        NoteID = noteID;
                        XRUIOSNoteID = xrUiOSNoteID;
                        Text = text;
                        Images = images;
                    }
                }

                public static DecryptedNote DecryptNote(Note item)
                {
                    DecryptedNote newitem = new DecryptedNote();

                    newitem.NoteID = Decrypt(item.NoteID, UserPassword);
                    newitem.XRUIOSNoteID = Decrypt(item.XRUIOSNoteID, UserPassword);
                    newitem.Text = Decrypt(item.Text, UserPassword);
                    newitem.Images = new List<Texture2D>();

                    foreach (XRUIOS.Security.CultistKey picture in item.Images)
                    {
                        Texture2D decryptedImage = ((Texture2D)XRUIOS.Security.DecryptCultistKey(picture, UserPassword).Item);
                        newitem.Images.Add(decryptedImage);
                    }

                    // Assign values for new fields
                    newitem.Title = item.Title;
                    newitem.Category = item.Category;
                    newitem.Created = item.Created;
                    newitem.LastUpdate = item.LastUpdate;
                    newitem.SavedID = item.SavedID;
                    newitem.MiniDescription = item.MiniDescription;
                    newitem.UserDescription = item.UserDescription;
                    newitem.AIDescription = item.AIDescription;

                    return newitem;
                }

                public struct Journal
                {

                    public AESEncryptedText JournalName;
                    public AESEncryptedText Description;
                    public AESEncryptedText CoverImagePath;

                    public List<Category> Categories;

                    public Journal(DecryptedJournal item, string userPassword)
                    {

                        JournalName = Encrypt(item.JournalName, userPassword);
                        Description = Encrypt(item.Description, userPassword);
                        CoverImagePath = Encrypt(item.CoverImagePath, userPassword);


                        Categories = new List<Category>();

                        foreach (DecryptedCategory category in item.Categories)
                        {
                            Categories.Add(new Category(category, userPassword));
                        }
                    }
                }

                public struct DecryptedJournal
                {
                    // New fields
                    public string JournalName;
                    public string Description;
                    public string CoverImagePath;

                    // Existing fields
                    public List<DecryptedCategory> Categories;

                    // Constructor
                    public DecryptedJournal(string journalName, string description, string coverImagePath, List<DecryptedCategory> categories)
                    {
                        // Assign new fields
                        JournalName = journalName;
                        Description = description;
                        CoverImagePath = coverImagePath;

                        // Assign existing fields
                        Categories = categories;
                    }
                }

                public struct Category
                {

                    public AESEncryptedText Title;
                    public AESEncryptedText Description;
                    public AESEncryptedText MainImage;
                    public AESEncryptedText MiniImage;


                    public List<Note> Notes;

                    public Category(DecryptedCategory item, string userPassword)
                    {

                        Title = Encrypt(item.Title, UserPassword);
                        Description = Encrypt(item.Description, userPassword);
                        MainImage = Encrypt(item.MainImage, userPassword);
                        MiniImage = Encrypt(item.MiniImage, userPassword);


                        Notes = new List<Note>();

                        foreach (DecryptedNote note in item.Notes)
                        {
                            Notes.Add(new Note(note, userPassword));
                        }
                    }
                }

                public struct DecryptedCategory
                {

                    public string Title;
                    public string Description;
                    public string MainImage;
                    public string MiniImage;

                    public List<DecryptedNote> Notes;

                    public DecryptedCategory(string title, string description, string mainImage, string miniImage, List<DecryptedNote> notes)
                    {

                        Title = title;
                        Description = description;
                        MainImage = mainImage;
                        MiniImage = miniImage;


                        Notes = notes;
                    }
                }

                public static DecryptedJournal DecryptJournal(Journal item)
                {
                    DecryptedJournal newitem = new DecryptedJournal();


                    newitem.JournalName = Decrypt(item.JournalName, UserPassword);
                    newitem.Description = Decrypt(item.Description, UserPassword);
                    newitem.CoverImagePath = Decrypt(item.CoverImagePath, UserPassword);


                    newitem.Categories = new List<DecryptedCategory>();

                    foreach (Category category in item.Categories)
                    {
                        newitem.Categories.Add(DecryptCategory(category));
                    }

                    return newitem;
                }

                public static DecryptedCategory DecryptCategory(Category item)
                {
                    List<DecryptedNote> decryptedNotes = new List<DecryptedNote>();

                    foreach (Note note in item.Notes)
                    {
                        decryptedNotes.Add(DecryptNote(note));
                    }

                    DecryptedCategory decryptedCategory = new DecryptedCategory(
                        Decrypt(item.Title, UserPassword),
                        Decrypt(item.Description, UserPassword),
                        Decrypt(item.MainImage, UserPassword),
                        Decrypt(item.MiniImage, UserPassword),
                        decryptedNotes
                    );

                    return decryptedCategory;
                }








                public static List<DecryptedJournal> Journals;

                public static string JournalPath;



                public static DecryptedCategory GetCategory(string JournalName, string CategoryName)
                {
                    var funcjournal = GetJournal(JournalName);
                    DecryptedCategory tempcategory = default;
                    foreach (DecryptedCategory item in funcjournal.Categories)
                    {
                        if (item.Title == CategoryName)
                        {
                            tempcategory = item;
                        }
                    }

                    return tempcategory;
                }




                public static List<DecryptedJournal> GetAllJournals()
                {
                    var journaldb = (List<Journal>)UniversalSave.Load(JournalPath, DataFormat.JSON).Get("Journals");

                    var returnjournal = new List<DecryptedJournal>();

                    foreach (Journal journal in journaldb)
                    {
                        returnjournal.Add(DecryptJournal(journal));
                    }

                    return returnjournal;
                }

                // Method to get a specific journal by its name
                public static DecryptedJournal GetJournal(string journalName)
                {
                    DecryptedJournal returnval = default;
                    var journalslist = GetAllJournals();
                    foreach (DecryptedJournal item in journalslist)
                    {
                        if (item.JournalName == journalName)
                        {
                            returnval = item;
                            break;
                        }
                        else
                        {
                            //Do nothing
                        }
                    }

                    return returnval;
                }

                public static void UpdateJournal(string journamName, DecryptedJournal updatedJournal)
                {
                    var journalItem = GetJournal(journamName);

                    journalItem.JournalName = updatedJournal.JournalName;
                    journalItem.Description = updatedJournal.Description;
                    journalItem.CoverImagePath = updatedJournal.CoverImagePath;

                    // Assuming the updated categories are provided in the updatedJournal parameter
                    journalItem.Categories = updatedJournal.Categories;



                    // Save the updated journal
                    var journaldb = (List<Journal>)UniversalSave.Load(JournalPath, DataFormat.JSON).Get("Journals");

                    var journalslist = GetAllJournals();

                    foreach (DecryptedJournal item in journalslist)
                    {
                        if (item.JournalName == journamName)
                        {
                            journalslist.Remove(item);
                            journalslist.Add(journalItem);
                        }
                        else
                        {
                            //Do nothing
                        }
                    }

                    List<Journal> finallist = new List<Journal>();

                    foreach (DecryptedJournal item in journalslist)
                    {
                        finallist.Add(new Journal(item, UserPassword));
                    }

                    var savedata = UniversalSave.Load(JournalPath, DataFormat.JSON);

                    savedata.Set("Journals", finallist);

                    UniversalSave.Save(JournalPath, savedata);

                }

                //Add and delete journal funtion + Find note (note title) or get note (Formatted string) function


                public static DecryptedNote GetNote(string Journalname, string Notename, string Category)
                {
                    var listnotes = GetCategory(Journalname, Category);
                    DecryptedNote returnval = default;
                    foreach (DecryptedNote item in listnotes.Notes)
                    {
                        if (item.Title == Notename)
                        {
                            returnval = item;
                        }

                    }

                    return returnval;
                }


                public static DecryptedNote GetNoteFromPack(string context)
                {
                    string[] parts = context.Split('|');

                    // Assign the split strings to the out parameters
                    var str1 = parts.Length > 0 ? parts[0] : string.Empty;
                    var str2 = parts.Length > 1 ? parts[1] : string.Empty;
                    var str3 = parts.Length > 2 ? parts[2] : string.Empty;

                    var journalloaded = GetJournal(str1);
                    var finalnote = GetNote(str1, str2, str3);

                    return finalnote;


                }

                public static string PackNote(string journalName, string NoteTile, string Category)
                {
                    string formattedString = string.Format(journalName, NoteTile, Category);
                    return formattedString;
                }

            }

        }



        public static class Media
        {

            #region structs

            public struct VideoInfo
            {
                public AESEncryptedText lasttime;
                public AESEncryptedText usernote;
                public AESEncryptedText videopath;


                public VideoInfo(DecryptedVideoInfo item, string userpass)
                {
                    this.lasttime = Encrypt((item.lasttime.ToString()), userpass);
                    this.usernote = Encrypt(item.usernote, userpass);
                    this.videopath = Encrypt(item.videopath, userpass);
                }
            }

            public struct DecryptedVideoInfo
            {
                public int lasttime;
                public string usernote;
                public string videopath;


                public DecryptedVideoInfo(int time, string usernote, string videopath)
                {
                    this.lasttime = time;
                    this.usernote = usernote;
                    this.videopath = videopath;
                }
            }

            public static DecryptedVideoInfo DecryptVideoInfo(VideoInfo videoinfo)
            {
                var lasttime = Decrypt((videoinfo.lasttime), UserPassword);
                var usernote = Decrypt(videoinfo.usernote, UserPassword);
                var lasttimeint = int.Parse(lasttime);
                var videopath = Decrypt((videoinfo.videopath), UserPassword);
                var vidinfo = new DecryptedVideoInfo(lasttimeint, usernote, videopath);

                return vidinfo;
            }



            #endregion




            public static class VideoInfos
            {
                public static string VideoInfoData = "caca";
                public static DecryptedVideoInfo GetCertainVideoInfo(string videopath)
                {
                    //First let's see if the object exists in the list

                    //Get the list of video path data
                    var FileWithVideoInfo = UniversalSave.Load(VideoInfoData, DataFormat.JSON);

                    //Get the defaultformatters dictionary ajnd default app opener list
                    List<VideoInfo> RecentlyRecordedPaths = (List<VideoInfo>)FileWithVideoInfo.Get("VideoInfo"); //The app we choose to open a file with but encrypted

                    //Now decrypt

                    DecryptedVideoInfo chosen = default;

                    foreach (VideoInfo item in RecentlyRecordedPaths)
                    {
                        var pathtocheck = Decrypt(item.videopath, UserPassword);

                        if (pathtocheck == videopath)
                        {
                            chosen = DecryptVideoInfo(item);
                        }
                    }

                    return chosen;
                }

                public static List<DecryptedVideoInfo> GetAllVideoInfo()
                {
                    //First let's see if the object exists in the list

                    //Get the list of video path data
                    var FileWithVideoInfo = UniversalSave.Load(VideoInfoData, DataFormat.JSON);

                    //Get the defaultformatters dictionary ajnd default app opener list
                    List<VideoInfo> RecentlyRecordedPaths = (List<VideoInfo>)FileWithVideoInfo.Get("VideoInfo"); //The app we choose to open a file with but encrypted

                    //Now decrypt

                    List<DecryptedVideoInfo> listofvideoinfo = default;

                    foreach (VideoInfo item in RecentlyRecordedPaths)
                    {
                        listofvideoinfo.Add(DecryptVideoInfo(item));
                    }

                    return listofvideoinfo;
                }

                public static void UpdateVideoInfo(DecryptedVideoInfo updatedvideo, string videopath)
                {
                    //First let's see if the object exists in the list

                    //Get the list of video path data
                    var FileWithVideoInfo = UniversalSave.Load(VideoInfoData, DataFormat.JSON);

                    //Get the defaultformatters dictionary ajnd default app opener list
                    List<VideoInfo> RecentlyRecordedPaths = (List<VideoInfo>)FileWithVideoInfo.Get("VideoInfo"); //The app we choose to open a file with but encrypted


                    foreach (VideoInfo item in RecentlyRecordedPaths)
                    {
                        var videopathtemp = DecryptVideoInfo(item);
                        if (videopathtemp.videopath == updatedvideo.videopath)
                        {
                            var newvidinfo = new VideoInfo(updatedvideo, UserPassword);
                            var pos = RecentlyRecordedPaths.IndexOf(item);
                            RecentlyRecordedPaths.RemoveAt(pos);
                            RecentlyRecordedPaths.Insert(pos, newvidinfo);
                            FileWithVideoInfo.Set("VideoInfo", RecentlyRecordedPaths);
                            UniversalSave.Save(VideoInfoData, FileWithVideoInfo);
                            break;
                        }
                    }

                }

                public static void DeleteVideoInfo(int pos)
                {
                    //First let's see if the object exists in the list

                    //Get the list of video path data
                    var FileWithVideoInfo = UniversalSave.Load(VideoInfoData, DataFormat.JSON);

                    //Get the defaultformatters dictionary ajnd default app opener list
                    List<VideoInfo> RecentlyRecordedPaths = (List<VideoInfo>)FileWithVideoInfo.Get("VideoInfo"); //The app we choose to open a file with but encrypted

                    RecentlyRecordedPaths.RemoveAt(pos);
                    FileWithVideoInfo.Set("VideoInfo", RecentlyRecordedPaths);
                    UniversalSave.Save(VideoInfoData, FileWithVideoInfo);

                }

                public static void AddVideoInfo(DecryptedVideoInfo updatedvideo, string videopath)
                {
                    //First let's see if the object exists in the list

                    //Get the list of video path data
                    var FileWithVideoInfo = UniversalSave.Load(VideoInfoData, DataFormat.JSON);

                    //Get the defaultformatters dictionary ajnd default app opener list
                    List<VideoInfo> RecentlyRecordedPaths = (List<VideoInfo>)FileWithVideoInfo.Get("VideoInfo"); //The app we choose to open a file with but encrypted



                    var newvidinfo = new VideoInfo(updatedvideo, UserPassword);
                    RecentlyRecordedPaths.Add(newvidinfo);
                    FileWithVideoInfo.Set("VideoInfo", RecentlyRecordedPaths);
                    UniversalSave.Save(VideoInfoData, FileWithVideoInfo);


                }
            }
            public static class Image
            {
                public static void GetImage(string imageUrl, Action<Texture2D> onImageReceived)
                {
                    BestHTTP.HTTPManager.SendRequest(imageUrl, (request, response) => OnImageDownloaded(request, response, onImageReceived));
                }

                private static void OnImageDownloaded(BestHTTP.HTTPRequest request, BestHTTP.HTTPResponse response, Action<Texture2D> onImageReceived)
                {
                    Texture2D returnTexture = null;

                    if (response != null)
                    {
                        Debug.Log("Download finished!");

                        // Set the texture to the newly downloaded one
                        returnTexture = response.DataAsTexture2D;
                    }
                    else
                    {
                        Debug.LogError("No response received: " + (request.Exception != null ? (request.Exception.Message + "/n" + request.Exception.StackTrace) : "No Exception"));
                    }

                    // Invoke the callback with the downloaded texture
                    onImageReceived?.Invoke(returnTexture);
                }

            }
            public static class RecentlyRecorded
            {

                static string RecentlyRecordedPath = "caca"; //Contains a list of recently recorded item paths

                public static List<string> GetRecentlyRecorded()
                {
                    //First let's see if the object exists in the list

                    //Get what we should open this with and the default app opener list
                    var FileWithRecentlyRecordedPaths = UniversalSave.Load(RecentlyRecordedPath, DataFormat.JSON);

                    //Get the defaultformatters dictionary ajnd default app opener list
                    List<AESEncryptedText> RecentlyRecordedPaths = (List<AESEncryptedText>)FileWithRecentlyRecordedPaths.Get("RecentlyRecorded"); //The app we choose to open a file with but encrypted

                    List<string> recentlyrecordedpaths = default;

                    foreach (AESEncryptedText item in RecentlyRecordedPaths)
                    {
                        recentlyrecordedpaths.Add(Decrypt(item, UserPassword));
                    }

                    return recentlyrecordedpaths;
                }



                public static string AddToRecentlyRecorded(string filepath)
                {
                    //First let's see if the object exists in the list

                    //Get what we should open this with and the default app opener list
                    var FileWithRecentlyRecordedPaths = UniversalSave.Load(RecentlyRecordedPath, DataFormat.JSON);

                    //Get the defaultformatters dictionary ajnd default app opener list
                    List<AESEncryptedText> RecentlyRecordedPaths = (List<AESEncryptedText>)FileWithRecentlyRecordedPaths.Get("RecentlyRecorded"); //The app we choose to open a file with but encrypted

                    //Later on i'll add something so users can customize how manyy items they want on their list, for now it's 20 items
                    //Let's add our item to the list

                    RecentlyRecordedPaths.Add(Encrypt(filepath, UserPassword));

                    //Now to ensure the list is no more than 20 items
                    if (RecentlyRecordedPaths.Count > 20)
                    {
                        RecentlyRecordedPaths.RemoveAt(21);
                    }

                    //And finally we can push it to the file as the new settings
                    FileWithRecentlyRecordedPaths.Set("RecentlyRecorded", RecentlyRecordedPaths);
                    UniversalSave.Save(RecentlyRecordedPath, FileWithRecentlyRecordedPaths);


                    return "complete";

                    //I'll add some default error returns to everything later
                }


            }
            public static class ImageInfo
            {
                public static string imageInfoData = "caca";

                public struct imageInfo
                {
                    public AESEncryptedText lasttime;
                    public AESEncryptedText usernote;
                    public AESEncryptedText imagepath;


                    public imageInfo(DecryptedimageInfo item, string userpass)
                    {
                        this.lasttime = Encrypt((item.lasttime.ToString()), userpass);
                        this.usernote = Encrypt(item.usernote, userpass);
                        this.imagepath = Encrypt(item.imagepath, userpass);
                    }
                }


                public struct DecryptedimageInfo
                {
                    public int lasttime;
                    public string usernote;
                    public string imagepath;


                    public DecryptedimageInfo(int time, string usernote, string imagepath)
                    {
                        this.lasttime = time;
                        this.usernote = usernote;
                        this.imagepath = imagepath;
                    }
                }

                public static DecryptedimageInfo DecryptimageInfo(imageInfo imageinfo)
                {
                    var lasttime = Decrypt((imageinfo.lasttime), UserPassword);
                    var usernote = Decrypt(imageinfo.usernote, UserPassword);
                    var lasttimeint = int.Parse(lasttime);
                    var imagepath = Decrypt((imageinfo.imagepath), UserPassword);
                    var vidinfo = new DecryptedimageInfo(lasttimeint, usernote, imagepath);

                    return vidinfo;
                }


                public static DecryptedimageInfo GetCertainimageInfo(string imagepath)
                {
                    //First let's see if the object exists in the list

                    //Get the list of image path data
                    var FileWithimageInfo = UniversalSave.Load(imageInfoData, DataFormat.JSON);

                    //Get the defaultformatters dictionary ajnd default app opener list
                    List<imageInfo> RecentlyRecordedPaths = (List<imageInfo>)FileWithimageInfo.Get("imageInfo"); //The app we choose to open a file with but encrypted

                    //Now decrypt

                    DecryptedimageInfo chosen = default;

                    foreach (imageInfo item in RecentlyRecordedPaths)
                    {
                        var pathtocheck = Decrypt(item.imagepath, UserPassword);

                        if (pathtocheck == imagepath)
                        {
                            chosen = DecryptimageInfo(item);
                        }
                    }

                    return chosen;
                }

                public static List<DecryptedimageInfo> GetAllimageInfo()
                {
                    //First let's see if the object exists in the list

                    //Get the list of image path data
                    var FileWithimageInfo = UniversalSave.Load(imageInfoData, DataFormat.JSON);

                    //Get the defaultformatters dictionary ajnd default app opener list
                    List<imageInfo> RecentlyRecordedPaths = (List<imageInfo>)FileWithimageInfo.Get("imageInfo"); //The app we choose to open a file with but encrypted

                    //Now decrypt

                    List<DecryptedimageInfo> listofimageinfo = default;

                    foreach (imageInfo item in RecentlyRecordedPaths)
                    {
                        listofimageinfo.Add(DecryptimageInfo(item));
                    }

                    return listofimageinfo;
                }

                public static void UpdateimageInfo(DecryptedimageInfo updatedimage, string imagepath)
                {
                    //First let's see if the object exists in the list

                    //Get the list of image path data
                    var FileWithimageInfo = UniversalSave.Load(imageInfoData, DataFormat.JSON);

                    //Get the defaultformatters dictionary ajnd default app opener list
                    List<imageInfo> RecentlyRecordedPaths = (List<imageInfo>)FileWithimageInfo.Get("imageInfo"); //The app we choose to open a file with but encrypted


                    foreach (imageInfo item in RecentlyRecordedPaths)
                    {
                        var imagepathtemp = DecryptimageInfo(item);
                        if (imagepathtemp.imagepath == updatedimage.imagepath)
                        {
                            var newvidinfo = new imageInfo(updatedimage, UserPassword);
                            var pos = RecentlyRecordedPaths.IndexOf(item);
                            RecentlyRecordedPaths.RemoveAt(pos);
                            RecentlyRecordedPaths.Insert(pos, newvidinfo);
                            FileWithimageInfo.Set("imageInfo", RecentlyRecordedPaths);
                            UniversalSave.Save(imageInfoData, FileWithimageInfo);
                            break;
                        }
                    }

                }

                public static void DeleteimageInfo(int pos)
                {
                    //First let's see if the object exists in the list

                    //Get the list of image path data
                    var FileWithimageInfo = UniversalSave.Load(imageInfoData, DataFormat.JSON);

                    //Get the defaultformatters dictionary ajnd default app opener list
                    List<imageInfo> RecentlyRecordedPaths = (List<imageInfo>)FileWithimageInfo.Get("imageInfo"); //The app we choose to open a file with but encrypted

                    RecentlyRecordedPaths.RemoveAt(pos);
                    FileWithimageInfo.Set("imageInfo", RecentlyRecordedPaths);
                    UniversalSave.Save(imageInfoData, FileWithimageInfo);

                }

                public static void AddimageInfo(DecryptedimageInfo updatedimage, string imagepath)
                {
                    //First let's see if the object exists in the list

                    //Get the list of image path data
                    var FileWithimageInfo = UniversalSave.Load(imageInfoData, DataFormat.JSON);

                    //Get the defaultformatters dictionary ajnd default app opener list
                    List<imageInfo> RecentlyRecordedPaths = (List<imageInfo>)FileWithimageInfo.Get("imageInfo"); //The app we choose to open a file with but encrypted



                    var newvidinfo = new imageInfo(updatedimage, UserPassword);
                    RecentlyRecordedPaths.Add(newvidinfo);
                    FileWithimageInfo.Set("imageInfo", RecentlyRecordedPaths);
                    UniversalSave.Save(imageInfoData, FileWithimageInfo);


                }




            }

            public static class MediaPlayer
            {


                public static string MediaLibraryPath;



                public struct MediaDirectory
                {
                    public List<AESEncryptedText> MediaDirectoryName; //The name of the media directory
                    public List<AESEncryptedText> MediaDirectoryPath; //The path (As in the folder, we check every supported extension in here)
                    public AESEncryptedText PublicMediaFolderPath; //The public folder path
                    public AESEncryptedText UsersDefaultMediaPath; //Where files go by default

                    public List<AESEncryptedText> AlbumFiles; //String of AlbumMusic Files (Their paths)
                    public AESEncryptedText PublicAlbumsFolder; //The Public AlbumMusic Folder
                    public AESEncryptedText UsersDefaultAlbumFolder; //The Default AlbumMusic (Used to get where AlbumFiles should be placed by removing the last directory

                    public List<AESEncryptedText> FavoritesMedia; //List of favorite files (Their direct paths)
                    public List<AESEncryptedText> FavoritesMediaPlaylist; //List of favorite playlists (Their direct paths)

                    public List<AESEncryptedText> SupportedExtensions; //Supported extensions, can be increased by adding a scriptable object, FileRenderer takes care of this, although only XRUIOS side stuff will be considered


                    public MediaDirectory(DecryptedMediaDirectory mediaDirectory, string UserPassword)
                    {

                        this.MediaDirectoryName = new List<AESEncryptedText>();
                        foreach (string name in mediaDirectory.MediaDirectoryName)
                        {
                            MediaDirectoryName.Add(Encrypt(name, UserPassword));
                        }

                        this.MediaDirectoryPath = new List<AESEncryptedText>();
                        foreach (string name in mediaDirectory.MediaDirectoryPath)
                        {
                            MediaDirectoryPath.Add(Encrypt(name, UserPassword));
                        }

                        this.PublicMediaFolderPath = (Encrypt(mediaDirectory.PublicMediaFolderPath, UserPassword));
                        this.UsersDefaultMediaPath = (Encrypt(mediaDirectory.UsersDefaultMediaPath, UserPassword));

                        this.AlbumFiles = new List<AESEncryptedText>();
                        foreach (string item in mediaDirectory.AlbumFiles)
                        {
                            AlbumFiles.Add(Encrypt(item, UserPassword));
                        }

                        this.PublicAlbumsFolder = (Encrypt(mediaDirectory.PublicAlbumsFolder, UserPassword));
                        this.UsersDefaultAlbumFolder = (Encrypt(mediaDirectory.UsersDefaultAlbumFolder, UserPassword));

                        this.FavoritesMedia = new List<AESEncryptedText>();
                        foreach (string item in mediaDirectory.FavoritesMedia)
                        {
                            FavoritesMedia.Add(Encrypt(item, UserPassword));
                        }

                        this.FavoritesMediaPlaylist = new List<AESEncryptedText>();
                        foreach (string item in mediaDirectory.FavoritesMediaPlaylist)
                        {
                            FavoritesMediaPlaylist.Add(Encrypt(item, UserPassword));
                        }
                        this.SupportedExtensions = new List<AESEncryptedText>();
                        foreach (string item in mediaDirectory.SupportedExtensions)
                        {
                            SupportedExtensions.Add(Encrypt(item, UserPassword));
                        }
                    }
                }

                public struct DecryptedMediaDirectory
                {
                    public List<string> MediaDirectoryName;
                    public List<string> MediaDirectoryPath;
                    public string PublicMediaFolderPath;
                    public string UsersDefaultMediaPath;

                    public List<string> AlbumFiles;
                    public string PublicAlbumsFolder;
                    public string UsersDefaultAlbumFolder;

                    public List<string> FavoritesMedia;
                    public List<string> FavoritesMediaPlaylist;

                    public List<string> SupportedExtensions;


                    public DecryptedMediaDirectory

                        (

                        List<string> tempMediaDirectoryName,
                        List<string> tempMusicDirectoryPath,
                        string tempPublicMediaFolderPath,
                        string tempUsersDefaultMediaPath,

                        List<string> tempAlbumFiles,
                        string tempPublicAlbumsFolder,
                        string tempUsersDefaultAlbumFolder,

                        List<string> tempFavoritesMedia,
                        List<string> tempFavoritesMediaPlaylist,

                        List<string> tempSupportedExtensions

                        )

                    {
                        this.MediaDirectoryName = tempMediaDirectoryName;
                        this.MediaDirectoryPath = tempMusicDirectoryPath;
                        this.PublicMediaFolderPath = tempPublicMediaFolderPath;
                        this.UsersDefaultMediaPath = tempUsersDefaultMediaPath;

                        this.AlbumFiles = tempAlbumFiles;
                        this.PublicAlbumsFolder = tempPublicAlbumsFolder;
                        this.UsersDefaultAlbumFolder = tempUsersDefaultAlbumFolder;

                        this.FavoritesMedia = tempFavoritesMedia;
                        this.FavoritesMediaPlaylist = tempFavoritesMediaPlaylist;

                        this.SupportedExtensions = tempSupportedExtensions;

                    }
                }

                public static DecryptedMediaDirectory DecryptMediaDirectory(MediaDirectory mediaDirectory)
                {
                    var MediaDirectoryName = new List<string>();
                    foreach (AESEncryptedText name in mediaDirectory.MediaDirectoryName)
                    {
                        MediaDirectoryName.Add(Decrypt(name, UserPassword));
                    }

                    var MediaDirectoryPath = new List<string>();
                    foreach (AESEncryptedText name in mediaDirectory.MediaDirectoryPath)
                    {
                        MediaDirectoryPath.Add(Decrypt(name, UserPassword));
                    }

                    var PublicMediaFolderPath = (Decrypt(mediaDirectory.PublicMediaFolderPath, UserPassword));
                    var UsersDefaultMediaPath = (Decrypt(mediaDirectory.UsersDefaultMediaPath, UserPassword));

                    var AlbumFiles = new List<string>();
                    foreach (AESEncryptedText item in mediaDirectory.AlbumFiles)
                    {
                        AlbumFiles.Add(Decrypt(item, UserPassword));
                    }

                    var PublicAlbumsFolder = (Decrypt(mediaDirectory.PublicAlbumsFolder, UserPassword));
                    var UsersDefaultAlbumFolder = (Decrypt(mediaDirectory.UsersDefaultAlbumFolder, UserPassword));

                    var FavoritesMedia = new List<string>();
                    foreach (AESEncryptedText item in mediaDirectory.FavoritesMedia)
                    {
                        FavoritesMedia.Add(Decrypt(item, UserPassword));
                    }

                    var FavoritesMediaPlaylist = new List<string>();
                    foreach (AESEncryptedText item in mediaDirectory.FavoritesMediaPlaylist)
                    {
                        FavoritesMediaPlaylist.Add(Decrypt(item, UserPassword));
                    }
                    var SupportedExtensions = new List<string>();
                    foreach (AESEncryptedText item in mediaDirectory.SupportedExtensions)
                    {
                        SupportedExtensions.Add(Decrypt(item, UserPassword));
                    }

                    return new DecryptedMediaDirectory(MediaDirectoryName,
                    MediaDirectoryPath,
                    PublicMediaFolderPath,
                    UsersDefaultMediaPath,
                    AlbumFiles,
                    PublicAlbumsFolder,
                    UsersDefaultAlbumFolder,
                    FavoritesMedia,
                    FavoritesMediaPlaylist,
                    SupportedExtensions);

                }




                public struct AlbumMedia
                {
                    public AESEncryptedText AlbumName;
                    public AESEncryptedText AlbumDescription;
                    public XRUIOS.Security.EncryptedBoolData IsFavorite;
                    public Color UIColor;
                    public Color UIColorAlt;
                    public AESEncryptedText CoverImageFilePath;
                    public List<AESEncryptedText> Media;

                    public AlbumMedia(DecryptedAlbumMedia decrypted, string UserPassword)
                    {
                        this.AlbumName = Encrypt(decrypted.AlbumName, UserPassword);
                        this.AlbumDescription = Encrypt(decrypted.AlbumDescription, UserPassword);
                        this.IsFavorite = XRUIOS.Security.EncryptBool(decrypted.IsFavorite, UserPassword);
                        this.UIColor = decrypted.UIColor;
                        this.UIColorAlt = decrypted.UIColorAlt;
                        this.CoverImageFilePath = Encrypt(decrypted.CoverImageFilePath, UserPassword);

                        // Encrypt the list of songs
                        this.Media = new List<AESEncryptedText>();
                        foreach (var media in decrypted.Media)
                        {
                            this.Media.Add(Encrypt(media, UserPassword));
                        }
                    }
                }

                public struct DecryptedAlbumMedia
                {
                    public string AlbumName;
                    public string AlbumDescription;
                    public bool IsFavorite;
                    public Color UIColor;
                    public Color UIColorAlt;
                    public string CoverImageFilePath;
                    public List<string> Media;

                    public DecryptedAlbumMedia(string AlbumName, string AlbumDescription, bool IsFavorite, Color UIColor, Color UIColorAlt, string CoverImageFilePath, List<string> Media)
                    {
                        this.AlbumName = AlbumName;
                        this.AlbumDescription = AlbumDescription;
                        this.IsFavorite = IsFavorite;
                        this.UIColor = UIColor;
                        this.UIColorAlt = UIColorAlt;
                        this.CoverImageFilePath = CoverImageFilePath;
                        this.Media = Media;
                    }
                }


                public static DecryptedMediaDirectory GetMediaDirectory()
                {
                    return default(DecryptedMediaDirectory);
                }


            }

        }



        public static class ImageInfo
        {
            public static string imageInfoData = "caca";

            public struct imageInfo
            {
                public AESEncryptedText lasttime;
                public AESEncryptedText usernote;
                public AESEncryptedText imagepath;


                public imageInfo(DecryptedimageInfo item, string userpass)
                {
                    this.lasttime = Encrypt((item.lasttime.ToString()), userpass);
                    this.usernote = Encrypt(item.usernote, userpass);
                    this.imagepath = Encrypt(item.imagepath, userpass);
                }
            }


            public struct DecryptedimageInfo
            {
                public int lasttime;
                public string usernote;
                public string imagepath;


                public DecryptedimageInfo(int time, string usernote, string imagepath)
                {
                    this.lasttime = time;
                    this.usernote = usernote;
                    this.imagepath = imagepath;
                }
            }

            public static DecryptedimageInfo DecryptimageInfo(imageInfo imageinfo)
            {
                var lasttime = Decrypt((imageinfo.lasttime), UserPassword);
                var usernote = Decrypt(imageinfo.usernote, UserPassword);
                var lasttimeint = int.Parse(lasttime);
                var imagepath = Decrypt((imageinfo.imagepath), UserPassword);
                var vidinfo = new DecryptedimageInfo(lasttimeint, usernote, imagepath);

                return vidinfo;
            }


            public static DecryptedimageInfo GetCertainimageInfo(string imagepath)
            {
                //First let's see if the object exists in the list

                //Get the list of image path data
                var FileWithimageInfo = UniversalSave.Load(imageInfoData, DataFormat.JSON);

                //Get the defaultformatters dictionary ajnd default app opener list
                List<imageInfo> RecentlyRecordedPaths = (List<imageInfo>)FileWithimageInfo.Get("imageInfo"); //The app we choose to open a file with but encrypted

                //Now decrypt

                DecryptedimageInfo chosen = default;

                foreach (imageInfo item in RecentlyRecordedPaths)
                {
                    var pathtocheck = Decrypt(item.imagepath, UserPassword);

                    if (pathtocheck == imagepath)
                    {
                        chosen = DecryptimageInfo(item);
                    }
                }

                return chosen;
            }

            public static List<DecryptedimageInfo> GetAllimageInfo()
            {
                //First let's see if the object exists in the list

                //Get the list of image path data
                var FileWithimageInfo = UniversalSave.Load(imageInfoData, DataFormat.JSON);

                //Get the defaultformatters dictionary ajnd default app opener list
                List<imageInfo> RecentlyRecordedPaths = (List<imageInfo>)FileWithimageInfo.Get("imageInfo"); //The app we choose to open a file with but encrypted

                //Now decrypt

                List<DecryptedimageInfo> listofimageinfo = default;

                foreach (imageInfo item in RecentlyRecordedPaths)
                {
                    listofimageinfo.Add(DecryptimageInfo(item));
                }

                return listofimageinfo;
            }

            public static void UpdateimageInfo(DecryptedimageInfo updatedimage, string imagepath)
            {
                //First let's see if the object exists in the list

                //Get the list of image path data
                var FileWithimageInfo = UniversalSave.Load(imageInfoData, DataFormat.JSON);

                //Get the defaultformatters dictionary ajnd default app opener list
                List<imageInfo> RecentlyRecordedPaths = (List<imageInfo>)FileWithimageInfo.Get("imageInfo"); //The app we choose to open a file with but encrypted


                foreach (imageInfo item in RecentlyRecordedPaths)
                {
                    var imagepathtemp = DecryptimageInfo(item);
                    if (imagepathtemp.imagepath == updatedimage.imagepath)
                    {
                        var newvidinfo = new imageInfo(updatedimage, UserPassword);
                        var pos = RecentlyRecordedPaths.IndexOf(item);
                        RecentlyRecordedPaths.RemoveAt(pos);
                        RecentlyRecordedPaths.Insert(pos, newvidinfo);
                        FileWithimageInfo.Set("imageInfo", RecentlyRecordedPaths);
                        UniversalSave.Save(imageInfoData, FileWithimageInfo);
                        break;
                    }
                }

            }

            public static void DeleteimageInfo(int pos)
            {
                //First let's see if the object exists in the list

                //Get the list of image path data
                var FileWithimageInfo = UniversalSave.Load(imageInfoData, DataFormat.JSON);

                //Get the defaultformatters dictionary ajnd default app opener list
                List<imageInfo> RecentlyRecordedPaths = (List<imageInfo>)FileWithimageInfo.Get("imageInfo"); //The app we choose to open a file with but encrypted

                RecentlyRecordedPaths.RemoveAt(pos);
                FileWithimageInfo.Set("imageInfo", RecentlyRecordedPaths);
                UniversalSave.Save(imageInfoData, FileWithimageInfo);

            }

            public static void AddimageInfo(DecryptedimageInfo updatedimage, string imagepath)
            {
                //First let's see if the object exists in the list

                //Get the list of image path data
                var FileWithimageInfo = UniversalSave.Load(imageInfoData, DataFormat.JSON);

                //Get the defaultformatters dictionary ajnd default app opener list
                List<imageInfo> RecentlyRecordedPaths = (List<imageInfo>)FileWithimageInfo.Get("imageInfo"); //The app we choose to open a file with but encrypted



                var newvidinfo = new imageInfo(updatedimage, UserPassword);
                RecentlyRecordedPaths.Add(newvidinfo);
                FileWithimageInfo.Set("imageInfo", RecentlyRecordedPaths);
                UniversalSave.Save(imageInfoData, FileWithimageInfo);


            }




        }



        public static class Image
        {
            public static void GetImage(string imageUrl, Action<Texture2D> onImageReceived)
            {
                BestHTTP.HTTPManager.SendRequest(imageUrl, (request, response) => OnImageDownloaded(request, response, onImageReceived));
            }

            private static void OnImageDownloaded(BestHTTP.HTTPRequest request, BestHTTP.HTTPResponse response, Action<Texture2D> onImageReceived)
            {
                Texture2D returnTexture = null;

                if (response != null)
                {
                    Debug.Log("Download finished!");

                    // Set the texture to the newly downloaded one
                    returnTexture = response.DataAsTexture2D;
                }
                else
                {
                    Debug.LogError("No response received: " + (request.Exception != null ? (request.Exception.Message + "/n" + request.Exception.StackTrace) : "No Exception"));
                }

                // Invoke the callback with the downloaded texture
                onImageReceived?.Invoke(returnTexture);
            }

        }





        public static class DesktopMirrors
        {

            public class Monitors //For uDesktopDuplication, specifically 
            {

                public struct Monitor
                {
                    public AESEncryptedText MonitorNumber;

                    public Security.EncryptedBoolData InvertedX;

                    public Security.EncryptedBoolData InvertedY;

                    public Monitor(DecryptedMonitor item)
                    {
                        this.MonitorNumber = Encrypt(item.MonitorNumber.ToString(), UserPassword);
                        this.InvertedX = Security.EncryptBool(false, UserPassword);
                        this.InvertedY = Security.EncryptBool(false, UserPassword);
                    }

                }



                public struct DecryptedMonitor
                {

                    public int MonitorNumber;

                    public bool InvertedX;

                    public bool InvertedY;

                    public DecryptedMonitor(int monitornumber, bool invertedX, bool invertedY)
                    {
                        this.MonitorNumber = monitornumber;
                        this.InvertedX = invertedX;
                        this.InvertedY = invertedY;
                    }

                }

                public static DecryptedMonitor DecryptMonitor(Monitor item, string UserPassword)
                {
                    var MonitorNumber = int.Parse(Decrypt(item.MonitorNumber, UserPassword));
                    var InvertedX = Security.DecryptBool(item.InvertedX, UserPassword);
                    var InvertedY = Security.DecryptBool(item.InvertedY, UserPassword);

                    var finalreturn = new DecryptedMonitor(MonitorNumber, InvertedX, InvertedY);

                    return finalreturn;
                }



            }

            public class Apps //For uWindowsCapture, specifically Uwc Window Texture.cs
            {

                public enum RenderingMode { OnlyWhenVisible, AllFrames };


                public struct App
                {

                    public AESEncryptedText AppPath;

                    public Security.CultistKey CaptureAPI;

                    public Security.CultistKey Priority;

                    public AESEncryptedText CaptureFPS;

                    public Security.CultistKey RenderingModeType;

                    public AESEncryptedText PartialAppTitle;

                    public App(DecryptedApp item)
                    {
                        this.AppPath = Encrypt(item.AppPath, UserPassword);
                        this.CaptureAPI = new Security.CultistKey(item.CaptureAPI, UserPassword);
                        this.Priority = new Security.CultistKey(item.Priority, UserPassword);
                        this.CaptureFPS = Encrypt(item.CaptureFPS.ToString(), UserPassword);
                        this.RenderingModeType = new Security.CultistKey(item.RenderingModeType, UserPassword);
                        this.PartialAppTitle = Encrypt(item.PartialAppTitle, UserPassword);
                    }


                }

                public struct DecryptedApp
                {

                    public string AppPath;

                    public uWindowCapture.CaptureMode CaptureAPI;

                    public uWindowCapture.CapturePriority Priority;

                    public int CaptureFPS;

                    //We will draw the cursor whenever the user's raycast is on it

                    public RenderingMode RenderingModeType;

                    //Create Child Windows is always open

                    //We always do searches when the parameter changes

                    public string PartialAppTitle;

                    public DecryptedApp(string appPath, uWindowCapture.CaptureMode captureAPI, uWindowCapture.CapturePriority priority, int captureFPS, RenderingMode renderingModeType, string partialAppTitle)
                    {
                        this.AppPath = appPath;
                        this.CaptureAPI = captureAPI;
                        this.Priority = priority;
                        this.CaptureFPS = captureFPS;
                        this.RenderingModeType = renderingModeType;
                        this.PartialAppTitle = partialAppTitle;
                    }



                }


                public static DecryptedApp DecryptApp(App item, string UserPassword)
                {
                    var AppPath = Decrypt(item.AppPath, UserPassword);
                    var CaptureAPI = (uWindowCapture.CaptureMode)Security.DecryptCultistKey(item.CaptureAPI, UserPassword).Item;
                    var Priority = (uWindowCapture.CapturePriority)Security.DecryptCultistKey(item.Priority, UserPassword).Item;
                    var CaptureFPS = int.Parse(Decrypt(item.CaptureFPS, UserPassword));
                    var RenderingModeType = (RenderingMode)Security.DecryptCultistKey(item.RenderingModeType, UserPassword).Item;
                    var PartialAppTitle = Decrypt(item.PartialAppTitle, UserPassword);

                    var finalreturn = new DecryptedApp(AppPath, CaptureAPI, Priority, CaptureFPS, RenderingModeType, PartialAppTitle);

                    return finalreturn;
                }


            }


        }




        public class Location
        {

            private float latitude;
            private float longitude;

            public void GetExactCoordinates()
            {
                if (Input.location.status == LocationServiceStatus.Running)
                {
                    LocationInfo location = Input.location.lastData;

                    // Check if location data is valid
                    if (location.latitude != 0 && location.longitude != 0)
                    {
                        LocationPoint locationPoint = new LocationPoint
                        {
                            Timestamp = DateTime.Now,
                            Latitude = location.latitude,
                            Longitude = location.longitude
                        };

                        // Add location point to history or do something with it
                        locationhistory.Add(locationPoint);
                    }
                    else
                    {
                        Debug.LogWarning("Unable to determine location.");
                    }
                }
                else
                {
                    Debug.LogWarning("Location services not running.");
                }
            }

            private List<LocationPoint> locationhistory;

            public struct LocationPoint
            {
                public DateTime Timestamp;
                public double Latitude;
                public double Longitude;

                public LocationPoint(DateTime timestamp, double latitude, double longitude)
                {
                    Timestamp = timestamp;
                    Latitude = latitude;
                    Longitude = longitude;
                }
            }


            public RelativePoint currentrelativelocation;

            public struct RelativePoint
            {
                public float latmin;
                public float latmax;
                public float longmin;
                public float longmax;

                public RelativePoint(float latmin, float latmax, float longmin, float longmax)
                {
                    this.latmin = latmin;
                    this.latmax = latmax;
                    this.longmin = longmin;
                    this.longmax = longmax;
                }
            }


            private List<RelativePoint> relativelocationhistory;


            private void GetRelativeCoordinates()
            {
                try
                {

                    RelativePoint relativePoint = new RelativePoint();

                    for (int i = 0; i < 5; i++)
                    {
                        relativePoint.latmin += UnityEngine.Random.Range(-0.03f, 0.03f) + latitude;
                    }

                    relativePoint.latmax = UnityEngine.Random.Range(latitude - 0.15f, latitude + 0.15f);

                    for (int i = 0; i < 5; i++)
                    {
                        relativePoint.longmin += UnityEngine.Random.Range(-0.03f, 0.03f) + longitude;
                    }

                    relativePoint.longmax = UnityEngine.Random.Range(longitude - 0.15f, longitude + 0.15f);

                    relativelocationhistory.Add(relativePoint);
                    currentrelativelocation = relativePoint;
                }
                catch (Exception ex)
                {
                    Debug.LogError("Error: " + ex.Message);
                }
            }



        }



        public static class FiletypeViewer
        {
            //This takes care of two types; seeing what an app can open and how it opens it



            public static string ViewerDataPath = "caca"; //Holds FileWithFiletypeRenderer





            //Opens app with the default
            public static string OpenFile(string filepath)
            {

                //First let's get extension type
                var extension = Path.GetExtension(filepath);
                var status = "Failed";

                //Get what we should open this with and the default app opener list
                var FileWithDefaultViewer = UniversalSave.Load(ViewerDataPath, DataFormat.JSON);

                //Get the defaultformatters dictionary ajnd default app opener list
                FiletypeRenderer DefaultViewers = (FiletypeRenderer)FileWithDefaultViewer.Get("FiletypeRenderer"); //The app we choose to open a file with but encrypted

                //First let's decrypt everything in DefaultViewers.
                Dictionary<string, string> DecryptedDefaultViewers = new();

                foreach (KeyValuePair<AESEncryptedText, AESEncryptedText> objects in DefaultViewers.DefaultApps)
                {
                    var item1 = Decrypt(objects.Key, UserPassword);
                    var item2 = Decrypt(objects.Value, UserPassword);
                    DecryptedDefaultViewers.Add(item1, item2);
                }

                //Now our list is decrypted! Let's see if we have a default app!

                if (DecryptedDefaultViewers.ContainsKey(extension))
                {
                    //Let's get the name of the default app
                    string appname = DecryptedDefaultViewers[extension];

                    //Now let's get the app information
                    AppInfo tempappinfo = Apps.GetAppByName(appname);

                    //And now let's open the app
                    var appobj = Apps.OpenApp(tempappinfo, Apps.AppStatus.loadingfile);

                    //Now we load in our file (or try anyways)

                    var apptype = XRUIOS.Security.DecryptCultistKey(tempappinfo.AppType, UserPassword).Item;

                    switch (apptype)
                    {
                        case Apptype.BaseOS:
                            //Use the load function
                            break;

                        case Apptype.XRUIOS:
                            break;
                    }

                    //And FINALLY set the status of the gameobject to loaded! We just assume it's loaded, in the future i'll make some fancy code for this
                    Apps.ChangeAppStatus(appobj, Apps.AppStatus.loaded);
                }
                return status;
            }

            public static string OpenFileWithSpecificApp(string filepath, string AppName)
            {
                //First let's get the appinfo!
                AppInfo tempappinfo = Apps.GetAppByName(AppName);

                //And now let's open the app
                var appobj = Apps.OpenApp(tempappinfo, Apps.AppStatus.loadingfile);

                //Now we load in our file (or try anyways)

                var apptype = XRUIOS.Security.DecryptCultistKey(tempappinfo.AppType, UserPassword).Item;

                switch (apptype)
                {
                    case Apptype.BaseOS:
                        //Use the load function
                        break;

                    case Apptype.XRUIOS:
                        break;
                }

                //And FINALLY set the status of the gameobject to loaded! We just assume it's loaded, in the future i'll make some fancy code for this
                Apps.ChangeAppStatus(appobj, Apps.AppStatus.loaded);
                return "done";


            }

            public enum changeapprenderersupporttype { Add, Delete }

            public static string ChangeDefaultRenderer(string extension, string AppName)
            {
                //First let's see if the object exists in the list

                var status = "Failed";

                //Get what we should open this with and the default app opener list
                var FileWithDefaultViewer = UniversalSave.Load(ViewerDataPath, DataFormat.JSON);

                //Get the defaultformatters dictionary ajnd default app opener list
                FiletypeRenderer DefaultViewers = (FiletypeRenderer)FileWithDefaultViewer.Get("FiletypeRenderer"); //The app we choose to open a file with but encrypted


                var DecryptedFiletypeRenderer = DecryptFiletypeRenderer(DefaultViewers);

                //Now our list is decrypted! Let's see if we have this item!

                if (DecryptedFiletypeRenderer.DefaultApps.ContainsKey(extension))
                {
                    //If it does, let's just change the value of the item in the DecryptedFiletypeRenderer.DefaultApps
                    DecryptedFiletypeRenderer.DefaultApps.Remove(extension);
                    DecryptedFiletypeRenderer.DefaultApps.Add(extension, AppName);
                }

                //Otherwise we just make it
                else
                {
                    DecryptedFiletypeRenderer.DefaultApps.Add(extension, AppName);
                }

                //Now we encrypt DecryptedDefaultViewers and save it in the file

                //Now encrypt it all
                var itemtosave = new FiletypeRenderer(DecryptedFiletypeRenderer, UserPassword);

                //And finally we can push it to the file as the new settings
                FileWithDefaultViewer.Set("FiletypeRenderer", itemtosave);
                UniversalSave.Save(ViewerDataPath, FileWithDefaultViewer);

                return "Done";

            }

            public static string ChangeAppRendererSupport(string supportedapp, string extension, changeapprenderersupporttype state)
            {

                //First let's see if the object exists in the list

                var status = "Failed";

                //Get what we should open this with and the default app opener list
                var FileWithDefaultViewer = UniversalSave.Load(ViewerDataPath, DataFormat.JSON);

                //Get the defaultformatters dictionary ajnd default app opener list
                FiletypeRenderer DefaultViewers = (FiletypeRenderer)FileWithDefaultViewer.Get("FiletypeRenderer"); //The app we choose to open a file with but encrypted


                var DecryptedFiletypeRenderer = DecryptFiletypeRenderer(DefaultViewers);

                //Now our list is decrypted! Let's see if we have this item!

                bool listcontainsitem = false;
                int chosenrenderer = default;

                foreach (DecryptedRenderers item in DecryptedFiletypeRenderer.Renderers)
                {

                    //Is this the extension we are looking for
                    var itemext = item.ExtensionType;
                    if (itemext == extension)
                    {
                        //If we have this extension in the list let's set the variable to true and the chosenrender to this
                        listcontainsitem = true;
                        chosenrenderer = DecryptedFiletypeRenderer.Renderers.IndexOf(item);
                        break;
                    }
                }

                //Our extension exists
                if (listcontainsitem)
                {
                    switch (state)
                    {
                        case changeapprenderersupporttype.Add:
                            //We will add to the list item
                            DecryptedFiletypeRenderer.Renderers[chosenrenderer].RenderingApps.Add(supportedapp);
                            //In the future i'll add a script here which ensures the supported app is first checked all the way at the top, before the file is even called
                            break;

                        case changeapprenderersupporttype.Delete:
                            //We will delete from the list item
                            DecryptedFiletypeRenderer.Renderers[chosenrenderer].RenderingApps.Remove(supportedapp);
                            //In the future i'll add a script here which ensures the supported app is first checked all the way at the top, before the file is even called
                            break;
                    }
                }

                else
                {
                    switch (state)
                    {
                        case changeapprenderersupporttype.Add:
                            //We will add to the list item, first make a new renderer
                            var templist = new List<string>();
                            templist.Add(supportedapp);
                            var temprenderer = new DecryptedRenderers(extension, templist);

                            //Now add it to our main list
                            DecryptedFiletypeRenderer.Renderers.Add(temprenderer);

                            break;

                        case changeapprenderersupporttype.Delete:
                            //We will add to the list item, first make a new renderer
                            var templist2 = new List<string>();
                            //We won't add anything to this list since it was supposed to be "deleted"
                            var temprenderer2 = new DecryptedRenderers(extension, templist2);

                            //Now add it to our main list
                            DecryptedFiletypeRenderer.Renderers.Add(temprenderer2);

                            break;
                    }
                }

                //If the list contains the item, we simply replace it. Else, we will add this

                //Now we encrypt DecryptedDefaultViewers and save it in the file

                //Now encrypt it all
                var itemtosave = new FiletypeRenderer(DecryptedFiletypeRenderer, UserPassword);

                //And finally we can push it to the file as the new settings
                FileWithDefaultViewer.Set("FiletypeRenderer", itemtosave);
                UniversalSave.Save(ViewerDataPath, FileWithDefaultViewer);

                return "Done";


            }




            //Include if the extension type isn't listed

            //These just give a list of apps which can run a certain file extension, one only XRUIOS and one only Windows

            //Gets list of all XRUIOS apps capable of running a file
            public static List<DecryptedAppInfo> GetAllXRUIOSAppsCapableOfRunningThisExtension(string fileExtension)
            {
                //When an app object first adds itself to the XRUIOS, it should have a function in the headser called "OpenFileType" under a script called "FileRunner"
                //This is completely optional and should be added only if a dev wants to do that kind of thing
                //Devs have completely free access to use the "ChangeAppRendererSupport", and if under "FileRunner", it will be ran immediately after downloading
                //I'll make a method for this later

                //To get the XRUIOS apps we want to use, let's check FiletypeRenderer.Opener
                var FileWithFiletypeRenderer = UniversalSave.Load(ViewerDataPath, DataFormat.JSON);
                FiletypeRenderer Frender = (FiletypeRenderer)FileWithFiletypeRenderer.Get("FileWithFiletypeRenderer"); //The app we choose to open a file with

                //Now load the list of Renderers by decrypting them all
                List<DecryptedRenderers> extensionsandtheirapps = new List<DecryptedRenderers>();

                foreach (Renderers item in Frender.Renderers)
                {
                    var itemtoadd = DecryptRenderers(item);
                    extensionsandtheirapps.Add(itemtoadd);
                }

                DecryptedRenderers foundRenderer = default;

                //This list has all extensions and app paths capable of running it. Let's get the list we need
                foreach (DecryptedRenderers renderer in extensionsandtheirapps)
                {
                    if (renderer.ExtensionType == fileExtension)
                    {
                        foundRenderer = renderer;
                        break;
                    }
                }

                //Now take the RenderingApps and get a DecryptedAppInfo for each
                //Load in a list
                List<DecryptedAppInfo> decryptedApps = Apps.GetAllApps(); // We get all the apps in the system

                List<DecryptedAppInfo> matchingApps = new List<DecryptedAppInfo>();

                // Extract the program paths associated with the foundRenderer
                List<string> programPaths = foundRenderer.RenderingApps;

                // Convert programPaths to a HashSet for faster lookup
                HashSet<string> appPathsSet = new HashSet<string>(programPaths);

                foreach (DecryptedAppInfo appInfo in decryptedApps)
                {
                    if (appPathsSet.Contains(appInfo.AppPath))
                    {
                        // The AppPath exists in the list of program paths, so add it to the matchingApps list
                        matchingApps.Add(appInfo);
                    }
                }

                return matchingApps;
            }

            //Gets a list of all Windows apps capable of running a file
            public static List<DecryptedAppInfo> GetAllBaseOSAppsCapableOfRunningThisExtension(string fileExtension)
            {
                List<string> programs = BaseOSUtilityStuff(fileExtension); //Get all apps which can run this on the Windows level

                //Let's get the AppInfo equivalent

                //First make container for the apps


                List<DecryptedAppInfo> temp = new List<DecryptedAppInfo>();
                foreach (string program in programs)
                {
                    AppInfo p1 = (Apps.GetAppByPath(program));
                    temp.Add(DecryptAppInfo(p1));
                }

                //Now this can be used to make a panel right, on button press you can use the "Open EXE" function
                return temp;
            }

            //Utility which actually looks for the files and updates everything in the list
            private static List<string> BaseOSUtilityStuff(string fileExtension)
            {
                List<string> programPaths = new List<string>();

                try
                {
                    // Open the Windows Registry key for the specified file extension
                    RegistryKey key = Registry.ClassesRoot.OpenSubKey(fileExtension);
                    if (key != null)
                    {
                        // Get the file type associated with the file extension
                        string fileType = key.GetValue(null) as string;
                        if (!string.IsNullOrEmpty(fileType))
                        {
                            // Open the Registry key for the "open" action of the file type
                            RegistryKey appKey = Registry.ClassesRoot.OpenSubKey(fileType + @"/shell/open/command");
                            if (appKey != null)
                            {
                                // Get the command associated with the "open" action
                                string command = appKey.GetValue(null) as string;
                                if (!string.IsNullOrEmpty(command))
                                {
                                    // Extract the program executable path from the command (remove any arguments)
                                    int spaceIndex = command.IndexOf(' ');
                                    if (spaceIndex != -1)
                                    {
                                        command = command.Substring(0, spaceIndex);
                                    }

                                    // Add the program executable path to the list
                                    programPaths.Add(command);
                                }
                                appKey.Close();
                            }
                        }
                        key.Close();


                        //Now let's check each item, for if it is an item in our AppList
                        //Check each DecryptedAppInfo item
                        List<DecryptedAppInfo> decryptedApps = Apps.GetAllApps(); // We get all the apps in the system
                                                                                  //Makea list for holding apps to add
                        List<string> app = default;

                        //We already have programPaths, so we will turn it to a hashset

                        HashSet<string> appPathsSet = new HashSet<string>(programPaths); // Convert the list of app paths to a HashSet for faster lookup

                        foreach (DecryptedAppInfo appInfo in decryptedApps)
                        {
                            if (!appPathsSet.Contains(appInfo.AppPath))
                            {
                                // The AppPath does not exist in the list of strings, so we will add it to our string list "Apps to add"
                                app.Add(appInfo.AppPath);
                            }
                        }

                        foreach (string appPath in app)
                        {
                            DecryptedAppInfo appInfo = new DecryptedAppInfo(appPath, null, null, null, null, null, null);
                            Apps.AddAppToVault(appInfo);
                        }

                        //Now that we know that all apps which should exist, we are finally done and we can return the programPaths

                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("Error retrieving program list: " + e.Message);
                }

                return programPaths;
            }


        }





        public static class Apps
        {


            //There are only TWO types of things which can run here; scripts and assemblies. While yes, this is open source, we also put an Obfuscator on everything else someone could easily inject malicious code

            //We are using MNodtool because it allows us to remove all dangerous namespaces, can be customized and can automatically scan new items automatically!

            //I can also automatically share the project details and certain files to make development easier!

            //The project settings of the app needs to be the same as the XRUIOS Project Settings

            //When a mod is added at runtime, it is not simply dumped into Unity. Folders are created in several places within the XRUIOS folders. This includes a directory for the asset bundles to be stored and an editable folder

            //These are loaded in, although I might have to make a custom importer for this to work.


            //I should encrypt AppType

            public static string AppInfoVaultPath; //A list of appinfo
            public static string AppPermissionsData; //A list of AppInfoSecurity, for XRUIOS only, Base OS in future

            //App system revision v1b
            //There are TWO types of apps; Base OS and XRUIOS apps. One day there will be a third; VM apps. However that's easier said than done.
            //XRUIOS Apps are referenced as "Mods" in the programming, whereas runnable files may only be EXE (i'm not sure yet). 
            //The apps list is taken from the data after checking the mod manager and the system files.
            //For the mod manager, we will run the create apps list async function when the mod finds any new app.
            //For the app manager, we run this function when we are dealing with a computer program and the system detects a new file added to the directory.
            //Mods are not simply downloaded onto a folder but instead have a default folder created on download, given they do have a special filetype. The system itself takes care of this by creating a XRUIOS folder for this object on detection. 
            //There are several mod types, ranging from themes to AIs. In our case, .xur is the app file we are looking for
            //Most apps require usage of the permission system but in the near future, the store will have an added system where users can ignore the permission system (even if it is dumb for the most part). This would be dangerous so I would need to make an AI for detecting this which is better than the current one.





            //Gets a specific app
            public static AppInfo GetAppByName(string name)
            {
                //Get the JSON File holding the AppInfo
                var FileWithAppInfo = UniversalSave.Load(AppInfoVaultPath, DataFormat.JSON);

                //Get the App Info List
                List<AppInfo> AppInfoList = (List<AppInfo>)FileWithAppInfo.Get("AppReferences");

                AppInfo app = default;

                foreach (AppInfo appdata in AppInfoList)
                {
                    var appnametocheck = Decrypt(appdata.AppName, UserPassword);
                    if (appnametocheck == name)
                    {
                        app = appdata;
                        break;
                    }
                }

                return app;
            }

            public static AppInfo GetAppByPath(string path)
            {
                //Get the JSON File holding the AppInfo
                var FileWithAppInfo = UniversalSave.Load(AppInfoVaultPath, DataFormat.JSON);

                //Get the App Info List
                List<AppInfo> AppInfoList = (List<AppInfo>)FileWithAppInfo.Get("AppReferences");

                AppInfo app = default;

                foreach (AppInfo appdata in AppInfoList)
                {
                    var appnametocheck = Decrypt(appdata.AppPath, UserPassword);
                    if (appnametocheck == path)
                    {
                        app = appdata;
                        break;
                    }
                }

                return app;
            }
            //Gets all apps, all XRUIOS or all Computer apps


            public static List<DecryptedAppInfo> GetAllApps()
            {
                var FileWithAppInfo = UniversalSave.Load(AppInfoVaultPath, DataFormat.JSON);

                //Get the App Info List
                List<AppInfo> AppInfoList = (List<AppInfo>)FileWithAppInfo.Get("AppInfoList");

                List<DecryptedAppInfo> container = default;

                foreach (AppInfo app in AppInfoList)
                {
                    var decrypted = DecryptAppInfo(app);
                    container.Add(decrypted);
                }
                return container;
            }

            public static List<DecryptedAppInfo> GetAppByType(Apptype type)
            {
                var FileWithAppInfo = UniversalSave.Load(AppInfoVaultPath, DataFormat.JSON);

                //Get the App Info List
                List<AppInfo> AppInfoList = (List<AppInfo>)FileWithAppInfo.Get("AppInfoList");

                List<DecryptedAppInfo> container = default;

                foreach (AppInfo app in AppInfoList)
                {
                    var apptype = (Apptype)XRUIOS.Security.DecryptCultistKey(app.AppType, UserPassword).Item;
                    if (apptype == type)
                    {
                        var decrypted = DecryptAppInfo(app);
                        container.Add(decrypted);
                    }

                }
                return container;
            }

            public static string AddAppToVault(DecryptedAppInfo app)
            {
                string status = "Failed";
                var FileWithAppInfo = UniversalSave.Load(AppInfoVaultPath, DataFormat.JSON);

                //Get the App Info List
                List<AppInfo> AppInfoList = (List<AppInfo>)FileWithAppInfo.Get("AppInfoList");

                var newapp = new AppInfo(app, UserPassword);

                AppInfoList.Add(newapp);

                FileWithAppInfo.Set("AppInfoList", AppInfoList);
                UniversalSave.Save(AppInfoVaultPath, FileWithAppInfo);

                status = "Success";

                return status;

            } //This should be set as useradded, i'll eventually force this

            public static string DeleteAppFromVault(DecryptedAppInfo app)
            {
                string status = "Failed";
                var FileWithAppInfo = Pariah_Cybersecurity.DataHandler.JSONDataHandler.Load(AppInfoVaultPath, DataFormat.JSON);

                //Get the App Info List
                List<AppInfo> AppInfoList = (List<AppInfo>)FileWithAppInfo.Get("AppInfoList");

                var newapp = new AppInfo(app, UserPassword);

                AppInfoList.Remove(newapp);

                FileWithAppInfo.Set("AppInfoList", AppInfoVaultPath);
                UniversalSave.Save(AppInfoVaultPath, FileWithAppInfo);

                status = "Success";

                return status;
            }

            public static string UpdateAppInVault(string appname, DecryptedAppInfo app)
            {
                string status = "Failed";
                var FileWithAppInfo = UniversalSave.Load(AppInfoVaultPath, DataFormat.JSON);

                //Get the App Info List
                List<AppInfo> AppInfoList = (List<AppInfo>)FileWithAppInfo.Get("AppInfoList");

                var newapp = new AppInfo(app, UserPassword);

                foreach (AppInfo currentapp in AppInfoList)
                {
                    var currentappname = Decrypt(currentapp.AppName, UserPassword);
                    if (currentappname == appname)
                    {
                        var pos = AppInfoList.IndexOf(currentapp);
                        AppInfoList.Remove(currentapp);
                        AppInfoList.Insert(pos, newapp);
                        status = "Success";

                        break;
                    }
                }

                FileWithAppInfo.Set("AppInfoList", AppInfoVaultPath);
                UniversalSave.Save(AppInfoVaultPath, FileWithAppInfo);

                return status;
            }

            public static void CheckAllAppsWork()
            {

            }

            public static void CheckSpecificAppWorks(string AppName)
            {

            }





            //Creates new apps, updates others and deletes the final
            //If there is a new app, use the AppInfo construct
            //If an app is found at a folder previously used, we replace the AppInfo
            //If the folder no longer exists, we delete the AppInfo

            public static string SyncXRUIOSPrograms()
            {
                string status = "Failed";
                //Get the JSON File holding the AppInfo
                var FileWithAppInfo = UniversalSave.Load(AppInfoVaultPath, DataFormat.JSON);

                List<DecryptedAppInfo> TempAppList = default;

                //Get the App Info List
                List<AppInfo> AppInfoList = (List<AppInfo>)FileWithAppInfo.Get("AppInfoList");
                foreach (Mod mod in ModManager.mods)
                {

                    //First let's check if this is a XRUIOS app at all, since there are 3D scripted objects as well






                    //Let's get the mod name, location and image
                    string appName = mod.name;
                    string appInstallLocation = default; //I'm lazy lol
                    string appIcon = default; //I'm lazy lol i'll fix later
                    Dictionary<string, GameObject> WidgetsPath = default; //For 1.2
                    Dictionary<String, GameObject> Pages = default; //For 1.2
                    Dictionary<string, SceneData.DecryptedSession> SavedSessions = default; //For 1.2
                    GameObject BackgroundProcess = default; //For 1.2


                    var newapp = new DecryptedAppInfo(appInstallLocation, appName, WidgetsPath, Pages, SavedSessions, BackgroundProcess, UserPassword);

                    TempAppList.Add(newapp);

                }
                FileWithAppInfo.Set("AppInfoList", AppInfoVaultPath);
                UniversalSave.Save(AppInfoVaultPath, FileWithAppInfo);

                //Now let's remove all instances we don't need from the list

                //First make a container for the decrypted items
                List<DecryptedAppInfo> tempvault = default;

                //Now we decrypt everything in AppInfoList while deleting all DecryptedAppList that is BaseOS
                foreach (AppInfo item in AppInfoList)
                {
                    //First let's decrypt the item
                    var itemtoadd = DecryptAppInfo(item);
                    //Now let's see what kind of app we are working with! We add the item to the tempvault list unless it is a BaseOS app
                    switch (itemtoadd.AppType)
                    {
                        case Apptype.BaseOS:
                            tempvault.Add(itemtoadd);
                            break;
                        case Apptype.XRUIOS:
                            break;

                        case Apptype.UserAddedBaseOS:
                            tempvault.Add(itemtoadd);
                            break;
                        case Apptype.UserAddedXRUIOS:
                            break;
                    }
                }

                //Now we add in the new XRUIOS stuff to this list

                //Make a list for the new app stuff

                List<AppInfo> finallist = default;
                foreach (DecryptedAppInfo item in TempAppList)
                {
                    //Basically encrypt the app and put it in our new list
                    finallist.Add(new AppInfo(item, UserPassword));
                }

                //Now let'z save this and be done
                FileWithAppInfo.Set("AppInfoList", finallist);
                UniversalSave.Save(AppInfoVaultPath, FileWithAppInfo);
                return "Dummy";

            }




            public static void SyncComputerPrograms()
            {



                List<DecryptedAppInfo> TempAppList = new List<DecryptedAppInfo>();


                //Let's load our list for now
                var FileWithAppInfo = UniversalSave.Load(AppInfoVaultPath, DataFormat.JSON);


                //Get the App Info List
                List<AppInfo> AppInfoList = (List<AppInfo>)FileWithAppInfo.Get("AppInfoList");







                //Now let's look for any objects in the system

                // Path to the Start Menu directory
                string startMenuPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs");

                // Get all exe and lnk files in the Start Menu directory and its subdirectories
                var startMenuApps = Directory.GetFiles(startMenuPath, "*.exe", SearchOption.AllDirectories)
                    .Union(Directory.GetFiles(startMenuPath, "*.lnk", SearchOption.AllDirectories));

                List<string> filteredapps = new List<string>();

                foreach (string app in startMenuApps)
                {
                    if (app.Contains("uninstall"))
                    {
                        //Ignore it if it has "Uninstall" on it
                    }
                    else
                    {
                        //If it does not, add it to the list
                        filteredapps.Add(app);
                    }
                }

                //Now our list filteredapps has all of our app directories! Let's now check for any objects which already exists in the 





                //First make a container for the decrypted items
                List<DecryptedAppInfo> tempvault = new List<DecryptedAppInfo>();

                //Now we move all XRUIOS and UserAddedXRUIOS programs to the system! We will check if the referenced file for UserAddedXRUIOS exists and if the app on BaseOS exists too
                foreach (AppInfo item in AppInfoList)
                {
                    //First let's decrypt the item
                    var itemtoadd = DecryptAppInfo(item);
                    //Now let's see what kind of app we are working with! We add the item to the tempvault list unless it is a BaseOS app
                    switch (itemtoadd.AppType)
                    {
                        case Apptype.BaseOS:
                            var appname = (Decrypt(item.AppName, UserPassword));
                            var check = filteredapps.Contains(appname); //Check if the app exists (By name, on AppInfoList), the app itself exists else it wouldn't be here
                            var check2 = filteredapps.Contains(Decrypt(item.AppPath, UserPassword)); //Check if the app path is the same within the item or if they are different

                            // We only add this object if the appname and apppath is the same, else it's basically updated

                            if (check == false || (check == true && check2 == false))

                            {
                                tempvault.Add(itemtoadd);
                            }
                            else
                            {
                                //Do nothing, don't add to list
                            }
                            continue;

                        case Apptype.XRUIOS:
                            tempvault.Add(itemtoadd);
                            continue;

                        case Apptype.UserAddedBaseOS:

                            var check3 = File.Exists(Decrypt(item.AppPath, UserPassword)); //Recheck

                            if (check3 == true)
                            {
                                tempvault.Add(itemtoadd);
                            }
                            else
                            {
                                //Do nothing, don't add to list
                            }

                            continue;


                        case Apptype.UserAddedXRUIOS:
                            tempvault.Add(itemtoadd);
                            continue;

                    }
                }

                List<string> apppathsverified = new List<string>();
                foreach (DecryptedAppInfo item in tempvault)
                {
                    apppathsverified.Add(item.AppPath);
                }



                //Now to update the list
                foreach (string app in filteredapps)
                {

                    if (apppathsverified.Contains(app))
                    {
                        //Do nothing and go to the next iteration since an appobject exists
                        continue;
                    }


                    else
                    {
                        //Create a new object since it doesn't eixst
                        string appInstallLocation = app; //Shortcut path, acts close enough to an app location lol

                        string appName = Path.GetFileNameWithoutExtension(app);
                        //App Icon Path is made by the "create New App" fnction

                        Dictionary<string, GameObject> WidgetsPath = new Dictionary<string, GameObject>();
                        Dictionary<string, GameObject> Pages = new Dictionary<string, GameObject>();
                        Dictionary<string, SceneData.DecryptedSession> SavedSessions = new Dictionary<string, SceneData.DecryptedSession>();
                        GameObject BackgroundProcess = new GameObject();


                        Debug.Log(appInstallLocation);
                        Debug.Log(appName);



                        DecryptedAppInfo newapp = new(appInstallLocation, appName, WidgetsPath, Pages, SavedSessions, BackgroundProcess, UserPassword);


                        TempAppList.Add(newapp);
                    }
                }


                //Now to combine tempapplist and ttempvault

                TempAppList.AddRange(tempvault);




                //Now we add in the new XRUIOS stuff to this list

                //Make a list for the new app stuff (Basically encrypt it all)

                List<AppInfo> finallist = new List<AppInfo>();
                foreach (DecryptedAppInfo item in TempAppList)
                {
                    //Basically encrypt the app and put it in our new list
                    finallist.Add(new AppInfo(item, UserPassword));
                }

                //Now let'z save this and be done with it
                FileWithAppInfo.Set("AppInfoList", finallist);
                UniversalSave.Save(AppInfoVaultPath, FileWithAppInfo);

            }




            public static string SyncAllPrograms()
            {
                SyncComputerPrograms();
                SyncXRUIOSPrograms();
                return "Dummy";
            }

            public enum AppStatus { loadingfile, error, loaded } //Loading means something is happening, error means no load and loaded shows the app
                                                                 //There is an object you should make under the headser called "Status", which will basically be like a loading page. Great for doing BG stuff like loading in files

            public static GameObject OpenApp(AppInfo appinfo, AppStatus status, Transform spawndata)
            {

                string pathToExecutable = Decrypt(appinfo.AppPath, UserPassword);

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = pathToExecutable,
                    UseShellExecute = true,
                    CreateNoWindow = true // Set this to false if you want to show the console window of the executable
                };

                Process process = new Process { StartInfo = startInfo };

                try
                {
                    process.Start();
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error executing {pathToExecutable}: {e.Message}");
                }







                //This basically is the standardized way to open an app.
                return default(GameObject);
            }

            public static void ChangeAppStatus(GameObject app, AppStatus status)
            {

            }

            public static GameObject OpenApp(AppInfo appinfo, AppStatus status)
            {
                //This basically is the standardized way to open an app but in the default position
                return default(GameObject);
            }

            //Basically thw two above will return the object which was created (or pooled)
            //It automatically checks if a file is XRUIOS or BaseOS and runs as it should



            public static GameObject OpenWindowsApp(string filepath)
            {
                return default(GameObject);
            }

        }
























        public static class Application
        {

            #region structs


            public enum Apptype { BaseOS, XRUIOS, UserAddedBaseOS, UserAddedXRUIOS, ScriptedObject }

            public struct AppInfo //Even singular files are counted as apps
            {

                public XRUIOS.Security.CultistKey AppType;

                public AESEncryptedText AppPath; //In the case of a base app, this is an EXE file or anything that can be ran. In the case of a XRUIOS app, this is a resource.
                public AESEncryptedText AppName; //Taken from the name of the EXE or Resource

                public XRUIOS.Security.CultistKey AppIcon; //Basically the icon set for the resource/EXE file.

                public Dictionary<AESEncryptedText, XRUIOS.Security.CultistKey> WidgetsPath; //A widget for this program. In the case of a XRUIOS app, this is not needed since it should be within the resources as a folder named "Widgets". However, this is an option for EXE apps. You can make a pipeline and showcase information.

                public Dictionary<AESEncryptedText, XRUIOS.Security.CultistKey> Pages; //What should be opened when a specific name type is called, can work for all XRUIOS apps and some EXE apps.

                public Dictionary<AESEncryptedText, XRUIOS.Security.CultistKey> SavedSessions;
                //Save session data for an app. Works with all XRUIOS only, in future will find a way to add for EXE files.

                public XRUIOS.Security.CultistKey BackgroundProcess; //The singular file allowed to run at the back of a XRUIOS runtime. Can't do much about EXE files but the XRUIOS can be ran.

                //Also this file can not be tampered with. It is updated on the start of a user's runtime as well.

                ////FORDEFAULTAPPSONLY////


                // Let's make a dummy example here

                //AppPath - ".../XRUIOS Data/ProgramFiles/WIDummyApp (Asset bumdle named Walker Industries, Image of logo named Icon
                //AppInfoVaultPath - .../XRUIOS Data/ProgramFiles/WIDummyApp/Info.TXT (Made 1.1.2077, Company: Walker Industries, Developer: WalkerDev, Version: 0.9)
                //WidgetsPath - .../XRUIOS Data/ProgramFiles/WIDummyApp/Widgets (Minimized Widget, Wide Widget, Tall Widget)
                //Pages - .../XRUIOS Data/ProgramFiles/WIDummyApp/Pages.TXT (Home - Default, Info - Page1, ContactUs - Page2)
                //DefaultDataFolder - .../XRUIOS Data/ProgramData
                //Presets - .../XRUIOS Data/ProgramFiles/WIDummyApp/Presets.TXT (All - Home, Info, ContactUs/// Info - Info, ContactUs)
                //BackgroundProcess - .../XRUIOS Data/ProgramFiles/WIDummyApp/Background.CSS (Insert script here)

                //I actually can't do too much with this until I have ModTool in my project so consider this the demo level!








                public AppInfo(DecryptedAppInfo target, string UserPassword)
                {

                    this.AppType = new XRUIOS.Security.CultistKey(target.AppType, UserPassword);
                    this.AppPath = Encrypt(target.AppPath, UserPassword);
                    this.AppName = Encrypt(target.AppName, UserPassword);
                    this.AppIcon = new XRUIOS.Security.CultistKey(target.AppIcon, UserPassword);



                    Dictionary<AESEncryptedText, XRUIOS.Security.CultistKey> tempwidgetpath = default;

                    foreach (KeyValuePair<string, GameObject> objects in target.WidgetsPath)
                    {
                        var item1 = Encrypt(objects.Key, UserPassword);
                        var item2 = new XRUIOS.Security.CultistKey(objects.Value, UserPassword);
                        tempwidgetpath.Add(item1, item2);
                    }

                    this.WidgetsPath = tempwidgetpath;


                    Dictionary<AESEncryptedText, XRUIOS.Security.CultistKey> temppages = default;

                    foreach (KeyValuePair<string, GameObject> objects in target.Pages)
                    {
                        var item1 = Encrypt(objects.Key, UserPassword);
                        var item2 = new XRUIOS.Security.CultistKey(objects.Value, UserPassword);
                        temppages.Add(item1, item2);
                    }

                    this.Pages = temppages;


                    Dictionary<AESEncryptedText, XRUIOS.Security.CultistKey> tempss = default;

                    foreach (KeyValuePair<string, SceneData.DecryptedSession> objects in target.SavedSessions)
                    {
                        var item1 = Encrypt(objects.Key, UserPassword);
                        var item2 = new XRUIOS.Security.CultistKey(objects.Value, UserPassword);
                        tempss.Add(item1, item2);
                    }

                    this.SavedSessions = tempss;

                    this.BackgroundProcess = new XRUIOS.Security.CultistKey(target.BackgroundProcess, UserPassword);




                }


            }

            public struct DecryptedAppInfo
            {

                //I can make some upgrades

                public Apptype AppType;

                public string AppPath; //In the case of a base app, this is an EXE file or anything that can be ran. In the case of a XRUIOS app, this is a resource.
                public string AppName; //Taken from the name of the EXE or Resource

                public System.Drawing.Icon AppIcon; //Basically the icon set for the resource/EXE file.
                public Dictionary<string, GameObject> WidgetsPath; //A widget for this program. In the case of a XRUIOS app, this is not needed since it should be within the resources as a folder named "Widgets". However, this is an option for EXE apps. You can make a pipeline and showcase information.

                public Dictionary<String, GameObject> Pages; //What should be opened when a specific name type is called, can work for all XRUIOS apps and some EXE apps.

                public Dictionary<string, SceneData.DecryptedSession> SavedSessions;
                //Save session data for an app. Works with all XRUIOS only, in future will find a way to add for EXE files.

                public GameObject BackgroundProcess; //The singular file allowed to run at the back of a XRUIOS runtime. Can't do much about EXE files but the XRUIOS can be ran.

                //Also this file can not be tampered with. It is updated on the start of a user's runtime as well.

                public DecryptedAppInfo(string AppPath, string AppName, Dictionary<string, GameObject> WidgetsPath, Dictionary<String, GameObject> Pages, Dictionary<string, SceneData.DecryptedSession> SavedSessions, GameObject BackgroundProcess, string UserPassword)
                {
                    //Let's start by setting our first variable, AKA our selected datatype
                    this.AppPath = AppPath;
                    var appextension = Path.GetExtension(AppPath);
                    Apptype temp = default;
                    if (appextension == ".exe")
                    {
                        temp = Apptype.BaseOS;
                    }

                    if (appextension == ".xur")
                    {
                        temp = Apptype.BaseOS;
                    }

                    this.AppType = temp;
                    //Let's make a few variable containers!
                    string tempappname = default;
                    System.Drawing.Icon tempappicon = default;
                    Dictionary<string, GameObject> tempwidgetspath = default;
                    Dictionary<string, GameObject> temppages = default;
                    Dictionary<string, SceneData.DecryptedSession> tempsavedsessions = default;
                    GameObject tempbackgroundprocess = default;






                    //Now we will create a switch, depending on our apptype
                    switch (AppType)
                    {
                        case Apptype.XRUIOS: //If this is a mod, this is the location of a resource
                                             //Get 
                            ModInfo xurapp = ModInfo.Load(AppName); //CHECK THIS LATER

                            //The app name
                            tempappname = xurapp.name;
                            //The resource icon, taken by System.Drawing
                            tempappicon = default; //Do this later
                                                   //The widgets path, you would still need to use the permissions system but you could pull data from an app if you know how to or if you saved it to a file or something
                            tempwidgetspath = WidgetsPath;
                            //Specific pages for a program, as an example the bookmrks or history page of OperaGX. We don't touch for now.
                            temppages = Pages;
                            //Saved Session Details, we don't touch this (yet) and for XRUIOS only
                            tempsavedsessions = SavedSessions;
                            //Background Process, for XRUIOS only. The singular BG process should be linked to this.
                            tempbackgroundprocess = BackgroundProcess;
                            break;



                        case Apptype.BaseOS: //If this is a XRUIOS app
                                             //The app name is the name of the EXE file
                            tempappname = Path.GetFileNameWithoutExtension(AppPath);

                            //The app icon, taken by System.Drawing
                            tempappicon = Icon.ExtractAssociatedIcon(AppPath);
                            //The widgets path, you would still need to use the permissions system but you could pull data from an app if you know how to or if you saved it to a file or something
                            tempwidgetspath = WidgetsPath;
                            //Specific pages for a program, as an example the bookmrks or history page of OperaGX. We don't touch for now.
                            temppages = Pages;
                            //Saved Session Details, we don't touch this (yet) and for XRUIOS only
                            tempsavedsessions = SavedSessions;
                            //Background Process, for XRUIOS only. The singular BG process should be linked to this.
                            tempbackgroundprocess = BackgroundProcess;

                            break;
                    }

                    this.AppName = tempappname; //Gets this automatically
                    this.AppIcon = tempappicon; //Gets this automatically
                    this.WidgetsPath = tempwidgetspath;
                    this.Pages = temppages;
                    this.SavedSessions = tempsavedsessions;
                    this.BackgroundProcess = tempbackgroundprocess;
                }
            }

            public static DecryptedAppInfo DecryptAppInfo(AppInfo app)
            {


                string tempapppath = Decrypt(app.AppPath, UserPassword);
                string tempappname = Decrypt(app.AppName, UserPassword);

                GameObject AppIcon = (GameObject)XRUIOS.Security.DecryptCultistKey(app.AppIcon, UserPassword).Item;


                Dictionary<string, GameObject> tempwidgetpath = default;

                foreach (KeyValuePair<AESEncryptedText, XRUIOS.Security.CultistKey> objects in app.WidgetsPath)
                {
                    var item1 = Decrypt(objects.Key, UserPassword);
                    var item2 = (GameObject)XRUIOS.Security.DecryptCultistKey(objects.Value, UserPassword).Item;
                    tempwidgetpath.Add(item1, item2);
                }

                Dictionary<string, GameObject> temppages = default;

                foreach (KeyValuePair<AESEncryptedText, XRUIOS.Security.CultistKey> objects in app.Pages)
                {
                    var item1 = Decrypt(objects.Key, UserPassword);
                    var item2 = (GameObject)XRUIOS.Security.DecryptCultistKey(objects.Value, UserPassword).Item;
                    temppages.Add(item1, item2);
                }

                Dictionary<string, SceneData.DecryptedSession> tempsavedsessions = default;

                foreach (KeyValuePair<AESEncryptedText, XRUIOS.Security.CultistKey> objects in app.SavedSessions)
                {
                    var item1 = Decrypt(objects.Key, UserPassword);
                    var item2 = (SceneData.DecryptedSession)XRUIOS.Security.DecryptCultistKey(objects.Value, UserPassword).Item;
                    tempsavedsessions.Add(item1, item2);
                }


                GameObject tempbg = (GameObject)XRUIOS.Security.DecryptCultistKey(app.BackgroundProcess, UserPassword).Item;

                var newdecryptedappinfo = new DecryptedAppInfo(tempapppath, tempappname, tempwidgetpath, temppages, tempsavedsessions, tempbg, UserPassword);

                return newdecryptedappinfo;



            }


            public struct AppInfoSecurity //How we check perms
            {
                public XRUIOS.Security.CultistKey ReferencedApp;
                public AESEncryptedText AppID; //Saved as (Name+AppID), randomgly generated, if name does not match we generate a new ID. Exception at update)
                public List<AESEncryptedText> EditableDirectories; //Directories that can be manipulated for data purposes, can NEVER edit variables on another object
                public List<XRUIOS.Security.CultistKey> Permissions; //Saved as (Permname+PermValue+AppID), if wrong will reset the permission to false
            }


            public struct FiletypeRenderer //Holds XRUIOS Apps and Windows Apps capable of opening an application, but for default at the XRUIOS Level (IF you prefer the OS level or XRUIOS level app)
            {
                public Dictionary<AESEncryptedText, AESEncryptedText> DefaultApps; //The default opener for a file. It is the type and the app path. If a path is nonexistent, it will instead open the computer default. If that deosn't exist, an "Open with" message shows.
                public List<Renderers> Renderers; //XRUIOS Level Renderers

                public FiletypeRenderer(DecryptedFiletypeRenderer filetypeRenderer, string UserPassword)
                {

                    //Encrypt DefaultApps
                    Dictionary<AESEncryptedText, AESEncryptedText> tempdapp = default;
                    foreach (KeyValuePair<string, string> item in filetypeRenderer.DefaultApps)
                    {
                        var p1 = Encrypt(item.Key, UserPassword);
                        var p2 = Encrypt(item.Value, UserPassword);
                        tempdapp.Add(p1, p2);
                    }

                    //Encrypt Renderers
                    List<Renderers> tempapp = default;

                    foreach (DecryptedRenderers item in filetypeRenderer.Renderers)
                    {
                        var temp = new Renderers(item, UserPassword);
                        tempapp.Add(temp);
                    }

                    this.DefaultApps = tempdapp;
                    this.Renderers = tempapp;
                }
            }

            public struct DecryptedFiletypeRenderer //Good
            {
                public Dictionary<string, string> DefaultApps;
                public List<DecryptedRenderers> Renderers;

                public DecryptedFiletypeRenderer(Dictionary<string, string> DefaultApps, List<DecryptedRenderers> Renderers)
                {
                    this.DefaultApps = DefaultApps;
                    this.Renderers = Renderers;
                }
            }

            public static DecryptedFiletypeRenderer DecryptFiletypeRenderer(FiletypeRenderer Target) //Good
            {
                Dictionary<string, string> tempdapp = default;
                foreach (KeyValuePair<AESEncryptedText, AESEncryptedText> item in Target.DefaultApps)
                {
                    var p1 = Decrypt(item.Key, UserPassword);
                    var p2 = Decrypt(item.Value, UserPassword);
                    tempdapp.Add(p1, p2);
                }

                List<DecryptedRenderers> tempapp = default;

                foreach (Renderers item in Target.Renderers)
                {
                    var temp = DecryptRenderers(item);
                    tempapp.Add(temp);
                }



                return new DecryptedFiletypeRenderer(tempdapp, tempapp);
            }



            public struct Renderers //Holds an extension type and what apps can run it
            {
                public AESEncryptedText ExtensionType; //The extension
                public List<AESEncryptedText> RenderingApps; //List of apps

                public Renderers(DecryptedRenderers Renderer, string UserPassword)
                {
                    this.ExtensionType = Encrypt(Renderer.ExtensionType, UserPassword);

                    List<AESEncryptedText> tempapp = default;
                    foreach (string RenderingApp in Renderer.RenderingApps)
                    {
                        tempapp.Add(Encrypt(RenderingApp, UserPassword));
                    }

                    this.RenderingApps = tempapp;
                }
            }

            public struct DecryptedRenderers //Good
            {
                public string ExtensionType;
                public List<string> RenderingApps;

                public DecryptedRenderers(string ExtensionType, List<string> RenderingApp)
                {
                    this.ExtensionType = ExtensionType;
                    this.RenderingApps = RenderingApp;
                }
            }

            public static DecryptedRenderers DecryptRenderers(Renderers Renderer) //Good
            {
                var extensiontype = Decrypt(Renderer.ExtensionType, UserPassword);

                List<string> tempapp = default;

                foreach (AESEncryptedText AppName in Renderer.RenderingApps)
                {
                    tempapp.Add(Decrypt(AppName, UserPassword));
                }

                return new DecryptedRenderers(extensiontype, tempapp);
            }


            #endregion



            public static class Apps
            {


                //There are only TWO types of things which can run here; scripts and assemblies. While yes, this is open source, we also put an Obfuscator on everything else someone could easily inject malicious code

                //We are using MNodtool because it allows us to remove all dangerous namespaces, can be customized and can automatically scan new items automatically!

                //I can also automatically share the project details and certain files to make development easier!

                //The project settings of the app needs to be the same as the XRUIOS Project Settings

                //When a mod is added at runtime, it is not simply dumped into Unity. Folders are created in several places within the XRUIOS folders. This includes a directory for the asset bundles to be stored and an editable folder

                //These are loaded in, although I might have to make a custom importer for this to work.


                //I should encrypt AppType

                public static string AppInfoVaultPath; //A list of appinfo
                public static string AppPermissionsData; //A list of AppInfoSecurity, for XRUIOS only, Base OS in future

                //App system revision v1b
                //There are TWO types of apps; Base OS and XRUIOS apps. One day there will be a third; VM apps. However that's easier said than done.
                //XRUIOS Apps are referenced as "Mods" in the programming, whereas runnable files may only be EXE (i'm not sure yet). 
                //The apps list is taken from the data after checking the mod manager and the system files.
                //For the mod manager, we will run the create apps list async function when the mod finds any new app.
                //For the app manager, we run this function when we are dealing with a computer program and the system detects a new file added to the directory.
                //Mods are not simply downloaded onto a folder but instead have a default folder created on download, given they do have a special filetype. The system itself takes care of this by creating a XRUIOS folder for this object on detection. 
                //There are several mod types, ranging from themes to AIs. In our case, .xur is the app file we are looking for
                //Most apps require usage of the permission system but in the near future, the store will have an added system where users can ignore the permission system (even if it is dumb for the most part). This would be dangerous so I would need to make an AI for detecting this which is better than the current one.





                //Gets a specific app
                public static AppInfo GetAppByName(string name)
                {
                    //Get the JSON File holding the AppInfo
                    var FileWithAppInfo = UniversalSave.Load(AppInfoVaultPath, DataFormat.JSON);

                    //Get the App Info List
                    List<AppInfo> AppInfoList = (List<AppInfo>)FileWithAppInfo.Get("AppReferences");

                    AppInfo app = default;

                    foreach (AppInfo appdata in AppInfoList)
                    {
                        var appnametocheck = Decrypt(appdata.AppName, UserPassword);
                        if (appnametocheck == name)
                        {
                            app = appdata;
                            break;
                        }
                    }

                    return app;
                }

                public static AppInfo GetAppByPath(string path)
                {
                    //Get the JSON File holding the AppInfo
                    var FileWithAppInfo = UniversalSave.Load(AppInfoVaultPath, DataFormat.JSON);

                    //Get the App Info List
                    List<AppInfo> AppInfoList = (List<AppInfo>)FileWithAppInfo.Get("AppReferences");

                    AppInfo app = default;

                    foreach (AppInfo appdata in AppInfoList)
                    {
                        var appnametocheck = Decrypt(appdata.AppPath, UserPassword);
                        if (appnametocheck == path)
                        {
                            app = appdata;
                            break;
                        }
                    }

                    return app;
                }
                //Gets all apps, all XRUIOS or all Computer apps


                public static List<DecryptedAppInfo> GetAllApps()
                {
                    var FileWithAppInfo = UniversalSave.Load(AppInfoVaultPath, DataFormat.JSON);

                    //Get the App Info List
                    List<AppInfo> AppInfoList = (List<AppInfo>)FileWithAppInfo.Get("AppInfoList");

                    List<DecryptedAppInfo> container = default;

                    foreach (AppInfo app in AppInfoList)
                    {
                        var decrypted = DecryptAppInfo(app);
                        container.Add(decrypted);
                    }
                    return container;
                }

                public static List<DecryptedAppInfo> GetAppByType(Apptype type)
                {
                    var FileWithAppInfo = UniversalSave.Load(AppInfoVaultPath, DataFormat.JSON);

                    //Get the App Info List
                    List<AppInfo> AppInfoList = (List<AppInfo>)FileWithAppInfo.Get("AppInfoList");

                    List<DecryptedAppInfo> container = default;

                    foreach (AppInfo app in AppInfoList)
                    {
                        var apptype = (Apptype)XRUIOS.Security.DecryptCultistKey(app.AppType, UserPassword).Item;
                        if (apptype == type)
                        {
                            var decrypted = DecryptAppInfo(app);
                            container.Add(decrypted);
                        }

                    }
                    return container;
                }

                public static string AddAppToVault(DecryptedAppInfo app)
                {
                    string status = "Failed";
                    var FileWithAppInfo = UniversalSave.Load(AppInfoVaultPath, DataFormat.JSON);

                    //Get the App Info List
                    List<AppInfo> AppInfoList = (List<AppInfo>)FileWithAppInfo.Get("AppInfoList");

                    var newapp = new AppInfo(app, UserPassword);

                    AppInfoList.Add(newapp);

                    FileWithAppInfo.Set("AppInfoList", AppInfoList);
                    UniversalSave.Save(AppInfoVaultPath, FileWithAppInfo);

                    status = "Success";

                    return status;

                } //This should be set as useradded, i'll eventually force this

                public static string DeleteAppFromVault(DecryptedAppInfo app)
                {
                    string status = "Failed";
                    var FileWithAppInfo = Pariah_Cybersecurity.DataHandler.JSONDataHandler.Load(AppInfoVaultPath, DataFormat.JSON);

                    //Get the App Info List
                    List<AppInfo> AppInfoList = (List<AppInfo>)FileWithAppInfo.Get("AppInfoList");

                    var newapp = new AppInfo(app, UserPassword);

                    AppInfoList.Remove(newapp);

                    FileWithAppInfo.Set("AppInfoList", AppInfoVaultPath);
                    UniversalSave.Save(AppInfoVaultPath, FileWithAppInfo);

                    status = "Success";

                    return status;
                }

                public static string UpdateAppInVault(string appname, DecryptedAppInfo app)
                {
                    string status = "Failed";
                    var FileWithAppInfo = UniversalSave.Load(AppInfoVaultPath, DataFormat.JSON);

                    //Get the App Info List
                    List<AppInfo> AppInfoList = (List<AppInfo>)FileWithAppInfo.Get("AppInfoList");

                    var newapp = new AppInfo(app, UserPassword);

                    foreach (AppInfo currentapp in AppInfoList)
                    {
                        var currentappname = Decrypt(currentapp.AppName, UserPassword);
                        if (currentappname == appname)
                        {
                            var pos = AppInfoList.IndexOf(currentapp);
                            AppInfoList.Remove(currentapp);
                            AppInfoList.Insert(pos, newapp);
                            status = "Success";

                            break;
                        }
                    }

                    FileWithAppInfo.Set("AppInfoList", AppInfoVaultPath);
                    UniversalSave.Save(AppInfoVaultPath, FileWithAppInfo);

                    return status;
                }

                public static void CheckAllAppsWork()
                {

                }

                public static void CheckSpecificAppWorks(string AppName)
                {

                }





                //Creates new apps, updates others and deletes the final
                //If there is a new app, use the AppInfo construct
                //If an app is found at a folder previously used, we replace the AppInfo
                //If the folder no longer exists, we delete the AppInfo

                public static string SyncXRUIOSPrograms()
                {
                    string status = "Failed";
                    //Get the JSON File holding the AppInfo
                    var FileWithAppInfo = UniversalSave.Load(AppInfoVaultPath, DataFormat.JSON);

                    List<DecryptedAppInfo> TempAppList = default;

                    //Get the App Info List
                    List<AppInfo> AppInfoList = (List<AppInfo>)FileWithAppInfo.Get("AppInfoList");
                    foreach (Mod mod in ModManager.mods)
                    {

                        //First let's check if this is a XRUIOS app at all, since there are 3D scripted objects as well






                        //Let's get the mod name, location and image
                        string appName = mod.name;
                        string appInstallLocation = default; //I'm lazy lol
                        string appIcon = default; //I'm lazy lol i'll fix later
                        Dictionary<string, GameObject> WidgetsPath = default; //For 1.2
                        Dictionary<String, GameObject> Pages = default; //For 1.2
                        Dictionary<string, SceneData.DecryptedSession> SavedSessions = default; //For 1.2
                        GameObject BackgroundProcess = default; //For 1.2


                        var newapp = new DecryptedAppInfo(appInstallLocation, appName, WidgetsPath, Pages, SavedSessions, BackgroundProcess, UserPassword);

                        TempAppList.Add(newapp);

                    }
                    FileWithAppInfo.Set("AppInfoList", AppInfoVaultPath);
                    UniversalSave.Save(AppInfoVaultPath, FileWithAppInfo);

                    //Now let's remove all instances we don't need from the list

                    //First make a container for the decrypted items
                    List<DecryptedAppInfo> tempvault = default;

                    //Now we decrypt everything in AppInfoList while deleting all DecryptedAppList that is BaseOS
                    foreach (AppInfo item in AppInfoList)
                    {
                        //First let's decrypt the item
                        var itemtoadd = DecryptAppInfo(item);
                        //Now let's see what kind of app we are working with! We add the item to the tempvault list unless it is a BaseOS app
                        switch (itemtoadd.AppType)
                        {
                            case Apptype.BaseOS:
                                tempvault.Add(itemtoadd);
                                break;
                            case Apptype.XRUIOS:
                                break;

                            case Apptype.UserAddedBaseOS:
                                tempvault.Add(itemtoadd);
                                break;
                            case Apptype.UserAddedXRUIOS:
                                break;
                        }
                    }

                    //Now we add in the new XRUIOS stuff to this list

                    //Make a list for the new app stuff

                    List<AppInfo> finallist = default;
                    foreach (DecryptedAppInfo item in TempAppList)
                    {
                        //Basically encrypt the app and put it in our new list
                        finallist.Add(new AppInfo(item, UserPassword));
                    }

                    //Now let'z save this and be done
                    FileWithAppInfo.Set("AppInfoList", finallist);
                    UniversalSave.Save(AppInfoVaultPath, FileWithAppInfo);
                    return "Dummy";

                }




                public static void SyncComputerPrograms()
                {



                    List<DecryptedAppInfo> TempAppList = new List<DecryptedAppInfo>();


                    //Let's load our list for now
                    var FileWithAppInfo = UniversalSave.Load(AppInfoVaultPath, DataFormat.JSON);


                    //Get the App Info List
                    List<AppInfo> AppInfoList = (List<AppInfo>)FileWithAppInfo.Get("AppInfoList");







                    //Now let's look for any objects in the system

                    // Path to the Start Menu directory
                    string startMenuPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs");

                    // Get all exe and lnk files in the Start Menu directory and its subdirectories
                    var startMenuApps = Directory.GetFiles(startMenuPath, "*.exe", SearchOption.AllDirectories)
                        .Union(Directory.GetFiles(startMenuPath, "*.lnk", SearchOption.AllDirectories));

                    List<string> filteredapps = new List<string>();

                    foreach (string app in startMenuApps)
                    {
                        if (app.Contains("uninstall"))
                        {
                            //Ignore it if it has "Uninstall" on it
                        }
                        else
                        {
                            //If it does not, add it to the list
                            filteredapps.Add(app);
                        }
                    }

                    //Now our list filteredapps has all of our app directories! Let's now check for any objects which already exists in the 





                    //First make a container for the decrypted items
                    List<DecryptedAppInfo> tempvault = new List<DecryptedAppInfo>();

                    //Now we move all XRUIOS and UserAddedXRUIOS programs to the system! We will check if the referenced file for UserAddedXRUIOS exists and if the app on BaseOS exists too
                    foreach (AppInfo item in AppInfoList)
                    {
                        //First let's decrypt the item
                        var itemtoadd = DecryptAppInfo(item);
                        //Now let's see what kind of app we are working with! We add the item to the tempvault list unless it is a BaseOS app
                        switch (itemtoadd.AppType)
                        {
                            case Apptype.BaseOS:
                                var appname = (Decrypt(item.AppName, UserPassword));
                                var check = filteredapps.Contains(appname); //Check if the app exists (By name, on AppInfoList), the app itself exists else it wouldn't be here
                                var check2 = filteredapps.Contains(Decrypt(item.AppPath, UserPassword)); //Check if the app path is the same within the item or if they are different

                                // We only add this object if the appname and apppath is the same, else it's basically updated

                                if (check == false || (check == true && check2 == false))

                                {
                                    tempvault.Add(itemtoadd);
                                }
                                else
                                {
                                    //Do nothing, don't add to list
                                }
                                continue;

                            case Apptype.XRUIOS:
                                tempvault.Add(itemtoadd);
                                continue;

                            case Apptype.UserAddedBaseOS:

                                var check3 = File.Exists(Decrypt(item.AppPath, UserPassword)); //Recheck

                                if (check3 == true)
                                {
                                    tempvault.Add(itemtoadd);
                                }
                                else
                                {
                                    //Do nothing, don't add to list
                                }

                                continue;


                            case Apptype.UserAddedXRUIOS:
                                tempvault.Add(itemtoadd);
                                continue;

                        }
                    }

                    List<string> apppathsverified = new List<string>();
                    foreach (DecryptedAppInfo item in tempvault)
                    {
                        apppathsverified.Add(item.AppPath);
                    }



                    //Now to update the list
                    foreach (string app in filteredapps)
                    {

                        if (apppathsverified.Contains(app))
                        {
                            //Do nothing and go to the next iteration since an appobject exists
                            continue;
                        }


                        else
                        {
                            //Create a new object since it doesn't eixst
                            string appInstallLocation = app; //Shortcut path, acts close enough to an app location lol

                            string appName = Path.GetFileNameWithoutExtension(app);
                            //App Icon Path is made by the "create New App" fnction

                            Dictionary<string, GameObject> WidgetsPath = new Dictionary<string, GameObject>();
                            Dictionary<string, GameObject> Pages = new Dictionary<string, GameObject>();
                            Dictionary<string, SceneData.DecryptedSession> SavedSessions = new Dictionary<string, SceneData.DecryptedSession>();
                            GameObject BackgroundProcess = new GameObject();


                            Debug.Log(appInstallLocation);
                            Debug.Log(appName);



                            DecryptedAppInfo newapp = new(appInstallLocation, appName, WidgetsPath, Pages, SavedSessions, BackgroundProcess, UserPassword);


                            TempAppList.Add(newapp);
                        }
                    }


                    //Now to combine tempapplist and ttempvault

                    TempAppList.AddRange(tempvault);




                    //Now we add in the new XRUIOS stuff to this list

                    //Make a list for the new app stuff (Basically encrypt it all)

                    List<AppInfo> finallist = new List<AppInfo>();
                    foreach (DecryptedAppInfo item in TempAppList)
                    {
                        //Basically encrypt the app and put it in our new list
                        finallist.Add(new AppInfo(item, UserPassword));
                    }

                    //Now let'z save this and be done with it
                    FileWithAppInfo.Set("AppInfoList", finallist);
                    UniversalSave.Save(AppInfoVaultPath, FileWithAppInfo);

                }




                public static string SyncAllPrograms()
                {
                    SyncComputerPrograms();
                    SyncXRUIOSPrograms();
                    return "Dummy";
                }

                public enum AppStatus { loadingfile, error, loaded } //Loading means something is happening, error means no load and loaded shows the app
                                                                     //There is an object you should make under the headser called "Status", which will basically be like a loading page. Great for doing BG stuff like loading in files

                public static GameObject OpenApp(AppInfo appinfo, AppStatus status, Transform spawndata)
                {

                    string pathToExecutable = Decrypt(appinfo.AppPath, UserPassword);

                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        FileName = pathToExecutable,
                        UseShellExecute = true,
                        CreateNoWindow = true // Set this to false if you want to show the console window of the executable
                    };

                    Process process = new Process { StartInfo = startInfo };

                    try
                    {
                        process.Start();
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"Error executing {pathToExecutable}: {e.Message}");
                    }







                    //This basically is the standardized way to open an app.
                    return default(GameObject);
                }

                public static void ChangeAppStatus(GameObject app, AppStatus status)
                {

                }

                public static GameObject OpenApp(AppInfo appinfo, AppStatus status)
                {
                    //This basically is the standardized way to open an app but in the default position
                    return default(GameObject);
                }

                //Basically thw two above will return the object which was created (or pooled)
                //It automatically checks if a file is XRUIOS or BaseOS and runs as it should



                public static GameObject OpenWindowsApp(string filepath)
                {
                    return default(GameObject);
                }

            }

            public static class FiletypeViewer
            {
                //This takes care of two types; seeing what an app can open and how it opens it



                public static string ViewerDataPath = "caca"; //Holds FileWithFiletypeRenderer





                //Opens app with the default
                public static string OpenFile(string filepath)
                {

                    //First let's get extension type
                    var extension = Path.GetExtension(filepath);
                    var status = "Failed";

                    //Get what we should open this with and the default app opener list
                    var FileWithDefaultViewer = UniversalSave.Load(ViewerDataPath, DataFormat.JSON);

                    //Get the defaultformatters dictionary ajnd default app opener list
                    FiletypeRenderer DefaultViewers = (FiletypeRenderer)FileWithDefaultViewer.Get("FiletypeRenderer"); //The app we choose to open a file with but encrypted

                    //First let's decrypt everything in DefaultViewers.
                    Dictionary<string, string> DecryptedDefaultViewers = new();

                    foreach (KeyValuePair<AESEncryptedText, AESEncryptedText> objects in DefaultViewers.DefaultApps)
                    {
                        var item1 = Decrypt(objects.Key, UserPassword);
                        var item2 = Decrypt(objects.Value, UserPassword);
                        DecryptedDefaultViewers.Add(item1, item2);
                    }

                    //Now our list is decrypted! Let's see if we have a default app!

                    if (DecryptedDefaultViewers.ContainsKey(extension))
                    {
                        //Let's get the name of the default app
                        string appname = DecryptedDefaultViewers[extension];

                        //Now let's get the app information
                        AppInfo tempappinfo = Apps.GetAppByName(appname);

                        //And now let's open the app
                        var appobj = Apps.OpenApp(tempappinfo, Apps.AppStatus.loadingfile);

                        //Now we load in our file (or try anyways)

                        var apptype = XRUIOS.Security.DecryptCultistKey(tempappinfo.AppType, UserPassword).Item;

                        switch (apptype)
                        {
                            case Apptype.BaseOS:
                                //Use the load function
                                break;

                            case Apptype.XRUIOS:
                                break;
                        }

                        //And FINALLY set the status of the gameobject to loaded! We just assume it's loaded, in the future i'll make some fancy code for this
                        Apps.ChangeAppStatus(appobj, Apps.AppStatus.loaded);
                    }
                    return status;
                }

                public static string OpenFileWithSpecificApp(string filepath, string AppName)
                {
                    //First let's get the appinfo!
                    AppInfo tempappinfo = Apps.GetAppByName(AppName);

                    //And now let's open the app
                    var appobj = Apps.OpenApp(tempappinfo, Apps.AppStatus.loadingfile);

                    //Now we load in our file (or try anyways)

                    var apptype = XRUIOS.Security.DecryptCultistKey(tempappinfo.AppType, UserPassword).Item;

                    switch (apptype)
                    {
                        case Apptype.BaseOS:
                            //Use the load function
                            break;

                        case Apptype.XRUIOS:
                            break;
                    }

                    //And FINALLY set the status of the gameobject to loaded! We just assume it's loaded, in the future i'll make some fancy code for this
                    Apps.ChangeAppStatus(appobj, Apps.AppStatus.loaded);
                    return "done";


                }

                public enum changeapprenderersupporttype { Add, Delete }

                public static string ChangeDefaultRenderer(string extension, string AppName)
                {
                    //First let's see if the object exists in the list

                    var status = "Failed";

                    //Get what we should open this with and the default app opener list
                    var FileWithDefaultViewer = UniversalSave.Load(ViewerDataPath, DataFormat.JSON);

                    //Get the defaultformatters dictionary ajnd default app opener list
                    FiletypeRenderer DefaultViewers = (FiletypeRenderer)FileWithDefaultViewer.Get("FiletypeRenderer"); //The app we choose to open a file with but encrypted


                    var DecryptedFiletypeRenderer = DecryptFiletypeRenderer(DefaultViewers);

                    //Now our list is decrypted! Let's see if we have this item!

                    if (DecryptedFiletypeRenderer.DefaultApps.ContainsKey(extension))
                    {
                        //If it does, let's just change the value of the item in the DecryptedFiletypeRenderer.DefaultApps
                        DecryptedFiletypeRenderer.DefaultApps.Remove(extension);
                        DecryptedFiletypeRenderer.DefaultApps.Add(extension, AppName);
                    }

                    //Otherwise we just make it
                    else
                    {
                        DecryptedFiletypeRenderer.DefaultApps.Add(extension, AppName);
                    }

                    //Now we encrypt DecryptedDefaultViewers and save it in the file

                    //Now encrypt it all
                    var itemtosave = new FiletypeRenderer(DecryptedFiletypeRenderer, UserPassword);

                    //And finally we can push it to the file as the new settings
                    FileWithDefaultViewer.Set("FiletypeRenderer", itemtosave);
                    UniversalSave.Save(ViewerDataPath, FileWithDefaultViewer);

                    return "Done";

                }

                public static string ChangeAppRendererSupport(string supportedapp, string extension, changeapprenderersupporttype state)
                {

                    //First let's see if the object exists in the list

                    var status = "Failed";

                    //Get what we should open this with and the default app opener list
                    var FileWithDefaultViewer = UniversalSave.Load(ViewerDataPath, DataFormat.JSON);

                    //Get the defaultformatters dictionary ajnd default app opener list
                    FiletypeRenderer DefaultViewers = (FiletypeRenderer)FileWithDefaultViewer.Get("FiletypeRenderer"); //The app we choose to open a file with but encrypted


                    var DecryptedFiletypeRenderer = DecryptFiletypeRenderer(DefaultViewers);

                    //Now our list is decrypted! Let's see if we have this item!

                    bool listcontainsitem = false;
                    int chosenrenderer = default;

                    foreach (DecryptedRenderers item in DecryptedFiletypeRenderer.Renderers)
                    {

                        //Is this the extension we are looking for
                        var itemext = item.ExtensionType;
                        if (itemext == extension)
                        {
                            //If we have this extension in the list let's set the variable to true and the chosenrender to this
                            listcontainsitem = true;
                            chosenrenderer = DecryptedFiletypeRenderer.Renderers.IndexOf(item);
                            break;
                        }
                    }

                    //Our extension exists
                    if (listcontainsitem)
                    {
                        switch (state)
                        {
                            case changeapprenderersupporttype.Add:
                                //We will add to the list item
                                DecryptedFiletypeRenderer.Renderers[chosenrenderer].RenderingApps.Add(supportedapp);
                                //In the future i'll add a script here which ensures the supported app is first checked all the way at the top, before the file is even called
                                break;

                            case changeapprenderersupporttype.Delete:
                                //We will delete from the list item
                                DecryptedFiletypeRenderer.Renderers[chosenrenderer].RenderingApps.Remove(supportedapp);
                                //In the future i'll add a script here which ensures the supported app is first checked all the way at the top, before the file is even called
                                break;
                        }
                    }

                    else
                    {
                        switch (state)
                        {
                            case changeapprenderersupporttype.Add:
                                //We will add to the list item, first make a new renderer
                                var templist = new List<string>();
                                templist.Add(supportedapp);
                                var temprenderer = new DecryptedRenderers(extension, templist);

                                //Now add it to our main list
                                DecryptedFiletypeRenderer.Renderers.Add(temprenderer);

                                break;

                            case changeapprenderersupporttype.Delete:
                                //We will add to the list item, first make a new renderer
                                var templist2 = new List<string>();
                                //We won't add anything to this list since it was supposed to be "deleted"
                                var temprenderer2 = new DecryptedRenderers(extension, templist2);

                                //Now add it to our main list
                                DecryptedFiletypeRenderer.Renderers.Add(temprenderer2);

                                break;
                        }
                    }

                    //If the list contains the item, we simply replace it. Else, we will add this

                    //Now we encrypt DecryptedDefaultViewers and save it in the file

                    //Now encrypt it all
                    var itemtosave = new FiletypeRenderer(DecryptedFiletypeRenderer, UserPassword);

                    //And finally we can push it to the file as the new settings
                    FileWithDefaultViewer.Set("FiletypeRenderer", itemtosave);
                    UniversalSave.Save(ViewerDataPath, FileWithDefaultViewer);

                    return "Done";


                }




                //Include if the extension type isn't listed

                //These just give a list of apps which can run a certain file extension, one only XRUIOS and one only Windows

                //Gets list of all XRUIOS apps capable of running a file
                public static List<DecryptedAppInfo> GetAllXRUIOSAppsCapableOfRunningThisExtension(string fileExtension)
                {
                    //When an app object first adds itself to the XRUIOS, it should have a function in the headser called "OpenFileType" under a script called "FileRunner"
                    //This is completely optional and should be added only if a dev wants to do that kind of thing
                    //Devs have completely free access to use the "ChangeAppRendererSupport", and if under "FileRunner", it will be ran immediately after downloading
                    //I'll make a method for this later

                    //To get the XRUIOS apps we want to use, let's check FiletypeRenderer.Opener
                    var FileWithFiletypeRenderer = UniversalSave.Load(ViewerDataPath, DataFormat.JSON);
                    FiletypeRenderer Frender = (FiletypeRenderer)FileWithFiletypeRenderer.Get("FileWithFiletypeRenderer"); //The app we choose to open a file with

                    //Now load the list of Renderers by decrypting them all
                    List<DecryptedRenderers> extensionsandtheirapps = new List<DecryptedRenderers>();

                    foreach (Renderers item in Frender.Renderers)
                    {
                        var itemtoadd = DecryptRenderers(item);
                        extensionsandtheirapps.Add(itemtoadd);
                    }

                    DecryptedRenderers foundRenderer = default;

                    //This list has all extensions and app paths capable of running it. Let's get the list we need
                    foreach (DecryptedRenderers renderer in extensionsandtheirapps)
                    {
                        if (renderer.ExtensionType == fileExtension)
                        {
                            foundRenderer = renderer;
                            break;
                        }
                    }

                    //Now take the RenderingApps and get a DecryptedAppInfo for each
                    //Load in a list
                    List<DecryptedAppInfo> decryptedApps = Apps.GetAllApps(); // We get all the apps in the system

                    List<DecryptedAppInfo> matchingApps = new List<DecryptedAppInfo>();

                    // Extract the program paths associated with the foundRenderer
                    List<string> programPaths = foundRenderer.RenderingApps;

                    // Convert programPaths to a HashSet for faster lookup
                    HashSet<string> appPathsSet = new HashSet<string>(programPaths);

                    foreach (DecryptedAppInfo appInfo in decryptedApps)
                    {
                        if (appPathsSet.Contains(appInfo.AppPath))
                        {
                            // The AppPath exists in the list of program paths, so add it to the matchingApps list
                            matchingApps.Add(appInfo);
                        }
                    }

                    return matchingApps;
                }

                //Gets a list of all Windows apps capable of running a file
                public static List<DecryptedAppInfo> GetAllBaseOSAppsCapableOfRunningThisExtension(string fileExtension)
                {
                    List<string> programs = BaseOSUtilityStuff(fileExtension); //Get all apps which can run this on the Windows level

                    //Let's get the AppInfo equivalent

                    //First make container for the apps


                    List<DecryptedAppInfo> temp = new List<DecryptedAppInfo>();
                    foreach (string program in programs)
                    {
                        AppInfo p1 = (Apps.GetAppByPath(program));
                        temp.Add(DecryptAppInfo(p1));
                    }

                    //Now this can be used to make a panel right, on button press you can use the "Open EXE" function
                    return temp;
                }

                //Utility which actually looks for the files and updates everything in the list
                private static List<string> BaseOSUtilityStuff(string fileExtension)
                {
                    List<string> programPaths = new List<string>();

                    try
                    {
                        // Open the Windows Registry key for the specified file extension
                        RegistryKey key = Registry.ClassesRoot.OpenSubKey(fileExtension);
                        if (key != null)
                        {
                            // Get the file type associated with the file extension
                            string fileType = key.GetValue(null) as string;
                            if (!string.IsNullOrEmpty(fileType))
                            {
                                // Open the Registry key for the "open" action of the file type
                                RegistryKey appKey = Registry.ClassesRoot.OpenSubKey(fileType + @"/shell/open/command");
                                if (appKey != null)
                                {
                                    // Get the command associated with the "open" action
                                    string command = appKey.GetValue(null) as string;
                                    if (!string.IsNullOrEmpty(command))
                                    {
                                        // Extract the program executable path from the command (remove any arguments)
                                        int spaceIndex = command.IndexOf(' ');
                                        if (spaceIndex != -1)
                                        {
                                            command = command.Substring(0, spaceIndex);
                                        }

                                        // Add the program executable path to the list
                                        programPaths.Add(command);
                                    }
                                    appKey.Close();
                                }
                            }
                            key.Close();


                            //Now let's check each item, for if it is an item in our AppList
                            //Check each DecryptedAppInfo item
                            List<DecryptedAppInfo> decryptedApps = Apps.GetAllApps(); // We get all the apps in the system
                                                                                      //Makea list for holding apps to add
                            List<string> app = default;

                            //We already have programPaths, so we will turn it to a hashset

                            HashSet<string> appPathsSet = new HashSet<string>(programPaths); // Convert the list of app paths to a HashSet for faster lookup

                            foreach (DecryptedAppInfo appInfo in decryptedApps)
                            {
                                if (!appPathsSet.Contains(appInfo.AppPath))
                                {
                                    // The AppPath does not exist in the list of strings, so we will add it to our string list "Apps to add"
                                    app.Add(appInfo.AppPath);
                                }
                            }

                            foreach (string appPath in app)
                            {
                                DecryptedAppInfo appInfo = new DecryptedAppInfo(appPath, null, null, null, null, null, null);
                                Apps.AddAppToVault(appInfo);
                            }

                            //Now that we know that all apps which should exist, we are finally done and we can return the programPaths

                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("Error retrieving program list: " + e.Message);
                    }

                    return programPaths;
                }


            }


        }












        public class PowerOff()
        {

            public void ShutDown()
            {
                System.Diagnostics.Process.Start("shutdown.exe", "/s /t 0");
            }

            public void Sleep()
            {

            }


        }



        public static class ThemeSystem
        {

            public record ThemeColors
            {
                public (string, string) BackgroundPrimary; //Image, color
                public (string, string) BackgroundSecondary;
                public (string, string) Surface;
                public (string, string) AccentPrimary;
                public (string, string) AccentSecondary;
                public string TextPrimary;
                public string TextMuted;
                public string Error;
                public string Warning;
                public string Success;

                public ThemeColors() { }

                public ThemeColors(
                    (string, string) backgroundPrimary,
                    (string, string) backgroundSecondary,
                    (string, string) surface,
                    (string, string) accentPrimary,
                    (string, string) accentSecondary,
                    string textPrimary,
                    string textMuted,
                    string error,
                    string warning,
                    string success)
                {
                    BackgroundPrimary = backgroundPrimary;
                    BackgroundSecondary = backgroundSecondary;
                    Surface = surface;
                    AccentPrimary = accentPrimary;
                    AccentSecondary = accentSecondary;
                    TextPrimary = textPrimary;
                    TextMuted = textMuted;
                    Error = error;
                    Warning = warning;
                    Success = success;
                }
            }

            public record ThemeTypography
            {
                public List<string> PrimaryFont; //h1 = h6, paragraph, caption
                public float FontScale;

                public ThemeTypography() { }

                public ThemeTypography(
                    List<string> primaryFont,
                    float fontScale)
                {
                    PrimaryFont = primaryFont;
                    FontScale = fontScale;
                }
            }

            public record ThemeSpatial
            {
                public float PanelThickness;
                public float CornerRadius;
                public float PanelCurvature;
                public string PhysicalityPreset;
                public bool EnableVolumetricShadows;

                public ThemeSpatial() { }

                public ThemeSpatial(
                    float panelThickness,
                    float cornerRadius,
                    float panelCurvature,
                    string physicalityPreset,
                    bool enableVolumetricShadows)
                {
                    PanelThickness = panelThickness;
                    CornerRadius = cornerRadius;
                    PanelCurvature = panelCurvature;
                    PhysicalityPreset = physicalityPreset;
                    EnableVolumetricShadows = enableVolumetricShadows;
                }
            }

            public sealed class UIAudioRoles
            {
                public string? Navigate;   // focus change, cursor move, gaze hop
                public string? Select;     // confirm / primary action
                public string? Back;       // cancel / escape
                public string? Error;      // invalid action
                public string? Warning;    // “hey idiot, careful”
                public string? Success;    // completion, save, done
                public string? Disabled;   // interaction blocked
                public string? Hover;      // subtle, optional

                // Parameterless constructor
                public UIAudioRoles() { }

                // Full constructor
                public UIAudioRoles(
                    string? navigate,
                    string? select,
                    string? back,
                    string? error,
                    string? warning,
                    string? success,
                    string? disabled,
                    string? hover)
                {
                    Navigate = navigate;
                    Select = select;
                    Back = back;
                    Error = error;
                    Warning = warning;
                    Success = success;
                    Disabled = disabled;
                    Hover = hover;
                }
            }

            public sealed class AppAudioRoles
            {
                public string? Launch;
                public string? Close;
                public string? Crash;       // yes, really
                public string? Background;
                public string? Foreground;

                // Parameterless constructor
                public AppAudioRoles() { }

                // Full constructor
                public AppAudioRoles(
                    string? launch,
                    string? close,
                    string? crash,
                    string? background,
                    string? foreground)
                {
                    Launch = launch;
                    Close = close;
                    Crash = crash;
                    Background = background;
                    Foreground = foreground;
                }
            }

            public record ThemeIdentity
            {
                public string ThemeID;
                public string Name;
                public string Author;
                public string Version;
                public List<string> TargetModes;
        

                public ThemeIdentity() { }

                public ThemeIdentity(
                    string themeID,
                    string name,
                    string author,
                    string version,
                    List<string> targetModes)
                {
                    ThemeID = themeID;
                    Name = name;
                    Author = author;
                    Version = version;
                    TargetModes = targetModes;

                }
            }

            public record DefaultApp
            {
                public string AppID;            // Unique identifier
                public string Role;             // Launcher, Calendar, Browser, etc
                public int LaunchPriority;      // Lower = earlier
                public bool AutoStart;          // Should it start automatically
                public string? CustomPanel;     // Optional: panel ID if tied to a panel
                public List<DefaultAppImage> Images;
                public List<ThemeTypography> FontOverrides;
                public List<DefaultAppSound> SoundOverrides;


                public DefaultApp() { }

                public DefaultApp(string appID, string role, int launchPriority, bool autoStart,
                    List<DefaultAppImage> images, List<ThemeTypography> fontOverrides,
                    List<DefaultAppSound> soundOverrides, string? customPanel = null)
                {
                    AppID = appID;
                    Role = role;
                    LaunchPriority = launchPriority;
                    AutoStart = autoStart;
                    CustomPanel = customPanel;
                    Images = images;
                    FontOverrides = fontOverrides;
                    SoundOverrides = soundOverrides;
                }
            
            }

            public record DefaultAppSound
            {
                public string Role;          // Launcher, Calendar, Notification, etc
                public string Path;          // Path to sound file
                public float Volume;         // 0-1
                public float Pitch;          // Optional pitch modification
                public bool IsDefault;

                public DefaultAppSound() { }

                public DefaultAppSound(string role, string path, float volume = 1f, float pitch = 1f, bool isDefault = false)
                {
                    Role = role;
                    Path = path;
                    Volume = volume;
                    Pitch = pitch;
                    IsDefault = isDefault;
                }
            }

            public record DefaultAppImage
            {
                public string Role;          // Launcher, Calendar, Browser, etc
                public string Path;          // Path to image
                public int Width;            // Pixels
                public int Height;           // Pixels
                public float AspectRatio;    // Width / Height
                public bool IsDefault;       // True = primary image for this role

                public DefaultAppImage() { }

                public DefaultAppImage(string role, string path, int width, int height, bool isDefault = false)
                {
                    Role = role;
                    Path = path;
                    Width = width;
                    Height = height;
                    AspectRatio = width / (float)height;
                    IsDefault = isDefault;
                }
            }


            public record XRUIOSTheme
            {
                public ThemeIdentity Identity;
                public ThemeColors Colors;
                public ThemeTypography Typography;
                public ThemeSpatial Spatial;
                public AppAudioRoles AppAudio;
                public UIAudioRoles Audio;
                public List<DefaultApp> Defaults;

                public XRUIOSTheme() { }

                public XRUIOSTheme(
                    ThemeIdentity identity,
                    ThemeColors colors,
                    ThemeTypography typography,
                    ThemeSpatial spatial,
                    AppAudioRoles appAudio,
                    UIAudioRoles audio,
                    List<DefaultApp> defaults)
                {
                    Identity = identity;
                    Colors = colors;
                    Typography = typography;
                    Spatial = spatial;
                    AppAudio = appAudio;
                    Audio = audio;
                    Defaults = defaults;
                }
            }


            internal static XRUIOSTheme CurrentTheme;
            //Keep in mind a XRUIOS theme might completely ignore the values from the themes or ignore them partly


            //C
            //Remember to create a folder with the same name in the directory containing all the assets!

            public static async Task SaveTheme(XRUIOSTheme theme)
            {
                var directoryPath = Path.Combine(DataPath, "Themes");
                var fileName = $"{theme.Identity.Name} v{theme.Identity.Version} by {theme.Identity.Author}, ID {theme.Identity.ThemeID}";

                var filePath = Path.Combine(DataPath, "Themes", fileName);

                if (File.Exists(filePath))
                {
                    throw new InvalidOperationException("This theme already exists; please change the name.");
                }

                var xruiosFile = await BinaryConverter.NCObjectToByteArrayAsync<XRUIOSTheme>(theme);

                await JSONDataHandler.CreateJsonFile(fileName, directoryPath, new JsonObject());

                var json = await JSONDataHandler.LoadJsonFile(directoryPath, fileName);
                json = await JSONDataHandler.AddToJson<byte[]>(json, "Data", xruiosFile, encryptionKey);
                await JSONDataHandler.SaveJson(json);
            }

            //R
            public static async Task<List<XRUIOSTheme>> GetAllXRUIOSThemes()
            {
                List<XRUIOSTheme> Themes = new List<XRUIOSTheme>();

                var directoryPath = Path.Combine(DataPath, "Themes");

                var themePaths = Directory.EnumerateFiles(directoryPath);

                foreach (var item in themePaths)
                {
                    var json = await JSONDataHandler.LoadJsonFile(directoryPath, (Path.GetFileNameWithoutExtension(item)));

                    var bytes = (byte[])await JSONDataHandler.GetVariable<byte[]>(json, "Data", encryptionKey);

                    var themeFile = (XRUIOSTheme)await BinaryConverter.NCByteArrayToObjectAsync<XRUIOSTheme>(bytes);

                    Themes.Add(themeFile);

                }

                return Themes;
            }

            public static async Task<XRUIOSTheme> GetXRUIOSTheme(string FileName)
            {

                var directoryPath = Path.Combine(DataPath, "Themes");

                    var json = await JSONDataHandler.LoadJsonFile(directoryPath, FileName);

                    var bytes = (byte[])await JSONDataHandler.GetVariable<byte[]>(json, "Data", encryptionKey);

                    var themeFile = await BinaryConverter.NCByteArrayToObjectAsync<XRUIOSTheme>(bytes);

                return themeFile;
            }
            public static async Task<XRUIOSTheme> GetCurrentTheme(string FileName)
            {
                return CurrentTheme;
            }

            //U

            //Remember to put Identity.Version up
            public static async Task UpdateTheme(XRUIOSTheme theme)
            {
                var directoryPath = Path.Combine(DataPath, "Themes");
                var fileName = $"{theme.Identity.Name} v{theme.Identity.Version} by {theme.Identity.Author}, ID {theme.Identity.ThemeID}";

                var filePath = Path.Combine(DataPath, "Themes", fileName);

                if (!File.Exists(filePath))
                {
                    throw new InvalidOperationException("This theme does not exist.");
                }

                var xruiosFile = await BinaryConverter.NCObjectToByteArrayAsync<XRUIOSTheme>(theme);

                var json = await JSONDataHandler.LoadJsonFile(directoryPath, fileName);
                json = await JSONDataHandler.UpdateJson<byte[]>(json, "Data", xruiosFile, encryptionKey);
                await JSONDataHandler.SaveJson(json);
            }

            public static async Task SetTheme(string FileName)
            {
                CurrentTheme = await GetXRUIOSTheme(FileName);

            }


            //D
            public static async Task DeleteXRUIOSTheme(string FileName)
            {
                var directoryPath = Path.Combine(DataPath, "Themes");
                var filePath = Path.Combine(DataPath, "Themes", FileName);

                File.Delete(filePath);
            }


        }

        //Flavors

        public class Color
        {
            public int R = 0;
            public int G = 0;
            public int B = 0;
            public int A = 0;

            public Color(int r, int g, int b, int a)
            {
                this.R = r;
                this.G = g;
                this.B = b;
                this.A = a;
            }

            public static readonly Color Red = new Color(255, 0, 0, 255);
            public static readonly Color Green = new Color(0, 255, 0, 255);
            public static readonly Color Blue = new Color(0, 0, 255, 255);
            public static readonly Color White = new Color(255, 255, 255, 255);
            public static readonly Color Black = new Color(0, 0, 0, 255);
            public static readonly Color Yellow = new Color(255, 255, 0, 255);
            public static readonly Color Cyan = new Color(0, 255, 255, 255);
            public static readonly Color Magenta = new Color(255, 0, 255, 255);
            public static readonly Color Gray = new Color(128, 128, 128, 255);
            public static readonly Color DarkGray = new Color(64, 64, 64, 255);
            public static readonly Color LightGray = new Color(192, 192, 192, 255);
            public static readonly Color Orange = new Color(255, 165, 0, 255);
            public static readonly Color Pink = new Color(255, 192, 203, 255);
            public static readonly Color Purple = new Color(128, 0, 128, 255);
            public static readonly Color Brown = new Color(139, 69, 19, 255);
            public static readonly Color Maroon = new Color(128, 0, 0, 255);
            public static readonly Color Olive = new Color(128, 128, 0, 255);
            public static readonly Color Navy = new Color(0, 0, 128, 255);
            public static readonly Color Teal = new Color(0, 128, 128, 255);
            public static readonly Color Lime = new Color(0, 255, 0, 255);
            public static readonly Color Gold = new Color(255, 215, 0, 255);
            public static readonly Color Silver = new Color(192, 192, 192, 255);
            public static readonly Color Coral = new Color(255, 127, 80, 255);
            public static readonly Color Salmon = new Color(250, 128, 114, 255);
            public static readonly Color Indigo = new Color(75, 0, 130, 255);
            public static readonly Color Violet = new Color(238, 130, 238, 255);
            public static readonly Color SkyBlue = new Color(135, 206, 235, 255);
            public static readonly Color DeepSkyBlue = new Color(0, 191, 255, 255);
            public static readonly Color Transparent = new Color(0, 0, 0, 0);

            public static readonly Color CyberpunkYellow = new Color(255, 230, 0, 255);    // Neon yellow
            public static readonly Color JohnnySilver = new Color(192, 192, 192, 255);  // Metallic silver
            public static readonly Color VergilBlue = new Color(50, 90, 255, 255);    // Devil May Cry Vergil blue
            public static readonly Color GyroGreen = new Color(0, 200, 120, 255);    // JoJo Gyro green
            public static readonly Color ValentinePurple = new Color(150, 0, 200, 255);    // Rell Valentine purple
            public static readonly Color BondrewdBlack = new Color(20, 20, 30, 255);     // Deep black w/ blue tint
            public static readonly Color KingCrimson = new Color(220, 20, 60, 255);    // Crimson red
            public static readonly Color MayanoOrange = new Color(255, 140, 0, 255);    // Bright orange
            public static readonly Color KillerQueenPink = new Color(255, 105, 180, 255);  // Hot pink
            public static readonly Color RiasRed = new Color(200, 0, 50, 255);     // Rias Gremory red
            public static readonly Color GordonOrange = new Color(255, 100, 0, 255);    // Gordon Freeman orange
            public static readonly Color ZaWaurdoYellow = new Color(255, 255, 50, 255);   // Bright yellow
            public static readonly Color ChiefGreen = new Color(0, 100, 0, 255);      // Master Chief armor green
            public static readonly Color UndyneBlue = new Color(0, 90, 255, 255);     // Strong blue
            public static readonly Color SusiePurple = new Color(160, 50, 180, 255);   // Deep purple

            public static readonly Color KazumaBrown = new Color(139, 69, 19, 255);    // Dark brown
            public static readonly Color BatmanBlack = new Color(10, 10, 10, 255);     // Jet black
            public static readonly Color SaberYellow = new Color(255, 215, 0, 255);    // Golden yellow
            public static readonly Color ShiroOrange = new Color(255, 120, 40, 255);   // Warm orange
            public static readonly Color GokuBlack = new Color(60, 0, 60, 255);      // Dark purple-black

            public static readonly Color MakishimaWhite = new Color(240, 240, 240, 255);  // Soft white
            public static readonly Color UltraInstinctWhite = new Color(255, 255, 255, 255);  // Bright white
            public static readonly Color TheDrinkPurple = new Color(128, 0, 255, 255);    // Neon purple




        }


        //This is just for XR but let's allow desktops so people can do some pretty cool stuff!
        public class AreaManager
        {

        }
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                        









        //This will interface with C++ later, for now it's datatypes
        public class DesktopMirrors
        {
            public class Monitors //For uDesktopDuplication, specifically 
            {
                public record Monitor
                {

                    public int MonitorNumber;

                    public bool InvertedX;

                    public bool InvertedY;

                    public Monitor() { }

                    public Monitor(int monitornumber, bool invertedX, bool invertedY)
                    {
                        this.MonitorNumber = monitornumber;
                        this.InvertedX = invertedX;
                        this.InvertedY = invertedY;
                    }

                }

            }

            public class Apps //For uWindowsCapture, specifically Uwc Window Texture.cs
            {

                public enum RenderingMode { OnlyWhenVisible, AllFrames };
                public enum CapturePriority
                {
                    High = 0,
                    Middle = 1,
                    Low = 2,
                };

                public enum CaptureMode
                {
                    None = -1,
                    PrintWindow = 0,
                    BitBlt = 1,
                    NativeToOS = 2,
                    Auto = 3,
                };

                public record App
                {

                    public string AppPath;

                    public CaptureMode CaptureAPI;

                    public CapturePriority Priority;

                    public int CaptureFPS;

                    //We will draw the cursor whenever the user's raycast is on it

                    public RenderingMode RenderingModeType;

                    //Create Child Windows is always open

                    //We always do searches when the parameter changes

                    public string PartialAppTitle;

                    public App() { }

                    public App(string appPath, CaptureMode captureAPI, CapturePriority priority, int captureFPS, RenderingMode renderingModeType, string partialAppTitle)
                    {
                        this.AppPath = appPath;
                        this.CaptureAPI = captureAPI;
                        this.Priority = priority;
                        this.CaptureFPS = captureFPS;
                        this.RenderingModeType = renderingModeType;
                        this.PartialAppTitle = partialAppTitle;
                    }



                }



            }


        }

#if Test

        public class DataSlotClass
        {
            #region structs

            public record Session
            {
                public byte[] SavedSession; //The actual save data for our session, this can be a gameobj holding an entire scene or a single 3D object
                public string SaveSessionType; //Our type of Save Session, this is for organization and the user can create their own, in realtime we just check the parent object and decide accordingly!
                public bool IsFavorite; //If this is favorited
                public string DateAndTime; //The date and time
                public string Title; //Title
                public string Description; //Description

                public Session() { }


                public Session(byte[] SavedSession, string SaveSessionType, bool IsFavorite, string DateTimeVar,
                    string Title, string Description, string UserPassword)

                {
                    this.SavedSession = SavedSession;
                    this.SaveSessionType = SaveSessionType;
                    this.IsFavorite = IsFavorite;
                    var timenow = DateTime.Now;
                    string temptimenowasstring = default;

                    //If we are using DecryptSession we just give the value decrypted, however if this is a new one we will set the datetime
                    if (DateTimeVar == default)
                    {
                        temptimenowasstring = timenow.ToString("yyyy-MM-dd//THH:mm:ss//Z");
                    }
                    else
                    {
                        temptimenowasstring = DateTimeVar;
                    }
                    var timenowasstring = temptimenowasstring;
                    this.DateAndTime = timenowasstring;
                    this.Title = Title;
                    this.Description = Description;



                }
            }

            public struct DataSlot
            {
                public bool IsFavorite; //If this is favorited
                public string DateAndTime; //The date and time it was made
                public string Title; //Title
                public string Description; //Description
                public string ImgPath; //The path to the img icon
                public string TextureFolder; //2.5D images for previewing, for v2
                public List<WorldPoint> WorldPoints;

                public DataSlot(bool IsFavorite, string DateTimeVar,
                        string Title, string Description, string ImgPath, string UserPassword, List<string> ObjectsAndTransforms, string TextureFolder, Dictionary<string, Color> Categories,
                        List<WorldPoint> structWorldPoints)

                {
                    this.IsFavorite = IsFavorite;
                    var timenow = DateTime.Now;
                    string temptimenowasstring = default;

                    //If we are using DecryptSession we just give the value decrypted, however if this is a new one we will set the datetime
                    if (DateTimeVar == default)
                    {
                        temptimenowasstring = timenow.ToString("yyyy-MM-dd//THH:mm:ss//Z");
                    }
                    else
                    {
                        temptimenowasstring = DateTimeVar;
                    }
                    var timenowasstring = temptimenowasstring;
                    this.DateAndTime = timenowasstring;
                    this.Title = Title;
                    this.Description = Description;
                    this.ImgPath = ImgPath;




                    this.TextureFolder = default; //I'm a dumbass i'll make it later I hate how stupid I am I bet someone else could do this in one sitting, maybe 5 minutes

                    this.WorldPoints = structWorldPoints;


                }
            }

            public struct WorldPoint
            {
                public DesktopMirrors.Apps.RenderingMode RenderingMode;
                public object PointData; // Point data
                public string PointName;
                public string PointDescription;
                public string PointImagePath;
                public bool UserCentric; // Is this a point which is at a fixed point relative to the user
                public List<Objects.DecryptedStaticObject> StaticObjs;
                public List<Objects.App> AppObjs;
                public List<Objects.DesktopScreen> DesktopScreenObjs;
                public List<Objects.StaciaItems> StaciaObjs;

                public WorldPoint(DesktopMirrors.Apps.RenderingMode renderingMode, object pointData, string pointName, string pointDescription, string pointImagePath,
                    bool userCentric, List<Objects.DecryptedStaticObject> staticObjs, List<Objects.App> appObjs,
                    List<Objects.DesktopScreen> desktopScreenObjs, List<Objects.StaciaItems> staciaObjs)
                {
                    RenderingMode = renderingMode;
                    PointData = pointData;
                    PointName = pointName;
                    PointDescription = pointDescription;
                    PointImagePath = pointImagePath;
                    UserCentric = userCentric;
                    StaticObjs = staticObjs;
                    AppObjs = appObjs;
                    DesktopScreenObjs = desktopScreenObjs;
                    StaciaObjs = staciaObjs;
                }
            }

 
            #endregion



 

   
            public struct AppInfo
            {

                //I can make some upgrades

                public Apptype AppType;

                public string AppPath; //In the case of a base app, this is an EXE file or anything that can be ran. In the case of a XRUIOS app, this is a resource.
                public string AppName; //Taken from the name of the EXE or Resource

                public System.Drawing.Icon AppIcon; //Basically the icon set for the resource/EXE file.
                public Dictionary<string, GameObject> WidgetsPath; //A widget for this program. In the case of a XRUIOS app, this is not needed since it should be within the resources as a folder named "Widgets". However, this is an option for EXE apps. You can make a pipeline and showcase information.

                public Dictionary<String, GameObject> Pages; //What should be opened when a specific name type is called, can work for all XRUIOS apps and some EXE apps.

                public Dictionary<string, SceneData.DecryptedSession> SavedSessions;
                //Save session data for an app. Works with all XRUIOS only, in future will find a way to add for EXE files.

                public GameObject BackgroundProcess; //The singular file allowed to run at the back of a XRUIOS runtime. Can't do much about EXE files but the XRUIOS can be ran.

                //Also this file can not be tampered with. It is updated on the start of a user's runtime as well.

                public AppInfo(string AppPath, string AppName, Dictionary<string, GameObject> WidgetsPath, Dictionary<String, GameObject> Pages, Dictionary<string, SceneData.DecryptedSession> SavedSessions, GameObject BackgroundProcess, string UserPassword)
                {
                    //Let's start by setting our first variable, AKA our selected datatype
                    this.AppPath = AppPath;
                    var appextension = Path.GetExtension(AppPath);
                    Apptype temp = default;
                    if (appextension == ".exe")
                    {
                        temp = Apptype.BaseOS;
                    }

                    if (appextension == ".xur")
                    {
                        temp = Apptype.BaseOS;
                    }

                    this.AppType = temp;
                    //Let's make a few variable containers!
                    string tempappname = default;
                    System.Drawing.Icon tempappicon = default;
                    Dictionary<string, GameObject> tempwidgetspath = default;
                    Dictionary<string, GameObject> temppages = default;
                    Dictionary<string, SceneData.DecryptedSession> tempsavedsessions = default;
                    GameObject tempbackgroundprocess = default;






                    //Now we will create a switch, depending on our apptype
                    switch (AppType)
                    {
                        case Apptype.XRUIOS: //If this is a mod, this is the location of a resource
                                             //Get 
                            ModInfo xurapp = ModInfo.Load(AppName); //CHECK THIS LATER

                            //The app name
                            tempappname = xurapp.name;
                            //The resource icon, taken by System.Drawing
                            tempappicon = default; //Do this later
                                                   //The widgets path, you would still need to use the permissions system but you could pull data from an app if you know how to or if you saved it to a file or something
                            tempwidgetspath = WidgetsPath;
                            //Specific pages for a program, as an example the bookmrks or history page of OperaGX. We don't touch for now.
                            temppages = Pages;
                            //Saved Session Details, we don't touch this (yet) and for XRUIOS only
                            tempsavedsessions = SavedSessions;
                            //Background Process, for XRUIOS only. The singular BG process should be linked to this.
                            tempbackgroundprocess = BackgroundProcess;
                            break;



                        case Apptype.BaseOS: //If this is a XRUIOS app
                                             //The app name is the name of the EXE file
                            tempappname = Path.GetFileNameWithoutExtension(AppPath);

                            //The app icon, taken by System.Drawing
                            tempappicon = Icon.ExtractAssociatedIcon(AppPath);
                            //The widgets path, you would still need to use the permissions system but you could pull data from an app if you know how to or if you saved it to a file or something
                            tempwidgetspath = WidgetsPath;
                            //Specific pages for a program, as an example the bookmrks or history page of OperaGX. We don't touch for now.
                            temppages = Pages;
                            //Saved Session Details, we don't touch this (yet) and for XRUIOS only
                            tempsavedsessions = SavedSessions;
                            //Background Process, for XRUIOS only. The singular BG process should be linked to this.
                            tempbackgroundprocess = BackgroundProcess;

                            break;
                    }

                    this.AppName = tempappname; //Gets this automatically
                    this.AppIcon = tempappicon; //Gets this automatically
                    this.WidgetsPath = tempwidgetspath;
                    this.Pages = temppages;
                    this.SavedSessions = tempsavedsessions;
                    this.BackgroundProcess = tempbackgroundprocess;
                }
            }

            public class Objects //Exception to struct rule, this is a chunk of structs
            {
                public enum PositionalTrackingMode { Follow, Anchored, FollowingExternal }
                public enum RotationalTrackingMode { Static, LAM }
                public enum ObjectOSLabel { Default, Software, Objects, Voice, Music, Alerts, Ui, Other }

                //For referncing objects, they are imported using modtool but are at a different list. 

                //Naming = Name.AR/VR


                public struct DecryptedStaticObject
                {
                    public PositionalTrackingMode PTrackingType;
                    public RotationalTrackingMode RTrackingType;
                    public string Name; // Path to the object
                    public Vector3 SpatialData;
                    public ObjectOSLabel ObjectLabel;

                    public DecryptedStaticObject(PositionalTrackingMode pTrackingType, RotationalTrackingMode rTrackingType, string name, Vector3 spatialData, ObjectOSLabel objectLabel)
                    {
                        PTrackingType = pTrackingType;
                        RTrackingType = rTrackingType;
                        Name = name;
                        SpatialData = spatialData;
                        ObjectLabel = objectLabel;
                    }
                }

                public struct App
                {
                    public PositionalTrackingMode PTrackingType;
                    public RotationalTrackingMode RTrackingType;
                    public AppInfo WindowsAppInfo;
                    public DesktopMirrors.Apps.App MainAppData;
                    public Vector3 SpatialData;
                    public ObjectOSLabel ObjectLabel;

                    public App(PositionalTrackingMode pTrackingType, RotationalTrackingMode rTrackingType, AppInfo windowsAppInfo, DesktopMirrors.Apps.App mainAppData, Vector3 spatialData, ObjectOSLabel objectLabel)
                    {
                        PTrackingType = pTrackingType;
                        RTrackingType = rTrackingType;
                        WindowsAppInfo = windowsAppInfo;
                        MainAppData = mainAppData;
                        SpatialData = spatialData;
                        ObjectLabel = objectLabel;
                    }
                }

                public struct DesktopScreen
                {
                    public PositionalTrackingMode PTrackingType;
                    public RotationalTrackingMode RTrackingType;
                    public DesktopMirrors.Monitors.Monitor DesktopData;
                    public Vector3 SpatialData;
                    public ObjectOSLabel ObjectLabel;

                    public DesktopScreen(PositionalTrackingMode pTrackingType, RotationalTrackingMode rTrackingType, DesktopMirrors.Monitors.Monitor desktopData, Vector3 spatialData, ObjectOSLabel objectLabel)
                    {
                        PTrackingType = pTrackingType;
                        RTrackingType = rTrackingType;
                        DesktopData = desktopData;
                        SpatialData = spatialData;
                        ObjectLabel = objectLabel;
                    }
                }

                public struct StaciaItems
                {
                    public PositionalTrackingMode PTrackingType;
                    public RotationalTrackingMode RTrackingType;
                    public string BinaryData;
                    public Vector3 SpatialData;
                    public ObjectOSLabel ObjectLabel;

                    public StaciaItems(PositionalTrackingMode pTrackingType, RotationalTrackingMode rTrackingType, string binaryData, Vector3 spatialData, ObjectOSLabel objectLabel)
                    {
                        PTrackingType = pTrackingType;
                        RTrackingType = rTrackingType;
                        BinaryData = binaryData;
                        SpatialData = spatialData;
                        ObjectLabel = objectLabel;
                    }
                }












            }


        }

#endif
        public class DateClass
        {


        }

        public class Apps
            {

            }





















            public async Task InitiateFileSync(List<string> SongDirectory)
            {
                //First, let's get 
            }


            public List<string> SongDirectories { get; set; }


             
   





            public async Task RemoveHashSetFromCollection(string filePath)
            {

                var PathTitle = Path.GetDirectoryName(filePath);

                string SongOverviewFile = Path.Combine(DataPath, "Music", "Overviews", PathTitle);
                string SongDetailedFile = Path.Combine(DataPath, "Music", "Details", PathTitle);
                string HashsetsFile = Path.Combine(DataPath, "Music", "Hashsets", PathTitle);

                Path.GetDirectoryName(SongOverviewFile);




            }

            public async Task AddHashSetToCollection(string filePath)
            {
                string SongOverviewFile = Path.Combine(DataPath, "Music", "Overviews");
                string SongDetailedFile = Path.Combine(DataPath, "Music", "Details");
                string HashsetsFile = Path.Combine(DataPath, "Music", "Hashsets");



            }

            public async Task UpdateCollection(string filePath)
            {
                string SongOverviewFile = Path.Combine(DataPath, "Music", "Overviews");
                string SongDetailedFile = Path.Combine(DataPath, "Music", "Details");
                string HashsetsFile = Path.Combine(DataPath, "Music", "Hashsets");



            }

            public async Task UpdateMedia(string SongPath)
            {
                string SongOverview = Path.Combine(DataPath, "Music", "Overviews");
                string SongDetailed = Path.Combine(DataPath, "Music", "Details");
                string Hashsets = Path.Combine(DataPath, "Music", "Hashsets");
                string Images = Path.Combine(DataPath, "Music", "Images");
            }


            public async Task Update(List<string> SongDirectory)
            {

                foreach (var SongLibrary in SongDirectory)

                {

                    string SongOverview = Path.Combine(DataPath, "Music", "Overviews");
                    string SongDetailed = Path.Combine(DataPath, "Music", "Details");
                    string Hashsets = Path.Combine(DataPath, "Music", "Hashsets");
                    string Images = Path.Combine(DataPath, "Music", "Images");




                }


            }










        }





    }
