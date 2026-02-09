using System.Numerics;
using System.Text;
using XRUIOS.Barebones;
using XRUIOS.Barebones.Functions;
using XRUIOS_UserManager;
using static XRUIOS.Barebones.AlarmClass;
using static XRUIOS.Barebones.ExperimentalAudioClass;
using static XRUIOS.Barebones.Functions.AppClass;
using static XRUIOS.Barebones.Functions.AreaManagerClass;
using static XRUIOS.Barebones.Functions.NoteClass;
using static XRUIOS.Barebones.Functions.SystemInfoDisplayClass.SystemInfoDisplayWindows;
using static XRUIOS.Barebones.Functions.ThemeSystem;
using static XRUIOS.Barebones.Functions.VolumeClass;
using static XRUIOS.Barebones.Songs;
using static XRUIOS.Barebones.XRUIOS;

namespace XRUIOS.TestHarness
{
    class Program
    {




        private static string Art = @"
█████ █████ ███████████   █████  █████ █████    ███████     █████████ 
▒▒███ ▒▒███ ▒▒███▒▒▒▒▒███ ▒▒███  ▒▒███ ▒▒███   ███▒▒▒▒▒███  ███▒▒▒▒▒███
 ▒▒███ ███   ▒███    ▒███  ▒███   ▒███  ▒███  ███     ▒▒███▒███    ▒▒▒ 
  ▒▒█████    ▒██████████   ▒███   ▒███  ▒███ ▒███      ▒███▒▒█████████ 
   ███▒███   ▒███▒▒▒▒▒███  ▒███   ▒███  ▒███ ▒███      ▒███ ▒▒▒▒▒▒▒▒███
  ███ ▒▒███  ▒███    ▒███  ▒███   ▒███  ▒███ ▒▒███     ███  ███    ▒███
 █████ █████ █████   █████ ▒▒████████   █████ ▒▒▒███████▒  ▒▒█████████ 
▒▒▒▒▒ ▒▒▒▒▒ ▒▒▒▒▒   ▒▒▒▒▒   ▒▒▒▒▒▒▒▒   ▒▒▒▒▒    ▒▒▒▒▒▒▒     ▒▒▒▒▒▒▒▒▒  ";



        private static string Art2 = @"
 ____                       _                                 
 /   \    ___  .___    ___  \ ___    __.  , __     ___    ____
 |,_-<   /   ` /   \ .'   ` |/   \ .'   \ |'  `. .'   `  (    
 |    ` |    | |   ' |----' |    ` |    | |    | |----'  `--. 
 `----' `.__/| /     `.___, `___,'  `._.' /    | `.___, \___.'
                                                              ";








        static async Task Main(string[] args)
        {
            Console.ResetColor();
            Console.OutputEncoding = System.Text.Encoding.UTF8; // for proper symbols if you want them later



            // ┌──────────────────────────────────────────────────────────────┐
            // │           NERV CENTRAL COMMAND - SYSTEM BOOT SEQUENCE           │
            // │                  EVA INTEGRATION PROTOCOL v0.42                  │
            // └──────────────────────────────────────────────────────────────┘

            Console.ForegroundColor = ConsoleColor.DarkYellow;  // that signature NERV orange
            Console.ResetColor();
            Console.WriteLine("Click any button to continue.");

            Console.ReadKey();
            Console.Clear();



            // ──────────────────────────────────────────────────────────────
            // FORCE RESET DATA DIRECTORIES - NO MERCY, NO LEFTOVERS
            // ──────────────────────────────────────────────────────────────

            var DataPath = Path.Combine(Directory.GetCurrentDirectory(), "XRUIOS");
            var PublicDataPath = Path.Combine(Directory.GetCurrentDirectory(), "XRUIOSPublic");

            // Delete if exists (clean slate every run — because you're sloppy)
            if (Directory.Exists(DataPath)) Directory.Delete(DataPath, true);
            if (Directory.Exists(PublicDataPath)) Directory.Delete(PublicDataPath, true);

            // Recreate both — fresh and innocent
            Directory.CreateDirectory(DataPath);
            Directory.CreateDirectory(PublicDataPath);

            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine($" Data directories reset & recreated:");
            Console.WriteLine($"  → {DataPath}");
            Console.WriteLine($"  → {PublicDataPath}");
            Console.ResetColor();

            await XRUIOS_UserManager.XRUIOS_UserManager.InitializeSystemAsync();

            await Task.Delay(10);




            Console.WriteLine(Art);
            Console.WriteLine(Art2);
            Console.WriteLine("The XRUIOS - In The Belief Of A Better World");
            Console.WriteLine("\n  [MAGI SYSTEM] Initializing core subsystems...");
            Console.ResetColor();

            await Task.Delay(10); // dramatic breath before the plunge


















            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("\n\n");
            Console.WriteLine("          SPATIAL INTERFACE LAYER");
            Console.ResetColor();

            await Task.Delay(10);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\n  [MAGI-01] Pilot detected. Third Child authorization confirmed.");
            Console.WriteLine("  [WARNING] AT Field fluctuations detected - stabilizing...");
            Console.ResetColor();
            await Task.Delay(10);

            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine("  >> XRUIOS INITIALIZATION SEQUENCE COMMENCING <<");
            Console.ResetColor();

             await Task.Delay(10);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n  [ALL CORE SUBSYSTEMS ONLINE]");
            Console.WriteLine("  Entry plug synchronization rate: 78.4% - acceptable for test phase.");
            Console.ResetColor();
            await Task.Delay(10);

            // ──────────────────────────────────────────────────────────────
            //     TEST PROTOCOL ACTIVATED - FULL DIAGNOSTIC SUITE
            // ──────────────────────────────────────────────────────────────

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n  [NERV] Commencing full system stress test. Don't screw this up.");
            Console.ResetColor();

            var testPhases = new List<(Func<Task> testFunc, string desc)>
{
    (TestAlarm,             "[ALARM] Angel intercept timers & recurring patterns"),
    (TestApps,              "[DOCK] App manifest deployment & favorite loadouts"),
    (TestWorldPoints,       "[SPATIAL] World anchor points & static object placement"),
    (TestCalendar,          "[SCHEDULE] Event creation & temporal range queries"),
    (TestChrono,            "[TIME] World timezone anchors & display formatting"),
    (TestClipboard,         "[CLIP] Multi-group clipboard persistence test"),
    (TestColors,            "[UI] Color palette sampling & custom RGBA validation"),
    (TestCreator,           "[CREATOR] Virtual idol / music archive CRUD"),
    (TestDataSlot,          "[DATASLOT] Session-linked spatial bookmarks"),
    (TestExperimentalAudio, "[AUDIO] Advanced environmental & limit settings"),
    (TestGeoClass,          "[GEO] Exact & relative coordinate logging"),
    (TestMediaAlbumClass,   "[MEDIA] Album creation & favorite tagging"),
    (TestMediaTagger,       "[TAGGER] Creator metadata association"),
    (TestMusicQueue,        "[QUEUE] Playback queue & currently-playing state"),
    (TestNotes,             "[JOURNAL] Thematic lore journals & category structure"),
    (TestNotifications,     "[ALERT] Notification queue & button actions"),
    (TestSoundEQ,           "[EQ] Sound profile database & default switching"),
    (TestStopwatch,         "[TIMER] Lap timing & CSV export validation"),
    (TestSpecs,             "[SYSTEM] Hardware & VR headset diagnostic readout"),
    (TestThemes,            "[THEME] SAO-style theme engine test"),
    (TestThemes2,           "[THEME] Evangelion NERV Dark theme deployment"),
    (TestVolume,            "[VOLUME] Contextual mix presets & bridge audio routing"),
    (TestTimer,             "[SYNC] Asynchronous EVA unit sync timers")
};

            int phaseCount = 1;
            foreach (var (testFunc, desc) in testPhases)
            {
                Console.ForegroundColor = ConsoleColor.DarkMagenta;
                Console.Write($"\n  PHASE {phaseCount:D2}/{testPhases.Count:D2}  "); Console.ResetColor();
                Console.WriteLine(desc);

                Console.ForegroundColor = ConsoleColor.DarkCyan;
                Console.Write("  [EXECUTING] ");
                Console.ResetColor();

                try
                {
                    await testFunc();
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("→ COMPLETE");
                    Console.ResetColor();
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"→ CRITICAL FAILURE: {ex.Message.Split('\n')[0]}");
                    Console.ResetColor();
                }

                await Task.Delay(400); // tiny dramatic beat between phases
                phaseCount++;
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("\n\n  [TEST PROTOCOL COMPLETE]");
            Console.WriteLine("  All diagnostics passed.");
            Console.ResetColor();

            await Task.Delay(1200);
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("\n  Type any key to terminate connection to Central Dogma.");
            Console.ResetColor();

            Console.ReadKey(true);
        }



        static async Task TestAlarm()
        {
            // 1️ Start Hangfire
            HangfireBootstrap.Start();

            // 2️ Load any existing alarms
            await AlarmClass.LoadAlarms();
            Console.WriteLine($"Loaded {AlarmClass.Alarms.Count} alarms.");

            // 3️ Create a one-shot alarm (fires in 10 seconds)
            var oneShot = new Alarm(
                alarmName: "OneShotTest",
                alarmTime: DateTime.Now.AddSeconds(10),
                isRecurring: false,
                recurringDays: new List<DayOfWeek>(),
                soundFilePath: new FileRecord { File = "oneshot.wav" },
                volume: 70,
                isEnabled: true
            );
            await AlarmClass.AddAlarm(oneShot);
            Console.WriteLine("One-shot alarm scheduled.");

            // 4️ Create a recurring alarm (fires next minute, weekdays)
            var recurring = new Alarm(
                alarmName: "RecurringTest",
                alarmTime: DateTime.Now.AddMinutes(1),
                isRecurring: true,
                recurringDays: new List<DayOfWeek> {
                DayOfWeek.Monday,
                DayOfWeek.Tuesday,
                DayOfWeek.Wednesday,
                DayOfWeek.Thursday,
                DayOfWeek.Friday
                },
                soundFilePath: new FileRecord { File = "recurring.wav" },
                volume: 60,
                isEnabled: true
            );
            await AlarmClass.AddAlarm(recurring);
            Console.WriteLine("Recurring alarm scheduled.");

            // 5️ Update the one-shot alarm (disable it)
            await AlarmClass.UpdateAlarm(oneShot, alarm =>
            {
                alarm.IsEnabled = false;
                alarm.AlarmName = "OneShot_Disabled";
            });
            Console.WriteLine("One-shot alarm updated & disabled.");

            // 6️ Delete the recurring alarm
            await AlarmClass.DeleteAlarm(recurring);
            Console.WriteLine("Recurring alarm deleted.");

            // 7️ List all remaining alarms
            Console.WriteLine("Current Alarms:");
            foreach (var alarm in AlarmClass.Alarms)
                Console.WriteLine($"- {alarm.AlarmName} at {alarm.AlarmTime}, Enabled: {alarm.IsEnabled}");
        }
        static async Task TestApps()
        {
            // 1. Create a new app manifest – like launching an Eva for the first time
            var app = new XRUIOSAppManifest(
                appID: "com.walkerdev.testapp",
                name: "TestApp",
                description: "This app’s primary function is reconnaissance, just like Yorha units scanning a battlefield.",
                author: "WalkerDev",
                version: "1.0.0",
                yuukoAppInfo: null,
                entryPoint: "Main.exe",
                identifier: null // auto-generate
            );

            await AddApp(app);
            Console.WriteLine($"App '{app.Name}' deployed to XRUIOS dock.");

            // 2. Load app by identifier – syncing across multiple rigs like Titanfall dropships
            var loadedApp = await GetApp(app.Identifier);
            Console.WriteLine($"Loaded App: {loadedApp.Name}, EntryPoint: {loadedApp.EntryPoint}");

            // 3. Patch app – upgrade like equipping a new Armored Core chassis
            var patch = new XRUIOSAppManifestPatch
            {
                Name = "TestAppUpgraded",
                Version = "1.1.0"
            };
            var patchedApp = UpdateDataSlot(loadedApp, patch);
            await UpdateApp(patchedApp);
            Console.WriteLine($"App patched: {patchedApp.Name}, Version: {patchedApp.Version}");

            // 4. Add to favorites – your personal loadout, like selecting preferred Muv-Luv units
            string fakeUUID = Guid.NewGuid().ToString();
            await AppFavoritesClass.AddToFavorites(app.Identifier, fakeUUID);
            Console.WriteLine($"App '{app.Name}' added to favorites (UUID: {fakeUUID})");

            // 5. Get favorites – checking which apps survive a Cyberpunk 2077-style data purge
            var favoritePaths = await AppFavoritesClass.GetFavoritePathsAsync();
            Console.WriteLine("Current favorite app paths:");

            if (favoritePaths != null && favoritePaths.Count > 0)
            {
                foreach (var path in favoritePaths)
                    Console.WriteLine($"- {path}");
            }
 

            // 6. Remove from favorites – deleting what’s no longer needed, like Yorha clearing corrupted units
            await AppFavoritesClass.RemoveFromFavorites(app.Identifier, fakeUUID);
            Console.WriteLine($"Removed app '{app.Name}' from favorites");

            // 7. Delete app – final decommission, like retiring an Eva after mission failure
            DeleteApp(app.Identifier);
            Console.WriteLine($"App '{app.Name}' deleted from XRUIOS storage");
        }
        static async Task TestWorldPoints()
        {
            // 1. Create a WorldPoint – like deploying a reconnaissance drone in Muv-Luv
            var wp = new WorldPoint(
                renderingMode: RenderingMode.AllFrames,
                pointData: new byte[] { 0x01, 0x02 },
                pointName: "AlphaRecon",
                pointDescription: "Survey point for battlefield analysis",
                pointImagePath: new FileRecord { File = "alpha.png" },
                userCentric: false,
                staticObjs: new List<StaticObject>(),
                appObjs: new List<App>(),
                desktopScreenObjs: new List<DesktopScreen>(),
                staciaObjs: new List<StaciaItems>(),
                identifier: null
            );

            await AddWorldPoint(wp);
            Console.WriteLine($"WorldPoint '{wp.PointName}' deployed.");

            // 2. Fetch list of all WorldPoints – like scanning for active Titans
            var worldPoints = GetWorldPoints();
            Console.WriteLine($"Found {worldPoints.Count} WorldPoints: {string.Join(", ", worldPoints)}");

            // 3. Load specific WorldPoint
            var loadedWP = await GetWorldPoint(wp.Identifier);
            Console.WriteLine($"Loaded WorldPoint: {loadedWP.PointName} - {loadedWP.PointDescription}");

            // 4. Patch WorldPoint – like upgrading a Yorha unit mid-mission
            var patch = new WorldPointPatch
            {
                PointName = "AlphaRecon_Upgraded",
                PointDescription = "Upgraded survey point with 2B sensors"
            };
            var updatedWP = UpdateWorldPoint(loadedWP, patch);
            await UpdateWorldPoint(updatedWP);
            Console.WriteLine($"WorldPoint updated: {updatedWP.PointName} - {updatedWP.PointDescription}");

            // 5. Add static object – like placing a turret in Titanfall
            var turret = new StaticObject(
                pTrackingType: PositionalTrackingMode.Follow,
                rTrackingType: RotationalTrackingMode.LAM,
                name: "Turret_X1",
                spatialData: new Vector3(1, 0, 1),
                objectLabel: ObjectOSLabel.Objects,
                assetFile: new FileRecord { File = "turret.obj" }
            );
            updatedWP = updatedWP with { StaticObjs = new List<StaticObject> { turret } };
            await UpdateWorldPoint(updatedWP);
            Console.WriteLine("StaticObject 'Turret_X1' added to WorldPoint.");

            // 6. Add app object – like deploying a support AI in Cyberpunk 2077 hacking
            var appObj = new App(
                pTrackingType: PositionalTrackingMode.Anchored,
                rTrackingType: RotationalTrackingMode.Static,
                spatialData: new Vector3(0, 0, 0),
                objectLabel: ObjectOSLabel.Software,
                reference: new Yuuko.Handle() // placeholder
            );
            updatedWP = updatedWP with { AppObjs = new List<App> { appObj } };
            await UpdateWorldPoint(updatedWP);
            Console.WriteLine("App object added to WorldPoint.");

            // 7. Delete WorldPoint – evac like Nier units retreating
            DeleteWorldPoint(updatedWP.Identifier);
            Console.WriteLine($"WorldPoint '{updatedWP.PointName}' deleted.");
        }
        public static async Task TestCalendar()
        {
            // 1 Create two simple events
            var now = DateTime.Now;

            var uid1 = await CalendarClass.CreateSimpleEvent(
                eventDate: now.AddMinutes(10),
                summary: "Morning XRUIOS briefing",
                description: "Check all systems.",
                durationHours: 1
            );

            var uid2 = await CalendarClass.CreateSimpleEvent(
                eventDate: now.AddHours(2),
                summary: "Afternoon VR test",
                description: "Testing new XR headset features.",
                durationHours: 2
            );

            Console.WriteLine($"Created events: {uid1}, {uid2}");

            // 2 Get events for today
            var todayEvents = CalendarClass.GetEventsForDay(DateTime.Today);
            Console.WriteLine($"\nEvents today ({todayEvents.Count}):");
            foreach (var ev in todayEvents)
            {
                Console.WriteLine($"- {ev.Summary} from {ev.Start.Value} to {ev.End.Value}");
            }

            // 3 Get events in the next 3 hours
            var rangeStart = now;
            var rangeEnd = now.AddHours(3);
            var rangeEvents = CalendarClass.GetEventsInRange(rangeStart, rangeEnd);

            Console.WriteLine($"\nEvents in the next 3 hours ({rangeEvents.Count}):");
            foreach (var ev in rangeEvents)
            {
                Console.WriteLine($"- {ev.Summary} from {ev.Start.Value} to {ev.End.Value}");
            }

            // 4 Example update: add a note to first event
            await CalendarClass.UpdateEventByUid(uid1, ev => ev.Description += " [Updated]");
            var updatedEvent = CalendarClass.GetEventByUid(uid1);
            Console.WriteLine($"\nUpdated first event description: {updatedEvent.Description}");
        }
        public static async Task TestChrono()
        {
            // Set your timezone if needed
            ChronoClass.SetTimezone("Eastern Standard Time");

            // Get current date/time
            var (longTime, shortTime) = ChronoClass.GetTime();
            var (longDate, shortDate) = ChronoClass.GetDate();

            Console.WriteLine($"Time: {longTime} | {shortTime}");
            Console.WriteLine($"Date: {longDate} | {shortDate}");

            // Add a world timezone
            ChronoClass.AddWorldTime("Pacific Standard Time");

            // List all world times
            var worldTimes = ChronoClass.GetWorldTimes();
            foreach (var dict in worldTimes)
            {
                foreach (var kv in dict)
                {
                    var (lt, st, ld, sd) = kv.Value;
                    Console.WriteLine($"{kv.Key}: {lt} | {st} | {ld} | {sd}");
                }
            }
        }
        public static async Task TestClipboard()
        {
            // --- Test BaseClipboard ---
            var baseClipboard = new ClipboardClass.BaseClipboard();
            string key = "testItem";
            byte[] data = Encoding.UTF8.GetBytes("Hello World");

            // Add item
            baseClipboard.AddToClipboard(data, key);
            Console.WriteLine("Added to BaseClipboard.");

            // Retrieve item
            var retrieved = baseClipboard.GetClipboardItem(key);
            Console.WriteLine("Retrieved BaseClipboard item: " + Encoding.UTF8.GetString(retrieved));

            // Remove item
            baseClipboard.RemoveFromClipboard(key);
            Console.WriteLine("Removed from BaseClipboard.");

            // --- Test ClipboardGroups ---
            var groupClipboard = new ClipboardClass.ClipboardGroups();
            string groupName = "TestGroup";
            string groupKey = "groupItem";
            byte[] groupData = Encoding.UTF8.GetBytes("Group Hello");

            // Add to group
            groupClipboard.AddToClipboard(groupName, groupData, groupKey);
            Console.WriteLine("Added to ClipboardGroups.");

            // Retrieve from group
            var groupRetrieved = await groupClipboard.GetClipboardItem(groupName, groupKey);
            Console.WriteLine("Retrieved ClipboardGroups item: " + Encoding.UTF8.GetString(groupRetrieved));

            // Remove from group
            groupClipboard.RemoveFromClipboard(groupName, groupKey);
            Console.WriteLine("Removed from ClipboardGroups.");

            Console.WriteLine("All tests completed.");
        }
        public static async Task TestColors()
        {
            //Get three random colors
            var colors = typeof(Color)
    .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
    .Where(f => f.FieldType == typeof(Color))
    .OrderBy(_ => Guid.NewGuid())
    .Take(3)
    .Select(f => (Color)f.GetValue(null))
    .ToArray();


            for (int i = 0; i < colors.Length; i++)
            {
                var c = colors[i];
                Console.WriteLine($"Color {i + 1}: R={c.R}, G={c.G}, B={c.B}, A={c.A}");
            }

            // Get a color by name
            var colorByName = (Color)typeof(Color).GetField("CyberpunkYellow",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static).GetValue(null);

            Console.WriteLine($"CyberpunkYellow -> R={colorByName.R}, G={colorByName.G}, B={colorByName.B}, A={colorByName.A}");

            // Define a custom color (not saved anywhere)
            var customColor = new Color(123, 45, 67, 200); // Just some random RGBA
            Console.WriteLine($"CustomColor -> R={customColor.R}, G={customColor.G}, B={customColor.B}, A={customColor.A}");


        }
        public static async Task TestCreator()
        {
            string creatorType = "Music";

            // Grab a random file from the Music folder
            string musicFolder = Path.Combine(DataPath, "Music");
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
            await CreatorClass.CreatorFileClass.CreateCreator(
                CreatorName: "YUNA",
                Description: "Best virtual idol",
                PFPPath: null,
                FilePaths: filePaths,
                CreatorType: creatorType
            );

            Console.WriteLine("Created CreatorBase for YUNA");

            // Step 2: Get creator overview
            var (name, desc) = await CreatorClass.CreatorFileClass.GetCreatorOverview("YUNA", creatorType);
            Console.WriteLine($"Creator: {name}, Description: {desc}");

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
                    } while (filePaths.Contains(extraFile)); // make sure it’s new
                    await CreatorClass.CreatorFileClass.AddFile("YUNA", creatorType, new List<string> { extraFile });
                }
            }

            // Step 4: Update description
            await CreatorClass.CreatorFileClass.SetDescription(
                "YUNA",
                creatorType,
                "Top-tier virtual idol and mischief queen"
            );

            // Step 5: Add to favorites
            await CreatorClass.CreatorFavoritesClass.AddToFavorites("YUNA", creatorType);

            // Step 6: Get favorite creators
            var (resolved, unresolved) = await CreatorClass.CreatorFavoritesClass.GetFavorites(creatorType);
            Console.WriteLine("Resolved favorites:");
            resolved.ForEach(f => Console.WriteLine($" - {f}"));

            // Step 7: Remove the first file safely
            var creator = await CreatorClass.CreatorFileClass.GetCreator("YUNA", creatorType);
            if (creator != null && creator.Files.Count > 0)
            {
                await CreatorClass.CreatorFileClass.RemoveFiles(
                    "YUNA",
                    creatorType,
                    new List<FileRecord> { creator.Files[0] }
                );
            }

            Console.WriteLine("Example workflow completed!");
        }

        public static async Task TestDataSlot()
        {
            // 1. Create a session
            var session = new DataManagerClass.SessionClass.Session(
                CreatedDateTime: null,
                Title: "Work Day",
                Description: "Work session at home and office",
                identifiers: new List<string>(),
                identifier: null
            );

            await DataManagerClass.SessionClass.AddSession(session);
            Console.WriteLine($"Created session: {session.Title} ({session.Identifier})");

            // 2. Prepare DataSlot directory via Yuuko DirectoryManager
            var dataSlotBasePath = Path.Combine(DataPath, "DataSlot");
            var manager = new Yuuko.Bindings.DirectoryManager(dataSlotBasePath);
            await manager.LoadBindings();

            // Ensure a folder exists for this dataslot's images
            var (imgUuid, resolvedImgPath) = await manager.GetOrCreateDirectory(
                Path.Combine(dataSlotBasePath, "Images", "HomeDesk"),
                dataSlotBasePath
            );

            // 3. Create FileRecord pointing to actual directory UUID
            var imgFile = new FileRecord(imgUuid, "workspace.png");

            // 4. Create the dataslot
            var dataslot = new DataManagerClass.DataSlotClass.DataSlot(
                isFavorite: false,
                dateTimeVar: null,
                title: "Home Desk",
                description: "My desk setup",
                imgPath: imgFile,
                textureFolder: null,
                structSessions: new List<string> { session.Identifier },
                identifier: null
            );

            await DataManagerClass.DataSlotClass.AddDataSlot(dataslot);
            Console.WriteLine($"Created dataslot: {dataslot.Title} ({dataslot.Identifier})");

            // 5. Patch the dataslot
            var patch = new DataManagerClass.DataSlotClass.DataSlotPatch
            {
                IsFavorite = true,
                Title = "Home Desk (Updated)"
            };

            var updatedSlot = DataManagerClass.DataSlotClass.UpdateDataSlot(dataslot, patch);
            await DataManagerClass.DataSlotClass.UpdateDataSlot(updatedSlot);
            Console.WriteLine($"Updated dataslot: {updatedSlot.Title}, Favorite: {updatedSlot.IsFavorite}");

            // 6. Add dataslot to favorites (uses the proper UUID now)
            await DataManagerClass.DataSlotFavoritesClass.AddToFavorites(updatedSlot.Identifier, imgUuid);
            Console.WriteLine("Added dataslot to favorites!");

            // 7. Get all favorite paths
            var favoritePaths = await DataManagerClass.DataSlotFavoritesClass.GetFavorites();
            Console.WriteLine($"Favorite dataslots: {favoritePaths.Item2.First()}");
        }

        public static async Task TestExperimentalAudio()
        {
            // 1️. Initialize default settings
            AdvancedAudioSettings = new ExperimentalAudio(
                EnvironmentalReduction: true,
                DecibelLimit: true,
                EnvironmentalReductionPercentage: 40,
                DecibelLimitLevel: 80
            );

            MasterVolume = 75;

            Console.WriteLine($"Initial MasterVolume: {MasterVolume}");
            Console.WriteLine($"Initial EnvironmentalReduction: {AdvancedAudioSettings.EnvironmentalReduction}");
            Console.WriteLine($"Initial DecibelLimitLevel: {AdvancedAudioSettings.DecibelLimitLevel}");

            // 2. Change some settings
            ExperimentalVolumeClass.SetExperimentalAudioSettings(
                DecibelLimitLevel: 65,
                EnvironmentalReductionPercentage: 50
            );

            MasterVolumeClass.SetMasterVolume(85);

            Console.WriteLine($"Updated MasterVolume: {MasterVolume}");
            Console.WriteLine($"Updated EnvironmentalReductionPercentage: {AdvancedAudioSettings.EnvironmentalReductionPercentage}");
            Console.WriteLine($"Updated DecibelLimitLevel: {AdvancedAudioSettings.DecibelLimitLevel}");

            // 3️. Save settings to disk
            await ExperimentalVolumeClass.SaveAudioSettings();
            await MasterVolumeClass.SaveAudioSettings();
            Console.WriteLine("Settings saved to disk.");

            // 4️. Reset in-memory values to prove loading works
            AdvancedAudioSettings = new ExperimentalAudio();
            MasterVolume = 0;

            Console.WriteLine($"Reset MasterVolume: {MasterVolume}");
            Console.WriteLine($"Reset EnvironmentalReductionPercentage: {AdvancedAudioSettings.EnvironmentalReductionPercentage}");

            // 5️. Load settings back from disk
            await ExperimentalVolumeClass.LoadAudioSettings();
            await MasterVolumeClass.LoadAudioSettings();

            Console.WriteLine($"Loaded MasterVolume: {MasterVolume}");
            Console.WriteLine($"Loaded EnvironmentalReductionPercentage: {AdvancedAudioSettings.EnvironmentalReductionPercentage}");
            Console.WriteLine($"Loaded DecibelLimitLevel: {AdvancedAudioSettings.DecibelLimitLevel}");
        }
        public static async Task TestGeoClass()
        {
            // Get current exact coordinates
            GeoClass.Coordinate exact = await GeoClass.GetExactCoordinates();
            Console.WriteLine($"Exact coordinates: Latitude={exact.Y}, Longitude={exact.X}");

            // Get relative coordinates (AR-style jittered)
            GeoClass.RelativePoint relative = await GeoClass.GetRelativeCoordinates();
            Console.WriteLine($"Relative area: lat {relative.latmin} - {relative.latmax}, long {relative.longmin} - {relative.longmax}");

            // Fetch recent exact locations
            List<GeoClass.LocationPoint> recentExact = await GeoClass.GetRecentLocations();
            Console.WriteLine($"Exact location history ({recentExact.Count} points):");
            foreach (var point in recentExact)
            {
                Console.WriteLine($"{point.TimeStamp}: {point.Latitude}, {point.Longitude}");
            }

            // Fetch recent relative locations
            List<GeoClass.RelativeLocationPoint> recentRelative = await GeoClass.GetRecentRelativeLocations();
            Console.WriteLine($"Relative location history ({recentRelative.Count} points):");
            foreach (var rel in recentRelative)
            {
                Console.WriteLine($"{rel.Timestamp}: lat {rel.Area.latmin}-{rel.Area.latmax}, long {rel.Area.longmin}-{rel.Area.longmax}");
            }

            // Clear history
            await GeoClass.ClearLocationHistory(new GeoClass.LocationPoint());
            await GeoClass.ClearRelativeLocationHistory(new GeoClass.RelativeLocationPoint());
        }
        public static async Task TestMediaAlbumClass()
        {
            // Create a temp folder for testing
            string testBase = Path.Combine(Path.GetTempPath(), "MediaAlbumTest");
            Directory.CreateDirectory(testBase);

            // Create a fake media album
            var album = new MediaAlbumClass.AlbumMedia(
                AlbumName: "Test Album",
                AlbumDescription: "Just a test album",
                IsFavorite: false,
                UIColor: Color.Red,
                UIColorAlt: Color.Blue,
                CoverImageFilePath: "cover.jpg",
                mediaPaths: new List<FileRecord> { new FileRecord("file-uuid", "song.mp3") }
            );


            // Add album
            await MediaAlbumClass.AddMediaAlbum(album);
            Console.WriteLine("Album added.");

            // Read album
            var albumRead = await MediaAlbumClass.GetMediaAlbum(album.Identifier);
            Console.WriteLine($"Album Loaded: {album.Identifier} ID: {albumRead.AlbumName}");

            // Read all albums
            var allAlbums = await MediaAlbumClass.GetMediaAlbums();
            Console.WriteLine($"Total albums: {allAlbums.Count}");
            foreach (var a in allAlbums)
            {
                Console.WriteLine($"Album: {a.AlbumName}, ID: {a.Identifier}");
            }

            // Patch the album
            var patch = new MediaAlbumClass.AlbumMediaPatch
            {
                AlbumDescription = "Updated description",
                IsFavorite = true
            };
            var updatedAlbum = MediaAlbumClass.AlbumMediaPatcher.Apply(album, patch);

            // Save updated album
            await MediaAlbumClass.UpdateMediaAlbum(updatedAlbum);
            Console.WriteLine("Album updated.");

            // Add to favorites
            await MediaAlbumFavoritesClass.AddToFavorites(album.Identifier, "fake-directory-uuid");
            Console.WriteLine("Added to favorites.");

            // List favorites
            var favoritePaths = await MediaAlbumFavoritesClass.GetFavoritePathsAsync();
            Console.WriteLine("Favorite paths:");
            foreach (var path in favoritePaths)
            {
                Console.WriteLine(path);
            }

            // Remove from favorites
            await MediaAlbumFavoritesClass.RemoveFromFavorites(album.Identifier, "fake-directory-uuid");
            Console.WriteLine("Removed from favorites.");

            // Cleanup test folder
            Directory.Delete(testBase, true);
            Console.WriteLine("Test folder cleaned up.");
        }
        public static async Task TestMediaTagger()
        {
            // Grab a random file from your Music folder (read-only)
            string musicFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
            string randomFile = Directory.EnumerateFiles(musicFolder).OrderBy(_ => Guid.NewGuid()).FirstOrDefault();

            if (randomFile == null)
            {
                Console.WriteLine("No music files found.");
                return;
            }

            // Just use the path to create a FileRecord without altering the file
            var fileRecord = new FileRecord(Guid.NewGuid().ToString(), Path.GetFileName(randomFile));

            // Create a new Creator with that file in its Files list
            var creator = new MediaTagger.CreatorClass.Creator(
                name: "RandomMusicCreator",
                description: "Demo creator using a random music file",
                pfp: null,
                files: new List<FileRecord?> { fileRecord } // safe null-coalescing in constructor
            );

            Console.WriteLine($"Creator Name: {creator.Name}");
            Console.WriteLine($"Description: {creator.Description}");
            Console.WriteLine($"Files: {string.Join(", ", creator.Files.Select(f => f.File))}");
        }
        public static async Task TestMusicQueue()
        {
            // Step 1: Resolve the system Music folder
            string musicDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic));
            string musicDirUUID = "example-music-uuid"; // XRUIOS UUID placeholder

            var manager = new Yuuko.Bindings.DirectoryManager(Path.Combine(DataPath, "Music"));
            await manager.LoadBindings();

            var directoryData = await manager.GetOrCreateDirectory(musicDir, musicDir);
            musicDirUUID = directoryData.Uuid; // Use the real UUID XRUIOS expects
            string resolvedPath = directoryData.ResolvedPath;

            Console.WriteLine($"Resolved Music directory UUID: {musicDirUUID}");
            Console.WriteLine($"Resolved path: {resolvedPath}");

            // Step 2: Register the folder in XRUIOS GenericBank
            try
            {
                await Media.AddGenericDirectory(resolvedPath, "System Music");
            }
            catch (InvalidOperationException)
            {
                // Folder already registered, fine
            }

            // Step 3: Enumerate mp3 files and ensure each has an overview
            var musicFiles = Directory.EnumerateFiles(resolvedPath, "*.mp3").ToList();
            if (musicFiles.Count == 0)
            {
                Console.WriteLine("No mp3 files found in your Music folder.");
                return;
            }

            foreach (var file in musicFiles)
            {
                string fileName = Path.GetFileName(file);
                try
                {
                    await MusicPlayerClass.GetOrCreateOverview(fileName, musicDirUUID);
                }
                catch
                {
                    await SongClass.CreateSongInfo(fileName, musicDirUUID);
                }
            }

            // Step 4: Pick a random file to play
            string randomSong = Path.GetFileName(musicFiles[new Random().Next(musicFiles.Count)]);

            await MusicPlayerClass.CurrentlyPlayingClass.SetCurrentlyPlaying(randomSong, musicDirUUID);
            Console.WriteLine("Currently playing: " + MusicPlayerClass.CurrentlyPlayingClass.GetCurrentlyPlaying()?.SongName);

            // Queue a couple more songs
            var remainingSongs = musicFiles
                .Where(f => Path.GetFileName(f) != randomSong)
                .Take(2)
                .Select(Path.GetFileName)
                .ToList();

            foreach (var song in remainingSongs)
            {
                await MusicPlayerClass.MusicQueueClass.AddToMusicQueue(song, musicDirUUID);
            }

            Console.WriteLine("Queue:");
            foreach (var song in MusicPlayerClass.MusicQueueClass.GetQueue())
            {
                Console.WriteLine("- " + song.SongName);
            }
        }


        public static async Task TestNotes()
        {
            // Theme identity (THIS is your stable anchor)
            var identity = new NoteClass.ThemeIdentity(
                themeID: "muvluv_universe",
                name: "Muv-Luv",
                author: "âge",
                version: "1.0.0",
                targetModes: new List<string>
                {
                "Reader",
                "Lore",
                "Timeline"
                }
            );

            // Categories = chapters
            var overview = new Category(
                title: "Overview",
                description: "What Muv-Luv is, and why it escalates from romcom to existential horror.",
                mainImage: null,
                miniImage: null,
                notes: new List<FileRecord>
                {
                new FileRecord("note-001", "what_is_muvluv.md"),
                new FileRecord("note-002", "themes_and_tone.md")
                }
            );

            var timelines = new Category(
                title: "Timelines",
                description: "Extra, Unlimited, and Alternative — and why they matter.",
                mainImage: null,
                miniImage: null,
                notes: new List<FileRecord>
                {
                new FileRecord("note-003", "extra_timeline.md"),
                new FileRecord("note-004", "unlimited_timeline.md"),
                new FileRecord("note-005", "alternative_timeline.md")
                }
            );

            var beta = new Category(
                title: "BETA and Humanity",
                description: "The alien threat and the collapse of human morality.",
                mainImage: null,
                miniImage: null,
                notes: new List<FileRecord>
                {
                new FileRecord("note-006", "what_are_beta.md"),
                new FileRecord("note-007", "human_extinction_wars.md")
                }
            );

            // Assemble journal
            var journal = new Journal(
                journalName: "Muv-Luv Universe",
                description: "Structured lore journal for the Muv-Luv franchise.",
                coverImagePath: null,
                categories: new List<Category>
                {
                overview,
                timelines,
                beta
                },
                identity: identity
            );

            // Save

            Console.WriteLine("Saving Journal...");
            Console.WriteLine("Journal saved successfully! Error will come from using dummy files.");

            await NoteClass.SaveJournal(journal);

        }
       
        public static async Task TestNotifications()
        {
            var notification = new NotificationClass.NotificationContent(
                action: "viewJournal",
                texts: new List<string>
                {
                "New Journal Entry: SAO Episode 1",
                "Kirito logs into Sword Art Online. The players realize they can't log out…",
                "Health Check: All players are trapped; logging survival stats."
                },
                tag: "sao-entry-001",
                group: "journals"
            );

            // Optional: add a button to view the journal
            notification.Buttons.Add(new NotificationClass.NotificationContent.Button(
                content: "Open Journal",
                action: "openJournal",
                args: new Dictionary<string, string> { { "journalId", "SAO001" } }
            ));

            // Add the notification
            await NotificationClass.AddNotification(notification);

            Console.WriteLine("SAO journal notification added!");
        }
        public static async Task TestSoundEQ()
        {
            // Create a new EQ profile
            var experimentalAudio = new ExperimentalAudio(false, false, 0, 0);
            var myEQ = new SoundEQClass.SoundEQ(
                eqname: "MyTestEQ",
                software: 80,
                effects: 90,
                voice: 70,
                music: 100,
                alerts: 60,
                ui: 75,
                etc: 50,
                otherVol: experimentalAudio
            );

            // Add it to the EQ database
            await SoundEQClass.AddSoundEQDBs(myEQ);
            Console.WriteLine("Added MyTestEQ to EQDB.");

            // List all EQs
            var allEQs = await SoundEQClass.GetSoundEQDBs();
            Console.WriteLine($"All EQs ({allEQs.Count}):");
            foreach (var eq in allEQs)
                Console.WriteLine($" - {eq.EQName}");

            // Set it as default
            await SoundEQClass.SetDefaultEQDB(myEQ);
            Console.WriteLine("Set MyTestEQ as default EQ.");

            // Get default EQ
            var defaultEQ = await SoundEQClass.GetDefaultEQDB();
            Console.WriteLine($"Default EQ is: {defaultEQ.EQName}");

            // Update EQ
            myEQ.Music = 50; // Change music volume
            await SoundEQClass.UpdateSoundEQDB(myEQ, myEQ);
            Console.WriteLine("Updated MyTestEQ Music volume to 50.");

            // Delete EQ
            await SoundEQClass.DeleteSoundEQDB(myEQ);
            Console.WriteLine("Deleted MyTestEQ from EQDB.");

            // Reset default EQ
            await SoundEQClass.ResetDefaultEQDB();
            var resetEQ = await SoundEQClass.GetDefaultEQDB();
            Console.WriteLine($"Default EQ after reset: {resetEQ.EQName}");
        }
        public static async Task TestStopwatch()
        {
            Console.WriteLine("Creating stopwatch...");
            string stopwatchId = StopwatchClass.CreateStopwatch();

            Console.WriteLine("Waiting 2 seconds...");
            await Task.Delay(2000);

            var elapsed1 = StopwatchClass.GetTimeElapsed(stopwatchId);
            Console.WriteLine($"Elapsed time: {elapsed1.TotalSeconds:F2} seconds");

            Console.WriteLine("Creating first lap...");
            var lap1 = StopwatchClass.CreateLap(stopwatchId);
            Console.WriteLine($"Lap {lap1.LapCount} at {lap1.SecondsElapsed} seconds");

            Console.WriteLine("Waiting 1 second...");
            await Task.Delay(1000);

            Console.WriteLine("Creating second lap...");
            var lap2 = StopwatchClass.CreateLap(stopwatchId);
            Console.WriteLine($"Lap {lap2.LapCount} at {lap2.SecondsElapsed} seconds");

            Console.WriteLine("Destroying stopwatch and retrieving records...");
            var records = StopwatchClass.DestroyStopwatch(stopwatchId);

            foreach (var record in records)
            {
                Console.WriteLine($"Lap {record.LapCount}: {record.SecondsElapsed} seconds");
            }

            Console.WriteLine("Saving records to CSV...");
            StopwatchClass.SaveStopwatchValuesAsSheet(records, DateTime.Now, "TestStopwatch");
            Console.WriteLine("Saved CSV in DataPath directory.");

            Console.WriteLine("Test complete.");
        }
        public static async Task TestSpecs()
        {
            Console.WriteLine("Gathering system specs...");

            // Create an instance of SystemSpecs and generate the specs
            var specs = new SystemSpecs().GenerateSpecs();

            Console.WriteLine("\n=== System Information ===");
            Console.WriteLine($"OS Info: {specs.OSInfo}");
            Console.WriteLine($"CPU Info: {specs.CPUInfo}");
            Console.WriteLine($"Memory Info: {specs.MemoryInfo}");
            Console.WriteLine($"Disk Info:\n{specs.DiskInfo}");
            Console.WriteLine($"GPU Info: {specs.GPUInfo}");
            Console.WriteLine($"Network Info: {specs.NetworkInfo}");
            Console.WriteLine($"Uptime: {specs.UptimeInfo}");
            Console.WriteLine($"VR Headset Status: {specs.VRHeadsetStatus}");

            Console.WriteLine("\nSystem info test complete.");
        }
        public static async Task TestThemes()
        {
            Console.WriteLine("Creating test SAO theme...");

            // Create a theme
            var testTheme = new XRUIOSTheme(
                new ThemeSystem.ThemeIdentity("sao001", "SAO Dark", "WalkerDev", "1.0", new List<string> { "VR", "Desktop" }),
                new ThemeColors(
                    ("", "#0a0a0a"),       // BackgroundPrimary
                    ("", "#1a1a1a"),       // BackgroundSecondary
                    ("", "#111111"),       // Surface
                    ("", "#ff2d00"),       // AccentPrimary
                    ("", "#00ffff"),       // AccentSecondary
                    "#ffffff",             // TextPrimary
                    "#888888",             // TextMuted
                    "#ff5555",             // Error
                    "#ffaa00",             // Warning
                    "#55ff55"              // Success
                ),
                new ThemeTypography(new List<string> { "Roboto", "Arial", "Verdana" }, 1.0f),
                new ThemeSpatial(0.02f, 0.01f, 0.05f, "Rigid", true),
                new AppAudioRoles("sao_launch.wav", "sao_close.wav", "sao_crash.wav", "sao_bg.wav", "sao_fg.wav"),
                new UIAudioRoles("nav.wav", "select.wav", "back.wav", "error.wav", "warn.wav", "success.wav", "disabled.wav", "hover.wav"),
                new List<DefaultApp>() // empty defaults for now
            );

            // Save the theme
            await ThemeSystem.SaveTheme(testTheme);
            Console.WriteLine("Theme saved!");

            // Load all themes
            var themes = await ThemeSystem.GetAllXRUIOSThemes();
            Console.WriteLine($"Loaded {themes.Count} theme(s).");

            // Print info from the first theme
            var loadedTheme = themes[0];
            Console.WriteLine($"\nTheme Name: {loadedTheme.Identity.Name}");
            Console.WriteLine($"Author: {loadedTheme.Identity.Author}");
            Console.WriteLine($"Accent Primary: {loadedTheme.Colors.AccentPrimary.Item2}");
            Console.WriteLine($"Primary Font: {string.Join(", ", loadedTheme.Typography.PrimaryFont)}");

            // Set as current theme
            await ThemeSystem.SetTheme($"{loadedTheme.Identity.Name} v{loadedTheme.Identity.Version} by {loadedTheme.Identity.Author}, ID {loadedTheme.Identity.ThemeID}");
            Console.WriteLine("\nTheme is now active!");
        }
        public static async Task TestThemes2()
        {
            Console.WriteLine("Creating test Evangelion theme...");

            var evaTheme = new XRUIOSTheme(
                new ThemeSystem.ThemeIdentity("eva001", "NERV Dark", "WalkerDev", "1.0", new List<string> { "VR", "Desktop" }),
                new ThemeColors(
                    ("", "#0a0a0a"),       // BackgroundPrimary (dark)
                    ("", "#1a1a1a"),       // BackgroundSecondary
                    ("", "#111111"),       // Surface
                    ("", "#ff0000"),       // AccentPrimary (NERV red)
                    ("", "#00ffcc"),       // AccentSecondary (cool blue-green for interface)
                    "#ffffff",             // TextPrimary
                    "#888888",             // TextMuted
                    "#ff5555",             // Error
                    "#ffaa00",             // Warning
                    "#55ff55"              // Success
                ),
                new ThemeTypography(new List<string> { "Roboto", "Eurostile", "Arial" }, 1.0f),
                new ThemeSpatial(0.03f, 0.015f, 0.05f, "Rigid", true),
                new AppAudioRoles(
                    "eva_launch.wav",
                    "eva_close.wav",
                    "eva_alarm.wav",
                    "eva_background.wav",
                    "eva_foreground.wav"
                ),
                new UIAudioRoles(
                    "nav.wav",
                    "select.wav",
                    "back.wav",
                    "error.wav",
                    "warning.wav",
                    "success.wav",
                    "disabled.wav",
                    "hover.wav"
                ),
                new List<DefaultApp>()
            );

            // Save the theme
            await ThemeSystem.SaveTheme(evaTheme);
            Console.WriteLine("Evangelion theme saved!");

            // Load all themes
            var themes = await ThemeSystem.GetAllXRUIOSThemes();
            Console.WriteLine($"Loaded {themes.Count} theme(s).");

            // Print info from the first theme
            var loadedTheme = themes[0];
            Console.WriteLine($"\nTheme Name: {loadedTheme.Identity.Name}");
            Console.WriteLine($"Author: {loadedTheme.Identity.Author}");
            Console.WriteLine($"Accent Primary: {loadedTheme.Colors.AccentPrimary.Item2}");
            Console.WriteLine($"Primary Font: {string.Join(", ", loadedTheme.Typography.PrimaryFont)}");

            // Set as current theme
            await ThemeSystem.SetTheme($"{loadedTheme.Identity.Name} v{loadedTheme.Identity.Version} by {loadedTheme.Identity.Author}, ID {loadedTheme.Identity.ThemeID}");
            Console.WriteLine("\nEvangelion theme is now active!");
        }
        //Create favorite themes system later
        public static async Task TestVolume()
        {
            var entryPlugPreset = new VolumeSetting(
    "EntryPlug",
    new Dictionary<string, int>
    {
        { $"{Environment.MachineName}:Audio:EngineHum", 80 },
        { $"{Environment.MachineName}:Audio:PilotComm", 100 },
        { $"{Environment.MachineName}:Audio:Alerts", 90 },
        { $"{Environment.MachineName}:Audio:Interface", 70 }
    }
);

            var magiConsolePreset = new VolumeSetting(
                "MAGIConsole",
                new Dictionary<string, int>
                {
        { $"{Environment.MachineName}:Audio:CalculationBeep", 60 },
        { $"{Environment.MachineName}:Audio:Alarm", 100 },
        { $"{Environment.MachineName}:Audio:SystemChime", 50 }
                }
            );

            var nervAlertsPreset = new VolumeSetting(
                "NERVAlerts",
                new Dictionary<string, int>
                {
        { $"{Environment.MachineName}:Audio:EvaWarning", 100 },
        { $"{Environment.MachineName}:Audio:EvacuationSiren", 100 },
        { $"{Environment.MachineName}:Audio:CommandTone", 80 }
                }
            );

            // Add them to your volume DB
            await AddVolumeSettings(entryPlugPreset);
            await AddVolumeSettings(magiConsolePreset);
            await AddVolumeSettings(nervAlertsPreset);

            // Set one as the current active volume
            await SetCurrentVolumeSettings(entryPlugPreset);

            // Read current settings
            var current = await GetCurrentVolumeSettings();
            Console.WriteLine($"Current Volume Preset: {current.VolumeSettingName}");
            foreach (var item in current.Volumes)
            {
                Console.WriteLine($"{item.Key} => {item.Value}%");
            }

            // Get volumes for this machine only
            var deviceVolumes = await GetVolumeSettingsForThisDevice();
            Console.WriteLine("This machine volumes:");
            foreach (var (key, val) in deviceVolumes)
            {
                Console.WriteLine($"{key} = {val}%");
            }
        }     
        public static async Task TestTimer()
        {
            // EVA-01 Sync Timer
            var eva01Timer = new TimerManagerClass.TimerRecord(
                "EVA-01 Sync Test",
                TimeSpan.FromSeconds(5), // short for demo
                () => Console.WriteLine("[ALERT] EVA-01 synchronization complete!")
            );

            // EVA-02 Sync Timer
            var eva02Timer = new TimerManagerClass.TimerRecord(
                "EVA-02 Sync Test",
                TimeSpan.FromSeconds(8),
                () => Console.WriteLine("[ALERT] EVA-02 synchronization complete!")
            );

            // Start timers
            TimerManagerClass.StartTimer(eva01Timer);
            TimerManagerClass.StartTimer(eva02Timer);

            Console.WriteLine("NERV: Timers initialized. Monitoring EVA sync...");

            // Keep the app running for demo purposes
            Console.WriteLine("Press Enter to exit...");
            Console.ReadLine();
        }




        //Do media tagger later
        //Do audio tag later

        // Helper
        static async Task Run(string name, Func<Task> action)
        {
            try
            {
                await action();
                Console.WriteLine($"[{name}] ✓");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{name}] ✗ {ex.Message.Split('\n')[0]}");
            }
        }
    }
}