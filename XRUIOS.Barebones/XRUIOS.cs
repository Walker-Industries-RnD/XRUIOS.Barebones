using Pariah_Cybersecurity;
using Standart.Hash.xxHash;
using System.Collections.ObjectModel;
using System.Text;
using WISecureData;
using static XRUIOS.Barebones.XRUIOS.Yuuko.App;


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


            public FileRecord(string? uuid, string file)
            {
                UUID = uuid ?? Guid.NewGuid().ToString();
                File = file;
            }
        }


        //This will go in a separate DLL later
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

                    public YuukoApp() { }

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

            public record Handle
            {
                // Only one of these will be non-null at a time
                public int? Desktop;
                public string? LocalApp;
                public YuukoApp? YuukoApp;
                public string? AppPath;
                public byte[]? BinaryData;
                public DirectoryRecord? DirectoryRef; // References a folder for previews/media

                public string DeviceOrigin; // Which device this lives on
                public string? DefaultCommand;

                public Handle() { }

                public Handle(
                    int? desktop = null,
                    string? localApp = null,
                    YuukoApp? yuukoApp = null,
                    string? appPath = null,
                    byte[]? binaryData = null,
                    DirectoryRecord? directoryRef = null,
                    string deviceOrigin = "",
                    string? defaultCommand = null)
                {
                    Desktop = desktop;
                    LocalApp = localApp;
                    YuukoApp = yuukoApp;
                    AppPath = appPath;
                    BinaryData = binaryData;
                    DirectoryRef = directoryRef;
                    DeviceOrigin = deviceOrigin;
                    DefaultCommand = defaultCommand;
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
                    returnVal = Directories.Item1.FirstOrDefault(b => b.Path == directory);
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

















        //Fml







    }





}
