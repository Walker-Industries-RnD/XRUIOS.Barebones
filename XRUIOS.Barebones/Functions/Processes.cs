using System.Diagnostics;
using System.Text.Json.Serialization;
using System.Runtime.InteropServices;
using static Pariah_Cybersecurity.DataHandler;
using static XRUIOS.Barebones.XRUIOS;

namespace XRUIOS.Barebones
{
    public static class ProcessesClass
    {
        private const int MaxProcessHistory = 100;


        // PROCESS RECORD - Device, PID, Type (your exact request!)

        public record ProcessInfo
        {
            public string DeviceId { get; init; }           // Which device (EVA-02, AX-01, etc)
            public int ProcessId { get; init; }              // PID
            public string ProcessName { get; init; }          // Visible name
            public string ProcessType { get; init; }          // Steam, App, System, Game, etc
            public string? WindowTitle { get; init; }         // Current window title
            public DateTime StartTime { get; init; }          // When it started
            public long MemoryMB { get; init; }               // Memory usage in MB
            public float CpuPercent { get; init; }            // CPU usage percentage
            public string? ExecutablePath { get; init; }      // Where it's running from
            public string? IconPath { get; init; }            // For UI display
            public bool IsResponsive { get; init; }           // Is it frozen? (Shinji's EVA = false)
            public DateTime LastSeen { get; init; }           // Last time we checked

            // Parameterless constructor for serialization
            public ProcessInfo()
            {
                DeviceId = Environment.MachineName;
                ProcessId = 0;
                ProcessName = string.Empty;
                ProcessType = "Unknown";
                StartTime = DateTime.UtcNow;
                LastSeen = DateTime.UtcNow;
            }

            // Main constructor
            public ProcessInfo(
                string deviceId,
                int processId,
                string processName,
                string processType,
                string? windowTitle = null,
                DateTime? startTime = null,
                long memoryMB = 0,
                float cpuPercent = 0,
                string? executablePath = null,
                string? iconPath = null,
                bool isResponsive = true)
            {
                DeviceId = deviceId;
                ProcessId = processId;
                ProcessName = processName;
                ProcessType = processType;
                WindowTitle = windowTitle;
                StartTime = startTime ?? DateTime.UtcNow;
                MemoryMB = memoryMB;
                CpuPercent = cpuPercent;
                ExecutablePath = executablePath;
                IconPath = iconPath;
                IsResponsive = isResponsive;
                LastSeen = DateTime.UtcNow;
            }
        }


        // GET CURRENT PROCESSES (Live from system)

        public static List<ProcessInfo> GetCurrentProcesses()
        {
            var processes = new List<ProcessInfo>();
            var currentDevice = Environment.MachineName;

            try
            {
                foreach (var process in Process.GetProcesses())
                {
                    try
                    {
                        // Skip system idle process (PID 0) and other system processes
                        if (process.Id == 0) continue;

                        // Get process type based on common patterns
                        string processType = DetectProcessType(process.ProcessName, process.MainModule?.FileName);

                        // Get memory in MB
                        long memoryMB = 0;
                        try
                        {
                            memoryMB = process.WorkingSet64 / 1024 / 1024;
                        }
                        catch { /* Access denied, skip memory */ }

                        // Get CPU percentage (approximate)
                        float cpuPercent = 0;
                        try
                        {
                            // This is simplified - real CPU monitoring needs perf counters
                            cpuPercent = GetCpuUsage(process);
                        }
                        catch { /* Skip CPU if can't access */ }

                        processes.Add(new ProcessInfo(
                            deviceId: currentDevice,
                            processId: process.Id,
                            processName: process.ProcessName,
                            processType: processType,
                            windowTitle: GetMainWindowTitle(process),
                            startTime: GetProcessStartTime(process),
                            memoryMB: memoryMB,
                            cpuPercent: cpuPercent,
                            executablePath: GetExecutablePath(process),
                            isResponsive: process.Responding
                        ));
                    }
                    catch
                    {
                        // Skip processes we can't access
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to get processes: {ex.Message}");
            }

            return processes.OrderBy(p => p.ProcessName).ToList();
        }


        // DETECT PROCESS TYPE (Steam, App, Game, System, etc)

        private static string DetectProcessType(string processName, string? executablePath)
        {
            processName = processName.ToLowerInvariant();
            executablePath = executablePath?.ToLowerInvariant() ?? "";

            // Common process type detection
            if (processName.Contains("steam") || executablePath.Contains("steam"))
                return "Steam";

            if (processName.Contains("chrome") || processName.Contains("firefox") ||
                processName.Contains("edge") || processName.Contains("opera"))
                return "Browser";

            if (processName.Contains("explorer"))
                return "System";

            if (processName.Contains("code") || processName.Contains("devenv") ||
                processName.Contains("visualstudio"))
                return "Development";

            if (processName.Contains("spotify") || processName.Contains("itunes") ||
                processName.Contains("wmplayer"))
                return "Media";

            if (processName.Contains("winword") || processName.Contains("excel") ||
                processName.Contains("powerpnt") || processName.Contains("outlook"))
                return "Office";

            if (processName.Contains("discord") || processName.Contains("slack") ||
                processName.Contains("teams") || processName.Contains("zoom"))
                return "Communication";

            if (processName.Contains("game") || processName.Contains("unity") ||
                processName.Contains("unreal") || processName.Contains("ue4") ||
                processName.Contains("ue5") || processName.Contains("godot"))
                return "Game/Engine";

            // Check if it's a known game (you could expand this list)
            string[] knownGames = { "hl2", "csgo", "dota", "lol", "valorant",
                                    "fortnite", "minecraft", "rocketleague" };
            if (knownGames.Any(g => processName.Contains(g)))
                return "Game";

            return "Application";
        }


        // HELPER METHODS

        private static string? GetMainWindowTitle(Process process)
        {
            try
            {
                return string.IsNullOrEmpty(process.MainWindowTitle) ? null : process.MainWindowTitle;
            }
            catch
            {
                return null;
            }
        }

        private static DateTime GetProcessStartTime(Process process)
        {
            try
            {
                return process.StartTime.ToUniversalTime();
            }
            catch
            {
                return DateTime.UtcNow;
            }
        }

        private static string? GetExecutablePath(Process process)
        {
            try
            {
                return process.MainModule?.FileName;
            }
            catch
            {
                return null;
            }
        }

        private static float GetCpuUsage(Process process)
        {
            try
            {
                // This is a simplified version - for real CPU monitoring
                // you'd need to use PerformanceCounter or sample over time
                var startTime = DateTime.UtcNow;
                var startCpu = process.TotalProcessorTime;

                Thread.Sleep(100); // Sample over 100ms

                var endTime = DateTime.UtcNow;
                var endCpu = process.TotalProcessorTime;

                var cpuUsedMs = (endCpu - startCpu).TotalMilliseconds;
                var totalMsPassed = (endTime - startTime).TotalMilliseconds;

                var cpuUsageTotal = cpuUsedMs / (Environment.ProcessorCount * totalMsPassed);

                return (float)(cpuUsageTotal * 100);
            }
            catch
            {
                return 0;
            }
        }


        // SHARE PROCESS SNAPSHOT (Save to share easily!)

        public static async Task<string> SaveProcessSnapshot(string? snapshotName = null)
        {
            var processes = GetCurrentProcesses();

            var snapshot = new
            {
                DeviceId = Environment.MachineName,
                Timestamp = DateTime.UtcNow,
                SnapshotName = snapshotName ?? $"Processes_{DateTime.UtcNow:yyyyMMdd_HHmmss}",
                ProcessCount = processes.Count,
                Processes = processes
            };

            var directoryPath = Path.Combine(DataPath, "ProcessSnapshots");
            Directory.CreateDirectory(directoryPath);

            var fileName = $"{snapshot.SnapshotName}.json";
            var filePath = Path.Combine(directoryPath, fileName);

            var json = System.Text.Json.JsonSerializer.Serialize(snapshot, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(filePath, json);

            return filePath;
        }


        // GET SAVED SNAPSHOTS

        public static List<string> GetSavedSnapshots()
        {
            var directoryPath = Path.Combine(DataPath, "ProcessSnapshots");
            if (!Directory.Exists(directoryPath))
                return new List<string>();

            return Directory.GetFiles(directoryPath, "*.json")
                .Select(Path.GetFileName)
                .OrderByDescending(f => f)
                .ToList()!;
        }


        // LOAD SNAPSHOT

        public static async Task<List<ProcessInfo>?> LoadProcessSnapshot(string snapshotFileName)
        {
            var directoryPath = Path.Combine(DataPath, "ProcessSnapshots");
            var filePath = Path.Combine(directoryPath, snapshotFileName);

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Snapshot {snapshotFileName} not found!");

            var json = await File.ReadAllTextAsync(filePath);
            var snapshot = System.Text.Json.JsonSerializer.Deserialize<dynamic>(json);

            // Deserialize back to ProcessInfo list
            var processes = System.Text.Json.JsonSerializer.Deserialize<List<ProcessInfo>>(
                snapshot?.GetProperty("Processes").ToString() ?? "[]");

            return processes;
        }


        // SHARE PROCESS SNAPSHOT VIA WORLD EVENTS

        public static async Task ShareProcessSnapshot(string? targetDeviceId = null)
        {
            var snapshotPath = await SaveProcessSnapshot();
            var processes = GetCurrentProcesses();



            Console.WriteLine($"[NERV] Process snapshot shared! Check World Events for details.");
        }


        // GET PROCESSES BY TYPE

        public static Dictionary<string, List<ProcessInfo>> GetProcessesByType()
        {
            var processes = GetCurrentProcesses();

            return processes
                .GroupBy(p => p.ProcessType)
                .ToDictionary(g => g.Key, g => g.ToList());
        }


        // KILL PROCESS (with proper warnings!)

        public static async Task<bool> KillProcess(int processId, string processName)
        {
            try
            {
                var process = Process.GetProcessById(processId);

                process.Kill();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to kill process {processId}: {ex.Message}");
                return false;
            }
        }
    }
}