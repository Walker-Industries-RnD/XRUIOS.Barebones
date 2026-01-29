using System;
using System.Collections.Generic;
using System.Text;
using static XRUIOS.Barebones.Songs;

namespace XRUIOS.Barebones
{
    public class MusicPlayerClass
    {

        internal static Songs.SongOverview? CurrentlyPlaying;
        internal static List<Songs.SongOverview> Queue;

        public static class CurrentlyPlayingClass
        {
            //R
            public static SongOverview GetCurrentlyPlaying()
            {
                return CurrentlyPlaying;
            }

            //U
            public static async Task SetCurrentlyPlaying(string audioFile, string directoryUUID)
            {
                var overview = await SongClass.GetSongInfo(audioFile, directoryUUID, SongClass.MusicInfoStyle.overview);

                //Try creating if an overview doesn't exist
                if (overview.Item1 == null)
                {
                    await SongClass.CreateSongInfo(audioFile, directoryUUID);
                    overview = await SongClass.GetSongInfo(audioFile, directoryUUID, SongClass.MusicInfoStyle.overview);
                }

                if (overview.Item1 == null)
                {

                    await SongClass.CreateSongInfo(audioFile, directoryUUID);

                    throw new InvalidOperationException("The song overview could not be found or created.");
                }

                CurrentlyPlaying = (Songs.SongOverview)overview.Item1;

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
            public static List<SongOverview> GetCurrentlyPlaying()
            {
                return Queue;
            }

            //R
            public static async Task AddToMusicQueue(string audioFile, string directoryUUID)
            {
                var overview = await SongClass.GetSongInfo(audioFile, directoryUUID, SongClass.MusicInfoStyle.overview);

                //Try creating if an overview doesn't exist
                if (overview.Item1 == null)
                {
                    await SongClass.CreateSongInfo(audioFile, directoryUUID);
                    overview = await SongClass.GetSongInfo(audioFile, directoryUUID, SongClass.MusicInfoStyle.overview);
                }

                if (overview.Item1 == null)
                {

                    await SongClass.CreateSongInfo(audioFile, directoryUUID);

                    throw new InvalidOperationException("The song overview could not be found or created.");
                }

                Queue.Add((SongOverview)overview.Item1);
            }

            //U
            public static async Task ReorderSong(SongOverview item, int ReorderNumber)
            {
                if (item == null || !Queue.Contains(item) || ReorderNumber < 0 || ReorderNumber >= Queue.Count)
                {
                    throw new InvalidOperationException("The reorder number is invalid.");
                }

                Queue.Remove(item);
                Queue.Insert(ReorderNumber, item);

            }

            //D
            public static async Task RemoveSong(SongOverview item, int ReorderNumber)
            {
                Queue.Remove(item);

            }

            public static async Task ResetQueue()
            {
                Queue = new List<Songs.SongOverview>();
            }

        }
        //Convert musicqueue to playlist
        public static class Random
        {

        }


    }

}
