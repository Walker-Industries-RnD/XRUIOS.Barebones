using Microsoft.VisualBasic;
using Pariah_Cybersecurity;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.Json.Nodes;
using static Pariah_Cybersecurity.DataHandler;
using static XRUIOS.Barebones.XRUIOS;

namespace XRUIOS.Barebones
{
    public class VolumeClass
    {




        public record ExperimentalAudio
        {
            public bool EnvironmentalReduction;
            public bool DecibelLimit;
            public int EnvironmentalReductionPercentage;
            public int DecibelLimitLevel;

            public ExperimentalAudio() { }

            public ExperimentalAudio(bool EnvironmentalReduction, bool DecibelLimit,
                int EnvironmentalReductionPercentage, int DecibelLimitLevel)
            {
                this.EnvironmentalReduction = EnvironmentalReduction;
                this.DecibelLimit = DecibelLimit;
                this.EnvironmentalReductionPercentage = EnvironmentalReductionPercentage;
                this.DecibelLimitLevel = DecibelLimitLevel;
            }
        }

        public static int MasterVolume;
        public static ExperimentalAudio AdvancedAudioSettings;
        public static SoundEQClass EQ;

        //Create "Save to file" and "Load from file" with presets
        public static class ExperimentalVolumeClass
        {

            public static ExperimentalAudio GetExperimentalAudioSettings(ExperimentalAudio tempAudio)
            {
                return AdvancedAudioSettings;
            }

            public static void SetExperimentalAudioSettings(
               bool? EnvironmentalReduction = null,
               bool? DecibelLimit = null,
               int? EnvironmentalReductionPercentage = null,
               int? DecibelLimitLevel = null)
            {

                var updated = new ExperimentalAudio
                {
                    EnvironmentalReduction = EnvironmentalReduction ?? AdvancedAudioSettings.EnvironmentalReduction,
                    DecibelLimit = DecibelLimit ?? AdvancedAudioSettings.DecibelLimit,
                    EnvironmentalReductionPercentage = EnvironmentalReductionPercentage ?? AdvancedAudioSettings.EnvironmentalReductionPercentage,
                    DecibelLimitLevel = DecibelLimitLevel ?? AdvancedAudioSettings.DecibelLimitLevel
                };

                AdvancedAudioSettings = updated;
            }

            public static async Task SaveAudioSettings()
            {
                var directoryPath = Path.Combine(DataPath, "ExpAudio");

                var data = BinaryConverter.NCObjectToByteArrayAsync<ExperimentalAudio>(AdvancedAudioSettings);

                if (File.Exists(Path.Combine(directoryPath + "ExpAudio.JSON")))
                {
                    await JSONDataHandler.CreateJsonFile(directoryPath, "ExpAudio", new JsonObject());
                }

                var json = await JSONDataHandler.LoadJsonFile("ExpAudio", directoryPath);

                if (await JSONDataHandler.CheckIfVariableExists(json, "Data"))
                {
                    json = await JSONDataHandler.UpdateJson<byte[]>(json, "Data", data, encryptionKey);
                }

                else
                {
                    json = await JSONDataHandler.AddToJson<byte[]>(json, "Data", data, encryptionKey);
                }

                await JSONDataHandler.SaveJson(json);


            }

            public static async Task LoadAudioSettings()
            {
                var directoryPath = Path.Combine(DataPath, "ExpAudio");

                var json = await JSONDataHandler.LoadJsonFile("ExpAudio", directoryPath);
                var loaded = (ExperimentalAudio)await JSONDataHandler.GetVariable<ExperimentalAudio>(json, "Data", encryptionKey);

                AdvancedAudioSettings = loaded;

            }


        }

        public static class MasterVolumeClass
        {
            public static int GetMasterVolume()
            {
                return MasterVolume;
            }

            public static void SetMasterVolume(int vol)
            {
                MasterVolume = vol;
            }

            public static async Task SaveAudioSettings()
            {
                var directoryPath = Path.Combine(DataPath, "MasterVol");

                var data = BinaryConverter.NCObjectToByteArrayAsync<int>(MasterVolume);

                if (File.Exists(Path.Combine(directoryPath + "MasterVol.JSON")))
                {
                    await JSONDataHandler.CreateJsonFile(directoryPath, "MasterVol", new JsonObject());
                }

                var json = await JSONDataHandler.LoadJsonFile("MasterVol", directoryPath);

                if (await JSONDataHandler.CheckIfVariableExists(json, "Data"))
                {
                    json = await JSONDataHandler.UpdateJson<byte[]>(json, "Data", data, encryptionKey);
                }

                else
                {
                    json = await JSONDataHandler.AddToJson<byte[]>(json, "Data", data, encryptionKey);
                }

                await JSONDataHandler.SaveJson(json);


            }

            public static async Task LoadAudioSettings()
            {
                var directoryPath = Path.Combine(DataPath, "MasterVol");

                var json = await JSONDataHandler.LoadJsonFile("MasterVol", directoryPath);
                var loaded = (int)await JSONDataHandler.GetVariable<int>(json, "Data", encryptionKey);

                MasterVolume = loaded;

            }
        }



        public class AppVolume
        {
            public void ChangeObjVolume(GameObject obj, int volume)
            {
                var audioSource = obj.GetComponent<AudioSource>();

                // Check if the AudioSource component was found
                if (audioSource != null)
                {
                    // You can now use the 'audioSource' variable to control the audio playback.
                    // For example, you can play the audio clip assigned to the AudioSource:
                    audioSource.volume = volume;
                }
                else
                {
                    Debug.LogError("No AudioSource component found on this GameObject.");
                }
            }
        }

        public static class mainVolume
        {



            static string UserSoundEQDBPath = "caca";

            public static List<SoundEQClass> GetSoundEQDB()
            {

                //Get the JSON File holding the MusicDirectory object for the user
                var FileWithSoundEQDB = UniversalSave.Load(UserSoundEQDBPath, DataFormat.JSON);

                //This file has a lot of things in it, so we are getting the variable called MusicDirectory, aka the object we are looking for
                List<SoundEQClass> target = (List<SoundEQClass>)FileWithSoundEQDB.Get("SoundEQDB");

                List<SoundEQClass> ourlist = new List<SoundEQClass>();

                foreach (SoundEQClass soundEQ in target)
                {
                    ourlist.Add(DecryptSoundEQ(soundEQ));
                }


                return ourlist;
            }

            public static void DeleteFromSoundEQDB(string DBName)
            {

                var ourdb = GetSoundEQDB();


                bool itemexists = false;
                int round = -1;
                foreach (SoundEQClass item in ourdb)
                {
                    round = round + 1;
                    if (item.EQName == DBName)
                    {
                        itemexists = true;
                        break;
                    }
                }

                if (itemexists == true)
                {
                    ourdb.RemoveAt(round);

                    var returnlist = new List<SoundEQClass>();

                    foreach (SoundEQClass item in ourdb)
                    {
                        returnlist.Add(new SoundEQ(item, UserPassword));
                    }

                    var FileWithSoundEQDB = UniversalSave.Load(UserSoundEQDBPath, DataFormat.JSON);

                    FileWithSoundEQDB.Set("MusicQueue", returnlist);

                    UniversalSave.Save(UserSoundEQDBPath, FileWithSoundEQDB);
                }


            }

            public static void UpdateFromSoundEQDB(string DBName, SoundEQClass input)
            {

                var ourdb = GetSoundEQDB();


                bool itemexists = false;
                int round = -1;
                foreach (SoundEQClass item in ourdb)
                {
                    round = round + 1;
                    if (item.EQName == DBName)
                    {
                        itemexists = true;
                        break;
                    }
                }

                if (itemexists == true)
                {
                    ourdb.RemoveAt(round);
                    ourdb.Insert(round, input);

                    var returnlist = new List<SoundEQClass>();

                    foreach (SoundEQClass item in ourdb)
                    {
                        returnlist.Add(new SoundEQ(item, UserPassword));
                    }

                    var FileWithSoundEQDB = DataHandler.JSONDataHandler.LoadJsonFile(UserSoundEQDBPath, DataFormat.JSON);

                    FileWithSoundEQDB.Set("MusicQueue", returnlist);

                    UniversalSave.Save(UserSoundEQDBPath, FileWithSoundEQDB);
                }


            }

            public static void AddToSoundEQDB(SoundEQClass input)
            {

                var ourdb = GetSoundEQDB();


                ourdb.Add(input);

                var returnlist = new List<SoundEQClass>();

                foreach (SoundEQClass item in ourdb)
                {
                    returnlist.Add(new SoundEQ(item, UserPassword));
                }

                var FileWithSoundEQDB = UniversalSave.Load(UserSoundEQDBPath, DataFormat.JSON);

                FileWithSoundEQDB.Set("MusicQueue", returnlist);

                UniversalSave.Save(UserSoundEQDBPath, FileWithSoundEQDB);
            }



            public static SoundEQClass GetDefaultSoundEQ()
            {

                //Get the JSON File holding the MusicDirectory object for the user
                var FileWithSoundEQDB = UniversalSave.Load(UserSoundEQDBPath, DataFormat.JSON);

                //This file has a lot of things in it, so we are getting the variable called MusicDirectory, aka the object we are looking for
                SoundEQClass target = (SoundEQClass)FileWithSoundEQDB.Get("DefaultSoundEQ");

                SoundEQClass item = DecryptSoundEQ(target);

                return item;
            }

            public static SoundEQClass GetUserDefaultSoundEQ()
            {

                //Get the JSON File holding the MusicDirectory object for the user
                var FileWithSoundEQDB = UniversalSave.Load(UserSoundEQDBPath, DataFormat.JSON);

                //This file has a lot of things in it, so we are getting the variable called MusicDirectory, aka the object we are looking for
                SoundEQClass target = (SoundEQClass)FileWithSoundEQDB.Get("UserDefaultSoundEQ");

                SoundEQClass item = DecryptSoundEQ(target);

                return item;
            }

            public static bool CheckIfUserDefaultSoundExists()
            {

                //Get the JSON File holding the MusicDirectory object for the user
                var FileWithSoundEQDB = UniversalSave.Load(UserSoundEQDBPath, DataFormat.JSON);

                //This file has a lot of things in it, so we are getting the variable called MusicDirectory, aka the object we are looking for
                var target = FileWithSoundEQDB.Get("UserDefaultSoundEQ");

                bool ourreturn;

                if (target == null)
                {
                    ourreturn = false;
                }

                else
                {
                    ourreturn = true;
                }

                return ourreturn;
            }

            public static void SetUserDefaultSoundEQ(SoundEQClass input)
            {

                var ourinput = new SoundEQ(input, UserPassword);

                var FileWithSoundEQDB = UniversalSave.Load(UserSoundEQDBPath, DataFormat.JSON);

                FileWithSoundEQDB.Set("UserDefaultSoundEQ", ourinput);

                UniversalSave.Save(UserSoundEQDBPath, FileWithSoundEQDB);


            }

            public static void ResetUserDefaultSoundEQ()
            {

                var fancyoptions = new ExperimentalAudio(false, false, 0, 0);

                var input = new SoundEQClass(default, 100, 100, 100, 100, 100, 100, 100, fancyoptions);

                var ourinput = new SoundEQ(input, UserPassword);

                var FileWithSoundEQDB = UniversalSave.Load(UserSoundEQDBPath, DataFormat.JSON);

                FileWithSoundEQDB.Set("UserDefaultSoundEQ", ourinput);

                UniversalSave.Save(UserSoundEQDBPath, FileWithSoundEQDB);


            }

        }

        public class AudioGroups
        {
            string AudioGroupsPath = "caca";


            public List<AudioGroupClass> GetAllAudioGroups()
            {

                //Get the JSON File holding the MusicDirectory object for the user
                var FileWithAudioGroups = UniversalSave.Load(AudioGroupsPath, DataFormat.JSON);

                //This file has a lot of things in it, so we are getting the variable called MusicDirectory, aka the object we are looking for
                List<AudioGroupClass> target = (List<AudioGroupClass>)FileWithAudioGroups.Get("AudioGroups");

                List<AudioGroupClass> ourlist = new List<AudioGroupClass>();

                foreach (AudioGroupClass audioGroup in target)
                {
                    ourlist.Add(DecryptAudioGroup(audioGroup));
                }


                return ourlist;
            }


            public void DeleteFromAudioGroups(string DBName)
            {

                var ourdb = GetAllAudioGroups();


                bool itemexists = false;
                int round = -1;
                foreach (AudioGroupClass item in ourdb)
                {
                    round = round + 1;
                    if (item.AudioGroupName == DBName)
                    {
                        itemexists = true;
                        break;
                    }
                }

                if (itemexists == true)
                {
                    ourdb.RemoveAt(round);

                    var returnlist = new List<AudioGroupClass>();

                    foreach (AudioGroupClass item in ourdb)
                    {
                        returnlist.Add(new AudioGroup(item, UserPassword));
                    }

                    var FileWithAudioGroups = UniversalSave.Load(AudioGroupsPath, DataFormat.JSON);

                    FileWithAudioGroups.Set("AudioGroups", returnlist);

                    UniversalSave.Save(AudioGroupsPath, FileWithAudioGroups);
                }


            }

            public void UpdateFromAudioGroups(string DBName, AudioGroupClass input)
            {

                var ourdb = GetAllAudioGroups();


                bool itemexists = false;
                int round = -1;
                foreach (AudioGroupClass item in ourdb)
                {
                    round = round + 1;
                    if (item.AudioGroupName == DBName)
                    {
                        itemexists = true;
                        break;
                    }
                }

                if (itemexists == true)
                {
                    ourdb.RemoveAt(round);
                    ourdb.Insert(round, input);

                    var returnlist = new List<AudioGroupClass>();

                    foreach (AudioGroupClass item in ourdb)
                    {
                        returnlist.Add(new AudioGroup(item, UserPassword));
                    }

                    var FileWithAudioGroups = UniversalSave.Load(AudioGroupsPath, DataFormat.JSON);

                    FileWithAudioGroups.Set("AudioGroups", returnlist);

                    UniversalSave.Save(AudioGroupsPath, FileWithAudioGroups);
                }


            }

            public void AddToAudioGroups(AudioGroupClass input)
            {

                var ourdb = GetAllAudioGroups();


                ourdb.Add(input);

                var returnlist = new List<AudioGroupClass>();

                foreach (AudioGroupClass item in ourdb)
                {
                    returnlist.Add(new AudioGroup(item, UserPassword));
                }

                var FileWithAudioGroups = UniversalSave.Load(AudioGroupsPath, DataFormat.JSON);

                FileWithAudioGroups.Set("AudioGroups", returnlist);

                UniversalSave.Save(AudioGroupsPath, FileWithAudioGroups);
            }

        }


    }


}
