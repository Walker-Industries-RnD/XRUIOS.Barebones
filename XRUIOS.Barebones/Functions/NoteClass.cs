using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Nodes;
using static Pariah_Cybersecurity.DataHandler;
using static Walker.Crypto.SimpleAESEncryption;
using static XRUIOS.Barebones.Functions.ThemeSystem;
using static XRUIOS.Barebones.XRUIOS;

namespace XRUIOS.Barebones.Functions
{
    public static class NoteClass
    {

        public record Note
        {
            // New fields
            public string Title;
            public string Category;
            public DateTime Created;
            public DateTime LastUpdate;
            public string SavedID;
            public string MiniDescription;
            public string NoteID;
            public string XRUIOSNoteID;
            public FileRecord Markdown;
            public List<FileRecord> Images;

            // Constructor
            public Note(string title, string category, DateTime created, DateTime lastUpdate, string savedID, string miniDescription, string noteID, string xRUIOSNoteID, FileRecord markdown, FileRecord FileRecord, List<FileRecord> images)
            {
                // Assign new fields
                Title = title;
                Category = category;
                Created = created;
                LastUpdate = lastUpdate;
                SavedID = savedID;
                MiniDescription = miniDescription;
;

                // Assign existing fields
                NoteID = noteID;
                XRUIOSNoteID = xRUIOSNoteID;
                Markdown = markdown;
                Images = images;
            }
        }


        public record Journal
        {
            // New fields
            public string JournalName;
            public string Description;
            public string CoverImagePath;
            public ThemeIdentity Identity;

            public List<Category> Categories; //We treat as chapters

            // Constructor
            public Journal(string journalName, string description, string coverImagePath, List<Category> categories, ThemeIdentity identity)
            {
                JournalName = journalName;
                Description = description;
                CoverImagePath = coverImagePath;
                Identity = identity;

                Categories = categories;
            }
        }

        public record Category
        {

            public string Title;
            public string Description;
            public string MainImage;
            public string MiniImage;

            public List<FileRecord> Notes; //We treat order as pages

            public Category(string title, string description, string mainImage, string miniImage, List<FileRecord> notes)
            {

                Title = title;
                Description = description;
                MainImage = mainImage;
                MiniImage = miniImage;


                Notes = notes;
            }
        }

        public record ThemeIdentity
        {
            public string ThemeID;
            public string Name;
            public string Author;
            public string Version;
            public List<string> TargetModes;


            public ThemeIdentity() { }

            public ThemeIdentity(
                string themeID,
                string name,
                string author,
                string version,
                List<string> targetModes)
            {
                ThemeID = themeID;
                Name = name;
                Author = author;
                Version = version;
                TargetModes = targetModes;

            }
        }

        //C
        //Remember to create a folder with the same name in the directory containing all the assets!

        public static async Task SaveJournal(Journal journal)
        {
            var directoryPath = Path.Combine(DataPath, "Journals");
            var fileName = $"{journal.Identity.Name} v{journal.Identity.Version} by {journal.Identity.Author}, ID {journal.Identity.ThemeID}";

            var filePath = Path.Combine(DataPath, "Journals", fileName);

            if (File.Exists(filePath))
            {
                throw new InvalidOperationException("This journal already exists; please change the name.");
            }

            await JSONDataHandler.CreateJsonFile(fileName, directoryPath, new JsonObject());

            var json = await JSONDataHandler.LoadJsonFile(directoryPath, fileName);
            json = await JSONDataHandler.AddToJson<Journal>(json, "Data", journal, encryptionKey);
            await JSONDataHandler.SaveJson(json);
        }

        //R
        public static async Task<List<Journal>> GetAllJournals()
        {
            List<Journal> Journals = new List<Journal>();

            var directoryPath = Path.Combine(DataPath, "Journals");

            var themePaths = Directory.EnumerateFiles(directoryPath);

            foreach (var item in themePaths)
            {
                var json = await JSONDataHandler.LoadJsonFile(directoryPath, (Path.GetFileNameWithoutExtension(item)));

                var themeFile = (Journal)await JSONDataHandler.GetVariable<Journal>(json, "Data", encryptionKey);

                Journals.Add(themeFile);

            }

            return Journals;
        }

        public static async Task<Journal> GetJournal(string FileName)
        {

            var directoryPath = Path.Combine(DataPath, "Journals");

            var json = await JSONDataHandler.LoadJsonFile(directoryPath, FileName);

            var journalFile = (Journal)await JSONDataHandler.GetVariable<Journal>(json, "Data", encryptionKey);

            return journalFile;
        }


        public static async Task<Category> GetCategory(string JournalName, string CategoryName)
        {
            var funcjournal = await GetJournal(JournalName);
            foreach (Category item in funcjournal.Categories)
            {
                if (item.Title == CategoryName)
                {
                    return item;
                }
            }

            return null;
        }


        //U

        //Remember to put Identity.Version up
        public static async Task UpdateJournal(Journal journal, Journal newJournal)
        {
            var directoryPath = Path.Combine(DataPath, "Journals");
            var fileName = $"{journal.Identity.Name} v{journal.Identity.Version} by {journal.Identity.Author}, ID {journal.Identity.ThemeID}";

            var filePath = Path.Combine(DataPath, "Journals", fileName);

            if (!File.Exists(filePath))
            {
                throw new InvalidOperationException("This journal does not exist.");
            }

            var json = await JSONDataHandler.LoadJsonFile(directoryPath, fileName);
            json = await JSONDataHandler.UpdateJson<Journal>(json, "Data", newJournal, encryptionKey);
            await JSONDataHandler.SaveJson(json);
        }

        //D
        public static async Task DeleteJournal(string FileName)
        {
            var directoryPath = Path.Combine(DataPath, "Journals");
            var filePath = Path.Combine(DataPath, "Journals", FileName);

            File.Delete(filePath);
        }







    }

}

