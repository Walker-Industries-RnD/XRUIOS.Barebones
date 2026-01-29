using Microsoft.VisualBasic;
using Pariah_Cybersecurity;
using System;
using System.Collections.Generic;
using System.Text;
using static XRUIOS.Barebones.VolumeClass;

namespace XRUIOS.Barebones.Functions
{
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

}
