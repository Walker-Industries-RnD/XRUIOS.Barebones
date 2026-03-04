using EclipseProject;
using CsvHelper;
using System.Diagnostics;
using System.Globalization;
using static XRUIOS.Barebones.Interfaces.StopwatchClass;
using static XRUIOS.Barebones.XRUIOS;

namespace XRUIOS.Barebones
{
    public class StopwatchClass : XRUIOSFunction
    {
        public override string FunctionName => "Stopwatch";
        public static readonly StopwatchClass Instance = new();
        private StopwatchClass() { }

        public static Dictionary<string, StopwatchEntry> StopWatches = new Dictionary<string, StopwatchEntry>();

        [SeaOfDirac("StopwatchClass.CreateStopwatch", null, typeof(string))]
        public static string CreateStopwatch()
        {
            return CreateStopwatch(string.Empty);
        }

        [SeaOfDirac("StopwatchClass.CreateStopwatch", new[] { "name" }, typeof(string), typeof(string))]
        public static string CreateStopwatch(string name)
        {
            string id = Guid.NewGuid().ToString("N");

            StopWatches.Add(id, new StopwatchEntry
            {
                Name = name,
                SegmentStartTimestamp = Stopwatch.GetTimestamp(),
                AccumulatedTime = TimeSpan.Zero,
                IsRunning = true,
                Laps = new List<StopwatchRecord>()
            });

            return id;
        }

        [SeaOfDirac("StopwatchClass.GetActiveStopwatches", null, typeof(ValueTuple<int, List<string>>))]
        public static (int Count, List<string> IDs) GetActiveStopwatches()
        {
            var ids = new List<string>(StopWatches.Keys);
            return (ids.Count, ids);
        }

        [SeaOfDirac("StopwatchClass.GetTimeElapsed", new[] { "id" }, typeof(TimeSpan), typeof(string))]
        public static TimeSpan GetTimeElapsed(string id)
        {
            try
            {
                var entry = StopWatches[id];
                if (entry.IsRunning)
                    return entry.AccumulatedTime + Stopwatch.GetElapsedTime(entry.SegmentStartTimestamp);
                return entry.AccumulatedTime;
            }
            catch
            {
                throw new InvalidOperationException("A Stopwatch with this ID does not exist.");
            }
        }

        [SeaOfDirac("StopwatchClass.PauseStopwatch", new[] { "id" }, typeof(void), typeof(string))]
        public static void PauseStopwatch(string id)
        {
            var entry = StopWatches[id];
            if (!entry.IsRunning) return;

            entry.AccumulatedTime += Stopwatch.GetElapsedTime(entry.SegmentStartTimestamp);
            entry.IsRunning = false;
        }

        [SeaOfDirac("StopwatchClass.ResumeStopwatch", new[] { "id" }, typeof(void), typeof(string))]
        public static void ResumeStopwatch(string id)
        {
            var entry = StopWatches[id];
            if (entry.IsRunning) return;

            entry.SegmentStartTimestamp = Stopwatch.GetTimestamp();
            entry.IsRunning = true;
        }

        [SeaOfDirac("StopwatchClass.IsStopwatchRunning", new[] { "id" }, typeof(bool), typeof(string))]
        public static bool IsStopwatchRunning(string id)
        {
            try
            {
                return StopWatches[id].IsRunning;
            }
            catch
            {
                throw new InvalidOperationException("A Stopwatch with this ID does not exist.");
            }
        }

        [SeaOfDirac("StopwatchClass.CreateLap", new[] { "id" }, typeof(StopwatchRecord), typeof(string))]
        public static StopwatchRecord CreateLap(string id)
        {
            var entry = StopWatches[id];
            var lapCount = entry.Laps.Count;
            var currentTime = (int)GetTimeElapsed(id).TotalSeconds;

            var newStopwatchRecord = new StopwatchRecord(lapCount, currentTime);

            entry.Laps.Add(newStopwatchRecord);

            return newStopwatchRecord;
        }

        [SeaOfDirac("StopwatchClass.DestroyStopwatch", new[] { "id" }, typeof(List<StopwatchRecord>), typeof(string))]
        public static List<StopwatchRecord> DestroyStopwatch(string id)
        {
            var records = StopWatches[id].Laps;
            StopWatches.Remove(id);
            return records;
        }

        [SeaOfDirac("StopwatchClass.SaveStopwatchValuesAsSheet", new[] { "Values", "RecordedOn", "FileName" }, typeof(void), typeof(List<StopwatchRecord>), typeof(DateTime), typeof(string))]
        public static void SaveStopwatchValuesAsSheet(List<StopwatchRecord> Values, DateTime RecordedOn, string FileName)
        {
            var directoryPath = Path.Combine(DataPath, $"{FileName}____RecordedOn_{RecordedOn:yyyy-MM-dd_HH-mm-ss}.csv");

            using (var writer = new StreamWriter(directoryPath))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(Values);
            }
        }



    }


}
