Unfinished, there are still chunks that need to be fixed for the v1 (But it should take a few hours; Windows Centric ver.)

<img width="225" height="224" alt="image" src="https://github.com/user-attachments/assets/752b019f-aaec-455a-a311-9b583618c25d" />


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



## **Functions:**

Core Functions (excluding records/enums):
Data Management: ~45 functions

Media/Music: ~65 functions

Calendar: ~12 functions

Time/Date: ~30 functions

Alarms/Timers: ~15 functions

Volume/Audio: ~20 functions

Theme System: ~10 functions

Utility Classes: ~15 functions

Clipboard: ~8 functions

Total: ~220+ distinct methods/functions (End me)



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



<img width="1844" height="1080" alt="image" src="https://github.com/user-attachments/assets/4d47635f-0d37-4ebc-a05f-a3779dd84e22" />


## **Core System Classes**

### **XRUIOS Main Class**
- `DataPath` (property) - System data directory path
- `PublicDataPath` (property) - Public data directory path
- `InitiateFileSync()` - Initiates file synchronization
- `RemoveHashSetFromCollection()` - Removes hashset from music collection
- `AddHashSetToCollection()` - Adds hashset to music collection
- `UpdateCollection()` - Updates music collection
- `UpdateMedia()` - Updates media files
- `Update()` - Main update method for song directories

### **Yuuko.Bindings (Directory Management)**
#### **DirectoryManager Class**
- `GetDirectoryById()` - Gets directory by ID
- `GetOrCreateDirectory()` - Safely gets or creates a folder
- `ResolveDirectory()` - Resolves directory path
- `DeleteBinding()` - Deletes a binding
- `UpdateBinding()` - Updates a binding
- `LoadBindings()` - Loads all saved folder info from disk
- `GetAllBindings()` - Gets all bindings in memory
- `GetBindingById()` - Gets binding by UUID

## **Music/Songs Module**

### **Songs Class**
- `AddSongDirectory()` - Adds a song directory
- `RefreshDirectory()` - Refreshes directory
- `Initialize()` - Initializes music system

### **SongClass**
- `CreateSongInfo()` - Creates song metadata (overview + detailed)
- `GetSongInfo()` - Gets song information
- `UpdateSongInfo()` - Updates song information
- `DeleteSongInfo()` - Deletes song information
- `PatchTouchesOverview()` - Checks if patch affects overview
- `PatchTouchesDetailed()` - Checks if patch affects detailed
- `GetRequiredCapabilities()` - Gets required tag capabilities
- `GetWritableCapabilities()` - Gets writable tag capabilities

### **SongDirectoriesClass**
- `AddSongDirectory()` - Adds song directory
- `GetSongDirectories()` - Gets song directories
- `UpdateSongDirectory()` - Updates song directory
- `RemoveSongDirectory()` - Removes song directory

### **SongFavoritesClass**
- `AddToFavorites()` - Adds song to favorites
- `GetFavorites()` - Gets favorite songs
- `GetFavoritePathsAsync()` - Gets favorite paths
- `RemoveFromFavorites()` - Removes song from favorites

### **SongGetClass**
- `GetAllSongs()` - Gets all songs
- `GetSongsInDirectoryAsync()` - Gets songs in specific directory
- `GetSongsByNameAsync()` - Searches songs by name
- `GetSongsByTag()` - Searches songs by tag

### **MusicHistoryClass**
- `AddToPlayHistory()` - Adds song to play history
- `GetPlayHistory()` - Gets play history
- `ClearPlayHistory()` - Clears play history

## **Media Management**

### **Media Class**
- `GetFile()` - Gets media file information
- `GetOrCreateDirectory()` - Gets or creates directory
- `AddGenericDirectory()` - Adds generic directory
- `GetGenericDirectories()` - Gets generic directories
- `UpdateGenericDirectory()` - Updates generic directory
- `RemoveGenericDirectory()` - Removes generic directory

## **Calendar System**

### **CalendarClass**
- `CreateSimpleEvent()` - Creates simple calendar event
- `CreateRecurringEvent()` - Creates recurring calendar event
- `LoadAllEvents()` - Loads all events
- `GetEventByUid()` - Gets event by UID
- `UpdateEventByUid()` - Updates event by UID
- `DeleteEventByUid()` - Deletes event by UID
- `ScheduleUpcomingOccurrences()` - Schedules upcoming occurrences

### **CalendarNotifications**
- `Notify()` - Sends calendar notification

## **Time Management**

### **StopwatchClass**
- `CreateStopwatch()` - Creates a stopwatch
- `GetTimeElapsed()` - Gets elapsed time
- `CreateLap()` - Creates lap record
- `DestroyStopwatch()` - Destroys stopwatch
- `SaveStopwatchValuesAsSheet()` - Saves stopwatch values as CSV

### **TimerManagerClass**
- `StartTimer()` - Starts a timer
- `AddTime()` - Adds time to timer
- `CancelTimer()` - Cancels timer
- `FireTimer()` - Fires timer notification

## **Clipboard System**

### **ClipboardClass.BaseClipboard**
- `LoadClipboard()` - Loads clipboard
- `GetClipboardItem()` - Gets clipboard item
- `AddToClipboard()` - Adds to clipboard
- `RemoveFromClipboard()` - Removes from clipboard

### **ClipboardClass.ClipboardGroups**
- `LoadClipboard()` - Loads clipboard group
- `GetClipboardItem()` - Gets item from clipboard group
- `AddToClipboard()` - Adds to clipboard group
- `RemoveFromClipboard()` - Removes from clipboard group

## **Creator Management**

### **CreatorFileClass**
- `CreateCreator()` - Creates creator profile
- `GetCreator()` - Gets creator information
- `GetCreatorOverview()` - Gets creator overview
- `GetCreatorFiles()` - Gets creator files
- `AddFile()` - Adds file to creator
- `SetDescription()` - Sets creator description
- `RemoveFiles()` - Removes files from creator

### **CreatorFavoritesClass**
- `AddToFavorites()` - Adds creator to favorites
- `GetFavorites()` - Gets favorite creators
- `GetFavoritePathsAsync()` - Gets favorite creator paths
- `RemoveFromFavorites()` - Removes creator from favorites

## **Music Player**

### **CurrentlyPlayingClass**
- `GetCurrentlyPlaying()` - Gets currently playing song
- `SetCurrentlyPlaying()` - Sets currently playing song
- `ResetCurrentlyPlaying()` - Resets currently playing

### **MusicQueueClass**
- `GetCurrentlyPlaying()` - Gets music queue
- `AddToMusicQueue()` - Adds to music queue
- `ReorderSong()` - Reorders song in queue
- `RemoveSong()` - Removes song from queue
- `ResetQueue()` - Resets queue

## **Volume/Audio Control**

### **ExperimentalVolumeClass**
- `GetExperimentalAudioSettings()` - Gets experimental audio settings
- `SetExperimentalAudioSettings()` - Sets experimental audio settings
- `SaveAudioSettings()` - Saves audio settings
- `LoadAudioSettings()` - Loads audio settings

### **MasterVolumeClass**
- `GetMasterVolume()` - Gets master volume
- `SetMasterVolume()` - Sets master volume
- `SaveAudioSettings()` - Saves volume settings
- `LoadAudioSettings()` - Loads volume settings

### **AppVolume Class**
- `ChangeObjVolume()` - Changes object volume

### **mainVolume Class**
- `GetSoundEQDB()` - Gets sound EQ database
- `DeleteFromSoundEQDB()` - Deletes from sound EQ database
- `UpdateFromSoundEQDB()` - Updates sound EQ database
- `AddToSoundEQDB()` - Adds to sound EQ database
- `GetDefaultSoundEQ()` - Gets default sound EQ
- `GetUserDefaultSoundEQ()` - Gets user default sound EQ
- `CheckIfUserDefaultSoundExists()` - Checks if user default exists
- `SetUserDefaultSoundEQ()` - Sets user default sound EQ
- `ResetUserDefaultSoundEQ()` - Resets user default sound EQ

### **AudioGroups Class**
- `GetAllAudioGroups()` - Gets all audio groups
- `DeleteFromAudioGroups()` - Deletes from audio groups
- `UpdateFromAudioGroups()` - Updates audio groups
- `AddToAudioGroups()` - Adds to audio groups

## **Alarm System**

### **AlarmClass**
- `AddAlarm()` - Adds alarm
- `LoadAlarms()` - Loads alarms
- `UpdateAlarm()` - Updates alarm
- `DeleteAlarm()` - Deletes alarm

### **AlarmScheduler**
- `ScheduleAlarm()` - Schedules alarm
- `ScheduleAllAlarms()` - Schedules all alarms
- `FireAlarm()` - Fires alarm (private)
- `ScheduleNextOccurrence()` - Schedules next occurrence (private)

## **Chrono/Time Display**

### **Date Class**
- `SaveDateData()` - Saves date preferences
- `LoadDateData()` - Loads date preferences
- `GetTimezone()` - Gets timezone
- `SetTimezone()` - Sets timezone
- `GetDate()` - Gets formatted date
- `SetDate()` - Sets date format
- `GetTime()` - Gets formatted time
- `SetTime()` - Sets time format
- `AddWorldTime()` - Adds world time
- `GetWorldTimezoneCollection()` - Gets world timezone collection
- `GetWorldTimes()` - Gets world times
- `GetTimeInTimezone()` - Gets time in specific timezone
- `DeleteWorldTime()` - Deletes world time

### **Times Class**
- `GetCurrentTime()` - Gets current time
- `StopGettingCurrentTime()` - Stops getting time
- `GetTime()` - Coroutine for time measurement
- `GetCurrentTimeFromWorldTimes()` - Gets time from world times
- `StopGettingCurrentTimeWorldTimes()` - Stops world time measurement
- `GetTimeWorldTimes()` - Coroutine for world times
- `StartStopwatch()` - Starts stopwatch
- `StopStopwatch()` - Stops stopwatch
- `ResetStopwatch()` - Resets stopwatch
- `RecordTime()` - Records stopwatch time
- `GetFormattedTime()` - Gets formatted stopwatch time

### **Location Class**
- `GetExactCoordinates()` - Gets exact GPS coordinates
- `GetRelativeCoordinates()` - Gets relative coordinates (private)

## **Theme System**

### **ThemeSystem**
- `SaveTheme()` - Saves theme
- `GetAllXRUIOSThemes()` - Gets all themes
- `GetXRUIOSTheme()` - Gets specific theme
- `GetCurrentTheme()` - Gets current theme
- `UpdateTheme()` - Updates theme
- `SetTheme()` - Sets current theme
- `DeleteXRUIOSTheme()` - Deletes theme

## **Power Management**
### **PowerOff Class**
- `ShutDown()` - Shuts down system
- `Sleep()` - Puts system to sleep

## **Date/Time Utilities**

### **BasicTimeClass**
- `GetCurrentTimeLocal()` - Gets local time
- `GetCurrentTimeUTC()` - Gets UTC time
- `SetCurrentTime()` - Sets system time

### **NumberConvert Class** (static)
- `NumberToWords()` - Converts numbers to words

### **MonthConverter Class** (static)
- `ConvertToWordedMonth()` - Converts month number to word

## **Helper Classes**

### **ObservableProperty<T>**
- `Set()` - Sets value with change notification
- `Get()` - Gets current value

## **Data Structures (Records)**
- `DirectoryRecord` - Directory information
- `FileRecord` - File information
- `SongOverview` - Basic song metadata
- `SongDetailed` - Detailed song metadata
- `SongChapter` - Song chapter information
- `LyricLine` - Lyric line with timestamp
- `ResolvedMedia` - Media file information
- `Alarm` - Alarm configuration
- `TimerRecord` - Timer information
- `SoundEQ` - Sound equalizer settings
- `AudioGroup` - Audio group configuration
- `ExperimentalAudio` - Advanced audio settings
- `Creator` - Creator profile
- `ThemeColors` - Theme color scheme
- `ThemeTypography` - Theme typography
- `ThemeSpatial` - Theme spatial settings
- `ThemeIdentity` - Theme identification
- `XRUIOSTheme` - Complete theme definition

## **Enums**
- `MusicInfoStyle` - Song info detail level
- `SongSearchField` - Song search criteria
- `AudioTagCapability` - Audio file tag capabilities
- `TimeFormat` - 12/24 hour format
- `ShortTime/LongTime` - Time display formats
- `ShortDate/LongDate` - Date display formats

