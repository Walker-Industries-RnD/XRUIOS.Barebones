using Pariah_Cybersecurity;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Nodes;
using static XRUIOS.Barebones.XRUIOS;

namespace XRUIOS.Barebones
{
    //Originally made for music, it's pretty expandable!
    public class CreatorClass
    {
        public record Creator
        {
            public string Name;
            public string Description;
            public FileRecord? PFP;
            public List<FileRecord> Files;

            public Creator() { }

            public Creator(string name, string description, FileRecord? pfp, List<FileRecord?> files)
            {
                this.Name = name;
                this.Description = description;
                this.PFP = pfp;
                this.Files = files;
            }

        }

        public class CreatorFileClass
        {
            //C

            //IMPORTANT, PFP and Files use Media, not Yuuko Bindings (Although they're connected)

            public static async Task CreateCreator(string CreatorName, string? Description, string? PFPPath, List<string> FilePaths, string CreatorType)
            {

                var directoryPath = Path.Combine(DataPath, "Creators", CreatorType);

                var manager = new Yuuko.Bindings.DirectoryManager(directoryPath);

                var folder = Path.Combine(DataPath, "Creators", CreatorType);
                var fileNameToFind = CreatorName;

                var foundFile = Directory.EnumerateFiles(folder, fileNameToFind + ".*").FirstOrDefault();

                if (foundFile != null)
                {
                    Console.WriteLine($"Creator already exists.");
                    return;
                }

                //Check if file exists 

                var fileExists = File.Exists(directoryPath);

                //No? Let's continue

                await manager.LoadBindings();


                if (Description == null)
                {
                    Description = "No Description Provided.";
                }

                FileRecord PossiblePFP = null;

                if (PFPPath != null)
                {

                    var fileDirectoryID = await Media.GetOrCreateDirectory(PFPPath, Path.GetDirectoryName(PFPPath), Guid.NewGuid().ToString());

                    var fileName = Path.GetFileName(PFPPath);

                    var newFileRecord = new FileRecord(fileDirectoryID.UUID, fileName);

                    PossiblePFP = newFileRecord;


                }

                List<FileRecord> Files = new List<FileRecord>();

                foreach (var file in FilePaths)
                {
                    var fileDirectoryID = await Media.GetOrCreateDirectory(file, Path.GetDirectoryName(file), Guid.NewGuid().ToString());
                    var fileName = Path.GetFileName(file);

                    Files.Add(new FileRecord(fileDirectoryID.UUID, fileName));
                }


                var newCreator = new Creator(CreatorName, Description, PossiblePFP, Files);

                var saveable = await BinaryConverter.NCObjectToByteArrayAsync<Creator>(newCreator);

                //And now we save

                await DataHandler.JSONDataHandler.CreateJsonFile(CreatorName, directoryPath, new JsonObject());

                var creatorFile = await DataHandler.JSONDataHandler.LoadJsonFile(CreatorName, directoryPath);

                var editedJSON = await DataHandler.JSONDataHandler.UpdateJson<List<byte[]>>(creatorFile, "Data", saveable, encryptionKey);

                await DataHandler.JSONDataHandler.SaveJson(editedJSON);

            }

            //R
            public static async Task<Creator> GetCreator(string CreatorName, string CreatorType)
            {

                var directoryPath = Path.Combine(DataPath, "Creators", CreatorType);

                var folder = Path.Combine(DataPath, "Creators", CreatorType);
                var fileNameToFind = CreatorName;

                var foundFile = Directory.EnumerateFiles(folder, fileNameToFind + ".*").FirstOrDefault();

                if (foundFile != null)
                {
                    Console.WriteLine($"Creator doesn't exist.");
                    return null;
                }

                var creatorFile = await DataHandler.JSONDataHandler.LoadJsonFile(CreatorName, directoryPath);

                var CreatorData = (byte[])await DataHandler.JSONDataHandler.GetVariable<List<byte[]>>(creatorFile, "Data", encryptionKey);

                var data = (Creator)await BinaryConverter.NCByteArrayToObjectAsync<Creator>(CreatorData);

                return data;

            }

            public static async Task<(string, string)> GetCreatorOverview(string CreatorName, string CreatorType)
            {

                var directoryPath = Path.Combine(DataPath, "Creators", CreatorType);

                var folder = Path.Combine(DataPath, "Creators", CreatorType);
                var fileNameToFind = CreatorName;

                var foundFile = Directory.EnumerateFiles(folder, fileNameToFind + ".*").FirstOrDefault();

                if (foundFile != null)
                {
                    Console.WriteLine($"Creator doesn't exist.");
                    return (null, null);
                }

                var creatorFile = await DataHandler.JSONDataHandler.LoadJsonFile(CreatorName, directoryPath);

                var CreatorData = (byte[])await DataHandler.JSONDataHandler.GetVariable<List<byte[]>>(creatorFile, "Data", encryptionKey);

                var data = (Creator)await BinaryConverter.NCByteArrayToObjectAsync<Creator>(CreatorData);

                return (data.Name, data.Description);

            }

            public static async Task<List<FileRecord>> GetCreatorFiles(string CreatorName, string CreatorType)
            {

                var directoryPath = Path.Combine(DataPath, "Creators", CreatorType);

                var folder = Path.Combine(DataPath, "Creators", CreatorType);
                var fileNameToFind = CreatorName;

                var foundFile = Directory.EnumerateFiles(folder, fileNameToFind + ".*").FirstOrDefault();

                if (foundFile != null)
                {
                    Console.WriteLine($"Creator doesn't exist.");
                    return null;
                }

                var creatorFile = await DataHandler.JSONDataHandler.LoadJsonFile(CreatorName, directoryPath);

                var CreatorData = (byte[])await DataHandler.JSONDataHandler.GetVariable<List<byte[]>>(creatorFile, "Data", encryptionKey);

                var data = (Creator)await BinaryConverter.NCByteArrayToObjectAsync<Creator>(CreatorData);

                return (data.Files);

            }

            //U
            //(You can't edit the name)

            public static async Task AddFile(string CreatorName, string CreatorType, List<string> FilePaths)
            {
                var CreatorFile = await GetCreator(CreatorName, CreatorType);
                if (CreatorFile == null)
                    throw new InvalidOperationException("Creator not found.");
                foreach (var file in FilePaths)
                {
                    var fileDirectoryID = await Media.GetOrCreateDirectory(file, Path.GetDirectoryName(file), Guid.NewGuid().ToString());
                    var fileName = Path.GetFileName(file);

                    CreatorFile.Files.Add(new FileRecord(fileDirectoryID.UUID, fileName));
                }

                var directoryPath = Path.Combine(DataPath, "Creators", CreatorType);

                var saveable = await BinaryConverter.NCObjectToByteArrayAsync<Creator>(CreatorFile);

                var creatorFile = await DataHandler.JSONDataHandler.LoadJsonFile(CreatorName, directoryPath);

                var CreatorData = await DataHandler.JSONDataHandler.UpdateJson<List<byte[]>>(creatorFile, "Data", saveable, encryptionKey);

                await DataHandler.JSONDataHandler.SaveJson(CreatorData);

            }

            public static async Task SetDescription(string CreatorName, string CreatorType, string Description)
            {
                var CreatorFile = await GetCreator(CreatorName, CreatorType);
                if (CreatorFile == null)
                    throw new InvalidOperationException("Creator not found.");

                CreatorFile.Description = Description;

                var directoryPath = Path.Combine(DataPath, "Creators", CreatorType);

                var saveable = await BinaryConverter.NCObjectToByteArrayAsync<Creator>(CreatorFile);

                var creatorFile = await DataHandler.JSONDataHandler.LoadJsonFile(CreatorName, directoryPath);

                var CreatorData = await DataHandler.JSONDataHandler.UpdateJson<List<byte[]>>(creatorFile, "Data", saveable, encryptionKey);

                await DataHandler.JSONDataHandler.SaveJson(CreatorData);

            }



            //D
            public static async Task RemoveFiles(string CreatorName, string CreatorType, List<FileRecord> filesToRemove)
            {
                var creator = await GetCreator(CreatorName, CreatorType);
                if (creator == null)
                    throw new InvalidOperationException("Creator not found.");

                var directoryPath = Path.Combine(DataPath, "Creators", CreatorType);

                foreach (var item in filesToRemove)
                {
                    var fileRecord = creator.Files.FirstOrDefault(f => f.File == item.File && f.UUID == item.UUID);
                    if (fileRecord != null)
                    {
                        creator.Files.Remove(fileRecord);
                    }
                    else
                    {
                        Console.WriteLine($"Warning: File '{item.File}' with UUID '{item.UUID}' not found in creator '{CreatorName}'.");
                    }
                }

                var saveable = await BinaryConverter.NCObjectToByteArrayAsync(creator);
                var creatorFile = await DataHandler.JSONDataHandler.LoadJsonFile(CreatorName, directoryPath);
                var editedJSON = await DataHandler.JSONDataHandler.UpdateJson<List<byte[]>>(creatorFile, "Data", saveable, encryptionKey);
                await DataHandler.JSONDataHandler.SaveJson(editedJSON);
            }
        }

        public class CreatorFavoritesClass
        {


            //C
            public static async Task AddToFavorites(string CreatorName, string CreatorType)
            {
                var directoryPath = Path.Combine(DataPath, "Creators", CreatorType);

                var folder = Path.Combine(DataPath, "Creators", CreatorType);
                var fileNameToFind = CreatorName;

                var foundFile = Directory.EnumerateFiles(folder, fileNameToFind + ".*").FirstOrDefault();

                if (foundFile != null)
                {
                    Console.WriteLine($"Creator doesn't exist.");
                    return;
                }
                var manager = new Yuuko.Bindings.DirectoryManager(directoryPath);

                await manager.LoadBindings();

                var favoritesFile = await DataHandler.JSONDataHandler.LoadJsonFile("CreatorFavorites", directoryPath);

                var favorites = (List<string>)await DataHandler.JSONDataHandler.GetVariable<List<string>>(favoritesFile, "Data", encryptionKey);
                //UUID, path name, path 


                if (!favorites.Any(d => d == CreatorName))
                {
                    favorites.Add(CreatorName);
                }

                else
                {
                    throw new InvalidOperationException($"Creator already favorited.");
                }


                var editedJSON = await DataHandler.JSONDataHandler.UpdateJson<List<string>>(favoritesFile, "Data", favorites, encryptionKey);

                await DataHandler.JSONDataHandler.SaveJson(editedJSON);

            }

            //R
            public static async Task<(List<string>, List<string>)> GetFavorites(string CreatorType)
            {

                var directoryPath = Path.Combine(DataPath, "Music");

                var manager = new Yuuko.Bindings.DirectoryManager(directoryPath);

                var favoritesFile = await DataHandler.JSONDataHandler.LoadJsonFile("CreatorFavorites", directoryPath);

                var favorites = (List<string>)await DataHandler.JSONDataHandler.GetVariable<List<string>>(favoritesFile, "Data", encryptionKey);
                //UUID, path name, path 

                //What bindings exist? Let's go through each and see

                List<string> resolvedFiles = new List<string>();
                List<string> unresolvedFiles = new List<string>();
                //UUID, Path Name, Path

                foreach (var item in favorites)
                {
                    var creatorData = CreatorFileClass.GetCreator(item, CreatorType);

                    if (creatorData == null)
                    {
                        unresolvedFiles.Add(item);
                    }

                    else
                    {
                        resolvedFiles.Add(item);

                    }
                }

                return (resolvedFiles, unresolvedFiles);

            }

            public static async Task<List<string>> GetFavoritePathsAsync(string CreatorType, bool onlyResolved = true)
            {
                var (resolved, unresolved) = await GetFavorites(CreatorType);

                if (onlyResolved)
                {
                    return resolved;
                }

                var all = new List<string>(resolved);
                all.AddRange(unresolved);
                return all;
            }

            //D
            public static async Task RemoveFromFavorites(string CreatorName, string CreatorType)
            {
                var directoryPath = Path.Combine(DataPath, "Creators", CreatorType);

                var folder = Path.Combine(DataPath, "Creators", CreatorType);
                var fileNameToFind = CreatorName;

                var foundFile = Directory.EnumerateFiles(folder, fileNameToFind + ".*").FirstOrDefault();

                if (foundFile != null)
                {
                    Console.WriteLine($"Creator doesn't exist.");
                    return;
                }
                var manager = new Yuuko.Bindings.DirectoryManager(directoryPath);

                await manager.LoadBindings();

                var favoritesFile = await DataHandler.JSONDataHandler.LoadJsonFile("CreatorFavorites", directoryPath);

                var favorites = (List<string>)await DataHandler.JSONDataHandler.GetVariable<List<string>>(favoritesFile, "Data", encryptionKey);
                //UUID, path name, path 


                if (favorites.Any(d => d == CreatorName))
                {
                    favorites.Remove(CreatorName);
                }

                else
                {
                    throw new InvalidOperationException($"Creator not favorited.");
                }


                var editedJSON = await DataHandler.JSONDataHandler.UpdateJson<List<string>>(favoritesFile, "Data", favorites, encryptionKey);

                await DataHandler.JSONDataHandler.SaveJson(editedJSON);

            }
        }


        //Create a tag system later
        //Dictionary CreatorName, List<string> tags


    }

}
