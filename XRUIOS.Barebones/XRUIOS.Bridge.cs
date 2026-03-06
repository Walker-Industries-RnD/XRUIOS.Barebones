using EclipseProject;
using System.Reflection;
using System.Threading.Tasks;
using EclipseLCL;

public class XRUIOS_Bridge
{
    public static async Task Initialize()
    {
        EclipseServer.RunServer(Assembly.GetExecutingAssembly());
        Console.WriteLine("[ECLIPSE] Server started.");

        await EclipseClient.Initialize();

        /*Console.WriteLine("Creating stopwatch...");
        string stopwatchId = await EclipseClient.InvokeAsync<string>("StopwatchClass.CreateStopwatch");

        Console.WriteLine("Waiting 2 seconds...");
        await Task.Delay(2000);

        TimeSpan elapsed1 = await EclipseClient.InvokeAsync<TimeSpan>("StopwatchClass.GetTimeElapsed", ("id", stopwatchId));
        Console.WriteLine($"Elapsed time: {elapsed1.TotalSeconds:F2} seconds");

        Console.WriteLine("Creating first lap...");
        DiracPackage lap1 = await EclipseClient.InvokeAsync<DiracPackage>("StopwatchClass.CreateLap", ("id", stopwatchId));
        Console.WriteLine($"Lap {lap1.Fields!["LapCount"]} at {lap1.Fields["SecondsElapsed"]} seconds");

        Console.WriteLine("Waiting 1 second...");
        await Task.Delay(1000);

        Console.WriteLine("Creating second lap...");
        DiracPackage lap2 = await EclipseClient.InvokeAsync<DiracPackage>("StopwatchClass.CreateLap", ("id", stopwatchId));
        Console.WriteLine($"Lap {lap2.Fields!["LapCount"]} at {lap2.Fields["SecondsElapsed"]} seconds");

        Console.WriteLine("Destroying stopwatch and retrieving records...");
        DiracPackage records = await EclipseClient.InvokeAsync<DiracPackage>("StopwatchClass.DestroyStopwatch", ("id", stopwatchId));
        foreach (var record in records.Collection!)
            Console.WriteLine($"Lap {record.Fields!["LapCount"]}: {record.Fields["SecondsElapsed"]} seconds");


        Console.WriteLine("Saving records to CSV...");
        EclipseClient.InvokeAsync("StopwatchClass.SaveStopwatchValuesAsSheet", ("Values", records), ("RecordedOn", DateTime.Now), ("FileName", "TestStopwatch"));
        Console.WriteLine("Saved CSV in DataPath directory.");

        Console.WriteLine("Test complete.");*/
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
        DiracPackage overview = await EclipseClient.InvokeAsync<DiracPackage>("CreatorClass.CreatorFileClass.GetCreatorOverview",
            ("CreatorName", "YUNA"),
            ("CreatorType", creatorType));
        Console.WriteLine($"Creator: {overview.Fields!["Item1"]}, Description: {overview.Fields["Item2"]}");

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
        DiracPackage favorites = await EclipseClient.InvokeAsync<DiracPackage>("CreatorClass.CreatorFavoritesClass.GetFavorites",
            ("CreatorType", creatorType));
        Console.WriteLine("Resolved favorites:");
        if (favorites.Collection != null)
            foreach (var f in favorites.Collection)
                Console.WriteLine($" - {f}");

        // Step 7: Remove the first file safely
        DiracPackage creator = await EclipseClient.InvokeAsync<DiracPackage>("CreatorClass.CreatorFileClass.GetCreator",
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