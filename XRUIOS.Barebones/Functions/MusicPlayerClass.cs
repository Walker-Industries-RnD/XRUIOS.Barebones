using static XRUIOS.Barebones.Songs;

namespace XRUIOS.Barebones
{
    public class MusicPlayerClass
    {

        internal static Songs.SongOverview? CurrentlyPlaying;
        internal static List<Songs.SongOverview> Queue = new List<Songs.SongOverview>();

        public static class CurrentlyPlayingClass
        {
            //R
            public static SongOverview? GetCurrentlyPlaying()
            {
                return CurrentlyPlaying;
            }

            //U
            public static async Task SetCurrentlyPlaying(string audioFile, string directoryUUID)
            {
                CurrentlyPlaying = await GetOrCreateOverview(audioFile, directoryUUID);
                await MusicHistoryClass.AddToPlayHistory(audioFile, directoryUUID);
            }

            //D
            public static void ResetCurrentlyPlaying()
            {
                CurrentlyPlaying = null;
            }


        }

        public static class MusicQueueClass
        {
            //C
            public static List<SongOverview> GetQueue()
            {
                return Queue;
            }

            //R
            public static async Task AddToMusicQueue(string audioFile, string directoryUUID)
            {
                var song = await GetOrCreateOverview(audioFile, directoryUUID);
                Queue.Add(song);
            }

            //U
            public static async Task ReorderSong(SongOverview item, int newIndex)
            {
                if (item == null)
                    throw new ArgumentNullException(nameof(item), "Song cannot be null.");

                int currentIndex = Queue.IndexOf(item);
                if (currentIndex == -1)
                    throw new InvalidOperationException("Song not found in the queue.");

                // Allow moving to end
                if (newIndex < 0 || newIndex > Queue.Count)
                    throw new ArgumentOutOfRangeException(nameof(newIndex), "Reorder number is out of range.");

                Queue.RemoveAt(currentIndex);

                // If you removed an item before the newIndex and it was after it, shift newIndex down
                if (newIndex > currentIndex)
                    newIndex--;

                Queue.Insert(newIndex, item);
            }


            //D
            public static async Task RemoveSong(SongOverview item)
            {
                Queue.Remove(item);

            }

            public static async Task RemoveSong(int item)
            {
                Queue.RemoveAt(item);

            }


            public static async Task ResetQueue()
            {
                Queue = new List<Songs.SongOverview>();
            }
            //Helper



        }

        //Trying something new
        public static async Task<SongOverview> GetOrCreateOverview(string audioFile, string directoryUUID)
        {
            var overview = await SongClass.GetSongInfo(audioFile, directoryUUID, SongClass.MusicInfoStyle.overview);

            if (overview.Item1 == null)
            {
                await SongClass.CreateSongInfo(audioFile, directoryUUID);
                overview = await SongClass.GetSongInfo(audioFile, directoryUUID, SongClass.MusicInfoStyle.overview);

                if (overview.Item1 == null)
                    throw new InvalidOperationException("The song overview could not be found or created.");
            }

            return (SongOverview)overview.Item1;
        }

        //Convert musicqueue to playlist
        public static class Random
        {
            //Gets random music stuff, will do later
        }


    }

}
