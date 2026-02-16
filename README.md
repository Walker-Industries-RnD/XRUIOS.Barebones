ToDo:

<details>

Unfinished, there are still chunks that need to be fixed for the v1!

This is no longer Windows Centric (Although it uses Windows logic for specific things as an example); it instead now allows you to add on different interfaces through the Plagues Protocol system so Windows, Linux, Godot, etc. based logic is more easy to slap on!

Current Goals Include:

- Adding the Window Renderers for VR to Desktop and Desktop to VR
- Fixing Songs, Music Queue and Playlists (Possibly Geo stuff too, add virtual Geo)
- Making the ~~Stride3D~~ Godot runtime
- Connecting to Project Replicant with Eclipse/Dirac Sea
- Putting within Plagues Protocol

</details>



-----

# The Extended Reality User Interface Operating System - Orchestration Layer (+400 Functions)

<img width="300" alt="image" src="https://github.com/user-attachments/assets/1641c0bf-b74b-4ccf-a30b-766230fabded" />


# What is a XRUIOS?

**Imagine if your phone, computer, tablet, and TV could all work together like they're one magical device.**

## **The Dream:**
You're listening to music on your computer. You grab your phone to go out, and **the music automatically follows you** - not just the song, but your exact playlist position. You get in your car, and it's playing there too.

You're watching a movie on your TV. Your friend calls - you pause it, and later on your tablet in bed, **it remembers exactly where you stopped**.


Think of the sword Percy Jackson has:
- The pen is always right where you need it, even if you lose it
- Even if you lend it to a friend, it comes back

**The XRUIOS is that, but for your entire digital life.**

You set a timer while cooking. Your phone rings in another room. Instead of burning dinner, your smartwatch, TV, and phone ALL show the timer countdown. The oven beeps through ALL your devices.

You are playing a game with your friends but want to lay down a little. Use a quick command to see the display automatically moved onto your phone through screenshare.

Your grandma takes a photo on her tablet. Instead of "Where did it go? How do I send it?" It just appears on your phone, your dad's computer, and the digital picture frame automatically.

AND BEST OF ALL NO SUBSCRIPTIONS

## **What The Output Looks Like:**
<details>
    
1. Alarm Logic

<img width="663" height="223" alt="image" src="https://github.com/user-attachments/assets/133ce816-6c09-441b-8860-3d605f796711" />

2. Media Albums

<img width="746" height="241" alt="image" src="https://github.com/user-attachments/assets/598415fc-44ab-4c1e-8316-b30fdfa2f64d" />

3. Sound Settings

<img width="751" height="306" alt="image" src="https://github.com/user-attachments/assets/06452838-b557-43d0-9dfc-573f602dad3c" />


4. Data Slots

<img width="844" height="208" alt="image" src="https://github.com/user-attachments/assets/667d3a7e-10e6-4f18-9508-7fc9fb6c5098" />


## **What Using The XRUIOS Looks Like:**

<img width="678" height="641" alt="image" src="https://github.com/user-attachments/assets/cde0a424-0ae6-4465-ad9e-b0875b7a632b" />
</details>

Straight and to the point!

It features +400 functions across Yuuko Bindings (UUID-based file/folder portability), music/media management, spatial world points & sessions, calendar/alarms/timers, audio mixing & EQ, notifications, themes, journals, clipboard groups, and basic system utils. More are under development as well!

Because it uses Pariah Cybersecurity, it also automatically has NIST Ceritified PQC Resistant Algos at the helm!

# THE ONLY 5 FUNCTIONS YOU NEED TO REMEMBER

| Function | What it does | When to use |
|----------|--------------|-------------|
| `GetOrCreateDirectory(path)` | Turns path → UUID | When adding new content |
| `GetDirectoryById(uuid)` | Turns UUID → current path | Before accessing files |
| `AddGenericDirectory(path, name)` | Register a media folder | Setting up music/videos |
| `GetFile(uuid, filename)` | Gets actual file | Playing/opening files |
| `FileRecord` | Store (uuid + filename) | In your JSON data |


## **It Fixes These Annoying Things:**

1. **"Ugh, it's on my other device"** - Everything's everywhere, always
2. **"I can't open this file type"** - Everything just works, like magic
3. **"Which password did I use?"** - One secure key to your entire digital world
4. **"My phone is too small, my TV has no keyboard"** - Use the right device for each task, all connected

## **The Best Part - It Understands You:**

Current computers: **"Here are 1,000 files in folders"**
XRUIOS: **"Here's your work project, your vacation photos, the playlist you love"** 
Organized by meaning, not by techy folder names.

## **Imagine:**

- Your morning alarm knows if you stayed up late watching shows
- Your music player suggests songs based on ALL your devices' history
- Your calendar shows family events from everyone's devices
- Your games save progress across phone, computer, console

## **But Here's The Real Magic:**

It's **YOUR** system. Not Apple's, not Google's, not Microsoft's. It doesn't sell your data. It doesn't show ads. It's got NO SUBSCRIPTIONS EVER (Looking at you "metaverse"); It just makes your life easier.
Also did I mention it's open source and post quantum cryptography proof?

<img width="804" height="603" alt="image" src="https://github.com/user-attachments/assets/dfd1c491-2344-465e-9d12-9692b4765547" />


## **For Developers:**


For tech developers, it's a great tool as well.

Tired of sifting through code and trying to figure out how something works? The XRUIOS auto handles most of the headache of migrations and offers a library that's truly cross functional.

It uses Plagues Protocol for Principle of Least Privilege while also ensuring native based solutions are supported!

AKA, let's say you make a calendar app. Usually, you need to make a version for Windows and a version for Linux with slightly differing logic for optimization with each platform.

## **But Here's The Real Magic:**

With the XRUIOS? You can use any function and it will work without you needing to change things up! I could go more into this but it's 3:55AM and i'm tired



<img width="1200" height="733" alt="image" src="https://github.com/user-attachments/assets/742d31f9-8944-441b-9a0a-3b826f748441" />

# **Yuuko Bindings: The File System Abstraction**

## **What It Actually Does**

**Yuuko Bindings solves the fundamental problem of cross-device computing:** *"My files are on Device A, but I'm on Device B. Where are they now?"*

### **Core Concept:**
```csharp
// Original path on Windows Desktop:
"C:\Users\Alice\Music\Playlist\song.mp3"

// On Android phone becomes:
"/storage/emulated/0/Music/Playlist/song.mp3"

// On Linux laptop becomes:
"/home/alice/Music/Playlist/song.mp3"
```

<details>
    
**Yuuko Bindings gives all these paths the same UUID:** `"A3F8C9D2E1B7"`

### **How It Works (The Clever Parts):**

1. **Hash-Based UUID Generation:**
```csharp
// Uses xxHash64 on "DeviceName|FullPath"
// "Desktop-PC|C:\Users\Alice\Music" → "A3F8C9D2E1B7"
```
xxHash64 is fast, collision-resistant, and portable across platforms.

2. **Three-Tier Resolution:**
```csharp
public async Task<string?> GetDirectoryById(string directoryId)
{
    // 1. Try original path on original device
    if (binding.Ref.OriginalDevice == ThisDeviceId && 
        Directory.Exists(binding.Ref.FullPath))
        return binding.Ref.FullPath; // Fast path
    
    // 2. Try previously resolved path (verified)
    if (binding.Resolution?.Verified == true &&
        Directory.Exists(binding.Resolution.ResolvedPath))
        return binding.Resolution.ResolvedPath; // Cached
    
    // 3. Resolve/create with fallback
    var resolvedPath = ResolveDirectory(binding, defaultFolder);
    // Creates local version if needed
}
```

3. **Intelligent Fallback Creation:**
```csharp
// If "C:\Users\Alice\Music" doesn't exist on this device:
// Creates: "~/Documents/XRUIOS/A3F8C9D2E1B7/"
// And remembers this resolution
```

## **What This Enables (That Other Systems Don't):**

### **1. True Application Portability**
```csharp
// App code NEVER cares about actual paths:
var songPath = await dirManager.GetDirectoryById("A3F8C9D2E1B7");
// Works on ANY device without code changes
```

### **2. Seamless Device Switching**
- Save project on Windows desktop
- Open on Android tablet → files are there (copied/created locally)
- Edit on Linux laptop → same UUID, different physical path
- Sync changes back to original device when available

### **3. Partial/Offline Operation**
```csharp
// Device B can't reach Device A's original path?
// No problem: uses last-known resolution
// When Device A comes back online: automatic reconciliation
```

</details>

## **YuukoProtocol/PhotonStars*: The Missing Network Layer**

**This is where it gets interesting.** 

```
[Device A] ←→ [PhotonStars Network] ←→ [Device B]
                    ↑
              [Device C, D, E...]
```

### **The Vision: SAO: Ordinal Scale Style Device Mesh**

```csharp
public class PhotonStarNetwork
{
    // 1. DECENTRALIZED DISCOVERY
    public Task<List<StarNode>> DiscoverNodes()
    {
        // Not centralized servers, but peer-to-peer mesh
        // Each device is a "star" in the constellation
    }
    
    // 2. BINDING SYNCHRONIZATION
    public Task SyncBindings(StarNode targetNode)
    {
        // "Hey Device B, here are my directory bindings"
        // Device B merges them intelligently
        // Conflict resolution: timestamps, user preference
    }
    
    // 3. FEDERATED RESOLUTION
    public Task<string> FederatedResolve(string uuid)
    {
        // "Who has directory A3F8C9D2E1B7?"
        // Network consensus: "Device A has it at path X"
        // Or: "Multiple devices have it, here are options"
    }
    
    // 4. DYNAMIC REPLICATION
    public Task ReplicateIfHot(string uuid)
    {
        // If many devices request same directory
        // Automatically replicate to nearby nodes
        // Like BitTorrent for directories
    }
}
```

<details>
    
**Kirito's NerveGear (Device A)** ←→ **Akihiko's Server (Network Hub)** ←→ **Asuna's AmuSphere (Device B)**

Yuuko bindings are the **item/equipment database** that persists across logout/login (device changes).

## **How This Changes Everything:**

### **Current Reality:**
```
App → Local File Path → ERROR (not on this device)
```

### **With Yuuko Bindings:**
```
App → UUID → Local Cache → (optional) Network Fetch
```

### **With PhotonStars*:**
```
App → UUID → Local Cache → Network Query → 
       ↓
[10 nearby devices respond with their paths/sync status]
       ↓
Choose fastest/most recent → Stream/Sync
```
</details>




# How Yuuko Currently Works (%50 Complete)
    
### The Core Problem It Solves
Apps need to reference files across different devices, but file paths change between computers. What works on your dev machine breaks on someone else's. The Yuuko Protocol solves this by creating **device-independent identifiers** for directories that work everywhere.

### 1. Directory Identification (The Smart Part)
```csharp
public readonly struct DirectoryRef
```
This is where the magic happens. When you give it a path like `C:\Users\Walker\Music`, it:
- Takes the **full absolute path**
- Combines it with the **device name** (Environment.MachineName)
- Runs it through **xxHash64** with seed "YUUKO"
- Outputs a **16-character hex ID** (like `3F2A8B91C6D4E507`)

**This ID is the same on every device** that has the same path structure. No central server needed!

### 2. The Binding System
```csharp
public sealed class DirectoryBinding
```
Each binding stores:
- `Ref` - The original reference (path + device)
- `Resolution` - Where it actually exists NOW on this device

Think of it like a phonebook: you look up someone by name (UUID) to find their current address (resolved path).

### 3. The Resolution Logic
```csharp
public string ResolveDirectory(DirectoryBinding binding, string? defaultFolder = null)
```
When resolving a directory:
1. **Check if original path exists** → use it directly
2. **If not, create fallback** → `MyDocuments/XRUIOS/{UUID}`
3. **Save the resolution** for next time

This means if you move a folder, the system adapts without breaking references.

### 4. Cross-Device Magic
```csharp
if (binding.Ref.OriginalDevice == ThisDeviceId && Directory.Exists(binding.Ref.FullPath))
    return binding.Ref.FullPath;
```
If the current device is the original one AND the path still exists, use it. Otherwise, resolve locally. This lets you:
- Share directory references between devices
- Each device resolves to its own local copy
- No manual path fixing needed

### 5. The Generic Media System
The `Media` class wraps all this for actual file access:
```csharp
public static async Task<ResolvedMedia> GetFile(string directoryUuid, string fileName)
```
It:
1. Takes a **directory UUID** + **filename**
2. Resolves the directory path
3. Validates the file exists
4. Returns full metadata

### 6. The App Handle System
```csharp
public record Handle
```
This is genius—it lets apps be referenced in multiple ways:
- `Desktop` - Running app window ID
- `LocalApp` - Installed app name
- `YuukoApp` - Cross-platform app definition
- `BinaryData` - Ad-hoc app bytes
- `DirectoryRef` - App stored in a directory

</details>

### Why We Use This

1. **No Central Server** - Completely peer-to-peer ID generation
2. **Collision Resistant** - xxHash64 with seed + device + path = practically unique
3. **Self-Healing** - If paths move, it creates fallbacks
4. **Security Built In** - Uses Pariah's encryption everywhere
5. **Simple API** - `GetFile(uuid, filename)` just works

### Simple Example
```csharp
// On Device A
var (uuid, path) = await manager.GetOrCreateDirectory(@"C:\Music");

// Share just the UUID with Device B
// On Device B
var file = await Media.GetFile(uuid, "song.mp3");
// It just works, even if music is in D:\Songs on Device B!
```

See? It's elegant! You should be proud of this, dummy! *Crosses arms smugly.*


</details>

















<img width="1844" height="1080" alt="image" src="https://github.com/user-attachments/assets/4d47635f-0d37-4ebc-a05f-a3779dd84e22" />

# Functions

*Note; there are 400+ systems i've developed for this thing, i'm using AI to help here although when I make a proper wiki it'll get a good onceover; if you don't like that go suck an egg.*

<details>


---

## **AlarmClass.cs** (15 functions)

1. `Alarm()` - Constructor for Alarm record
2. `Alarm(string, DateTime, bool, List<DayOfWeek>, FileRecord, int, bool)` - Full constructor
3. `AddAlarm(Alarm)` - Creates and saves a new alarm, schedules it
4. `LoadAlarms()` - Loads all alarms from storage, schedules them
5. `UpdateAlarm(Alarm, Action<Alarm>)` - Updates existing alarm, reschedules
6. `DeleteAlarm(Alarm)` - Deletes alarm, removes scheduled jobs
7. `ScheduleAlarm(Alarm)` - Schedules alarm with Hangfire
8. `FireAlarm(Alarm)` - Triggered when alarm goes off
9. `ScheduleNextOccurrence(Alarm)` - Schedules next occurrence for recurring alarms
10. `ScheduleAllAlarms()` - Schedules all loaded alarms
11. `Start()` - Starts Hangfire server
12. `Stop()` - Stops Hangfire server

---

## **AppClass.cs** (16 functions)

13. `XRUIOSAppManifest()` - Constructor
14. `XRUIOSAppManifest(string, string, string, string, string, FileRecord, string, string)` - Full constructor
15. `XRUIOSAppManifestPatch()` - Constructor for patch record
16. `UpdateDataSlot(XRUIOSAppManifest, XRUIOSAppManifestPatch)` - Applies patch to manifest
17. `AddApp(XRUIOSAppManifest)` - Creates new app manifest
18. `GetApp()` - Gets all app manifests
19. `GetApp(string)` - Gets specific app by identifier
20. `UpdateApp(XRUIOSAppManifest)` - Updates existing app
21. `DeleteApp(string)` - Deletes app manifest
22. `AddToFavorites(string, string)` - Adds app to favorites
23. `GetFavorites()` - Gets favorite apps (resolved/unresolved)
24. `GetFavoritePathsAsync(bool)` - Gets favorite paths
25. `RemoveFromFavorites(string, string)` - Removes app from favorites

---

## **AreaManager.cs** (25 functions)

26. `WorldPoint()` - Constructor
27. `WorldPoint(RenderingMode, byte[], string, string, FileRecord, bool, List<StaticObject>, List<App>, List<DesktopScreen>, List<StaciaItems>, string)` - Full constructor
28. `StaticObject()` - Constructor
29. `StaticObject(PositionalTrackingMode?, RotationalTrackingMode?, string, Vector3?, ObjectOSLabel, FileRecord)` - Full constructor
30. `App()` - Constructor
31. `App(PositionalTrackingMode?, RotationalTrackingMode?, Vector3?, ObjectOSLabel, Yuuko.Handle?)` - Full constructor
32. `DesktopScreen()` - Constructor
33. `DesktopScreen(PositionalTrackingMode?, RotationalTrackingMode?, Vector3?, ObjectOSLabel, Yuuko.Handle?)` - Full constructor
34. `StaciaItems()` - Constructor
35. `StaciaItems(PositionalTrackingMode?, RotationalTrackingMode?, Vector3?, ObjectOSLabel, Yuuko.Handle?)` - Full constructor
36. `WorldPointPatch()` - Constructor
37. `StaticObjectPatch()` - Constructor
38. `AppPatch()` - Constructor
39. `DesktopScreenPatch()` - Constructor
40. `StaciaItemsPatch()` - Constructor
41. `UpdateWorldPoint(WorldPoint, WorldPointPatch)` - Applies patch to world point
42. `UpdateStaticObject(StaticObject, StaticObjectPatch)` - Applies patch to static object
43. `UpdateApp(App, AppPatch)` - Applies patch to app
44. `UpdateDesktopScreen(DesktopScreen, DesktopScreenPatch)` - Applies patch to desktop screen
45. `UpdateStaciaItems(StaciaItems, StaciaItemsPatch)` - Applies patch to Stacia items
46. `AddWorldPoint(WorldPoint)` - Creates new world point
47. `GetWorldPoints()` - Gets all world point identifiers
48. `GetWorldPoint(string)` - Gets specific world point
49. `UpdateWorldPoint(WorldPoint)` - Updates world point
50. `DeleteWorldPoint(string)` - Deletes world point

---

## **CalendarClass.cs** (14 functions)

51. `CreateSimpleEvent(DateTime, string, string, TimeZoneInfo, int, List<FileRecord>)` - Creates simple calendar event
52. `CreateRecurringEvent(DateTime, string, string, RecurrencePattern, TimeZoneInfo, int, List<FileRecord>)` - Creates recurring event
53. `LoadAllEvents()` - Loads all calendar events
54. `GetEventsForDay(DateTime)` - Gets events for specific day
55. `GetEventsInRange(DateTime, DateTime)` - Gets events in time range
56. `GetEventByUid(string)` - Gets event by UID
57. `UpdateEventByUid(string, Action<CalendarEvent>)` - Updates event by UID
58. `DeleteEventByUid(string)` - Deletes event by UID
59. `Notify(string)` - Sends calendar notification
60. `ScheduleUpcomingOccurrences(IEnumerable<Occurrence>, TimeSpan)` - Schedules upcoming occurrences

---

## **ChronoClass.cs** (20 functions)

61. `DateData()` - Constructor
62. `DateData(TimeFormat, ShortTime, ShortDate, LongTime, LongDate, string, List<string>)` - Full constructor
63. `SetCurrentDate(DateData)` - Sets current date/time settings
64. `GetCurrentDate()` - Gets current date/time settings
65. `SaveDateData()` - Saves date/time settings
66. `LoadDateData()` - Loads date/time settings
67. `GetTimezone(string)` - Gets timezone
68. `SetTimezone(string)` - Sets system timezone
69. `GetDate()` - Gets formatted date (long and short)
70. `SetDate(ShortDate, LongDate)` - Sets date formats
71. `GetTime()` - Gets formatted time (long and short)
72. `SetTime(ShortTime, LongTime)` - Sets time formats
73. `AddWorldTime(string)` - Adds world timezone
74. `GetWorldTimezoneCollection()` - Gets all world timezones
75. `GetWorldTimes()` - Gets formatted times for all world timezones
76. `GetTimeInTimezone(string)` - Gets formatted time in specific timezone
77. `DeleteWorldTime(string)` - Removes world timezone
78. `NumberToWords(long)` - Converts number to words
79. `ConvertToWordedMonth(int)` - Converts month number to name

---

## **ClipboardClass.cs** (9 functions)

80. `LoadClipboard()` - Loads clipboard (ungrouped)
81. `GetClipboardItem(string)` - Gets clipboard item
82. `AddToClipboard(byte[], string)` - Adds to clipboard
83. `RemoveFromClipboard(string)` - Removes from clipboard
84. `LoadClipboard(string)` - Loads clipboard group
85. `GetClipboardItem(string, string)` - Gets item from group
86. `AddToClipboard(string, byte[], string)` - Adds to group
87. `RemoveFromClipboard(string, string)` - Removes from group

---

## **Color.cs** (2 functions)

88. `Color()` - Constructor
89. `Color(int, int, int, int)` - Full constructor

---

## **CreatorClass.cs** (17 functions)

90. `Creator()` - Constructor
91. `Creator(string, string, FileRecord, List<FileRecord>)` - Full constructor
92. `CreateCreator(string, string?, string?, List<string>, string)` - Creates new creator
93. `GetCreator(string, string)` - Gets creator by name
94. `GetCreatorOverview(string, string)` - Gets creator name/description
95. `GetCreatorFiles(string, string)` - Gets creator's files
96. `AddFile(string, string, List<string>)` - Adds files to creator
97. `SetDescription(string, string, string)` - Sets creator description
98. `RemoveFiles(string, string, List<FileRecord>)` - Removes files from creator
99. `AddToFavorites(string, string)` - Adds creator to favorites
100. `GetFavorites(string)` - Gets favorite creators
101. `GetFavoritePathsAsync(string, bool)` - Gets favorite creator paths
102. `RemoveFromFavorites(string, string)` - Removes creator from favorites
103. `InitiateCreatorClass(string)` - Initializes creator system

---

## **DataManagerClass.cs** (27 functions)

104. `Session()` - Constructor
105. `Session(DateTime?, string, string, List<string>, string?)` - Full constructor
106. `AddSession(Session)` - Creates new session
107. `GetSession()` - Gets all session identifiers
108. `GetSession(string)` - Gets specific session
109. `UpdateSession(Session)` - Updates session
110. `DeleteSession(string)` - Deletes session
111. `UpdateSession(Session, SessionPatch)` - Applies patch to session
112. `InitiateSession(string)` - Initializes session
113. `DataSlot()` - Constructor
114. `DataSlot(bool, DateTime?, string, string, FileRecord, FileRecord?, List<string>, string?)` - Full constructor
115. `UpdateDataSlot(DataSlot, DataSlotPatch)` - Applies patch to data slot
116. `AddDataSlot(DataSlot)` - Creates new data slot
117. `GetDataSlot()` - Gets all data slot identifiers
118. `GetDataSlot(string)` - Gets specific data slot
119. `UpdateDataSlot(DataSlot)` - Updates data slot
120. `DeleteDataSlot(string)` - Deletes data slot
121. `InitiateDataSlot(string)` - Initializes data slot
122. `AddToFavorites(string, string)` - Adds data slot to favorites
123. `GetFavorites()` - Gets favorite data slots
124. `GetFavoritePathsAsync(bool)` - Gets favorite data slot paths
125. `RemoveFromFavorites(string, string)` - Removes data slot from favorites

---

## **ExperimentalAudioClass.cs** (11 functions)

126. `ExperimentalAudio()` - Constructor
127. `ExperimentalAudio(bool, bool, int, int)` - Full constructor
128. `GetExperimentalAudioSettings()` - Gets advanced audio settings
129. `SetExperimentalAudioSettings(bool?, bool?, int?, int?)` - Sets advanced audio settings
130. `SaveAudioSettings()` - Saves advanced audio settings
131. `LoadAudioSettings()` - Loads advanced audio settings
132. `GetMasterVolume()` - Gets master volume
133. `SetMasterVolume(int)` - Sets master volume
134. `SaveMasterVolume()` - Saves master volume
135. `LoadMasterVolume()` - Loads master volume

---

## **GeoClass.cs** (12 functions)

136. `Coordinate()` - Constructor
137. `Coordinate(double, double)` - Full constructor
138. `LocationPoint()` - Constructor
139. `LocationPoint(DateTime, double, double)` - Full constructor
140. `GetExactCoordinates()` - Gets current GPS coordinates
141. `SaveLocationHistory(LocationPoint)` - Saves location to history
142. `GetRecentLocations()` - Gets recent location history
143. `ClearLocationHistory(LocationPoint)` - Clears location history
144. `RelativePoint()` - Constructor
145. `RelativePoint(double, double, double, double)` - Full constructor
146. `RelativeLocationPoint()` - Constructor
147. `RelativeLocationPoint(DateTime, RelativePoint)` - Full constructor
148. `GetRelativeCoordinates()` - Gets relative (jittered) coordinates
149. `ConvertToRelativeCoordinates(double, double)` - Converts exact to relative
150. `SaveRelativeLocationHistory(RelativeLocationPoint)` - Saves relative location
151. `GetRecentRelativeLocations()` - Gets recent relative locations
152. `ClearRelativeLocationHistory(RelativeLocationPoint)` - Clears relative history
153. `SaveVirtualLocationHistory(LocationPoint)` - Saves virtual location
154. `GetVirtualRelativeLocations()` - Gets virtual locations
155. `ClearVirtualLocationHistory(LocationPoint)` - Clears virtual history

---

## **MediaAlbumClass.cs** (11 functions)

156. `AlbumMedia()` - Constructor
157. `AlbumMedia(string, string, bool, Color, Color, string, List<FileRecord>)` - Full constructor
158. `Apply(AlbumMedia, AlbumMediaPatch)` - Applies patch to album
159. `AddMediaAlbum(AlbumMedia)` - Creates new media album
160. `GetMediaAlbums()` - Gets all media albums
161. `GetMediaAlbum(string)` - Gets specific media album
162. `UpdateMediaAlbum(AlbumMedia)` - Updates media album
163. `DeleteMediaAlbum(string)` - Deletes media album
164. `AddToFavorites(string, string)` - Adds album to favorites
165. `GetFavorites()` - Gets favorite albums
166. `GetFavoritePathsAsync(bool)` - Gets favorite album paths
167. `RemoveFromFavorites(string, string)` - Removes album from favorites

---

## **MediaTagger.cs** (16 functions)

168. `Creator()` - Constructor
169. `Creator(string, string, FileRecord, List<FileRecord>)` - Full constructor
170. `CreateCreator(string, string?, string?, List<string>, string)` - Creates creator
171. `GetCreator(string, string)` - Gets creator
172. `GetCreatorOverview(string, string)` - Gets creator overview
173. `GetCreatorFiles(string, string)` - Gets creator files
174. `AddFile(string, string, List<string>)` - Adds files to creator
175. `SetDescription(string, string, string)` - Sets creator description
176. `RemoveFiles(string, string, List<FileRecord>)` - Removes files
177. `AddToFavorites(string, string)` - Adds creator to favorites
178. `GetFavorites(string)` - Gets favorite creators
179. `GetFavoritePathsAsync(string, bool)` - Gets favorite creator paths
180. `RemoveFromFavorites(string, string)` - Removes creator from favorites

---

## **MusicPlayerClass.cs** (8 functions)

181. `GetCurrentlyPlaying()` - Gets currently playing song
182. `SetCurrentlyPlaying(string, string)` - Sets currently playing song
183. `ResetCurrentlyPlaying()` - Clears currently playing
184. `GetQueue()` - Gets music queue
185. `AddToMusicQueue(string, string)` - Adds song to queue
186. `ReorderSong(SongOverview, int)` - Reorders song in queue
187. `RemoveSong(SongOverview)` - Removes song from queue
188. `RemoveSong(int)` - Removes song at index
189. `ResetQueue()` - Clears queue
190. `GetOrCreateOverview(string, string)` - Gets or creates song overview

---

## **NoteClass.cs** (15 functions)

191. `Note()` - Constructor
192. `Note(string, string, DateTime, DateTime, string, string, string, string, FileRecord, List<FileRecord>?)` - Full constructor
193. `Journal()` - Constructor
194. `Journal(string, string, string, List<Category>, ThemeIdentity)` - Full constructor
195. `Category()` - Constructor
196. `Category(string, string, string, string, List<FileRecord>)` - Full constructor
197. `ThemeIdentity()` - Constructor
198. `ThemeIdentity(string, string, string, string, List<string>)` - Full constructor
199. `SaveJournal(Journal)` - Saves journal
200. `GetAllJournals()` - Gets all journals
201. `GetJournal(string)` - Gets specific journal
202. `GetCategory(string, string)` - Gets category from journal
203. `UpdateJournal(Journal, Journal)` - Updates journal
204. `DeleteJournal(string)` - Deletes journal
205. `AddJournalToFavorites(string)` - Adds journal to favorites
206. `GetJournalFavorites()` - Gets favorite journals
207. `GetFavoriteJournalIdsAsync(bool)` - Gets favorite journal IDs
208. `RemoveJournalFromFavorites(string)` - Removes journal from favorites
209. `AddHistoryEntry(string, string, string, Dictionary<string, string>?)` - Adds history entry
210. `GetHistory(string, string?)` - Gets history entries

---

## **NotificationClass.cs** (7 functions)

211. `NotificationContent()` - Constructor
212. `NotificationContent(string, Dictionary<string,string>?, List<string>?, FileRecord?, FileRecord?, List<Button>?, string?, string?, DateTime?)` - Full constructor
213. `Button()` - Constructor
214. `Button(string, string, Dictionary<string,string>?, bool)` - Full constructor
215. `AddNotification(NotificationContent)` - Adds notification
216. `GetNotifications(bool)` - Gets notifications
217. `RemoveNotification(string, string)` - Removes notification
218. `ClearAllNotifications()` - Clears all notifications

---

## **ObservableProperty.cs** (3 functions)

219. `ObservableProperty(T)` - Constructor
220. `Set(T)` - Sets value and triggers event
221. `Get()` - Gets value

---

## **Processes.cs** (11 functions)

222. `ProcessInfo()` - Constructor
223. `ProcessInfo(string, int, string, string, string?, DateTime?, long, float, string?, string?, bool)` - Full constructor
224. `GetCurrentProcesses()` - Gets all running processes
225. `DetectProcessType(string, string?)` - Detects process type
226. `GetMainWindowTitle(Process)` - Gets window title
227. `GetProcessStartTime(Process)` - Gets start time
228. `GetExecutablePath(Process)` - Gets executable path
229. `GetCpuUsage(Process)` - Gets CPU usage
230. `SaveProcessSnapshot(string?)` - Saves process snapshot
231. `GetSavedSnapshots()` - Gets saved snapshots
232. `LoadProcessSnapshot(string)` - Loads snapshot
233. `ShareProcessSnapshot(string?)` - Shares snapshot
234. `GetProcessesByType()` - Groups processes by type
235. `KillProcess(int, string)` - Kills process

---

## **RecentlyRecordedClass.cs** (4 functions)

236. `GetRecentlyRecorded()` - Gets recently recorded items
237. `AddToRecentlyRecorded(FileRecord)` - Adds to recently recorded
238. `DeleteSoundRecentlyRecorded(FileRecord)` - Removes from recently recorded
239. `ClearRecentlyRecorded()` - Clears recently recorded

---

## **Songs.cs** (45 functions)

240. `SongOverview()` - Constructor
241. `SongOverview(string, string, DateTime?, string?, string?, TimeSpan?, DateTime?, Guid?, string?, int?, bool?, int)` - Full constructor
242. `SongDetailed()` - Constructor
243. `SongDetailed(string, string, DateTime?, string?, string?, string?, string?, string?, string?, string?, string?, string?, string?, List<string>?, string?, int?, int?, int?, int?, string?, string?, string?, string?, string?, int?, DateTime?, int?, DateTime?, string?, string?, string?, string?, string?, string?, string?, int?, string?, string?, string?, string?, string?, List<SongChapter>?, string?, List<LyricLine>?)` - Full constructor
244. `SongChapter()` - Constructor
245. `SongChapter(TimeSpan, TimeSpan?, string)` - Full constructor
246. `LyricLine()` - Constructor
247. `LyricLine(TimeSpan, string)` - Full constructor
248. `ExtractChapters(Track)` - Extracts chapters from audio
249. `ExtractUnsyncedLyrics(Track)` - Extracts unsynced lyrics
250. `ParseLrc(string?)` - Parses LRC lyrics
251. `ParseSrt(string?)` - Parses SRT lyrics
252. `ExtractSyncedLyrics(Track)` - Extracts synced lyrics
253. `AddSongDirectory(string)` - Adds song directory
254. `RefreshDirectory()` - Refreshes directory
255. `Initialize()` - Initializes music system
256. `CreateSongInfo(string, string, bool)` - Creates song metadata
257. `GetSongInfo(string, string, MusicInfoStyle)` - Gets song info
258. `UpdateSongInfo(string, string, SongInfoPatch, MusicInfoStyle, bool)` - Updates song info
259. `PatchTouchesOverview(SongInfoPatch)` - Checks if patch affects overview
260. `PatchTouchesDetailed(SongInfoPatch)` - Checks if patch affects detailed
261. `GetRequiredCapabilities(SongInfoPatch)` - Gets required tag capabilities
262. `GetWritableCapabilities(string, SongInfoPatch)` - Gets writable tag capabilities
263. `DeleteSongInfo(string, string, bool)` - Deletes song metadata
264. `AddSongDirectory(string, string)` - Adds song directory
265. `GetSongDirectories()` - Gets all song directories
266. `GetSongDirectories(bool)` - Gets song directory paths
267. `UpdateSongDirectory(string, string, string)` - Updates song directory
268. `RemoveSongDirectory(string, bool)` - Removes song directory
269. `AddToFavorites(string, string)` - Adds song to favorites
270. `GetFavorites()` - Gets favorite songs
271. `GetFavoritePathsAsync(bool)` - Gets favorite song paths
272. `RemoveFromFavorites(string, string)` - Removes song from favorites
273. `GetAllSongs(bool)` - Gets all songs (resolved/unresolved)
274. `GetAllSongs(bool, bool)` - Gets all song paths
275. `IsAudioFile(string)` - Checks if file is audio
276. `GetSongsInDirectoryAsync(string, bool)` - Gets songs in directory
277. `GetSongsByNameAsync(string, StringComparison, bool)` - Searches songs by name
278. `GetSongsByTag(SongSearchField, string, StringComparison, bool)` - Searches songs by tag
279. `AddToPlayHistory(string, string)` - Adds to play history
280. `GetPlayHistory()` - Gets play history
281. `ClearPlayHIstory(string, string)` - Clears play history
282. `Playlist()` - Constructor
283. `Playlist(string, string?, string?, List<PlaylistEntry>?, string?)` - Full constructor
284. `PlaylistEntry()` - Constructor
285. `PlaylistEntry(string, string, int)` - Full constructor
286. `CreatePlaylist(Playlist)` - Creates playlist
287. `GetAllPlaylists()` - Gets all playlists
288. `GetPlaylist(string)` - Gets specific playlist
289. `UpdatePlaylist(Playlist)` - Updates playlist
290. `DeletePlaylist(string)` - Deletes playlist
291. `AddSongToPlaylist(string, string, string)` - Adds song to playlist
292. `RemoveSongFromPlaylist(string, string, string)` - Removes song from playlist
293. `ReorderPlaylist(string, List<string>)` - Reorders playlist
294. `RecalculatePlaylistDuration(string)` - Recalculates playlist duration
295. `ToggleFavorite(string)` - Toggles playlist favorite
296. `GetResolvedPlaylistSongs(string)` - Gets resolved song paths
297. `CreatePlaylistFromFolder(string, string, string?)` - Creates playlist from folder
298. `CreatePlaylistFromFavorites(string)` - Creates playlist from favorites

---

## **SoundEQ.cs** (12 functions)

299. `SoundEQ()` - Constructor
300. `SoundEQ(string, float, float, float, float, float, float, float, ExperimentalAudio)` - Full constructor
301. `GetCurrentEQ()` - Gets current EQ setting
302. `SetCurrentEQ(SoundEQ)` - Sets current EQ
303. `AddSoundEQDBs(SoundEQ)` - Adds EQ to database
304. `GetSoundEQDBs()` - Gets all EQs
305. `GetSoundEQDB(string)` - Gets specific EQ
306. `UpdateSoundEQDB(SoundEQ, SoundEQ)` - Updates EQ
307. `DeleteSoundEQDB(SoundEQ)` - Deletes EQ
308. `ClearEQDB()` - Clears EQ database
309. `SetDefaultEQDB(SoundEQ)` - Sets default EQ
310. `GetDefaultEQDB()` - Gets default EQ
311. `ResetDefaultEQDB()` - Resets default EQ

---

## **StopwatchClass.cs** (5 functions)

312. `StopwatchRecord()` - Constructor
313. `StopwatchRecord(int, int)` - Full constructor
314. `CreateStopwatch()` - Creates new stopwatch
315. `GetTimeElapsed(string)` - Gets elapsed time
316. `CreateLap(string)` - Creates lap record
317. `DestroyStopwatch(string)` - Destroys stopwatch
318. `SaveStopwatchValuesAsSheet(List<StopwatchRecord>, DateTime, string)` - Saves as CSV

---

## **SystemInfoDisplayClass.cs** (8 functions)

319. `SystemSpecs()` - Constructor
320. `GenerateSpecs()` - Generates system specs
321. `GetOSInfo()` - Gets OS info
322. `GetCPUInfo()` - Gets CPU info
323. `GetMemoryInfo()` - Gets memory info
324. `GetDiskInfo()` - Gets disk info
325. `GetGPUInfo()` - Gets GPU info
326. `GetNetworkInfo()` - Gets network info
327. `GetUptimeInfo()` - Gets uptime info
328. `CheckHardware()` - Checks hardware capabilities

---

## **ThemeClass.cs** (22 functions)

329. `ThemeColors()` - Constructor
330. `ThemeColors((string,string), (string,string), (string,string), (string,string), (string,string), string, string, string, string, string)` - Full constructor
331. `ThemeTypography()` - Constructor
332. `ThemeTypography(List<string>, float)` - Full constructor
333. `ThemeSpatial()` - Constructor
334. `ThemeSpatial(float, float, float, string, bool)` - Full constructor
335. `UIAudioRoles()` - Constructor
336. `UIAudioRoles(string?, string?, string?, string?, string?, string?, string?, string?)` - Full constructor
337. `AppAudioRoles()` - Constructor
338. `AppAudioRoles(string?, string?, string?, string?, string?)` - Full constructor
339. `ThemeIdentity()` - Constructor
340. `ThemeIdentity(string, string, string, string, List<string>)` - Full constructor
341. `DefaultApp()` - Constructor
342. `DefaultApp(string, string, int, bool, List<DefaultAppImage>, List<ThemeTypography>, List<DefaultAppSound>, string?)` - Full constructor
343. `DefaultAppSound()` - Constructor
344. `DefaultAppSound(string, string, float, float, bool)` - Full constructor
345. `DefaultAppImage()` - Constructor
346. `DefaultAppImage(string, string, int, int, bool)` - Full constructor
347. `XRUIOSTheme()` - Constructor
348. `XRUIOSTheme(ThemeIdentity, ThemeColors, ThemeTypography, ThemeSpatial, AppAudioRoles, UIAudioRoles, List<DefaultApp>)` - Full constructor
349. `SaveTheme(XRUIOSTheme)` - Saves theme
350. `GetAllXRUIOSThemes()` - Gets all themes
351. `GetXRUIOSTheme(string)` - Gets specific theme
352. `GetCurrentTheme(string)` - Gets current theme
353. `UpdateTheme(XRUIOSTheme, XRUIOSTheme)` - Updates theme
354. `SetTheme(string)` - Sets current theme
355. `DeleteXRUIOSTheme(string)` - Deletes theme

---

## **TimerManagerClass.cs** (6 functions)

356. `TimerRecord(string, TimeSpan, Action?)` - Constructor
357. `StartTimer(TimerRecord)` - Starts timer
358. `AddTime(string, TimeSpan)` - Adds time to timer
359. `CancelTimer(string)` - Cancels timer
360. `ScheduleTimerJob(TimerRecord)` - Schedules timer job
361. `FireTimer(string)` - Fires timer callback

---

## **VolumeClass.cs** (12 functions)

362. `VolumeSetting()` - Constructor
363. `VolumeSetting(string, Dictionary<string,int>)` - Full constructor
364. `GetCurrentVolumeSettings()` - Gets current volume settings
365. `SetCurrentVolumeSettings(VolumeSetting)` - Sets current volume
366. `AddVolumeSettings(VolumeSetting)` - Adds volume setting
367. `GetVolumeSettings()` - Gets all volume settings
368. `GetVolumeSetting(string)` - Gets specific volume setting
369. `GetVolumeSettingsForThisDevice()` - Gets settings for current device
370. `UpdateVolumeSettingDB(VolumeSetting, VolumeSetting)` - Updates volume setting
371. `DeleteVolumeSettingDB(VolumeSetting)` - Deletes volume setting
372. `ClearEQDB()` - Clears volume database

---

## **WorldEventsClass.cs** (6 functions)

373. `WorldEvent()` - Constructor
374. `WorldEvent(string, DateTime, string, string, string?, string?)` - Full constructor
375. `GetWorldEvents()` - Gets world events
376. `AddWorldEvent(WorldEvent)` - Adds world event
377. `DeleteWorldEvent(WorldEvent)` - Deletes world event
378. `ClearWorldEvents()` - Clears all world events



## **XRUIOS.cs** (25 functions)

1. `DirectoryRecord()` - Constructor for directory record
2. `DirectoryRecord(string, string, string)` - Full constructor
3. `FileRecord()` - Constructor for file record
4. `FileRecord(string?, string)` - Full constructor
5. `DirectoryManager(string)` - Constructor, initializes directory manager
6. `LoadBindings(CancellationToken)` - Loads all binding files from disk
7. `GetAllBindings()` - Returns all directory bindings
8. `GetBindingById(string)` - Gets specific binding by UUID
9. `GetDirectoryById(string, string?, CancellationToken)` - Resolves directory path from binding
10. `GetOrCreateDirectory(string, string?, CancellationToken)` - Gets or creates directory binding
11. `ResolveDirectory(DirectoryBinding, string?)` - Resolves directory path
12. `DeleteBinding(string, CancellationToken)` - Deletes binding
13. `UpdateBinding(string, DirectoryResolution, CancellationToken)` - Updates binding
14. `SaveBinding(DirectoryBinding, CancellationToken)` - Saves binding to disk
15. `UpdateBindingInMemory(string, DirectoryBinding)` - Updates binding in memory
16. `DirectoryBinding()` - Constructor
17. `DirectoryBinding(DirectoryRef)` - Full constructor
18. `SetResolution(DirectoryResolution)` - Sets resolution
19. `ClearResolution()` - Clears resolution
20. `SetRef(DirectoryRef)` - Sets directory reference
21. `DirectoryRef(string, string)` - Constructor, computes directory ID
22. `ComputeDirectoryId(string, string)` - Computes directory hash ID
23. `DirectoryResolution()` - Constructor
24. `DirectoryResolution(string, bool)` - Full constructor
25. `CreateSign()` - Creates app signature (placeholder)
26. `Handle()` - Record constructor
27. `ResolvedMedia()` - Record constructor
28. `GetFile(string, string, CancellationToken)` - Gets resolved media file
29. `GetOrCreateDirectory(string, string?, string?, CancellationToken)` - Gets or creates generic directory
30. `AddGenericDirectory(string, string)` - Adds generic directory
31. `GetGenericDirectories()` - Gets all generic directories
32. `UpdateGenericDirectory(string, string, string)` - Updates generic directory
33. `RemoveGenericDirectory(string, bool)` - Removes generic directory

---


</details>



Copyright (c) 2026 Walker Industries R&D
All rights reserved.

This is a work-in-progress prototype.
You may view the source code for personal evaluation purposes only.
NO license is granted (express or implied) for:
- copying
- modification
- distribution
- commercial use
- derivative works
- or any other form of exploitation

It'll be open-sourced when it's actually ready and has examples ready.
Until then: look, don't touch. Seriously.

Oh and 
<img src="[https://github.com/user-attachments/assets/4d47635f-0d37-4ebc-a05f-a3779dd84e22](https://github.com/Walker-Industries-RnD/Malicious-Affiliation-Ban)" />

