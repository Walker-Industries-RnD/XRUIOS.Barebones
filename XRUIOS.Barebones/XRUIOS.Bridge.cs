using EclipseProject;
using System.Reflection;
using System.Threading.Tasks;
using EclipseLCL;
using YuukoProtocol;

public class XRUIOS_Bridge
{
    public static async Task Initialize()
    {
        EclipseServer.RunServer("Eclipse Server",Assembly.GetExecutingAssembly());
        Console.WriteLine("[ECLIPSE] Server started.");

        await EclipseClient.Initialize();
        await TestCreator();
    }

    public static async Task TestCreator()
    {
        string creatorType = "Music";

        // Grab a random file from the Music folder
        string musicFolder = System.Environment.SpecialFolder.MyMusic.ToString();
        List<string> filePaths = new List<string>();

        if (Directory.Exists(musicFolder))
        {
            var allFiles = Directory.GetFiles(musicFolder).ToList();
            if (allFiles.Count > 0)
            {
                var rand = new Random();
                filePaths.Add(allFiles[rand.Next(allFiles.Count)]);
            }
        }

        // Step 1: Create a new creator with at least one file
        await EclipseClient.InvokeAsync<DiracResponse>("CreatorClass.CreatorFileClass.CreateCreator",
            ("CreatorName", "YUNA"),
            ("Description", "Best virtual idol"),
            ("PFPPath", (string?)null),
            ("FilePaths", filePaths),
            ("CreatorType", creatorType));

        Console.WriteLine("Created CreatorBase for YUNA");

        // Step 2: Get creator overview
        KeyValuePair<string, string> overview = await EclipseClient.InvokeAsync<KeyValuePair<string, string>>("CreatorClass.CreatorFileClass.GetCreatorOverview",
            ("CreatorName", "YUNA"),
            ("CreatorType", creatorType));
        Console.WriteLine($"Creator: {overview.Key}, Description: {overview.Value}");

        // Step 3: Optionally add another random file (not the same one)
        if (Directory.Exists(musicFolder))
        {
            var allFiles = Directory.GetFiles(musicFolder).ToList();
            if (allFiles.Count > 1)
            {
                var rand = new Random();
                string extraFile;
                do
                {
                    extraFile = allFiles[rand.Next(allFiles.Count)];
                } while (filePaths.Contains(extraFile));

                await EclipseClient.InvokeAsync<DiracResponse>("CreatorClass.CreatorFileClass.AddFile",
                    ("CreatorName", "YUNA"),
                    ("CreatorType", creatorType),
                    ("FilePaths", new List<string> { extraFile }));
            }
        }

        // Step 4: Update description
        await EclipseClient.InvokeAsync<DiracResponse>("CreatorClass.CreatorFileClass.SetDescription",
            ("CreatorName", "YUNA"),
            ("CreatorType", creatorType),
            ("Description", "Top-tier virtual idol and mischief queen"));

        // Step 5: Add to favorites
        await EclipseClient.InvokeAsync<DiracResponse>("CreatorClass.CreatorFavoritesClass.AddToFavorites",
            ("CreatorName", "YUNA"),
            ("CreatorType", creatorType));

        // Step 6: Get favorite creators
        ValueTuple<List<string>, List<string>> favorites = await EclipseClient.InvokeAsync<ValueTuple<List<string>, List<string>>>("CreatorClass.CreatorFavoritesClass.GetFavorites",
            ("CreatorType", creatorType));
        Console.WriteLine("Resolved favorites:");
        Console.WriteLine($" - {String.Join(", ", favorites.Item1)} - {String.Join(", ", favorites.Item2)}");

        // Step 7: Remove the first file safely
        DiracPackage? creator = await EclipseClient.InvokeAsync<DiracPackage>("CreatorClass.CreatorFileClass.GetCreator",
            ("CreatorName", "YUNA"),
            ("CreatorType", creatorType));
        if (creator != null && creator.Collection?.Count > 0)
        {
            await EclipseClient.InvokeAsync<DiracResponse>("CreatorClass.CreatorFileClass.RemoveFiles",
                ("CreatorName", "YUNA"),
                ("CreatorType", creatorType),
                ("filesToRemove", new List<DiracPackage> { creator.Collection[0] }));
        }

        Console.WriteLine("Example workflow completed!");
    }
}