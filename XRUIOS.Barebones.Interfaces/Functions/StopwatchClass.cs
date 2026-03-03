
namespace XRUIOS.Barebones.Interfaces
{
    public static class StopwatchClass
    {

        public record StopwatchRecord
        {
            public int LapCount { get; set; }
            public int SecondsElapsed { get; set; }

            public StopwatchRecord() { }

            public StopwatchRecord(int lapCount, int secondsElapsed)
            {
                LapCount = lapCount;
                SecondsElapsed = secondsElapsed;
            }
        }

        public record StopwatchEntry
        {
            public string Name { get; set; }
            public long SegmentStartTimestamp { get; set; }
            public TimeSpan AccumulatedTime { get; set; }
            public bool IsRunning { get; set; }
            public List<StopwatchRecord> Laps { get; set; }

            public StopwatchEntry() { }
        }


    }


}
