using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Text;

namespace XRUIOS.Barebones
{
    public record AudioGroupClass
    {
        public string AudioGroupName;
        public List<string> AppNames;
        public List<float> Volumes;
        public bool IsStereo;

        public AudioGroupClass() { }


        public AudioGroupClass(string audiogroupname, List<string> appnames, List<float> volumes, bool isstereo)
        {
            AudioGroupName = audiogroupname;
            AppNames = appnames;
            Volumes = volumes;
            IsStereo = isstereo;


        }


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


