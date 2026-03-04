using EclipseProject;
using XRUIOS.Barebones;
using static Pariah_Cybersecurity.DataHandler;
using static XRUIOS.Barebones.Interfaces.VolumeClass;
using static XRUIOS.Barebones.XRUIOS;

namespace XRUIOS.Barebones.Functions
{
    public class VolumeClass : XRUIOSFunction
    {
        public override string FunctionName => "Volume";
        public static readonly VolumeClass Instance = new();
        private VolumeClass() { }

        //AudioGroupClass deleted, this is pretty much that but better tbh
        //Remember we use te master volume in SoundEQ as a main volume switch!

      



        //Volumes are Dictionary<string, Dictionary<string, int>>
        //AKA Dictionary<Default, Dictionary<11234:Renderer:Calendar, 50>>
        //In the string, we format it as (MachineID:OSStyle:AppName OR AppID)

        internal static ObservableProperty<VolumeSetting> VolumeSettings;

        //CurrentSoundSetting (Get, Set)
        [SeaOfDirac("VolumeClass.GetCurrentVolumeSettings", null, typeof(Task<VolumeSetting>))]
        public static async Task<VolumeSetting> GetCurrentVolumeSettings()
        {
            return VolumeSettings;
        }

        [SeaOfDirac("VolumeClass.SetCurrentVolumeSettings", new[] { "soundSetting" }, typeof(Task), typeof(VolumeSetting))]
        public static async Task SetCurrentVolumeSettings(VolumeSetting soundSetting)
        {
            VolumeSettings.Set(soundSetting);
        }

        //Volume Settings (CRUD)
        //C
        [SeaOfDirac("VolumeClass.AddVolumeSettings", new[] { "VolumeMixings" }, typeof(Task), typeof(VolumeSetting))]
        public static async Task AddVolumeSettings(VolumeSetting VolumeMixings)
        {
            var directoryPath = Path.Combine(DataPath, "VolumeMixings");

            //Get the JSON File holding the MusicDirectory object for the user
            var FileWithVolumeSettingDB = await JSONDataHandler.LoadJsonFile(directoryPath, "VolumeMixings");
            var loaded = (List<VolumeSetting>)await JSONDataHandler.GetVariable<List<VolumeSetting>>(FileWithVolumeSettingDB, "VolumeMixings", encryptionKey);

            if (loaded.Any(d => d.GetHashCode() == VolumeMixings.GetHashCode()))
            {
                throw new InvalidOperationException("This already exists.");
            }

            loaded.Add(VolumeMixings);

            var updatedJSON = await JSONDataHandler.UpdateJson<List<VolumeSetting>>(FileWithVolumeSettingDB, "VolumeMixings", loaded, encryptionKey);

            await JSONDataHandler.SaveJson(updatedJSON);

        }

        //R
        [SeaOfDirac("VolumeClass.GetVolumeSettings", null, typeof(Task<List<VolumeSetting>>))]
        public static async Task<List<VolumeSetting>> GetVolumeSettings()
        {
            var directoryPath = Path.Combine(DataPath, "VolumeMixings");

            //Get the JSON File holding the MusicDirectory object for the user
            var FileWithVolumeSettingDB = await JSONDataHandler.LoadJsonFile(directoryPath, "VolumeMixings");
            var loaded = (List<VolumeSetting>)await JSONDataHandler.GetVariable<List<VolumeSetting>>(FileWithVolumeSettingDB, "VolumeMixings", encryptionKey);

            return loaded;
        }

        [SeaOfDirac("VolumeClass.GetVolumeSetting", new[] { "volumeSettingName" }, typeof(Task<VolumeSetting>), typeof(string))]
        public static async Task<VolumeSetting> GetVolumeSetting(string volumeSettingName)
        {
            var directoryPath = Path.Combine(DataPath, "VolumeMixings");

            var FileWithVolumeSettingDB = await JSONDataHandler.LoadJsonFile(directoryPath, "VolumeMixings");
            var loaded = (List<VolumeSetting>)await JSONDataHandler.GetVariable<List<VolumeSetting>>(FileWithVolumeSettingDB, "VolumeMixings", encryptionKey);
            var returnVal = loaded.FirstOrDefault(d => d.VolumeSettingName == volumeSettingName);

            return returnVal;
        }

        //App name and stuff/int for this device
        [SeaOfDirac("VolumeClass.GetVolumeSettingsForThisDevice", null, typeof(Task<List<ValueTuple<string, int>>>))]
        public static async Task<List<(string, int)>> GetVolumeSettingsForThisDevice()
        {

            var finalLine = new List<(string, int)>();


            var valToCheck = await GetCurrentVolumeSettings();

            foreach (var item in valToCheck.Volumes.Keys)
            {
                var split = item.Split(":");
                if (split[0] == Environment.MachineName)
                {
                    finalLine.Add((item, valToCheck.Volumes[item]));
                }
            }
            return finalLine;
        }

        //U
        [SeaOfDirac("VolumeClass.UpdateVolumeSettingDB", new[] { "originalData", "newData" }, typeof(Task), typeof(VolumeSetting), typeof(VolumeSetting))]
        public static async Task UpdateVolumeSettingDB(VolumeSetting originalData, VolumeSetting newData)
        {
            var directoryPath = Path.Combine(DataPath, "VolumeMixings");

            //Get the JSON File holding the MusicDirectory object for the user
            var FileWithVolumeSettingDB = await JSONDataHandler.LoadJsonFile(directoryPath, "VolumeMixings");
            var loaded = (List<VolumeSetting>)await JSONDataHandler.GetVariable<List<VolumeSetting>>(FileWithVolumeSettingDB, "VolumeMixings", encryptionKey);

            if (!loaded.Any(d => d.GetHashCode() == originalData.GetHashCode()))
            {
                throw new InvalidOperationException("This does not exist as a saved, stored item.");
            }

            var dataToRemove = loaded.First(d => d.GetHashCode() == originalData.GetHashCode());
            {
                loaded.Remove(dataToRemove);
                loaded.Add(newData);
            }

            var updatedJSON = await JSONDataHandler.UpdateJson<List<VolumeSetting>>(FileWithVolumeSettingDB, "VolumeMixings", loaded, encryptionKey);

            await JSONDataHandler.SaveJson(FileWithVolumeSettingDB);
        }


        //D
        [SeaOfDirac("VolumeClass.DeleteVolumeSettingDB", new[] { "deletedData" }, typeof(Task), typeof(VolumeSetting))]
        public static async Task DeleteVolumeSettingDB(VolumeSetting deletedData)
        {
            var directoryPath = Path.Combine(DataPath, "VolumeMixings");

            //Get the JSON File holding the MusicDirectory object for the user
            var FileWithVolumeSettingDB = await JSONDataHandler.LoadJsonFile(directoryPath, "VolumeMixings");
            var loaded = (List<VolumeSetting>)await JSONDataHandler.GetVariable<List<VolumeSetting>>(FileWithVolumeSettingDB, "VolumeMixings", encryptionKey);

            if (!loaded.Any(d => d.GetHashCode() == deletedData.GetHashCode()))
            {
                throw new InvalidOperationException("This does not exist as a saved, stored item.");
            }

            var dataToRemove = loaded.First(d => d.GetHashCode() == deletedData.GetHashCode());
            {
                loaded.Remove(dataToRemove);
            }

            var updatedJSON = await JSONDataHandler.UpdateJson<List<VolumeSetting>>(FileWithVolumeSettingDB, "VolumeMixings", loaded, encryptionKey);

            await JSONDataHandler.SaveJson(FileWithVolumeSettingDB);
        }

        [SeaOfDirac("VolumeClass.ClearEQDB", null, typeof(Task))]
        public static async Task ClearEQDB()
        {
            var directoryPath = Path.Combine(DataPath, "VolumeMixings");

            //Get the JSON File holding the MusicDirectory object for the user
            var FileWithVolumeSettingDB = await JSONDataHandler.LoadJsonFile(directoryPath, "VolumeMixings");
            var loaded = (List<VolumeSetting>)await JSONDataHandler.GetVariable<List<VolumeSetting>>(FileWithVolumeSettingDB, "VolumeMixings", encryptionKey);

            loaded.Clear();
            var updatedJSON = await JSONDataHandler.UpdateJson<List<VolumeSetting>>(FileWithVolumeSettingDB, "VolumeMixings", loaded, encryptionKey);

            await JSONDataHandler.SaveJson(FileWithVolumeSettingDB);
        }


    }

}
