using XRUIOS.Barebones.Functions;

namespace XRUIOS.Barebones
{
    public abstract class XRUIOSFunction
    {
        public abstract string FunctionName { get; }

        public static readonly List<XRUIOSFunction> Functions = new()
        {
            AlarmClass.Instance,
            AppClass.Instance,
            AreaManagerClass.Instance,
            CalendarClass.Instance,
            ChronoClass.Instance,
            ClipboardClass.Instance,
            CreatorClass.Instance,
            DataManagerClass.Instance,
            ExperimentalAudioClass.Instance,
            FacadeClass.Instance,
            GeoClass.Instance,
            MediaAlbumClass.Instance,
            MusicPlayerClass.Instance,
            NoteClass.Instance,
            NotificationClass.Instance,
            ProcessesClass.Instance,
            RecentlyRecordedClass.Instance,
            Songs.Instance,
            SoundEQClass.Instance,
            StopwatchClass.Instance,
            SystemInfoDisplayClass.Instance,
            ThemeSystem.Instance,
            TimerManagerClass.Instance,
            VolumeClass.Instance,
            WorldEventsClass.Instance,
        };
    }
}
