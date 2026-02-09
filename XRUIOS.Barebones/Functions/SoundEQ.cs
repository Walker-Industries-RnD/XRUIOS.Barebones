using static Pariah_Cybersecurity.DataHandler;
using static XRUIOS.Barebones.ExperimentalAudioClass;
using static XRUIOS.Barebones.XRUIOS;

namespace XRUIOS.Barebones
{
    public static class SoundEQClass
    {
        public record SoundEQ
        {
            public string EQName;
            public float Software;
            public float Effects;
            public float Voice;
            public float Music;
            public float Alerts;
            public float UI;
            public float Etc;
            public ExperimentalAudio OtherVol;

            public SoundEQ() { }

            public SoundEQ(string eqname, float software, float effects, float voice, float music, float alerts, float ui, float etc, ExperimentalAudio otherVol)
            {
                EQName = eqname;
                Software = software;
                Effects = effects;
                Voice = voice;
                Music = music;
                Alerts = alerts;
                UI = ui;
                Etc = etc;
                OtherVol = otherVol;

            }
        }

        internal static ObservableProperty<SoundEQ> CurrentSoundSetting;

        //CurrentSoundSetting (Get, Set)
        public static async Task<SoundEQ> GetCurrentEQ()
        {
            return CurrentSoundSetting;
        }

        public static async Task SetCurrentEQ(SoundEQ soundSetting)
        {
            CurrentSoundSetting.Set(soundSetting);
        }



        //General EQDB Info
        //C
        public static async Task AddSoundEQDBs(SoundEQ EQDBData)
        {
            var directoryPath = Path.Combine(DataPath, "EQDB");

            //Get the JSON File holding the MusicDirectory object for the user
            var FileWithSoundEQDB = await JSONDataHandler.LoadJsonFile(directoryPath, "EQDB");
            var loaded = (List<SoundEQ>)await JSONDataHandler.GetVariable<List<SoundEQ>>(FileWithSoundEQDB, "EQDBData", encryptionKey);

            if (loaded.Any(d => d.GetHashCode() == EQDBData.GetHashCode()))
            {
                throw new InvalidOperationException("This already exists.");
            }

            if (loaded.Any(d => d.EQName == EQDBData.EQName))
            {
                throw new InvalidOperationException("This already exists.");
            }

            loaded.Add(EQDBData);

            var updatedJSON = await JSONDataHandler.UpdateJson<List<SoundEQ>>(FileWithSoundEQDB, "EQDBData", loaded, encryptionKey);

            await JSONDataHandler.SaveJson(updatedJSON);

        }

        //R
        public static async Task<List<SoundEQ>> GetSoundEQDBs()
        {
            var directoryPath = Path.Combine(DataPath, "EQDB");

            //Get the JSON File holding the MusicDirectory object for the user
            var FileWithSoundEQDB = await JSONDataHandler.LoadJsonFile(directoryPath, "EQDB");
            var loaded = (List<SoundEQ>)await JSONDataHandler.GetVariable<List<SoundEQ>>(FileWithSoundEQDB, "EQDBData", encryptionKey);

            return loaded;
        }


        public static async Task<SoundEQ> GetSoundEQDB(string eqDBName)
        {
            var directoryPath = Path.Combine(DataPath, "EQDB");

            //Get the JSON File holding the MusicDirectory object for the user
            var FileWithSoundEQDB = await JSONDataHandler.LoadJsonFile(directoryPath, "EQDB");
            var loaded = (List<SoundEQ>)await JSONDataHandler.GetVariable<List<SoundEQ>>(FileWithSoundEQDB, "EQDBData", encryptionKey);
            var returnVal = loaded.FirstOrDefault(d => d.EQName == eqDBName);

            return returnVal;
        }


        //U
        public static async Task UpdateSoundEQDB(SoundEQ originalData, SoundEQ newData)
        {
            var directoryPath = Path.Combine(DataPath, "EQDB");

            //Get the JSON File holding the MusicDirectory object for the user
            var FileWithSoundEQDB = await JSONDataHandler.LoadJsonFile(directoryPath, "EQDB");
            var loaded = (List<SoundEQ>)await JSONDataHandler.GetVariable<List<SoundEQ>>(FileWithSoundEQDB, "EQDBData", encryptionKey);

            if (!loaded.Any(d => d.EQName == originalData.EQName))
            {
                throw new InvalidOperationException("This does not exist as a saved, stored item.");
            }

            var dataToRemove = loaded.First(d => d.GetHashCode() == originalData.GetHashCode());
            {
                loaded.Remove(dataToRemove);
                loaded.Add(newData);
            }

            var updatedJSON = await JSONDataHandler.UpdateJson<List<SoundEQ>>(FileWithSoundEQDB, "EQDBData", loaded, encryptionKey);

            await JSONDataHandler.SaveJson(FileWithSoundEQDB);
        }


        //D
        public static async Task DeleteSoundEQDB(SoundEQ deletedData)
        {
            var directoryPath = Path.Combine(DataPath, "EQDB");

            //Get the JSON File holding the MusicDirectory object for the user
            var FileWithSoundEQDB = await JSONDataHandler.LoadJsonFile(directoryPath, "EQDB");
            var loaded = (List<SoundEQ>)await JSONDataHandler.GetVariable<List<SoundEQ>>(FileWithSoundEQDB, "EQDBData", encryptionKey);

            if (!loaded.Any(d => d.GetHashCode() == deletedData.GetHashCode()))
            {
                throw new InvalidOperationException("This does not exist as a saved, stored item.");
            }

            var dataToRemove = loaded.First(d => d.GetHashCode() == deletedData.GetHashCode());
            {
                loaded.Remove(dataToRemove);
            }

            var updatedJSON = await JSONDataHandler.UpdateJson<List<SoundEQ>>(FileWithSoundEQDB, "EQDBData", loaded, encryptionKey);

            await JSONDataHandler.SaveJson(FileWithSoundEQDB);
        }

        public static async Task ClearEQDB()
        {
            var directoryPath = Path.Combine(DataPath, "EQDB");

            //Get the JSON File holding the MusicDirectory object for the user
            var FileWithSoundEQDB = await JSONDataHandler.LoadJsonFile(directoryPath, "EQDB");
            var loaded = (List<SoundEQ>)await JSONDataHandler.GetVariable<List<SoundEQ>>(FileWithSoundEQDB, "EQDBData", encryptionKey);

            loaded.Clear();
            var updatedJSON = await JSONDataHandler.UpdateJson<List<SoundEQ>>(FileWithSoundEQDB, "EQDBData", loaded, encryptionKey);

            await JSONDataHandler.SaveJson(FileWithSoundEQDB);
        }




        //Default EQDB Info (Get/Set)

        //Set
        public static async Task SetDefaultEQDB(SoundEQ EQDBData)
        {
            var directoryPath = Path.Combine(DataPath, "EQDB");

            //Get the JSON File holding the MusicDirectory object for the user
            var FileWithSoundEQDB = await JSONDataHandler.LoadJsonFile(directoryPath, "EQDB");
            var loaded = (SoundEQ)await JSONDataHandler.GetVariable<SoundEQ>(FileWithSoundEQDB, "DefaultEQDB", encryptionKey);

            var updatedJSON = await JSONDataHandler.UpdateJson<SoundEQ>(FileWithSoundEQDB, "DefaultEQDB", EQDBData, encryptionKey);

            await JSONDataHandler.SaveJson(updatedJSON);

        }

        //Get
        public static async Task<SoundEQ> GetDefaultEQDB()
        {
            var directoryPath = Path.Combine(DataPath, "EQDB");

            //Get the JSON File holding the MusicDirectory object for the user
            var FileWithSoundEQDB = await JSONDataHandler.LoadJsonFile(directoryPath, "EQDB");
            var loaded = (SoundEQ)await JSONDataHandler.GetVariable<SoundEQ>(FileWithSoundEQDB, "DefaultEQDB", encryptionKey);

            return loaded;
        }

        //Reset

        public static async Task ResetDefaultEQDB()
        {
            var directoryPath = Path.Combine(DataPath, "EQDB");

            var fancyoptions = new ExperimentalAudio(false, false, 0, 0);
            var input = new SoundEQ(default, 100, 100, 100, 100, 100, 100, 100, fancyoptions);

            //Get the JSON File holding the MusicDirectory object for the user
            var FileWithSoundEQDB = await JSONDataHandler.LoadJsonFile(directoryPath, "EQDB");
            var loaded = (SoundEQ)await JSONDataHandler.GetVariable<SoundEQ>(FileWithSoundEQDB, "DefaultEQDB", encryptionKey);

            var updatedJSON = await JSONDataHandler.UpdateJson<SoundEQ>(FileWithSoundEQDB, "DefaultEQDB", loaded, encryptionKey);

            await JSONDataHandler.SaveJson(updatedJSON);

        }

    }
}
        