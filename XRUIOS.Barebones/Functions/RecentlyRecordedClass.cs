using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Text;
using static Pariah_Cybersecurity.DataHandler;
using static Walker.Crypto.SimpleAESEncryption;
using static XRUIOS.Barebones.XRUIOS;

namespace XRUIOS.Barebones
{
    public static class RecentlyRecordedClass
    {

        //Basically get and set with an add function

        public static async Task<List<FileRecord>> GetRecentlyRecorded()
        {
            var directoryPath = Path.Combine(DataPath, "RecentlyRecorded");

            var FileWithRecentlyRecorded= await JSONDataHandler.LoadJsonFile(directoryPath, "RecentlyRecorded");
            var loaded = (List<FileRecord>)await JSONDataHandler.GetVariable<List<FileRecord>>(FileWithRecentlyRecorded, "RecentlyRecorded", encryptionKey);

            return loaded;
        }

        public static async Task AddToRecentlyRecorded(FileRecord NewlyRecorded)
        {
            var directoryPath = Path.Combine(DataPath, "RecentlyRecorded");

            //Get the JSON File holding the MusicDirectory object for the user
            var FileWithRecentlyRecorded = await JSONDataHandler.LoadJsonFile(directoryPath, "RecentlyRecorded");
            var loaded = (List<FileRecord>)await JSONDataHandler.GetVariable<List<FileRecord>>(FileWithRecentlyRecorded, "RecentlyRecorded", encryptionKey);

            if (loaded.Count >= 30)
            {
                loaded.RemoveAt(29);
            }

            loaded.Add(NewlyRecorded);

            var updatedJSON = await JSONDataHandler.UpdateJson<List<FileRecord>>(FileWithRecentlyRecorded, "RecentlyRecorded", loaded, encryptionKey);

            await JSONDataHandler.SaveJson(updatedJSON);

        }

        public static async Task DeleteSoundRecentlyRecorded(FileRecord deletedData)
        {
            var directoryPath = Path.Combine(DataPath, "RecentlyRecorded");

            //Get the JSON File holding the MusicDirectory object for the user
            var FileWithRecentlyRecorded = await JSONDataHandler.LoadJsonFile(directoryPath, "RecentlyRecorded");
            var loaded = (List<FileRecord>)await JSONDataHandler.GetVariable<List<FileRecord>>(FileWithRecentlyRecorded, "RecentlyRecorded", encryptionKey);

            if (!loaded.Any(d => d.GetHashCode() == deletedData.GetHashCode()))
            {
                throw new InvalidOperationException("This does not exist as a saved, stored item.");
            }

            var dataToRemove = loaded.First(d => d.GetHashCode() == deletedData.GetHashCode());
            {
                loaded.Remove(dataToRemove);
            }

            var updatedJSON = await JSONDataHandler.UpdateJson<List<FileRecord>>(FileWithRecentlyRecorded, "RecentlyRecorded", loaded, encryptionKey);

            await JSONDataHandler.SaveJson(FileWithRecentlyRecorded);
        }

        public static async Task ClearRecentlyRecorded()
        {
            var directoryPath = Path.Combine(DataPath, "RecentlyRecorded");

            //Get the JSON File holding the MusicDirectory object for the user
            var FileWithRecentlyRecorded = await JSONDataHandler.LoadJsonFile(directoryPath, "RecentlyRecorded");
            var loaded = (List<FileRecord>)await JSONDataHandler.GetVariable<List<FileRecord>>(FileWithRecentlyRecorded, "RecentlyRecorded", encryptionKey);

            loaded.Clear();
            var updatedJSON = await JSONDataHandler.UpdateJson<List<FileRecord>>(FileWithRecentlyRecorded, "RecentlyRecorded", loaded, encryptionKey);

            await JSONDataHandler.SaveJson(FileWithRecentlyRecorded);
        }





    }

}
