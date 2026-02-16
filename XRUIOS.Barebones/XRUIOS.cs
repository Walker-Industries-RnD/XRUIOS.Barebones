using Pariah_Cybersecurity;
using Standart.Hash.xxHash;
using System.Collections.ObjectModel;
using System.Text;
using System.Text.Json.Nodes;
using WISecureData;
using static XRUIOS.Barebones.XRUIOS.Yuuko.App;

namespace XRUIOS.Barebones
{
    public class XRUIOS
    {
        public static string DataPath { get; private set; } = Path.Combine(Directory.GetCurrentDirectory(), "XRUIOS");
        public static string PublicDataPath { get; private set; } = Path.Combine(Directory.GetCurrentDirectory(), "XRUIOSPublic");
        public static SecureData encryptionKey = "Test".ToSecureData();

        ObservableProperty<string> MainSong;
        ObservableCollection<string> MainSongQueue;

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

        public class Yuuko
        {
            public class Bindings
            {
                public sealed class DirectoryManager
                {
                    private readonly string _bindingsFolder;
                    private readonly Dictionary<string, DirectoryBinding> _bindings = new();

                    internal string ThisDeviceId => Environment.MachineName;

                    public void UpdateBindingInMemory(string uuid, Yuuko.Bindings.DirectoryBinding newBinding)
                    {
                        if (_bindings.ContainsKey(uuid))
                        {
                            _bindings[uuid] = newBinding;
                        }
                    }

                    public DirectoryManager(string bindingsFolder)
                    {
                        _bindingsFolder = Path.GetFullPath(bindingsFolder);
                        Directory.CreateDirectory(_bindingsFolder);
                    }

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

                    public IEnumerable<DirectoryBinding> GetAllBindings() => _bindings.Values;

                    public DirectoryBinding? GetBindingById(string uuid)
                    {
                        _bindings.TryGetValue(uuid, out var binding);
                        return binding;
                    }

                    public async Task<string?> GetDirectoryById(string directoryId, string? defaultFolder = null, CancellationToken ct = default)
                    {
                        if (!_bindings.TryGetValue(directoryId, out var binding)) return null;

                        if (binding.Ref.OriginalDevice == ThisDeviceId && Directory.Exists(binding.Ref.FullPath))
                            return binding.Ref.FullPath;

                        if (defaultFolder == null) return null;

                        var resolvedPath = ResolveDirectory(binding, defaultFolder);
                        await SaveBinding(binding, ct);

                        return resolvedPath;
                    }

                    public async Task<(string Uuid, string ResolvedPath)> GetOrCreateDirectory(string fullPath, string? localOverride = null, CancellationToken ct = default)
                    {
                        var directoryRef = new DirectoryRef(fullPath, ThisDeviceId);

                        if (!_bindings.TryGetValue(directoryRef.DirectoryId, out var binding))
                        {
                            binding = new DirectoryBinding(directoryRef);
                            _bindings[directoryRef.DirectoryId] = binding;
                        }

                        var resolvedPath = ResolveDirectory(binding, localOverride);
                        await SaveBinding(binding, ct);

                        return (directoryRef.DirectoryId, resolvedPath);
                    }

                    public string ResolveDirectory(DirectoryBinding binding, string? defaultFolder = null)
                    {
                        if (Directory.Exists(binding.Ref.FullPath))
                        {
                            binding.SetResolution(new DirectoryResolution(binding.Ref.FullPath, verified: true));
                            return binding.Ref.FullPath;
                        }

                        var pathToUse = defaultFolder ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "XRUIOS", binding.Ref.DirectoryId);
                        Directory.CreateDirectory(pathToUse);
                        binding.SetResolution(new DirectoryResolution(pathToUse, verified: true));

                        return pathToUse;
                    }

                    public async Task<bool> DeleteBinding(string directoryId, CancellationToken ct = default)
                    {
                        var bindingPath = Path.Combine(_bindingsFolder, directoryId + ".binding");
                        if (!File.Exists(bindingPath)) return false;

                        File.Delete(bindingPath);
                        _bindings.Remove(directoryId);
                        return true;
                    }

                    public async Task<bool> UpdateBinding(string directoryId, DirectoryResolution newResolution, CancellationToken ct = default)
                    {
                        if (!_bindings.TryGetValue(directoryId, out var binding)) return false;

                        binding.SetResolution(newResolution);
                        await SaveBinding(binding, ct);
                        return true;

                    }

                    private async Task SaveBinding(DirectoryBinding binding, CancellationToken ct)
                    {
                        string path = Path.Combine(_bindingsFolder, binding.Ref.DirectoryId + ".binding");
                        var data = await BinaryConverter.NCObjectToByteArrayAsync(binding, cancellationToken: ct);
                        await File.WriteAllBytesAsync(path, data, ct);
                    }
                }

                public sealed class DirectoryBinding
                {
                    public DirectoryRef Ref { get; private set; }  // private setter
                    public DirectoryResolution? Resolution { get; private set; }

                    public DirectoryBinding() { }

                    public DirectoryBinding(DirectoryRef directoryRef)
                    {
                        Ref = directoryRef;
                    }

                    public void SetResolution(DirectoryResolution resolution) => Resolution = resolution;
                    public void ClearResolution() => Resolution = null;

                    public void SetRef(DirectoryRef newRef) => Ref = newRef;  // <-- add this
                }


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
                        string canonical = $"{device}|{Path.GetFullPath(path)}";
                        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(canonical));
                        ulong hash = await xxHash64.ComputeHashAsync(stream, bufferSize: 81920, seed: HashSeed);
                        return hash.ToString("X16");
                    }
                }

                public record DirectoryResolution
                {
                    public string ResolvedPath { get; set; }
                    public DateTime LastVerifiedUtc { get; set; }
                    public bool Verified { get; set; }

                    public DirectoryResolution() { }

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
                public record Overview(string YuukoAppName, string MinimumYuukoVersion, string DeveloperID, Dictionary<string, byte[]> PublicKey, string UUID, string AppName, string Description, string PrimaryPlatform);
                public record Parameter(string Name, Type ParamType, bool Required, object? Default, List<object>? Choices);
                public record Event(string Name, string Description, List<Parameter>? Parameters, string? Command);
                public record OSSpecificApp(string OS, string Version, string Download, List<Panel> PanelInfo);
                public record Panel(string Description, List<RenderStyle> Renders);
                public record RenderStyle(string Mode, string View);
                public record PreferredPlatformMapping(string? Mode2D, string? Mode3D, string? ModeCMD);
                public record YuukoApp(Overview YuukoInfo, Dictionary<string, PreferredPlatformMapping> Defaults, List<OSSpecificApp> OSSet, Dictionary<string, List<Event>> OSSpecificEntrypoints, List<Event> SharedEntrypoints);

                public static async Task CreateSign() { }
            }

            public record Handle(
                int? Desktop = null,
                string? LocalApp = null,
                YuukoApp? YuukoApp = null,
                string? AppPath = null,
                byte[]? BinaryData = null,
                DirectoryRecord? DirectoryRef = null,
                string DeviceOrigin = "",
                string? DefaultCommand = null
            );
        }

        public sealed record ResolvedMedia(string FullPath, string FileName, string DirectoryUuid, long SizeBytes, DateTime LastModifiedUtc);

        public static class Media
        {
            private static readonly Yuuko.Bindings.DirectoryManager _manager = new Yuuko.Bindings.DirectoryManager(Path.Combine(DataPath, "Generic"));

            static Media() => _manager.LoadBindings().GetAwaiter().GetResult();

            public static async Task<ResolvedMedia> GetFile(string directoryUuid, string fileName, CancellationToken ct = default)
            {
                if (string.IsNullOrWhiteSpace(directoryUuid)) throw new ArgumentException("Invalid directory UUID.", nameof(directoryUuid));
                if (string.IsNullOrWhiteSpace(fileName)) throw new ArgumentException("Invalid file name.", nameof(fileName));

                if (Path.IsPathRooted(fileName) || fileName.Contains(Path.DirectorySeparatorChar) || fileName.Contains(Path.AltDirectorySeparatorChar))
                    throw new InvalidOperationException("Invalid media file name.");

                var directory = await _manager.GetDirectoryById(directoryUuid, ct: ct);
                if (directory == null) throw new DirectoryNotFoundException("Media directory could not be resolved.");

                var fullPath = Path.Combine(directory, fileName);
                var info = new FileInfo(fullPath);
                if (!info.Exists) throw new FileNotFoundException("Media file not found.", fileName);

                return new ResolvedMedia(info.FullName, info.Name, directoryUuid, info.Length, info.LastWriteTimeUtc);
            }

            public static async Task<XRUIOS.DirectoryRecord?> GetOrCreateDirectory(string fullFilePath, string? directory, string? directoryName, CancellationToken ct = default)
            {
                var (resolved, unresolved) = await GetGenericDirectories();
                var match = resolved.Concat(unresolved).FirstOrDefault(r => string.Equals(r.Path, directory, StringComparison.OrdinalIgnoreCase));
                if (match != null) return match;

                await AddGenericDirectory(directory!, directoryName ?? Guid.NewGuid().ToString());

                (resolved, unresolved) = await GetGenericDirectories();
                return resolved.Concat(unresolved).FirstOrDefault(r => string.Equals(r.Path, directory, StringComparison.OrdinalIgnoreCase));
            }

            public static async Task<XRUIOS.DirectoryRecord> AddGenericDirectory(string directory, string directoryName)
            {
                var bankFilePath = Path.Combine(DataPath, "Generic", "GenericBank.json");

                if (!File.Exists(bankFilePath))
                {
                    await DataHandler.JSONDataHandler.CreateJsonFile("GenericBank", Path.Combine(DataPath, "Generic"), new JsonObject());
                    var json = await DataHandler.JSONDataHandler.LoadJsonFile("GenericBank", Path.Combine(DataPath, "Generic"));
                    json = await DataHandler.JSONDataHandler.AddToJson<List<DirectoryRecord>>(json, "Data", new List<DirectoryRecord>(), encryptionKey);
                    await DataHandler.JSONDataHandler.SaveJson(json);
                }

                var directoryFile = await DataHandler.JSONDataHandler.LoadJsonFile("GenericBank", Path.Combine(DataPath, "Generic"));
                var directories = (List<DirectoryRecord>)await DataHandler.JSONDataHandler.GetVariable<List<DirectoryRecord>>(directoryFile, "Data", encryptionKey);

                var (uuid, resolvedPath) = await _manager.GetOrCreateDirectory(directory);

                var existing = directories.FirstOrDefault(d => d.UUID == uuid);
                if (existing != null) return existing;

                var newRecord = new DirectoryRecord(uuid, directoryName, resolvedPath);
                directories.Add(newRecord);

                var editedJSON = await DataHandler.JSONDataHandler.UpdateJson<List<DirectoryRecord>>(directoryFile, "Data", directories, encryptionKey);
                await DataHandler.JSONDataHandler.SaveJson(editedJSON);

                return newRecord;
            }

            public static async Task<(List<DirectoryRecord>, List<DirectoryRecord>)> GetGenericDirectories()
            {
                var directoryFile = await DataHandler.JSONDataHandler.LoadJsonFile("GenericBank", Path.Combine(DataPath, "Generic"));
                var directories = (List<DirectoryRecord>)await DataHandler.JSONDataHandler.GetVariable<List<DirectoryRecord>>(directoryFile, "Data", encryptionKey);

                var resolved = new List<DirectoryRecord>();
                var unresolved = new List<DirectoryRecord>();

                foreach (var dir in directories)
                {
                    var path = await _manager.GetDirectoryById(dir.UUID);
                    if (path != null)
                        resolved.Add(dir with { Path = path });
                    else
                        unresolved.Add(dir);
                }

                return (resolved, unresolved);
            }

            public static async Task UpdateGenericDirectory(string uuid, string newDirectory, string newDirectoryName)
            {
                // Load JSON
                var directoryFile = await DataHandler.JSONDataHandler.LoadJsonFile("GenericBank", Path.Combine(DataPath, "Generic"));
                var directories = (List<DirectoryRecord>)await DataHandler.JSONDataHandler.GetVariable<List<DirectoryRecord>>(directoryFile, "Data", encryptionKey);

                // Find the record
                var recordIndex = directories.FindIndex(d => d.UUID == uuid);
                if (recordIndex == -1)
                {
                    Console.WriteLine($"No record found for UUID {uuid}.");
                    return;
                }

                // Update JSON record
                directories[recordIndex] = directories[recordIndex] with
                {
                    Path = newDirectory,
                    PathName = newDirectoryName
                };

                // Update binding safely
                var binding = _manager.GetBindingById(uuid);
                if (binding != null)
                {
                    // Update Resolution
                    binding.SetResolution(new Yuuko.Bindings.DirectoryResolution(newDirectory, verified: true));

                    // Update Ref properly
                    var newRef = new Yuuko.Bindings.DirectoryRef(newDirectory, Environment.MachineName);
                    binding.SetRef(newRef);  // <-- now works cleanly
                }

                // Save updated JSON
                var editedJSON = await DataHandler.JSONDataHandler.UpdateJson<List<DirectoryRecord>>(directoryFile, "Data", directories, encryptionKey);
                await DataHandler.JSONDataHandler.SaveJson(editedJSON);
            }






            public static async Task RemoveGenericDirectory(string uuid, bool deleteDirectory = true)
            {
                var directoryFile = await DataHandler.JSONDataHandler.LoadJsonFile("GenericBank", Path.Combine(DataPath, "Generic"));
                var directories = (List<DirectoryRecord>)await DataHandler.JSONDataHandler.GetVariable<List<DirectoryRecord>>(directoryFile, "Data", encryptionKey);

                var record = directories.FirstOrDefault(d => d.UUID == uuid);
                if (record == null)
                {
                    Console.WriteLine($"No record found for UUID {uuid}.");
                    return;
                }

                // Get path FIRST
                string? currentPath = await _manager.GetDirectoryById(uuid);

                if (deleteDirectory && currentPath != null && Directory.Exists(currentPath))
                {
                    Directory.Delete(currentPath, true);
                }

                // Now remove binding
                await _manager.DeleteBinding(uuid);

                // Remove JSON record
                directories.Remove(record);
                var editedJSON = await DataHandler.JSONDataHandler.UpdateJson<List<DirectoryRecord>>(directoryFile, "Data", directories, encryptionKey);
                await DataHandler.JSONDataHandler.SaveJson(editedJSON);
            }

        }
    }
}
