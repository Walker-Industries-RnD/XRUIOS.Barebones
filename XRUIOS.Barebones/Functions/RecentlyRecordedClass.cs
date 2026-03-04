using EclipseProject;
using static Pariah_Cybersecurity.DataHandler;
using static XRUIOS.Barebones.XRUIOS;
using YuukoProtocol;


namespace XRUIOS.Barebones
{
    public class RecentlyRecordedClass : XRUIOSFunction
    {
        public override string FunctionName => "Recently Recorded";
        public static readonly RecentlyRecordedClass Instance = new();
        private RecentlyRecordedClass() { }

        private const int MaxRecent = 30;

        [SeaOfDirac("RecentlyRecordedClass.GetRecentlyRecorded", null, typeof(Task<List<FileRecord>>))]
        public static async Task<List<FileRecord>> GetRecentlyRecorded()
        {
            var directoryPath = Path.Combine(DataPath, "RecentlyRecorded");
            var file = await JSONDataHandler.LoadJsonFile(directoryPath, "RecentlyRecorded");
            var loaded = (List<FileRecord>)await JSONDataHandler.GetVariable<List<FileRecord>>(file, "RecentlyRecorded", encryptionKey);
            return loaded;
        }

        [SeaOfDirac("RecentlyRecordedClass.AddToRecentlyRecorded", new[] { "newlyRecorded" }, typeof(Task), typeof(FileRecord))]
        public static async Task AddToRecentlyRecorded(FileRecord newlyRecorded)
        {
            var directoryPath = Path.Combine(DataPath, "RecentlyRecorded");
            var file = await JSONDataHandler.LoadJsonFile(directoryPath, "RecentlyRecorded");
            var loaded = (List<FileRecord>)await JSONDataHandler.GetVariable<List<FileRecord>>(file, "RecentlyRecorded", encryptionKey);

            // Remove oldest if over limit
            if (loaded.Count >= MaxRecent)
                loaded.RemoveAt(0);

            loaded.Add(newlyRecorded);

            var updatedJSON = await JSONDataHandler.UpdateJson<List<FileRecord>>(file, "RecentlyRecorded", loaded, encryptionKey);
            await JSONDataHandler.SaveJson(updatedJSON);
        }

        [SeaOfDirac("RecentlyRecordedClass.DeleteSoundRecentlyRecorded", new[] { "deletedData" }, typeof(Task), typeof(FileRecord))]
        public static async Task DeleteSoundRecentlyRecorded(FileRecord deletedData)
        {
            var directoryPath = Path.Combine(DataPath, "RecentlyRecorded");
            var file = await JSONDataHandler.LoadJsonFile(directoryPath, "RecentlyRecorded");
            var loaded = (List<FileRecord>)await JSONDataHandler.GetVariable<List<FileRecord>>(file, "RecentlyRecorded", encryptionKey);

            var item = loaded.FirstOrDefault(d => d.GetHashCode() == deletedData.GetHashCode());
            if (item == null)
                throw new InvalidOperationException("This does not exist as a saved, stored item.");

            loaded.Remove(item);

            var updatedJSON = await JSONDataHandler.UpdateJson<List<FileRecord>>(file, "RecentlyRecorded", loaded, encryptionKey);
            await JSONDataHandler.SaveJson(updatedJSON);
        }

        [SeaOfDirac("RecentlyRecordedClass.ClearRecentlyRecorded", null, typeof(Task))]
        public static async Task ClearRecentlyRecorded()
        {
            var directoryPath = Path.Combine(DataPath, "RecentlyRecorded");
            var file = await JSONDataHandler.LoadJsonFile(directoryPath, "RecentlyRecorded");
            var loaded = (List<FileRecord>)await JSONDataHandler.GetVariable<List<FileRecord>>(file, "RecentlyRecorded", encryptionKey);

            loaded.Clear();
            var updatedJSON = await JSONDataHandler.UpdateJson<List<FileRecord>>(file, "RecentlyRecorded", loaded, encryptionKey);
            await JSONDataHandler.SaveJson(updatedJSON);
        }
    }
}
