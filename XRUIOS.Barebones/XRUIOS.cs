using ATL;
using CsvHelper;
using Hangfire;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using KeeperOfTomes;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using Org.BouncyCastle.Asn1.Mozilla;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Pqc.Crypto.Lms;
using Org.BouncyCastle.Utilities;
using Pariah_Cybersecurity;
using Secure_Store;
using Standart.Hash.xxHash;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net.Mail;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices.Marshalling;
using System.Security.AccessControl;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using WISecureData;
using static Pariah_Cybersecurity.DataHandler;
using static Pariah_Cybersecurity.DataHandler.DataRequest;
using static Secure_Store.Storage;
using static System.Net.Mime.MediaTypeNames;
using static Walker.Crypto.SimpleAESEncryption;
using static XRUIOS.Barebones.XRUIOS;
using static XRUIOS.Barebones.XRUIOS.AlarmClass;
using static XRUIOS.Barebones.XRUIOS.ChronoClass.Times;
using static XRUIOS.Barebones.XRUIOS.CreatorClass;
using static XRUIOS.Barebones.XRUIOS.Songs;
using static XRUIOS.Barebones.XRUIOS.Yuuko.Bindings;
using Attachment = Ical.Net.DataTypes.Attachment;
using Calendar = Ical.Net.Calendar;

namespace XRUIOS.Barebones
{
    public class XRUIOS
    {

        public static string DataPath { get; private set; } = Path.Combine(Directory.GetCurrentDirectory(), "XRUIOS");
        public static string PublicDataPath { get; private set; } = Path.Combine(Directory.GetCurrentDirectory(), "XRUIOSPublic");


        ObservableProperty<string> MainSong;
        ObservableCollection<string> MainSongQueue;

        public static string XRUIOSPath;
        public static SecureData encryptionKey = "Test".ToSecureData();


        public record DirectoryRecord
        {
            public string UUID { get; set; }
            public string PathName { get; set; }
            public string Path { get; set; }

            public DirectoryRecord() { }


            public DirectoryRecord(string uuid, string pathName, string path)
            {
                UUID = uuid;
                PathName = pathName;
                Path = path;
            }
        }

        public record FileRecord
        {
            public string UUID { get; set; }
            public string File { get; set; }

            public FileRecord() { }


            public FileRecord(string uuid, string file)
            {
                UUID = uuid;
                File = file;
            }
        }


        public class Yuuko
        {

            public class Bindings
            {

                //Lowkenuinely need to put comments here or I WILL forget how it works
                public sealed class DirectoryManager
                {
                    private readonly string _bindingsFolder;
                    private readonly Dictionary<string, DirectoryBinding> _bindings = new();

                    //Gets a directory by the ID, this is how you should get it
                    public async Task<string?> GetDirectoryById(
                    string directoryId,
                    string? defaultFolder = null,
                    CancellationToken ct = default)
                    {
                        // 1. Look up the binding
                        if (!_bindings.TryGetValue(directoryId, out var binding))
                            return null; // Unknown directory ID

                        // 2. Fast path: if the original path exists, use it
                        if (binding.Ref.OriginalDevice == ThisDeviceId &&
                            Directory.Exists(binding.Ref.FullPath))
                        {
                            return binding.Ref.FullPath;
                        }


                        // 3. If a verified resolution exists and is valid, use it
                        if (binding.Resolution?.Verified == true &&
                            Directory.Exists(binding.Resolution.ResolvedPath))
                        {
                            return binding.Resolution.ResolvedPath;
                        }

                        // 4. If no default folder is provided, DO NOT create anything
                        if (defaultFolder == null)
                            return null;

                        // 5. Only now do we allow resolution + creation
                        var resolvedPath = ResolveDirectory(binding, defaultFolder);

                        // 6. Persist the updated binding
                        string bindingPath = Path.Combine(_bindingsFolder, directoryId + ".binding");
                        var data = await BinaryConverter.NCObjectToByteArrayAsync(
                            binding,
                            cancellationToken: ct
                        );
                        await File.WriteAllBytesAsync(bindingPath, data, ct);

                        return resolvedPath;
                    }



                    public DirectoryManager(string bindingsFolder)
                    {
                        _bindingsFolder = Path.GetFullPath(bindingsFolder);
                        Directory.CreateDirectory(_bindingsFolder);
                    }

                    internal string ThisDeviceId => Environment.MachineName;

                    //Safely get or create a folder
                    public async Task<(string Uuid, string ResolvedPath)> GetOrCreateDirectory(string fullPath, string localOverride = null, CancellationToken ct = default)
                    {
                        // 1. Create a reference for this folder on this device
                        var directoryRef = new DirectoryRef(fullPath, ThisDeviceId);

                        // 2. Get existing binding or create a new one
                        if (!_bindings.TryGetValue(directoryRef.DirectoryId, out var binding))
                        {
                            binding = new DirectoryBinding(directoryRef);
                            _bindings[directoryRef.DirectoryId] = binding;
                        }

                        // 3. Resolve the folder (make sure it exists)
                        var resolvedPath = ResolveDirectory(binding, localOverride);

                        // 4. Persist the binding to disk
                        string bindingPath = Path.Combine(_bindingsFolder, directoryRef.DirectoryId + ".binding");
                        byte[] data = await BinaryConverter.NCObjectToByteArrayAsync(binding, cancellationToken: ct);
                        await File.WriteAllBytesAsync(bindingPath, data, ct);

                        // 5. Return both the UUID and the resolved path
                        return (directoryRef.DirectoryId, resolvedPath);
                    }

                    //Force a folder path on this machine


                    public string ResolveDirectory(DirectoryBinding binding, string defaultFolder = null)
                    {
                        if (binding.Ref.OriginalDevice == ThisDeviceId && Directory.Exists(binding.Ref.FullPath))
                            return binding.Ref.FullPath;

                        if (binding.Resolution != null && binding.Resolution.Verified && Directory.Exists(binding.Resolution.ResolvedPath))
                            return binding.Resolution.ResolvedPath;

                        if (Directory.Exists(binding.Ref.FullPath))
                        {
                            binding.SetResolution(new DirectoryResolution(binding.Ref.FullPath, verified: true));
                            return binding.Ref.FullPath;
                        }

                        var pathToUse = defaultFolder ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "XRUIOS", binding.Ref.DirectoryId);

                        if (defaultFolder == null && !Directory.Exists(binding.Ref.FullPath))
                            throw new InvalidOperationException("ResolveDirectory called without defaultFolder");

                        Directory.CreateDirectory(pathToUse);
                        binding.SetResolution(new DirectoryResolution(pathToUse, verified: true));

                        return pathToUse;
                    }

                    public async Task<bool> DeleteBinding(string directoryId, CancellationToken ct = default)
                    {
                        var bindingPath = Path.Combine(
                            Path.GetFullPath(_bindingsFolder),
                            directoryId + ".binding"
                        );

                        if (!File.Exists(bindingPath))
                            return false;

                        File.Delete(bindingPath);

                        return true;
                    }

                    public async Task<bool> UpdateBinding(string directoryId, DirectoryResolution newResolution, CancellationToken ct = default)
                    {
                        // 1. Check if binding exists
                        if (!_bindings.TryGetValue(directoryId, out var binding))
                            return false;

                        // 2. Update resolution
                        binding.SetResolution(newResolution);

                        // 3. Persist to disk
                        string bindingPath = Path.Combine(_bindingsFolder, directoryId + ".binding");
                        var data = await BinaryConverter.NCObjectToByteArrayAsync(binding, cancellationToken: ct);
                        await File.WriteAllBytesAsync(bindingPath, data, ct);

                        return true;
                    }


                    //Load all saved folder info from disk
                    public async Task LoadBindings(CancellationToken ct = default)
                    {
                        _bindings.Clear();
                        if (!Directory.Exists(_bindingsFolder)) return;

                        foreach (var file in Directory.EnumerateFiles(_bindingsFolder, "*.binding"))
                        {
                            try
                            {
                                var bytes = await File.ReadAllBytesAsync(file, ct);
                                var binding = await BinaryConverter.NCByteArrayToObjectAsync<DirectoryBinding>(bytes, cancellationToken: ct);
                                if (binding != null)
                                    _bindings[binding.Ref.DirectoryId] = binding;
                            }
                            catch { }
                        }
                    }

                    //Gets all the bindings in memory
                    public IEnumerable<DirectoryBinding> GetAllBindings() => _bindings.Values;

                    public DirectoryBinding? GetBindingById(string uuid)
                    {
                        _bindings.TryGetValue(uuid, out var binding);
                        return binding;
                    }



                }


                //SetResolution will set the DirectoryResolution for this file
                //ClearResolution forgets it
                public sealed class DirectoryBinding
                {
                    public DirectoryRef Ref { get; }
                    public DirectoryResolution? Resolution { get; private set; }

                    public DirectoryBinding(DirectoryRef directoryRef) => Ref = directoryRef;
                    public DirectoryBinding(DirectoryRef directoryRef, DirectoryResolution? resolution)
                    {
                        Ref = directoryRef;
                        Resolution = resolution;
                    }

                    public void SetResolution(DirectoryResolution resolution) => Resolution = resolution;


                    public void ClearResolution() => Resolution = null;
                }



                //DirectoryRef asks for
                //A: the original folder path and
                //B: the device where this path originally existed
                public readonly struct DirectoryRef
                {
                    public string FullPath { get; }
                    public string OriginalDevice { get; }
                    public string DirectoryId { get; }

                    private const ulong HashSeed = 0x59554B4F; // "YUUKO" 

                    public DirectoryRef(string fullPath, string originalDevice)
                    {
                        FullPath = fullPath ?? throw new ArgumentNullException(nameof(fullPath));
                        OriginalDevice = originalDevice ?? throw new ArgumentNullException(nameof(originalDevice));

                        DirectoryId = ComputeDirectoryId(fullPath, originalDevice).GetAwaiter().GetResult();
                    }

                    private static async Task<string> ComputeDirectoryId(string path, string device)
                    {
                        // Canonical string format (DO NOT CHANGE lightly)
                        string canonical = $"{device}|{Path.GetFullPath(path)}";

                        await using var stream = new MemoryStream(
                            Encoding.UTF8.GetBytes(canonical)
                        );

                        ulong hash = await xxHash64.ComputeHashAsync(
                            stream,
                            bufferSize: 81920,
                            seed: HashSeed
                        );

                        // Hex string is protocol-friendly
                        return hash.ToString("X16");
                    }
                }


                //DirectoryResolution asks for
                //A: the local path used if the original doesn’t exist and
                //B: if we’ve checked that path exists on this side
                public sealed class DirectoryResolution
                {
                    public string ResolvedPath { get; set; }
                    public DateTime LastVerifiedUtc { get; set; }
                    public bool Verified { get; set; }


                    public DirectoryResolution(string resolvedPath, bool verified = false)
                    {
                        ResolvedPath = resolvedPath ?? throw new ArgumentNullException(nameof(resolvedPath));
                        Verified = verified;
                        LastVerifiedUtc = DateTime.UtcNow;
                    }
                }



            }

            public class App
            {
                //Platform can be { Windows, Linux, YuukOS, Android, MacOS, iPhone}

                public record Overview
                {
                    public string YuukoAppName;
                    public string MinimumYuukoVersion;
                    public string DeveloperID;
                    public Dictionary<string, byte[]> PublicKey;
                    public string UUID;
                    public string AppName;
                    public string Description;
                    public string PrimaryPlatform;

                    public Overview() { }

                    public Overview(string yuukoAppName, string minimumYuukoVersion, string developerID, Dictionary<string, byte[]> publicKey, string uuid, string appName, string description, string primaryPlatform)
                    {
                        YuukoAppName = yuukoAppName;
                        MinimumYuukoVersion = minimumYuukoVersion;
                        DeveloperID = developerID;
                        PublicKey = publicKey;
                        UUID = uuid;
                        AppName = appName;
                        Description = description;
                        PrimaryPlatform = primaryPlatform;
                    }
                }

                public record Parameter
                {
                    public string Name;
                    public Type ParamType;
                    public bool Required;
                    public object? Default;
                    public List<object>? Choices;

                    public Parameter()
                    {
                        Name = string.Empty;
                        ParamType = typeof(object);
                        Required = false;
                        Default = null;
                        Choices = null;
                    }

                    public Parameter(string name, Type paramType, bool required, object? defaultValue, List<object>? choices)
                    {
                        Name = name;
                        ParamType = paramType;
                        Required = required;
                        Default = defaultValue;
                        Choices = choices;
                    }
                }

                public record Event
                {
                    public string Name;
                    public string Description;
                    public List<Parameter>? Parameters;
                    public string? Command;

                    public Event()
                    {
                        Name = string.Empty;
                        Description = string.Empty;
                        Parameters = null;
                        Command = null;
                    }

                    public Event(string name, string description, List<Parameter>? parameters, string? command)
                    {
                        Name = name;
                        Description = description;
                        Parameters = parameters;
                        Command = command;
                    }
                }

                public record OSSpecificApp
                {
                    public string OS;
                    public string Version;
                    public string Download;
                    public List<Panel> PanelInfo;

                    public OSSpecificApp()
                    {
                        OS = string.Empty;
                        Version = string.Empty;
                        Download = string.Empty;
                        PanelInfo = new List<Panel>();
                    }

                    public OSSpecificApp(string os, string version, string download, List<Panel> panelInfo)
                    {
                        OS = os;
                        Version = version;
                        Download = download;
                        PanelInfo = panelInfo;
                    }
                }

                //Modes can be { Full, Panel, Max, Minimized, CLI}

                //Views can be 2D, 3D or CMD

                public record Panel
                {
                    public string Description;
                    public List<RenderStyle> Renders;

                    //All formatted to a shared interface that the runtime can call

                    public Panel()
                    {
                        Description = string.Empty;
                        Renders = new List<RenderStyle>();
                    }

                    public Panel(string description, List<RenderStyle> renders)
                    {
                        Description = description;
                        Renders = renders;
                    }
                }

                public record RenderStyle
                {
                    public string Mode;
                    public string View;

                    public RenderStyle()
                    {
                        Mode = string.Empty;
                        View = string.Empty;
                    }

                    public RenderStyle(string mode, string view)
                    {
                        Mode = mode;
                        View = view;
                    }
                }

                public record PreferredPlatformMapping
                {
                    public string? Mode2D { get; init; }
                    public string? Mode3D { get; init; }
                    public string? ModeCMD { get; init; }

                    public PreferredPlatformMapping() { }

                    public PreferredPlatformMapping(string? mode2D, string? mode3D, string? modeCMD)
                    {
                        Mode2D = mode2D;
                        Mode3D = mode3D;
                        ModeCMD = modeCMD;
                    }
                }

                public record YuukoApp
                {
                    public Overview YuukoInfo;
                    public Dictionary<string, PreferredPlatformMapping> Defaults;
                    public List<OSSpecificApp> OSSet;
                    public Dictionary<string, List<Event>> OSSpecificEntrypoints;
                    public List<Event> SharedEntrypoints;

                    public YuukoApp(){}

                    public YuukoApp(
                        Overview yuukoInfo,
                        Dictionary<string, PreferredPlatformMapping> defaults,
                        List<OSSpecificApp> osSet,
                        Dictionary<string, List<Event>> osSpecificEntrypoints,
                        List<Event> sharedEntrypoints)
                    {
                        YuukoInfo = yuukoInfo;
                        Defaults = defaults;
                        OSSet = osSet;
                        OSSpecificEntrypoints = osSpecificEntrypoints;
                        SharedEntrypoints = sharedEntrypoints;
                    }
                }

                //The way a YuukoApp looks pretty simple but it's actually VERY complicated
                //There are a number of checks we do to ensure things work properly
                //The goal is to
                    //1. Expose a public API for each project
                    //2. Figure out the download we should be looking at
                    //3. Tell all the devices on the network to download the versions we say to and
                    //4. Handle mode switches for UIs

                //Atop this, we check the information by getting the sign and ensuring it's right


                public static async Task CreateSign()
                {

                }




            }




        }


        


        public class Songs
        {



            //We have a folder with song overviews and another with songdetailed

            //We have a second folder with a huge hashset

            //Whenever we reference a song, internally we ensure that the sign is the same; if not, we update both the sign and the file

            //For images, we can get the images embedded in the file

            //However we have a third folder called "Media"; if a folder in there has the same generated UUID as the song overview file
            //we will take this image over the ones embedded (As these support gifs)

            //Also when updating the songoverview/detailed, we can simply use the data gathered from overview as input for detailed and use ATLDOTNET to fill in the holes

            public record SongOverview
            {
                // Title
                public string SongName { get; set; }
                public string? AlbumName { get; set; }

                // Artist
                public string TrackArtist { get; set; }
                public string? AlbumArtist { get; set; }

                // Media Playback
                public TimeSpan? Duration { get; set; }
                public DateTime? ReleasedOn { get; set; }
                public DateTime? LastPlayedOn { get; set; }

                // Identifiers
                public Guid? SongId { get; set; }
                public string? ISRC { get; set; }

                public string XRUUID { get; set; }

                // Other Data
                public int? TrackNumber { get; set; }
                public bool? IsFavorite { get; set; }
                public int PlayCount { get; set; }

                public SongOverview() { }


                // Constructor for easy initialization
                public SongOverview(
                    string songName,
                    string trackArtist,
                    DateTime? releasedOn,
                    string? albumName = null,
                    string? albumArtist = null,
                    TimeSpan? duration = null,
                    DateTime? lastPlayedOn = null,
                    Guid? songId = null,
                    string? isrc = null,
                    int? trackNumber = null,
                    bool? isFavorite = null,
                    int playCount = 0
                )
                {
                    SongName = songName;
                    TrackArtist = trackArtist;
                    ReleasedOn = releasedOn;
                    AlbumName = albumName;
                    AlbumArtist = albumArtist;
                    Duration = duration;
                    LastPlayedOn = lastPlayedOn;
                    SongId = songId;
                    ISRC = isrc;
                    TrackNumber = trackNumber;
                    IsFavorite = isFavorite;
                    PlayCount = playCount;
                }
            }

            public record SongDetailed
            {

                // Titles             
                public string TrackTitle { get; set; }
                public string? AlbumTitle { get; set; }
                public string? OriginalAlbumTitle { get; set; }
                public string? ContentGroupDescription { get; set; }


                // People & Organizations              
                public string TrackArtist { get; set; }
                public string? AlbumArtist { get; set; }
                public string? OriginalArtist { get; set; }
                public string? Composer { get; set; }
                public string? Conductor { get; set; }
                public string? Lyricist { get; set; }
                public string? Publisher { get; set; }
                public List<string>? InvolvedPeople { get; set; } // e.g., producers, arrangers
                public string? SeriesTitle { get; set; }


                // Count & Indexes               
                public int? TrackNumber { get; set; }
                public int? TotalTracks { get; set; }
                public int? DiscNumber { get; set; }
                public int? TotalDiscs { get; set; }
                public string? AlbumSortOrder { get; set; }
                public string? AlbumArtistSortOrder { get; set; }
                public string? ArtistSortOrder { get; set; }
                public string? TitleSortOrder { get; set; }
                public string? SeriesPartIndex { get; set; }


                // Dates               
                public DateTime? RecordingDate { get; set; }
                public int? RecordingYear { get; set; }
                public DateTime? OriginalReleaseDate { get; set; }
                public int? OriginalReleaseYear { get; set; }
                public DateTime? PublishingDate { get; set; }


                // Identifiers               
                public string? ISRC { get; set; }
                public string? CatalogNumber { get; set; }


                // Ripping & Encoding               
                public string? EncodedBy { get; set; }
                public string? Encoder { get; set; }


                // URLs               
                public string? AudioSourceUrl { get; set; }


                // Style               
                public string? Genre { get; set; }
                public int? BPM { get; set; }


                // Miscellaneous
                public string? Comment { get; set; }
                public string? Description { get; set; }
                public string? LongDescription { get; set; } // Podcast description
                public string? Language { get; set; }
                public string? Copyright { get; set; }
                public List<SongChapter>? Chapters { get; set; }
                public string? UnsynchronizedLyrics { get; set; }
                public List<LyricLine>? SynchronizedLyrics { get; set; }


                public SongDetailed() { }

                public SongDetailed(
                    string trackTitle,
                    string trackArtist,
                    DateTime? recordingDate = null,
                    string? albumTitle = null,
                    string? originalAlbumTitle = null,
                    string? contentGroupDescription = null,
                    string? albumArtist = null,
                    string? originalArtist = null,
                    string? composer = null,
                    string? conductor = null,
                    string? lyricist = null,
                    string? publisher = null,
                    List<string>? involvedPeople = null,
                    string? seriesTitle = null,
                    int? trackNumber = null,
                    int? totalTracks = null,
                    int? discNumber = null,
                    int? totalDiscs = null,
                    string? albumSortOrder = null,
                    string? albumArtistSortOrder = null,
                    string? artistSortOrder = null,
                    string? titleSortOrder = null,
                    string? seriesPartIndex = null,
                    int? recordingYear = null,
                    DateTime? originalReleaseDate = null,
                    int? originalReleaseYear = null,
                    DateTime? publishingDate = null,
                    string? isrc = null,
                    string? catalogNumber = null,
                    string? encodedBy = null,
                    string? encoder = null,
                    string? audioSourceUrl = null,
                    string? genre = null,
                    int? bpm = null,
                    string? comment = null,
                    string? description = null,
                    string? longDescription = null,
                    string? language = null,
                    string? copyright = null,
                 List<SongChapter>? chapters = null,
                 string? unsynchronizedLyrics = null,
                 List<LyricLine>? synchronizedLyrics = null

                )
                {
                    TrackTitle = trackTitle;
                    TrackArtist = trackArtist;
                    RecordingDate = recordingDate;
                    AlbumTitle = albumTitle;
                    OriginalAlbumTitle = originalAlbumTitle;
                    ContentGroupDescription = contentGroupDescription;
                    AlbumArtist = albumArtist;
                    OriginalArtist = originalArtist;
                    Composer = composer;
                    Conductor = conductor;
                    Lyricist = lyricist;
                    Publisher = publisher;
                    InvolvedPeople = involvedPeople;
                    SeriesTitle = seriesTitle;
                    TrackNumber = trackNumber;
                    TotalTracks = totalTracks;
                    DiscNumber = discNumber;
                    TotalDiscs = totalDiscs;
                    AlbumSortOrder = albumSortOrder;
                    AlbumArtistSortOrder = albumArtistSortOrder;
                    ArtistSortOrder = artistSortOrder;
                    TitleSortOrder = titleSortOrder;
                    SeriesPartIndex = seriesPartIndex;
                    RecordingYear = recordingYear;
                    OriginalReleaseDate = originalReleaseDate;
                    OriginalReleaseYear = originalReleaseYear;
                    PublishingDate = publishingDate;
                    ISRC = isrc;
                    CatalogNumber = catalogNumber;
                    EncodedBy = encodedBy;
                    Encoder = encoder;
                    AudioSourceUrl = audioSourceUrl;
                    Genre = genre;
                    BPM = bpm;
                    Comment = comment;
                    Description = description;
                    LongDescription = longDescription;
                    Language = language;
                    Copyright = copyright;
                    Chapters = chapters;
                    UnsynchronizedLyrics = unsynchronizedLyrics;
                    SynchronizedLyrics = synchronizedLyrics;
                }
            }

            public sealed record SongChapter
            {
                public TimeSpan Start { get; init; }
                public TimeSpan? End { get; init; }
                public string Title { get; init; } = string.Empty;

                public SongChapter() { }

                public SongChapter(TimeSpan start, TimeSpan? end, string title)
                {
                    Start = start;
                    End = end;
                    Title = title ?? string.Empty;
                }
            }

            public sealed record LyricLine
            {
                public TimeSpan Timestamp { get; init; }
                public string Text { get; init; } = string.Empty;

                public LyricLine() { }

                public LyricLine(TimeSpan timestamp, string text)
                {
                    Timestamp = timestamp;
                    Text = text ?? string.Empty;
                }
            }


            private static List<SongChapter>? ExtractChapters(Track song)
            {
                if (song.Chapters == null || song.Chapters.Count == 0)
                    return null;

                return song.Chapters
                    .Select(c => new SongChapter
                    {
                        // Convert uint milliseconds to TimeSpan
                        Start = TimeSpan.FromMilliseconds(c.StartTime),

                        // If EndTime is zero, set null
                        End = c.EndTime == 0 ? null : TimeSpan.FromMilliseconds(c.EndTime),

                        Title = c.Title ?? string.Empty
                    })
                    .ToList();
            }

            private static string? ExtractUnsyncedLyrics(Track song)
            {
                if (song.Lyrics == null || song.Lyrics.Count == 0)
                    return null;

                var lyricEntry = song.Lyrics[0];

                return string.IsNullOrWhiteSpace(lyricEntry.UnsynchronizedLyrics)
                    ? null
                    : lyricEntry.UnsynchronizedLyrics.Trim();
            }




            private static List<LyricLine>? ParseLrc(string? lrc)
            {
                if (string.IsNullOrWhiteSpace(lrc))
                    return null;

                var result = new List<LyricLine>();

                var lines = lrc.Split('\n', StringSplitOptions.RemoveEmptyEntries);

                foreach (var line in lines)
                {
                    // [mm:ss.xx] text
                    var matches = System.Text.RegularExpressions.Regex.Matches(
                        line,
                        @"\[(\d+):(\d+)(?:\.(\d+))?\]"
                    );

                    var text = System.Text.RegularExpressions.Regex.Replace(line, @"\[.*?\]", "").Trim();

                    foreach (System.Text.RegularExpressions.Match match in matches)
                    {
                        var minutes = int.Parse(match.Groups[1].Value);
                        var seconds = int.Parse(match.Groups[2].Value);
                        var millis = match.Groups[3].Success
                            ? int.Parse(match.Groups[3].Value.PadRight(3, '0'))
                            : 0;

                        result.Add(new LyricLine
                        {
                            Timestamp = new TimeSpan(0, 0, minutes, seconds, millis),
                            Text = text
                        });
                    }
                }

                return result.Count > 0
                    ? result.OrderBy(l => l.Timestamp).ToList()
                    : null;
            }


            private static List<LyricLine>? ParseSrt(string? srt)
            {
                if (string.IsNullOrWhiteSpace(srt))
                    return null;

                var result = new List<LyricLine>();
                var blocks = srt.Split("\n\n", StringSplitOptions.RemoveEmptyEntries);

                foreach (var block in blocks)
                {
                    var lines = block.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    if (lines.Length < 3)
                        continue;

                    // 00:01:15,000 --> 00:01:17,000
                    var timing = lines[1].Split("-->");
                    if (timing.Length != 2)
                        continue;

                    if (!TimeSpan.TryParse(timing[0].Trim().Replace(',', '.'), out var start))
                        continue;

                    var text = string.Join(" ", lines.Skip(2));

                    result.Add(new LyricLine
                    {
                        Timestamp = start,
                        Text = text
                    });
                }

                return result.Count > 0 ? result : null;
            }


            private static List<LyricLine>? ExtractSyncedLyrics(Track song)
            {
                if (song.Lyrics == null || song.Lyrics.Count == 0)
                    return null;

                // Take the first lyrics entry (if multiple exist)
                var lyricEntry = song.Lyrics[0];

                var text = lyricEntry.UnsynchronizedLyrics; // or .SynchronizedLyrics if ATL provides it
                if (string.IsNullOrWhiteSpace(text))
                    return null;

                // Try LRC first
                var lrc = ParseLrc(text);
                if (lrc != null) return lrc;

                // Then try SRT
                var srt = ParseSrt(text);
                if (srt != null) return srt;

                return null;
            }



            public void AddSongDirectory(string songPath)
            {

            }


            public void RefreshDirectory()
            {

            }


            //Our list of songs






            public async Task Initialize()
            {
                //Where data is saved

                string SongOverviewFile = Path.Combine(DataPath, "Music", "Overviews");
                string SongDetailedFile = Path.Combine(DataPath, "Music", "Details");
                string HashsetsFile = Path.Combine(DataPath, "Music", "Hashsets");
                string ImagesFile = Path.Combine(DataPath, "Music", "Images");

                Directory.CreateDirectory(SongOverviewFile);
                Directory.CreateDirectory(SongDetailedFile);
                Directory.CreateDirectory(HashsetsFile);
                Directory.CreateDirectory(ImagesFile);

            }


            public class SongClass
            {
                //Song Info

                //C
                //Auto tag does nothing for now but later will auto find names, artists, etc.
                public static async Task<(SongOverview, SongDetailed)> CreateSongInfo(string audioFile, string directoryUUID, bool autoTag = false)
                {
                    var directoryPath = Path.Combine(DataPath, "Music");

                    var manager = new Yuuko.Bindings.DirectoryManager(directoryPath);

                    await manager.LoadBindings();

                    //Does the ID being referenced exists?

                    var idDirectoryPath = await manager.GetDirectoryById(directoryUUID);

                    if (idDirectoryPath == null)
                    {
                        throw new InvalidOperationException($"Directory has not been resolved.");
                    }


                    //Does the audio source file exist?

                    var audioSourceUrl = Path.Combine(DataPath, idDirectoryPath, audioFile);

                    if (!File.Exists(audioSourceUrl))
                    {
                        throw new InvalidOperationException($"Song file does not exist.");
                    }

                    //Does the meta file already exist? Both

                    var audioMetadataUrlOverview = Path.Combine(idDirectoryPath, $"{audioFile}+.yuukoMusicOverview");
                    var audioMetadataUrlDetailed = Path.Combine(idDirectoryPath, $"{audioFile}+.yuukoMusicDetailed");


                    if (File.Exists(audioMetadataUrlOverview) && File.Exists(audioMetadataUrlDetailed))
                    {
                        throw new InvalidOperationException($"Metadata already exists.");
                    }

                    //If one file doesn't exist we rewrite both

                    #region Get Song Details


                    Track song = new Track(audioSourceUrl);

                    // Titles             
                    string TrackTitle = song.Title;
                    string? AlbumTitle = song.Album;
                    string? OriginalAlbumTitle = song.OriginalAlbum;
                    string? ContentGroupDescription = song.Group;


                    // People & Organizations              
                    string TrackArtist = song.Artist;
                    string? AlbumArtist = song.AlbumArtist;
                    string? OriginalArtist = song.OriginalArtist;
                    string? Composer = song.Composer;
                    string? Conductor = song.Conductor;
                    string? Lyricist = song.Lyricist;
                    string? Publisher = song.Publisher;
                    List<string> InvolvedPeople = song.InvolvedPeople
                        ?.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                        .ToList() ?? new List<string>(); string? SeriesTitle = song.SeriesTitle;


                    // Count & Indexes               
                    int? TrackNumber = song.TrackNumber;
                    int? TotalTracks = song.TrackTotal;
                    int? DiscNumber = song.DiscNumber;
                    int? TotalDiscs = song.DiscTotal;
                    string? AlbumSortOrder = song.SortAlbum;
                    string? AlbumArtistSortOrder = song.SortAlbumArtist;
                    string? ArtistSortOrder = song.SortArtist;
                    string? TitleSortOrder = song.SortTitle;
                    string? SeriesPartIndex = song.SeriesPart;


                    // Dates               
                    DateTime? RecordingDate = song.Date;
                    int? RecordingYear = song.Year;
                    DateTime? OriginalReleaseDate = song.OriginalReleaseDate;
                    int? OriginalReleaseYear = song.OriginalReleaseYear;
                    DateTime? PublishingDate = song.PublishingDate;


                    // Identifiers               
                    string? ISRC = song.ISRC;
                    string? CatalogNumber = song.CatalogNumber;


                    // Ripping & Encoding               
                    string? EncodedBy = song.EncodedBy;
                    string? Encoder = song.Encoder;


                    // URLs               
                    string? AudioSourceUrl = song.AudioSourceUrl;


                    // Style               
                    string? Genre = song.Genre;
                    int? BPM = song.BPM > 0 ? (int)song.BPM : null;


                    // Miscellaneous
                    string? Comment = song.Comment;
                    string? Description = song.Description;
                    string? LongDescription = song.LongDescription;
                    string? Language = song.Language;
                    string? Copyright = song.Copyright;

                    var Chapters = ExtractChapters(song);
                    var UnsynchronizedLyrics = ExtractUnsyncedLyrics(song);
                    var SynchronizedLyrics = ExtractSyncedLyrics(song);

                    #endregion

                    #region Create Overview and Detailed

                    double durationSeconds = song.Duration;

                    var overview = new SongOverview
                    {
                        SongName = TrackTitle,
                        AlbumName = AlbumTitle,
                        TrackArtist = TrackArtist,
                        AlbumArtist = AlbumArtist,
                        Duration = TimeSpan.FromSeconds(durationSeconds),
                        ReleasedOn = OriginalReleaseDate ?? RecordingDate,
                        ISRC = ISRC,
                        TrackNumber = TrackNumber,
                    };

                    var detailed = new SongDetailed
                    {
                        TrackTitle = TrackTitle,
                        AlbumTitle = AlbumTitle,
                        OriginalAlbumTitle = OriginalAlbumTitle,
                        ContentGroupDescription = ContentGroupDescription,

                        TrackArtist = TrackArtist,
                        AlbumArtist = AlbumArtist,
                        OriginalArtist = OriginalArtist,
                        Composer = Composer,
                        Conductor = Conductor,
                        Lyricist = Lyricist,
                        Publisher = Publisher,
                        InvolvedPeople = InvolvedPeople,
                        SeriesTitle = SeriesTitle,

                        TrackNumber = TrackNumber,
                        TotalTracks = TotalTracks,
                        DiscNumber = DiscNumber,
                        TotalDiscs = TotalDiscs,
                        AlbumSortOrder = AlbumSortOrder,
                        AlbumArtistSortOrder = AlbumArtistSortOrder,
                        ArtistSortOrder = ArtistSortOrder,
                        TitleSortOrder = TitleSortOrder,
                        SeriesPartIndex = SeriesPartIndex,

                        RecordingDate = RecordingDate,
                        RecordingYear = RecordingYear,
                        OriginalReleaseDate = OriginalReleaseDate,
                        OriginalReleaseYear = OriginalReleaseYear,
                        PublishingDate = PublishingDate,

                        ISRC = ISRC,
                        CatalogNumber = CatalogNumber,

                        EncodedBy = EncodedBy,
                        Encoder = Encoder,

                        AudioSourceUrl = AudioSourceUrl,

                        Genre = Genre,
                        BPM = BPM,

                        Comment = Comment,
                        Description = Description,
                        LongDescription = LongDescription,
                        Language = Language,
                        Copyright = Copyright,

                        Chapters = Chapters,
                        UnsynchronizedLyrics = UnsynchronizedLyrics,
                        SynchronizedLyrics = SynchronizedLyrics
                    };

                    #endregion

                    //Save file


                    var overviewData = await BinaryConverter.NCObjectToByteArrayAsync<SongOverview>(overview);
                    var detailedData = await BinaryConverter.NCObjectToByteArrayAsync<SongDetailed>(detailed);


                    var overviewFile = DataHandler.JSONDataHandler.CreateJsonFile($"{audioFile}+.yuukoMusicOverview", idDirectoryPath, new JsonObject());
                    var overviewFileLoaded = await JSONDataHandler.LoadJsonFile(idDirectoryPath, audioFile);
                    overviewFileLoaded = await JSONDataHandler.AddToJson<byte[]>(overviewFileLoaded, "Data", overviewData, encryptionKey);
                    await JSONDataHandler.SaveJson(overviewFileLoaded);

                    var detailedFile = DataHandler.JSONDataHandler.CreateJsonFile($"{audioFile}+.yuukoMusicdetailed", idDirectoryPath, new JsonObject());
                    var detailedFileLoaded = await JSONDataHandler.LoadJsonFile(idDirectoryPath, audioFile);
                    detailedFileLoaded = await JSONDataHandler.AddToJson<byte[]>(detailedFileLoaded, "Data", detailedData, encryptionKey);
                    await JSONDataHandler.SaveJson(detailedFileLoaded);



                    return (overview, detailed);

                }

                //R
                public enum MusicInfoStyle { overview, detailed, both };
                public static async Task<(SongOverview?, SongDetailed?)> GetSongInfo(string audioFile, string directoryUUID, MusicInfoStyle getData)
                {

                    var directoryPath = Path.Combine(DataPath, "Music");

                    var manager = new Yuuko.Bindings.DirectoryManager(directoryPath);

                    await manager.LoadBindings();

                    //Does the ID being referenced exists?

                    var idDirectoryPath = await manager.GetDirectoryById(directoryUUID);

                    if (idDirectoryPath == null)
                    {
                        throw new InvalidOperationException($"Directory has not been resolved.");
                    }


                    //Does the audio source file exist?

                    var audioSourceUrl = Path.Combine(DataPath, idDirectoryPath, audioFile);

                    if (!File.Exists(audioSourceUrl))
                    {
                        throw new InvalidOperationException($"Song file does not exist.");
                    }

                    //Does the meta file already exist? Both

                    var audioMetadataUrlOverview = Path.Combine(idDirectoryPath, $"{audioFile}+.yuukoMusicOverview");
                    var audioMetadataUrlDetailed = Path.Combine(idDirectoryPath, $"{audioFile}+.yuukoMusicDetailed");


                    if (!File.Exists(audioMetadataUrlOverview) || !File.Exists(audioMetadataUrlDetailed))
                    {
                        throw new InvalidOperationException($"Metadata does not fully exist.");
                    }

                    SongOverview? songOverview = null;
                    SongDetailed? songDetailed = null;

                    switch (getData)
                    {
                        case MusicInfoStyle.overview:
                            var overviewFileLoaded = await JSONDataHandler.LoadJsonFile(idDirectoryPath, audioFile);
                            var overviewBytes = (byte[])await JSONDataHandler.GetVariable<byte[]>(overviewFileLoaded, "Data", encryptionKey);
                            var overview = await BinaryConverter.NCByteArrayToObjectAsync<SongOverview>(overviewBytes);
                            songOverview = overview;
                            break;

                        case MusicInfoStyle.detailed:
                            var detailedFileLoaded = await JSONDataHandler.LoadJsonFile(idDirectoryPath, audioFile);
                            var detailedBytes = (byte[])await JSONDataHandler.GetVariable<byte[]>(detailedFileLoaded, "Data", encryptionKey);
                            var detailed = await BinaryConverter.NCByteArrayToObjectAsync<SongDetailed>(detailedBytes);
                            songDetailed = detailed;
                            break;
                        case MusicInfoStyle.both:
                            var overviewFileLoaded2 = await JSONDataHandler.LoadJsonFile(idDirectoryPath, audioFile);
                            var overviewBytes2 = (byte[])await JSONDataHandler.GetVariable<byte[]>(overviewFileLoaded, "Data", encryptionKey);
                            var overview2 = await BinaryConverter.NCByteArrayToObjectAsync<SongOverview>(overviewBytes2);
                            songOverview = overview2;
                            var detailedFileLoaded2 = await JSONDataHandler.LoadJsonFile(idDirectoryPath, audioFile);
                            var detailedBytes2 = (byte[])await JSONDataHandler.GetVariable<byte[]>(detailedFileLoaded, "Data", encryptionKey);
                            var detailed2 = await BinaryConverter.NCByteArrayToObjectAsync<SongDetailed>(detailedBytes2);
                            songDetailed = detailed2;
                            break;

                    }


                    return (songOverview, songDetailed);




                }

                //U 
                public sealed class SongInfoPatch
                {
                    // ───── Shared / Overview ─────
                    public string? SongName { get; init; }
                    public string? AlbumName { get; init; }
                    public string? TrackArtist { get; init; }
                    public string? AlbumArtist { get; init; }
                    public TimeSpan? Duration { get; init; }
                    public DateTime? ReleasedOn { get; init; }
                    public DateTime? LastPlayedOn { get; init; }
                    public Guid? SongId { get; init; }
                    public string? ISRC { get; init; }
                    public int? TrackNumber { get; init; }
                    public bool? IsFavorite { get; init; }
                    public int? PlayCount { get; init; }

                    // ───── Detailed-only ─────
                    public string? OriginalAlbumTitle { get; init; }
                    public string? ContentGroupDescription { get; init; }
                    public string? OriginalArtist { get; init; }
                    public string? Composer { get; init; }
                    public string? Conductor { get; init; }
                    public string? Lyricist { get; init; }
                    public string? Publisher { get; init; }
                    public List<string>? InvolvedPeople { get; init; }
                    public string? SeriesTitle { get; init; }

                    public int? TotalTracks { get; init; }
                    public int? DiscNumber { get; init; }
                    public int? TotalDiscs { get; init; }

                    public string? AlbumSortOrder { get; init; }
                    public string? AlbumArtistSortOrder { get; init; }
                    public string? ArtistSortOrder { get; init; }
                    public string? TitleSortOrder { get; init; }
                    public string? SeriesPartIndex { get; init; }

                    public DateTime? RecordingDate { get; init; }
                    public int? RecordingYear { get; init; }
                    public DateTime? OriginalReleaseDate { get; init; }
                    public int? OriginalReleaseYear { get; init; }
                    public DateTime? PublishingDate { get; init; }

                    public string? CatalogNumber { get; init; }
                    public string? EncodedBy { get; init; }
                    public string? Encoder { get; init; }
                    public string? AudioSourceUrl { get; init; }
                    public string? Genre { get; init; }
                    public int? BPM { get; init; }

                    public string? Comment { get; init; }
                    public string? Description { get; init; }
                    public string? LongDescription { get; init; }
                    public string? Language { get; init; }
                    public string? Copyright { get; init; }

                    public List<SongChapter>? Chapters { get; init; }
                    public string? UnsynchronizedLyrics { get; init; }
                    public List<LyricLine>? SynchronizedLyrics { get; init; }
                }

                public static async Task UpdateSongInfo(string audioFile, string directoryUUID, SongInfoPatch patch,
                    MusicInfoStyle mode = MusicInfoStyle.both, bool forceReParseFromAudio = false)
                {
                    var directoryPath = Path.Combine(DataPath, "Music");
                    var manager = new Yuuko.Bindings.DirectoryManager(directoryPath);
                    await manager.LoadBindings();

                    var idDirectoryPath = await manager.GetDirectoryById(directoryUUID);
                    if (idDirectoryPath == null)
                        throw new InvalidOperationException("Directory has not been resolved.");

                    var audioSourceUrl = Path.Combine(idDirectoryPath, audioFile);
                    if (!File.Exists(audioSourceUrl))
                        throw new InvalidOperationException("Song file does not exist.");

                    var overviewPath = Path.Combine(idDirectoryPath, $"{audioFile}+.yuukoMusicOverview");
                    var detailedPath = Path.Combine(idDirectoryPath, $"{audioFile}+.yuukoMusicDetailed");

                    // Check existence based on requested mode
                    if ((mode == MusicInfoStyle.overview || mode == MusicInfoStyle.both) && !File.Exists(overviewPath))
                        throw new InvalidOperationException("Overview metadata does not exist.");
                    if ((mode == MusicInfoStyle.detailed || mode == MusicInfoStyle.both) && !File.Exists(detailedPath))
                        throw new InvalidOperationException("Detailed metadata does not exist.");

                    SongOverview? overview = null;
                    SongDetailed? detailed = null;

                    // Load existing data
                    if (mode == MusicInfoStyle.overview || mode == MusicInfoStyle.both)
                    {
                        var overviewJson = await JSONDataHandler.LoadJsonFile(idDirectoryPath, $"{audioFile}+.yuukoMusicOverview");
                        var overviewBytes = (byte[])await JSONDataHandler.GetVariable<byte[]>(overviewJson, "Data", encryptionKey);
                        overview = await BinaryConverter.NCByteArrayToObjectAsync<SongOverview>(overviewBytes);
                    }

                    if (mode == MusicInfoStyle.detailed || mode == MusicInfoStyle.both)
                    {
                        var detailedJson = await JSONDataHandler.LoadJsonFile(idDirectoryPath, $"{audioFile}+.yuukoMusicDetailed");
                        var detailedBytes = (byte[])await JSONDataHandler.GetVariable<byte[]>(detailedJson, "Data", encryptionKey);
                        detailed = await BinaryConverter.NCByteArrayToObjectAsync<SongDetailed>(detailedBytes);
                    }

                    // Optional: force full re-parse from audio file (e.g. tags changed externally)
                    if (forceReParseFromAudio)
                    {
                        var (freshOverview, freshDetailed) = await CreateSongInfo(audioFile, directoryUUID, autoTag: true);
                        overview = freshOverview;
                        detailed = freshDetailed;
                    }

                    // Apply patch to overview
                    if (overview != null && (mode == MusicInfoStyle.overview || mode == MusicInfoStyle.both))
                    {
                        if (patch.SongName != null) overview.SongName = patch.SongName;
                        if (patch.AlbumName != null) overview.AlbumName = patch.AlbumName;
                        if (patch.TrackArtist != null) overview.TrackArtist = patch.TrackArtist;
                        if (patch.AlbumArtist != null) overview.AlbumArtist = patch.AlbumArtist;
                        if (patch.Duration.HasValue) overview.Duration = patch.Duration.Value;
                        if (patch.ReleasedOn.HasValue) overview.ReleasedOn = patch.ReleasedOn.Value;
                        if (patch.LastPlayedOn.HasValue) overview.LastPlayedOn = patch.LastPlayedOn.Value;
                        if (patch.SongId.HasValue) overview.SongId = patch.SongId.Value;
                        if (patch.ISRC != null) overview.ISRC = patch.ISRC;
                        if (patch.TrackNumber.HasValue) overview.TrackNumber = patch.TrackNumber.Value;
                        if (patch.IsFavorite.HasValue) overview.IsFavorite = patch.IsFavorite.Value;
                        if (patch.PlayCount.HasValue) overview.PlayCount = patch.PlayCount.Value;
                    }

                    // Apply patch to detailed
                    if (detailed != null && (mode == MusicInfoStyle.detailed || mode == MusicInfoStyle.both))
                    {
                        if (patch.OriginalAlbumTitle != null) detailed.OriginalAlbumTitle = patch.OriginalAlbumTitle;
                        if (patch.ContentGroupDescription != null) detailed.ContentGroupDescription = patch.ContentGroupDescription;
                        if (patch.OriginalArtist != null) detailed.OriginalArtist = patch.OriginalArtist;
                        if (patch.Composer != null) detailed.Composer = patch.Composer;
                        if (patch.Conductor != null) detailed.Conductor = patch.Conductor;
                        if (patch.Lyricist != null) detailed.Lyricist = patch.Lyricist;
                        if (patch.Publisher != null) detailed.Publisher = patch.Publisher;
                        if (patch.InvolvedPeople != null) detailed.InvolvedPeople = patch.InvolvedPeople;
                        if (patch.SeriesTitle != null) detailed.SeriesTitle = patch.SeriesTitle;
                        if (patch.TotalTracks.HasValue) detailed.TotalTracks = patch.TotalTracks.Value;
                        if (patch.DiscNumber.HasValue) detailed.DiscNumber = patch.DiscNumber.Value;
                        if (patch.TotalDiscs.HasValue) detailed.TotalDiscs = patch.TotalDiscs.Value;
                        if (patch.AlbumSortOrder != null) detailed.AlbumSortOrder = patch.AlbumSortOrder;
                        if (patch.AlbumArtistSortOrder != null) detailed.AlbumArtistSortOrder = patch.AlbumArtistSortOrder;
                        if (patch.ArtistSortOrder != null) detailed.ArtistSortOrder = patch.ArtistSortOrder;
                        if (patch.TitleSortOrder != null) detailed.TitleSortOrder = patch.TitleSortOrder;
                        if (patch.SeriesPartIndex != null) detailed.SeriesPartIndex = patch.SeriesPartIndex;
                        if (patch.RecordingDate.HasValue) detailed.RecordingDate = patch.RecordingDate.Value;
                        if (patch.RecordingYear.HasValue) detailed.RecordingYear = patch.RecordingYear.Value;
                        if (patch.OriginalReleaseDate.HasValue) detailed.OriginalReleaseDate = patch.OriginalReleaseDate.Value;
                        if (patch.OriginalReleaseYear.HasValue) detailed.OriginalReleaseYear = patch.OriginalReleaseYear.Value;
                        if (patch.PublishingDate.HasValue) detailed.PublishingDate = patch.PublishingDate.Value;
                        if (patch.CatalogNumber != null) detailed.CatalogNumber = patch.CatalogNumber;
                        if (patch.EncodedBy != null) detailed.EncodedBy = patch.EncodedBy;
                        if (patch.Encoder != null) detailed.Encoder = patch.Encoder;
                        if (patch.AudioSourceUrl != null) detailed.AudioSourceUrl = patch.AudioSourceUrl;
                        if (patch.Genre != null) detailed.Genre = patch.Genre;
                        if (patch.BPM.HasValue) detailed.BPM = patch.BPM.Value;
                        if (patch.Comment != null) detailed.Comment = patch.Comment;
                        if (patch.Description != null) detailed.Description = patch.Description;
                        if (patch.LongDescription != null) detailed.LongDescription = patch.LongDescription;
                        if (patch.Language != null) detailed.Language = patch.Language;
                        if (patch.Copyright != null) detailed.Copyright = patch.Copyright;
                        if (patch.Chapters != null) detailed.Chapters = patch.Chapters;
                        if (patch.UnsynchronizedLyrics != null) detailed.UnsynchronizedLyrics = patch.UnsynchronizedLyrics;
                        if (patch.SynchronizedLyrics != null) detailed.SynchronizedLyrics = patch.SynchronizedLyrics;
                    }

                    // Save updated data back
                    if (overview != null && (mode == MusicInfoStyle.overview || mode == MusicInfoStyle.both))
                    {
                        var overviewData = await BinaryConverter.NCObjectToByteArrayAsync(overview);
                        var overviewJson = await JSONDataHandler.LoadJsonFile(idDirectoryPath, $"{audioFile}+.yuukoMusicOverview");
                        overviewJson = await JSONDataHandler.UpdateJson<byte[]>(overviewJson, "Data", overviewData, encryptionKey);
                        await JSONDataHandler.SaveJson(overviewJson);
                    }

                    if (detailed != null && (mode == MusicInfoStyle.detailed || mode == MusicInfoStyle.both))
                    {
                        var detailedData = await BinaryConverter.NCObjectToByteArrayAsync(detailed);
                        var detailedJson = await JSONDataHandler.LoadJsonFile(idDirectoryPath, $"{audioFile}+.yuukoMusicDetailed");
                        detailedJson = await JSONDataHandler.UpdateJson<byte[]>(detailedJson, "Data", detailedData, encryptionKey);
                        await JSONDataHandler.SaveJson(detailedJson);
                    }
                }

                #region Utilities
                private static bool PatchTouchesOverview(SongInfoPatch p) =>
        p.SongName != null ||
        p.AlbumName != null ||
        p.TrackArtist != null ||
        p.AlbumArtist != null ||
        p.Duration != null ||
        p.ReleasedOn != null ||
        p.LastPlayedOn != null ||
        p.SongId != null ||
        p.ISRC != null ||
        p.TrackNumber != null ||
        p.IsFavorite != null ||
        p.PlayCount != null;

                private static bool PatchTouchesDetailed(SongInfoPatch p) =>
                    p.OriginalAlbumTitle != null ||
                    p.ContentGroupDescription != null ||
                    p.OriginalArtist != null ||
                    p.Composer != null ||
                    p.Lyricist != null ||
                    p.Publisher != null ||
                    p.InvolvedPeople != null ||
                    p.Chapters != null ||
                    p.SynchronizedLyrics != null ||
                    p.UnsynchronizedLyrics != null ||
                    p.Genre != null ||
                    p.BPM != null;

                [Flags]
                public enum AudioTagCapability
                {
                    None = 0,

                    // Core
                    Title = 1 << 0,
                    Album = 1 << 1,
                    Artist = 1 << 2,
                    AlbumArtist = 1 << 3,
                    TrackNumber = 1 << 4,
                    TotalTracks = 1 << 5,
                    DiscNumber = 1 << 6,
                    TotalDiscs = 1 << 7,

                    // Sorting
                    SortTitle = 1 << 8,
                    SortAlbum = 1 << 9,
                    SortArtist = 1 << 10,
                    SortAlbumArtist = 1 << 11,

                    // IDs
                    ISRC = 1 << 12,
                    CatalogNumber = 1 << 13,

                    // Dates
                    RecordingDate = 1 << 14,
                    RecordingYear = 1 << 15,
                    OriginalReleaseDate = 1 << 16,
                    OriginalReleaseYear = 1 << 17,
                    PublishingDate = 1 << 18,

                    // People
                    Composer = 1 << 19,
                    Conductor = 1 << 20,
                    Lyricist = 1 << 21,
                    Publisher = 1 << 22,
                    InvolvedPeople = 1 << 23,

                    // Style
                    Genre = 1 << 24,
                    BPM = 1 << 25,
                    Language = 1 << 26,

                    // Text blobs
                    Comment = 1 << 27,
                    Description = 1 << 28,
                    LongDescription = 1 << 29,

                    // Lyrics / structure
                    UnsyncedLyrics = 1 << 30,
                    SyncedLyrics = 1 << 31,
                    Chapters = 1 << 32
                }

                private static readonly Dictionary<string, AudioTagCapability> TagCapabilities =
        new(StringComparer.OrdinalIgnoreCase)
        {
            // ───────── MP3 (ID3v2.4) ─────────
            [".mp3"] =
            AudioTagCapability.Title |
            AudioTagCapability.Album |
            AudioTagCapability.Artist |
            AudioTagCapability.AlbumArtist |
            AudioTagCapability.TrackNumber |
            AudioTagCapability.TotalTracks |
            AudioTagCapability.DiscNumber |
            AudioTagCapability.TotalDiscs |
            AudioTagCapability.SortTitle |
            AudioTagCapability.SortAlbum |
            AudioTagCapability.SortArtist |
            AudioTagCapability.SortAlbumArtist |
            AudioTagCapability.ISRC |
            AudioTagCapability.CatalogNumber |
            AudioTagCapability.RecordingDate |
            AudioTagCapability.RecordingYear |
            AudioTagCapability.OriginalReleaseDate |
            AudioTagCapability.OriginalReleaseYear |
            AudioTagCapability.PublishingDate |
            AudioTagCapability.Composer |
            AudioTagCapability.Conductor |
            AudioTagCapability.Lyricist |
            AudioTagCapability.Publisher |
            AudioTagCapability.InvolvedPeople |
            AudioTagCapability.Genre |
            AudioTagCapability.BPM |
            AudioTagCapability.Language |
            AudioTagCapability.Comment |
            AudioTagCapability.UnsyncedLyrics |
            AudioTagCapability.SyncedLyrics |
            AudioTagCapability.Chapters,

            // ───────── FLAC (Vorbis comments) ─────────
            [".flac"] =
            AudioTagCapability.Title |
            AudioTagCapability.Album |
            AudioTagCapability.Artist |
            AudioTagCapability.AlbumArtist |
            AudioTagCapability.TrackNumber |
            AudioTagCapability.TotalTracks |
            AudioTagCapability.DiscNumber |
            AudioTagCapability.TotalDiscs |
            AudioTagCapability.SortTitle |
            AudioTagCapability.SortAlbum |
            AudioTagCapability.SortArtist |
            AudioTagCapability.SortAlbumArtist |
            AudioTagCapability.ISRC |
            AudioTagCapability.CatalogNumber |
            AudioTagCapability.RecordingDate |
            AudioTagCapability.RecordingYear |
            AudioTagCapability.OriginalReleaseDate |
            AudioTagCapability.Composer |
            AudioTagCapability.Lyricist |
            AudioTagCapability.Publisher |
            AudioTagCapability.InvolvedPeople |
            AudioTagCapability.Genre |
            AudioTagCapability.BPM |
            AudioTagCapability.Language |
            AudioTagCapability.Comment |
            AudioTagCapability.UnsyncedLyrics,

            // ───────── OGG / OPUS ─────────
            [".ogg"] =
                    AudioTagCapability.Title |
            AudioTagCapability.Album |
            AudioTagCapability.Artist |
            AudioTagCapability.AlbumArtist |
            AudioTagCapability.TrackNumber |
            AudioTagCapability.TotalTracks |
            AudioTagCapability.DiscNumber |
            AudioTagCapability.TotalDiscs |
            AudioTagCapability.Composer |
            AudioTagCapability.Lyricist |
            AudioTagCapability.InvolvedPeople |
            AudioTagCapability.Genre |
            AudioTagCapability.Language |
            AudioTagCapability.Comment |
            AudioTagCapability.UnsyncedLyrics,

            [".opus"] =
            AudioTagCapability.Title |
            AudioTagCapability.Album |
            AudioTagCapability.Artist |
            AudioTagCapability.AlbumArtist |
            AudioTagCapability.TrackNumber |
            AudioTagCapability.TotalTracks |
            AudioTagCapability.DiscNumber |
            AudioTagCapability.TotalDiscs |
            AudioTagCapability.Composer |
            AudioTagCapability.Lyricist |
            AudioTagCapability.InvolvedPeople |
            AudioTagCapability.Genre |
            AudioTagCapability.Language |
            AudioTagCapability.Comment |
            AudioTagCapability.UnsyncedLyrics,

            // ───────── M4A / MP4 ─────────
            [".m4a"] =
            AudioTagCapability.Title |
            AudioTagCapability.Album |
            AudioTagCapability.Artist |
            AudioTagCapability.AlbumArtist |
            AudioTagCapability.TrackNumber |
            AudioTagCapability.DiscNumber |
            AudioTagCapability.Genre |
            AudioTagCapability.Comment |
            AudioTagCapability.Composer |
            AudioTagCapability.Language,
            [".mp4"] =
            AudioTagCapability.Title |
            AudioTagCapability.Album |
            AudioTagCapability.Artist |
            AudioTagCapability.AlbumArtist |
            AudioTagCapability.TrackNumber |
            AudioTagCapability.DiscNumber |
            AudioTagCapability.Genre |
            AudioTagCapability.Comment |
            AudioTagCapability.Composer |
            AudioTagCapability.Language
        };

                private static AudioTagCapability GetRequiredCapabilities(SongInfoPatch p)
                {
                    AudioTagCapability c = AudioTagCapability.None;

                    if (p.SongName != null) c |= AudioTagCapability.Title;
                    if (p.AlbumName != null) c |= AudioTagCapability.Album;
                    if (p.TrackArtist != null) c |= AudioTagCapability.Artist;
                    if (p.AlbumArtist != null) c |= AudioTagCapability.AlbumArtist;

                    if (p.TrackNumber != null) c |= AudioTagCapability.TrackNumber;
                    if (p.TotalTracks != null) c |= AudioTagCapability.TotalTracks;
                    if (p.DiscNumber != null) c |= AudioTagCapability.DiscNumber;
                    if (p.TotalDiscs != null) c |= AudioTagCapability.TotalDiscs;

                    if (p.ISRC != null) c |= AudioTagCapability.ISRC;
                    if (p.CatalogNumber != null) c |= AudioTagCapability.CatalogNumber;

                    if (p.RecordingDate != null) c |= AudioTagCapability.RecordingDate;
                    if (p.RecordingYear != null) c |= AudioTagCapability.RecordingYear;
                    if (p.OriginalReleaseDate != null) c |= AudioTagCapability.OriginalReleaseDate;
                    if (p.OriginalReleaseYear != null) c |= AudioTagCapability.OriginalReleaseYear;
                    if (p.PublishingDate != null) c |= AudioTagCapability.PublishingDate;

                    if (p.Composer != null) c |= AudioTagCapability.Composer;
                    if (p.Conductor != null) c |= AudioTagCapability.Conductor;
                    if (p.Lyricist != null) c |= AudioTagCapability.Lyricist;
                    if (p.Publisher != null) c |= AudioTagCapability.Publisher;
                    if (p.InvolvedPeople != null) c |= AudioTagCapability.InvolvedPeople;

                    if (p.Genre != null) c |= AudioTagCapability.Genre;
                    if (p.BPM != null) c |= AudioTagCapability.BPM;
                    if (p.Language != null) c |= AudioTagCapability.Language;

                    if (p.Comment != null) c |= AudioTagCapability.Comment;
                    if (p.Description != null) c |= AudioTagCapability.Description;
                    if (p.LongDescription != null) c |= AudioTagCapability.LongDescription;

                    if (p.UnsynchronizedLyrics != null) c |= AudioTagCapability.UnsyncedLyrics;
                    if (p.SynchronizedLyrics != null) c |= AudioTagCapability.SyncedLyrics;
                    if (p.Chapters != null) c |= AudioTagCapability.Chapters;

                    return c;
                }

                private static AudioTagCapability GetWritableCapabilities(
        string audioPath,
        SongInfoPatch patch)
                {
                    var ext = Path.GetExtension(audioPath);

                    if (!TagCapabilities.TryGetValue(ext, out var supported))
                        return AudioTagCapability.None;

                    var required = GetRequiredCapabilities(patch);

                    // Return only what CAN be written
                    return supported & required;
                }



                #endregion


                //D

                public static async Task DeleteSongInfo(string audioFile, string directoryUUID, bool deleteSong = true)
                {

                    var directoryPath = Path.Combine(DataPath, "Music");

                    var manager = new Yuuko.Bindings.DirectoryManager(directoryPath);

                    await manager.LoadBindings();

                    //Does the ID being referenced exists?

                    var idDirectoryPath = await manager.GetDirectoryById(directoryUUID);

                    if (idDirectoryPath == null)
                    {
                        throw new InvalidOperationException($"Directory has not been resolved.");
                    }


                    //Does the meta file already exist? Both

                    var audioMetadataUrlOverview = Path.Combine(idDirectoryPath, $"{audioFile}+.yuukoMusicOverview");
                    var audioMetadataUrlDetailed = Path.Combine(idDirectoryPath, $"{audioFile}+.yuukoMusicDetailed");


                    if (File.Exists(audioMetadataUrlOverview))
                    {
                        File.Delete(audioMetadataUrlOverview);
                    }

                    if (File.Exists(audioMetadataUrlDetailed))
                    {
                        File.Delete(audioMetadataUrlDetailed);
                    }

                    if (deleteSong)
                    {
                        //Does the audio source file exist? (If no, just skip)

                        var audioSourceUrl = Path.Combine(DataPath, idDirectoryPath, audioFile);

                        if (!File.Exists(audioSourceUrl))
                        {
                            Console.WriteLine($"Song file does not exist, skipping.");
                        }
                    }

                }

            }

            public class SongDirectoriesClass
            {

                //Song Directories

                //C
                public static async Task AddSongDirectory(string directory, string directoryName)
                {
                    var directoryPath = Path.Combine(DataPath, "Music");

                    var manager = new Yuuko.Bindings.DirectoryManager(directoryPath);

                    await manager.LoadBindings();

                    var directoryFile = await DataHandler.JSONDataHandler.LoadJsonFile("MusicDirectory", directoryPath);

                    var directories = (List<DirectoryRecord>)await DataHandler.JSONDataHandler.GetVariable<List<DirectoryRecord>>(directoryFile, "Data", encryptionKey);
                    //UUID, path name, path 

                    var directoryDataInManagerReturn = await manager.GetOrCreateDirectory(directory);

                    var directoryUUID = directoryDataInManagerReturn.Uuid;

                    if (!directories.Any(d => d.UUID == directoryUUID))
                    {
                        //Create new record

                        var record = new DirectoryRecord(directoryUUID, directoryName, directory);

                        directories.Add(record);
                    }

                    else
                    {
                        throw new InvalidOperationException($"Cannot add song directory.");
                    }


                    var editedJSON = await DataHandler.JSONDataHandler.UpdateJson<List<DirectoryRecord>>(directoryFile, "Data", directories, encryptionKey);

                    await DataHandler.JSONDataHandler.SaveJson(editedJSON);

                }
                //R
                public static async Task<(List<DirectoryRecord>, List<DirectoryRecord>)> GetSongDirectories()
                {

                    var directoryPath = Path.Combine(DataPath, "Music");

                    var manager = new Yuuko.Bindings.DirectoryManager(directoryPath);

                    var directoryFile = await DataHandler.JSONDataHandler.LoadJsonFile("MusicDirectory", directoryPath);

                    var directories = (List<DirectoryRecord>)await DataHandler.JSONDataHandler.GetVariable<List<DirectoryRecord>>(directoryFile, "Data", encryptionKey);
                    //UUID, path name, path 

                    //What bindings exist? Let's go through each and see

                    List<DirectoryRecord> resolvedFiles = new List<DirectoryRecord>();
                    List<DirectoryRecord> unresolvedFiles = new List<DirectoryRecord>();
                    //UUID, Path Name, Path

                    foreach (var directory in directories)
                    {
                        string? foundDirectoryPath = await manager.GetDirectoryById(directory.UUID);

                        if (foundDirectoryPath == null)
                        {
                            unresolvedFiles.Add(directory);
                        }

                        else
                        {
                            resolvedFiles.Add(directory);

                        }
                    }

                    return (resolvedFiles, unresolvedFiles);

                }

                public static async Task<List<string>> GetSongDirectories(bool onlyResolved = true)
                {
                    var (resolved, unresolved) = await GetSongDirectories();
                    return onlyResolved ? resolved.Select(r => r.Path).ToList() : resolved.Concat(unresolved).Select(r => r.Path).ToList();
                }

                //U
                public static async Task UpdateSongDirectory(string uuid, string newDirectory, string newDirectoryName)
                {
                    var directoryPath = Path.Combine(DataPath, "Music");
                    var manager = new Yuuko.Bindings.DirectoryManager(directoryPath);

                    await manager.LoadBindings();

                    var directoryFile = await DataHandler.JSONDataHandler.LoadJsonFile("MusicDirectory", directoryPath);
                    var directories = (List<DirectoryRecord>)await DataHandler.JSONDataHandler.GetVariable<List<DirectoryRecord>>(directoryFile, "Data", encryptionKey);

                    // Get the binding by UUID
                    var binding = manager.GetBindingById(uuid);

                    if (binding != null)
                    {
                        // Get the file record
                        var fileRecordSelected = directories.FirstOrDefault(d => d.UUID == uuid);

                        if (fileRecordSelected != null)
                        {
                            // 1. Update the FileRecord in memory
                            fileRecordSelected.Path = newDirectory;
                            fileRecordSelected.PathName = newDirectoryName;
                        }
                        else
                        {
                            Console.WriteLine($"Warning: No FileRecord found with UUID {uuid}. JSON update skipped.");
                        }

                        // 2. Update the binding in the manager
                        var newResolution = new Yuuko.Bindings.DirectoryResolution(newDirectory, verified: true);
                        bool updated = await manager.UpdateBinding(uuid, newResolution);

                        if (!updated)
                        {
                            Console.WriteLine($"Warning: Binding exists but could not be updated for UUID {uuid}.");
                        }

                        // 3. Save updated JSON
                        var editedJSON = await DataHandler.JSONDataHandler.UpdateJson<List<DirectoryRecord>>(directoryFile, "Data", directories, encryptionKey);
                        await DataHandler.JSONDataHandler.SaveJson(editedJSON);
                    }
                    else
                    {
                        Console.WriteLine($"Warning: No binding found with UUID {uuid}. Update skipped.");
                    }
                }
                //D
                public static async Task RemoveSongDirectory(string uuid, bool deleteDirectory = false)
                {
                    var directoryPath = Path.Combine(DataPath, "Music");

                    var manager = new Yuuko.Bindings.DirectoryManager(directoryPath);

                    await manager.LoadBindings();

                    var directoryFile = await DataHandler.JSONDataHandler.LoadJsonFile("MusicDirectory", directoryPath);

                    var directories = (List<DirectoryRecord>)await DataHandler.JSONDataHandler.GetVariable<List<DirectoryRecord>>(directoryFile, "Data", encryptionKey);
                    //UUID, path name, path 

                    var directoryDataInManagerReturn = await manager.GetDirectoryById(uuid);

                    if (directories.Any(d => d.UUID == uuid))
                    {
                        //Delete
                        await manager.DeleteBinding(uuid);

                        if (deleteDirectory)
                        {
                            Directory.Delete(directoryPath, true);
                            throw new InvalidOperationException($"Cannot remove song directory: UUID '{uuid}' not found.");
                        }
                    }

                    else
                    {
                        //throw warning and stop

                    }

                    var editedJSON = await DataHandler.JSONDataHandler.UpdateJson<List<DirectoryRecord>>(directoryFile, "Data", directories, encryptionKey);

                    await DataHandler.JSONDataHandler.SaveJson(editedJSON);

                }

            }

            public class SongFavoritesClass
            {

                //Favorite Songs

                //C
                public static async Task AddToFavorites(string audioFileName, string directoryUUID)
                {
                    var directoryPath = Path.Combine(DataPath, "Music");

                    var manager = new Yuuko.Bindings.DirectoryManager(directoryPath);

                    await manager.LoadBindings();

                    var favoritesFile = await DataHandler.JSONDataHandler.LoadJsonFile("MusicFavorites", directoryPath);

                    var favorites = (List<FileRecord>)await DataHandler.JSONDataHandler.GetVariable<List<FileRecord>>(favoritesFile, "Data", encryptionKey);
                    //UUID, path name, path 


                    if (!favorites.Any(d => d.UUID == directoryUUID & d.File == audioFileName))
                    {
                        //Create new record

                        var record = new FileRecord(directoryUUID, audioFileName);

                        favorites.Add(record);
                    }

                    else
                    {
                        throw new InvalidOperationException($"Song already favorited.");
                    }


                    var editedJSON = await DataHandler.JSONDataHandler.UpdateJson<List<FileRecord>>(favoritesFile, "Data", favorites, encryptionKey);

                    await DataHandler.JSONDataHandler.SaveJson(editedJSON);

                }

                //R
                public static async Task<(List<string>, List<string>)> GetFavorites()
                {

                    var directoryPath = Path.Combine(DataPath, "Music");

                    var manager = new Yuuko.Bindings.DirectoryManager(directoryPath);

                    var favoritesFile = await DataHandler.JSONDataHandler.LoadJsonFile("MusicFavorites", directoryPath);

                    var favorites = (List<FileRecord>)await DataHandler.JSONDataHandler.GetVariable<List<FileRecord>>(favoritesFile, "Data", encryptionKey);
                    //UUID, path name, path 

                    //What bindings exist? Let's go through each and see

                    List<string> resolvedFiles = new List<string>();
                    List<string> unresolvedFiles = new List<string>();
                    //UUID, Path Name, Path

                    foreach (var file in favorites)
                    {
                        string? foundDirectoryPath = await manager.GetDirectoryById(file.UUID);

                        string resolvedPath = Path.Combine(foundDirectoryPath, file.File);

                        if (!File.Exists(resolvedPath))
                        {
                            unresolvedFiles.Add(resolvedPath);
                        }

                        else
                        {
                            resolvedFiles.Add(resolvedPath);

                        }
                    }

                    return (resolvedFiles, unresolvedFiles);

                }

                public static async Task<List<string>> GetFavoritePathsAsync(bool onlyResolved = true)
                {
                    var (resolved, unresolved) = await GetFavorites();

                    if (onlyResolved)
                    {
                        return resolved;
                    }

                    var all = new List<string>(resolved);
                    all.AddRange(unresolved);
                    return all;
                }

                //D
                public static async Task RemoveFromFavorites(string audioFileName, string directoryUUID)
                {
                    var directoryPath = Path.Combine(DataPath, "Music");
                    var manager = new Yuuko.Bindings.DirectoryManager(directoryPath);
                    await manager.LoadBindings();

                    var favoritesFile = await DataHandler.JSONDataHandler.LoadJsonFile("MusicFavorites", directoryPath);
                    var favorites = (List<FileRecord>)await DataHandler.JSONDataHandler.GetVariable<List<FileRecord>>(favoritesFile, "Data", encryptionKey);
                    var removedCount = favorites.RemoveAll(d => d.UUID == directoryUUID && d.File == audioFileName);

                    if (removedCount == 0)
                    {
                        Console.WriteLine($"Song '{audioFileName}' (UUID: {directoryUUID}) was not in favorites.");
                        return;
                    }

                    // Save updated list
                    var editedJSON = await DataHandler.JSONDataHandler.UpdateJson<List<FileRecord>>(favoritesFile, "Data", favorites, encryptionKey);
                    await DataHandler.JSONDataHandler.SaveJson(editedJSON);
                }
            }

            public class SongGetClass
            {


                //Get Songs

                // Returns full paths to audio files
                public static async Task<(List<string> Resolved, List<string> Unresolved)> GetAllSongs(
                    bool onlyFavorites = false)
                {
                    var directoryPath = Path.Combine(DataPath, "Music");
                    var manager = new Yuuko.Bindings.DirectoryManager(directoryPath);
                    await manager.LoadBindings();

                    HashSet<(string UUID, string File)> selectedFiles;

                    if (onlyFavorites)
                    {
                        var favoritesFile = await JSONDataHandler.LoadJsonFile("MusicFavorites", directoryPath);
                        var favorites = (List<FileRecord>)await JSONDataHandler.GetVariable<List<FileRecord>>(favoritesFile, "Data", encryptionKey);
                        selectedFiles = favorites
                            .Select(f => (f.UUID, f.File))
                            .ToHashSet();
                    }
                    else
                    {
                        // All known songs = all files in all bound directories
                        selectedFiles = new HashSet<(string, string)>();

                        var allBindings = manager.GetAllBindings();

                        foreach (var binding in allBindings)
                        {
                            var dirPath = await manager.GetDirectoryById(binding.Ref.DirectoryId);
                            if (string.IsNullOrEmpty(dirPath) || !Directory.Exists(dirPath))
                                continue;

                            var audioFiles = Directory.EnumerateFiles(dirPath, "*.*", SearchOption.AllDirectories)
                        .Where(IsAudioFile)
                        .Select(f => (
                            binding.Ref.DirectoryId,
                            Path.GetRelativePath(dirPath, f)
                        ));


                            foreach (var file in audioFiles)
                                selectedFiles.Add(file);
                        }
                    }

                    var resolved = new List<string>();
                    var unresolved = new List<string>();

                    foreach (var (uuid, filename) in selectedFiles)
                    {
                        var dirPath = await manager.GetDirectoryById(uuid);
                        if (dirPath == null)
                        {
                            unresolved.Add($"[unresolved:{uuid}]{filename}");
                            continue;
                        }

                        var fullPath = Path.Combine(dirPath, filename);
                        if (File.Exists(fullPath))
                            resolved.Add(fullPath);
                        else
                            unresolved.Add(fullPath);
                    }

                    return (resolved, unresolved);
                }

                public static async Task<List<string>> GetAllSongs(
                   bool onlyResolved = true,
                   bool onlyFavorites = false)
                {
                    var (resolved, unresolved) = await GetAllSongs(onlyFavorites);

                    return onlyResolved
                        ? resolved
                        : resolved.Concat(unresolved).ToList();
                }


                //Make dictionary later, might make public
                private static bool IsAudioFile(string path)
                {
                    var ext = Path.GetExtension(path).ToLowerInvariant();
                    return ext is ".mp3" or ".flac" or ".m4a" or ".mp4" or ".ogg" or ".opus" or ".wav" or ".aiff" or ".aac";
                }


                public static async Task<(List<string> ResolvedSongs, List<string> UnresolvedSongs)>
                GetSongsInDirectoryAsync(string directoryUUID, bool onlyFavorites = false)
                {
                    var directoryPath = Path.Combine(DataPath, "Music");
                    var manager = new Yuuko.Bindings.DirectoryManager(directoryPath);
                    await manager.LoadBindings();

                    var dirPath = await manager.GetDirectoryById(directoryUUID);
                    if (dirPath == null)
                        throw new InvalidOperationException($"Directory UUID not resolved: {directoryUUID}");

                    HashSet<string> favoriteFiles = new();

                    if (onlyFavorites)
                    {
                        var favFile = await JSONDataHandler.LoadJsonFile("MusicFavorites", directoryPath);
                        var favorites = (List<FileRecord>)await JSONDataHandler.GetVariable<List<FileRecord>>(favFile, "Data", encryptionKey);

                        favoriteFiles = favorites
                            .Where(f => f.UUID == directoryUUID)
                            .Select(f => f.File)
                            .ToHashSet(StringComparer.OrdinalIgnoreCase);
                    }

                    var resolved = new List<string>();
                    var unresolved = new List<string>();

                    var candidates = Directory.EnumerateFiles(dirPath, "*.*", SearchOption.AllDirectories)
                        .Where(f => IsAudioFile(f))
                        .Select(Path.GetFileName);

                    foreach (var filename in candidates)
                    {
                        if (onlyFavorites && !favoriteFiles.Contains(filename))
                            continue;

                        var fullPath = Path.Combine(dirPath, filename);

                        if (File.Exists(fullPath))
                            resolved.Add(fullPath);
                        else
                            unresolved.Add(fullPath);
                    }

                    return (resolved, unresolved);
                }




                public static async Task<List<string>> GetSongsByNameAsync(string nameFragment, StringComparison comparison = StringComparison.OrdinalIgnoreCase,
                bool onlyFavorites = false)
                {
                    var (allResolved, _) = await GetAllSongs(onlyFavorites);

                    return allResolved
                        .Where(path =>
                            Path.GetFileNameWithoutExtension(path)
                                .Contains(nameFragment, comparison))
                        .ToList();
                }

                public enum SongSearchField
                {
                    SongName,
                    AlbumName,
                    TrackArtist,
                    AlbumArtist,
                    Genre,
                    ISRC,
                    Comment,
                    // can add more later, for now this works
                }

                public static async Task<List<string>> GetSongsByTag(
                    SongSearchField field,
                    string value,
                    StringComparison comparison = StringComparison.OrdinalIgnoreCase,
                    bool onlyFavorites = false)
                {
                    var directoryPath = Path.Combine(DataPath, "Music");
                    var manager = new Yuuko.Bindings.DirectoryManager(directoryPath);
                    await manager.LoadBindings();

                    HashSet<(string UUID, string File)> selectedFiles;

                    if (onlyFavorites)
                    {
                        var favoritesFile = await JSONDataHandler.LoadJsonFile("MusicFavorites", directoryPath);
                        var favorites = (List<FileRecord>)await JSONDataHandler
                            .GetVariable<List<FileRecord>>(favoritesFile, "Favorites", encryptionKey);

                        selectedFiles = favorites
                            .Select(f => (f.UUID, f.File))
                            .ToHashSet();
                    }
                    else
                    {
                        selectedFiles = new HashSet<(string, string)>();
                        var allBindings = manager.GetAllBindings();

                        foreach (var binding in allBindings)
                        {
                            var dirPath = await manager.GetDirectoryById(binding.Ref.DirectoryId);
                            if (string.IsNullOrEmpty(dirPath) || !Directory.Exists(dirPath))
                                continue;

                            foreach (var file in Directory.EnumerateFiles(dirPath, "*.*", SearchOption.AllDirectories)
                                     .Where(IsAudioFile))
                            {
                                var relative = Path.GetRelativePath(dirPath, file);
                                selectedFiles.Add((binding.Ref.DirectoryId, relative));
                            }
                        }
                    }

                    // Cache UUID -> dirPath def makes sense trust
                    var uuidCache = new Dictionary<string, string>();

                    var matches = new List<string>();

                    foreach (var (uuid, relativePath) in selectedFiles)
                    {
                        if (!uuidCache.TryGetValue(uuid, out var dirPath))
                        {
                            dirPath = await manager.GetDirectoryById(uuid);
                            uuidCache[uuid] = dirPath ?? string.Empty;
                        }

                        if (string.IsNullOrEmpty(dirPath))
                            continue;

                        var fullPath = Path.Combine(dirPath, relativePath);
                        if (!File.Exists(fullPath))
                            continue;

                        try
                        {
                            var fileName = Path.GetFileName(fullPath);
                            var (overview, detailed) = await SongClass.GetSongInfo(fileName, uuid, SongClass.MusicInfoStyle.both);

                            bool match = field switch
                            {
                                SongSearchField.SongName => overview?.SongName?.Contains(value, comparison) == true,
                                SongSearchField.AlbumName => overview?.AlbumName?.Contains(value, comparison) == true,
                                SongSearchField.TrackArtist => overview?.TrackArtist?.Contains(value, comparison) == true,
                                SongSearchField.AlbumArtist => overview?.AlbumArtist?.Contains(value, comparison) == true,
                                SongSearchField.Genre => detailed?.Genre?.Contains(value, comparison) == true,
                                SongSearchField.ISRC => overview?.ISRC?.Equals(value, comparison) == true,
                                SongSearchField.Comment => detailed?.Comment?.Contains(value, comparison) == true,
                                _ => false
                            };

                            if (match)
                                matches.Add(fullPath);
                        }
                        catch
                        {
                            // skip broken metadata
                        }
                    }

                    return matches;
                }


            }

            public class MusicHistoryClass
            {
                //Favorite Songs

                //C
                public static async Task AddToPlayHistory(string audioFileName, string directoryUUID)
                {
                    var directoryPath = Path.Combine(DataPath, "Music");

                    var manager = new Yuuko.Bindings.DirectoryManager(directoryPath);

                    await manager.LoadBindings();

                    var historyFile = await DataHandler.JSONDataHandler.LoadJsonFile("MusicHistory", directoryPath);

                    var history = (List<FileRecord>)await DataHandler.JSONDataHandler.GetVariable<List<FileRecord>>(historyFile, "Data", encryptionKey);
                    //UUID, path name, path 


                    if (!history.Any(d => d.UUID == directoryUUID & d.File == audioFileName))
                    {
                        //Create new record

                        var record = new FileRecord(directoryUUID, audioFileName);

                        history.Add(record);
                    }

                    else
                    {
                        throw new InvalidOperationException($"Song already favorited.");
                    }


                    var editedJSON = await DataHandler.JSONDataHandler.UpdateJson<List<FileRecord>>(historyFile, "Data", history, encryptionKey);

                    await DataHandler.JSONDataHandler.SaveJson(editedJSON);

                }

                //R
                public static async Task<(List<string>, List<string>)> GetPlayHistory()
                {

                    var directoryPath = Path.Combine(DataPath, "Music");

                    var manager = new Yuuko.Bindings.DirectoryManager(directoryPath);

                    var historyFile = await DataHandler.JSONDataHandler.LoadJsonFile("MusicHistory", directoryPath);

                    var history = (List<FileRecord>)await DataHandler.JSONDataHandler.GetVariable<List<FileRecord>>(historyFile, "Data", encryptionKey);
                    //UUID, path name, path 

                    //What bindings exist? Let's go through each and see

                    List<string> resolvedFiles = new List<string>();
                    List<string> unresolvedFiles = new List<string>();
                    //UUID, Path Name, Path

                    foreach (var file in history)
                    {
                        string? foundDirectoryPath = await manager.GetDirectoryById(file.UUID);

                        string resolvedPath = Path.Combine(foundDirectoryPath, file.File);

                        if (!File.Exists(resolvedPath))
                        {
                            unresolvedFiles.Add(resolvedPath);
                        }

                        else
                        {
                            resolvedFiles.Add(resolvedPath);

                        }
                    }

                    return (resolvedFiles, unresolvedFiles);

                }



                //D
                public static async Task ClearPlayHIstory(string audioFileName, string directoryUUID)
                {
                    var directoryPath = Path.Combine(DataPath, "Music");
                    var manager = new Yuuko.Bindings.DirectoryManager(directoryPath);
                    await manager.LoadBindings();

                    var historyFile = await DataHandler.JSONDataHandler.LoadJsonFile("MusicHistory", directoryPath);
                    var overwriteData = new List<FileRecord>();

                    // Save updated list
                    var editedJSON = await DataHandler.JSONDataHandler.UpdateJson<List<FileRecord>>(historyFile, "Data", overwriteData, encryptionKey);
                    await DataHandler.JSONDataHandler.SaveJson(editedJSON);
                }





            }

        }


        public sealed record ResolvedMedia
        {
            public string FullPath { get; init; }
            public string FileName { get; init; }
            public string DirectoryUuid { get; init; }
            public long SizeBytes { get; init; }
            public DateTime LastModifiedUtc { get; init; }


            public ResolvedMedia() { }

        }


        public static class Media
        {
            //This is harmless so we give access to all files; it only gives basic information. However never expose any info beyond basic metadata
            public static async Task<ResolvedMedia> GetFile(
                string directoryUuid,
                string fileName,
                CancellationToken ct = default)
            {
                if (string.IsNullOrWhiteSpace(directoryUuid))
                    throw new ArgumentException("Invalid directory UUID.", nameof(directoryUuid));

                if (string.IsNullOrWhiteSpace(fileName))
                    throw new ArgumentException("Invalid file name.", nameof(fileName));

                // Prevent path traversal or absolute paths
                if (Path.IsPathRooted(fileName) ||
                    fileName.Contains(Path.DirectorySeparatorChar) ||
                    fileName.Contains(Path.AltDirectorySeparatorChar))
                {
                    throw new InvalidOperationException("Invalid media file name.");
                }

                var bindingsPath = Path.Combine(DataPath, "Generic");
                var manager = new Yuuko.Bindings.DirectoryManager(bindingsPath);

                await manager.LoadBindings(ct);

                ct.ThrowIfCancellationRequested();

                var directory = await manager.GetDirectoryById(directoryUuid, ct: ct);
                if (directory == null)
                    throw new DirectoryNotFoundException("Media directory could not be resolved.");

                var fullPath = Path.Combine(directory, fileName);
                var info = new FileInfo(fullPath);

                if (!info.Exists)
                    throw new FileNotFoundException("Media file not found.", fileName);

                return new ResolvedMedia
                {
                    FullPath = info.FullName,
                    FileName = info.Name,
                    DirectoryUuid = directoryUuid,
                    SizeBytes = info.Length,
                    LastModifiedUtc = info.LastWriteTimeUtc
                };
            }


            public static async Task<DirectoryRecord?> GetOrCreateDirectory(string fullFilePath, string? directory, string? directoryName, CancellationToken ct = default)
            {

                var Directories = await GetGenericDirectories();

                var check1 = Directories.Item1.Any(b => b.Path == directory);
                var check2 = Directories.Item2.Any(b => b.Path == directory);

                DirectoryRecord returnVal = null;

                if (check1)
                {
                    returnVal = Directories.Item1.FirstOrDefault (b => b.Path == directory);
                }

                if (check2)
                {
                    returnVal = Directories.Item2.FirstOrDefault(b => b.Path == directory);

                }

                else
                {
                    await AddGenericDirectory(directory, directoryName);
                }

                return returnVal;

            }




            //C
            public static async Task AddGenericDirectory(string directory, string directoryName)
            {
                var directoryPath = Path.Combine(DataPath, "Generic");

                var manager = new Yuuko.Bindings.DirectoryManager(directoryPath);

                await manager.LoadBindings();

                var directoryFile = await DataHandler.JSONDataHandler.LoadJsonFile("GenericBank", directoryPath);

                var directories = (List<DirectoryRecord>)await DataHandler.JSONDataHandler.GetVariable<List<DirectoryRecord>>(directoryFile, "Data", encryptionKey);
                //UUID, path name, path 

                var directoryDataInManagerReturn = await manager.GetOrCreateDirectory(directory);

                var directoryUUID = directoryDataInManagerReturn.Uuid;

                if (!directories.Any(d => d.UUID == directoryUUID))
                {
                    //Create new record

                    var record = new DirectoryRecord(directoryUUID, directoryName, directory);

                    directories.Add(record);
                }

                else
                {
                    throw new InvalidOperationException($"Cannot add directory directory.");
                }


                var editedJSON = await DataHandler.JSONDataHandler.UpdateJson<List<DirectoryRecord>>(directoryFile, "Data", directories, encryptionKey);

                await DataHandler.JSONDataHandler.SaveJson(editedJSON);

            }
            //R
            public static async Task<(List<DirectoryRecord>, List<DirectoryRecord>)> GetGenericDirectories()
            {

                var directoryPath = Path.Combine(DataPath, "Generic");

                var manager = new Yuuko.Bindings.DirectoryManager(directoryPath);

                var directoryFile = await DataHandler.JSONDataHandler.LoadJsonFile("GenericBank", directoryPath);

                var directories = (List<DirectoryRecord>)await DataHandler.JSONDataHandler.GetVariable<List<DirectoryRecord>>(directoryFile, "Data", encryptionKey);
                //UUID, path name, path 

                //What bindings exist? Let's go through each and see

                List<DirectoryRecord> resolvedFiles = new List<DirectoryRecord>();
                List<DirectoryRecord> unresolvedFiles = new List<DirectoryRecord>();
                //UUID, Path Name, Path

                foreach (var directory in directories)
                {
                    string? foundDirectoryPath = await manager.GetDirectoryById(directory.UUID);

                    if (foundDirectoryPath == null)
                    {
                        unresolvedFiles.Add(directory);
                    }

                    else
                    {
                        resolvedFiles.Add(directory);

                    }
                }

                return (resolvedFiles, unresolvedFiles);

            }

            public static async Task<List<string>> GetGenericDirectories(bool onlyResolved = true)
            {
                var (resolved, unresolved) = await GetGenericDirectories();
                return onlyResolved ? resolved.Select(r => r.Path).ToList() : resolved.Concat(unresolved).Select(r => r.Path).ToList();
            }

            //U
            public static async Task UpdateGenericDirectory(string uuid, string newDirectory, string newDirectoryName)
            {
                var directoryPath = Path.Combine(DataPath, "Generic");
                var manager = new Yuuko.Bindings.DirectoryManager(directoryPath);

                await manager.LoadBindings();

                var directoryFile = await DataHandler.JSONDataHandler.LoadJsonFile("GenericBank", directoryPath);
                var directories = (List<DirectoryRecord>)await DataHandler.JSONDataHandler.GetVariable<List<DirectoryRecord>>(directoryFile, "Data", encryptionKey);

                // Get the binding by UUID
                var binding = manager.GetBindingById(uuid);

                if (binding != null)
                {
                    // Get the file record
                    var fileRecordSelected = directories.FirstOrDefault(d => d.UUID == uuid);

                    if (fileRecordSelected != null)
                    {
                        // 1. Update the FileRecord in memory
                        fileRecordSelected.Path = newDirectory;
                        fileRecordSelected.PathName = newDirectoryName;
                    }
                    else
                    {
                        Console.WriteLine($"Warning: No FileRecord found with UUID {uuid}. JSON update skipped.");
                    }

                    // 2. Update the binding in the manager
                    var newResolution = new Yuuko.Bindings.DirectoryResolution(newDirectory, verified: true);
                    bool updated = await manager.UpdateBinding(uuid, newResolution);

                    if (!updated)
                    {
                        Console.WriteLine($"Warning: Binding exists but could not be updated for UUID {uuid}.");
                    }

                    // 3. Save updated JSON
                    var editedJSON = await DataHandler.JSONDataHandler.UpdateJson<List<DirectoryRecord>>(directoryFile, "Data", directories, encryptionKey);
                    await DataHandler.JSONDataHandler.SaveJson(editedJSON);
                }
                else
                {
                    Console.WriteLine($"Warning: No binding found with UUID {uuid}. Update skipped.");
                }
            }
            //D
            public static async Task RemoveGenericDirectory(string uuid, bool deleteDirectory = false)
            {
                var directoryPath = Path.Combine(DataPath, "Generic");

                var manager = new Yuuko.Bindings.DirectoryManager(directoryPath);

                await manager.LoadBindings();

                var directoryFile = await DataHandler.JSONDataHandler.LoadJsonFile("GenericBank", directoryPath);

                var directories = (List<DirectoryRecord>)await DataHandler.JSONDataHandler.GetVariable<List<DirectoryRecord>>(directoryFile, "Data", encryptionKey);
                //UUID, path name, path 

                var directoryDataInManagerReturn = await manager.GetDirectoryById(uuid);

                if (directories.Any(d => d.UUID == uuid))
                {
                    //Delete
                    await manager.DeleteBinding(uuid);

                    if (deleteDirectory)
                    {
                        Directory.Delete(directoryPath, true);
                        throw new InvalidOperationException($"Cannot remove directory directory: UUID '{uuid}' not found.");
                    }
                }

                else
                {
                    //throw warning and stop

                }

                var editedJSON = await DataHandler.JSONDataHandler.UpdateJson<List<DirectoryRecord>>(directoryFile, "Data", directories, encryptionKey);

                await DataHandler.JSONDataHandler.SaveJson(editedJSON);

            }


        }

        //Do Albums Later

        //Do Artists Later



        //Order for later, first do albums then playlists
        //Then artists
        //Them music player
        //Then solve .yuukoApp
        //Then data slots
        // then audio management
        //then alarm
        //Then keyboard
        //Then previews

        //You can do this!



        public static class CalendarClass
        {
            //We keep our XRUIOS events simple with three kinds of things
            //To keep things simple, we will attach media here as byte[] (Is what i'd like to tell myself)

            //C
            public static async Task<string> CreateSimpleEvent(
                DateTime eventDate,
                string summary,
                string description,
                TimeZoneInfo timezone = null,
                int durationHours = 0,
                List<FileRecord> attachmentsList = null)
            {
                timezone ??= TimeZoneInfo.Local;

                // Create start/end in the correct timezone
                var start = new CalDateTime(eventDate, timezone.Id);
                var end = new CalDateTime(eventDate.AddHours(durationHours), timezone.Id);

                var uid = Guid.NewGuid().ToString();

                var calendarEvent = new CalendarEvent
                {
                    Summary = summary,
                    Description = description,
                    Start = start,
                    End = end,
                    Uid = uid
                };

                // Attach files (only 1 image recommended but you can put other stuff long as it isn't big)
                if (attachmentsList != null && attachmentsList.Count > 0)
                {
                    var firstAttachment = attachmentsList[0];
                    var mediaPath = await Media.GetFile(firstAttachment.UUID, firstAttachment.File);

                    byte[] fileBytes;
                    using (var fs = new FileStream(
                        mediaPath.FullPath,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.ReadWrite))
                    {
                        fileBytes = new byte[fs.Length];
                        await fs.ReadAsync(fileBytes, 0, fileBytes.Length);
                    }

                    var binaryAttachment = new Attachment(fileBytes);

                    calendarEvent.Attachments = new List<Ical.Net.DataTypes.Attachment> { binaryAttachment };
                }

                // Create the calendar
                var calendar = new Ical.Net.Calendar();
                calendar.Events.Add(calendarEvent);

                if (timezone != null)
                {
                    var vtz = VTimeZone.FromSystemTimeZone(timezone);
                    calendar.AddTimeZone(vtz);
                }

                var serializer = new CalendarSerializer();
                var serializedCalendar = serializer.SerializeToString(calendar);

                var directoryPath = Path.Combine(DataPath, "Calendar");

                var filename = Guid.NewGuid().ToString() + ".ics"; //We do this to not worry about overlapping names

                var filePath = Path.Combine(directoryPath, filename);

                await File.WriteAllTextAsync(filePath, serializedCalendar);


                return uid;

            }

            public static async Task<string> CreateRecurringEvent(
                DateTime eventDate,
                string summary,
                string description,
                RecurrencePattern recurrencePattern,
                TimeZoneInfo timezone = null,
                int durationHours = 0,
                List<FileRecord> attachmentsList = null)
            {
                timezone ??= TimeZoneInfo.Local;

                // Start/end in the correct timezone
                var start = new CalDateTime(eventDate, timezone.Id);
                var end = new CalDateTime(eventDate.AddHours(durationHours), timezone.Id);

                var uid = Guid.NewGuid().ToString();


                var calendarEvent = new CalendarEvent
                {
                    Summary = summary,
                    Description = description,
                    Start = start,
                    End = end,
                    RecurrenceRules = new List<RecurrencePattern> { recurrencePattern },
                    Uid = uid

                };

                // Attach files (only 1 image recommended)
                if (attachmentsList != null && attachmentsList.Count > 0)
                {
                    var firstAttachment = attachmentsList[0];
                    var mediaPath = await Media.GetFile(firstAttachment.UUID, firstAttachment.File);

                    byte[] fileBytes;
                    using (var fs = new FileStream(
                        mediaPath.FullPath,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.ReadWrite))
                    {
                        fileBytes = new byte[fs.Length];
                        await fs.ReadAsync(fileBytes, 0, fileBytes.Length);
                    }

                    var binaryAttachment = new Attachment(fileBytes);


                    calendarEvent.Attachments = new List<Ical.Net.DataTypes.Attachment> { binaryAttachment };
                }

                // Create calendar
                var calendar = new Ical.Net.Calendar();
                calendar.Events.Add(calendarEvent);

                if (timezone != null)
                {
                    var vtz = VTimeZone.FromSystemTimeZone(timezone);
                    calendar.AddTimeZone(vtz);
                }

                var serializer = new CalendarSerializer();
                var serializedCalendar = serializer.SerializeToString(calendar);

                var directoryPath = Path.Combine(DataPath, "Calendar");

                var filename = Guid.NewGuid().ToString() + ".ics"; //We do this to not worry about overlapping names

                var filePath = Path.Combine(directoryPath, filename);

                await File.WriteAllTextAsync(filePath, serializedCalendar);

                return uid;

            }

            //R
            public static List<CalendarEvent> LoadAllEvents()
            {
                var directoryPath = Path.Combine(DataPath, "Calendar");

                if (!Directory.Exists(directoryPath))
                    return new List<CalendarEvent>();

                var files = Directory.GetFiles(directoryPath, "*.ics");
                var allEvents = new List<CalendarEvent>();

                foreach (var file in files)
                {
                    var calendar = Calendar.Load(File.ReadAllText(file));
                    allEvents.AddRange(calendar.Events);
                }

                return allEvents;
            }

            public static CalendarEvent? GetEventByUid(string uid)
            {
                var directoryPath = Path.Combine(DataPath, "Calendar");
                if (!Directory.Exists(directoryPath)) return null;

                foreach (var file in Directory.GetFiles(directoryPath, "*.ics"))
                {
                    var calendar = Calendar.Load(File.ReadAllText(file));
                    var ev = calendar.Events.FirstOrDefault(e => e.Uid == uid);
                    if (ev != null) return ev;
                }

                return null;
            }

            // U
            public static CalendarEvent? UpdateEventByUid(
            string uid,
            Action<CalendarEvent> updateAction)
            {
                var directoryPath = Path.Combine(DataPath, "Calendar");

                if (!Directory.Exists(directoryPath))
                    return null;

                foreach (var file in Directory.GetFiles(directoryPath, "*.ics"))
                {
                    var calendar = Calendar.Load(File.ReadAllText(file));
                    var calendarEvent = calendar.Events.FirstOrDefault(e => e.Uid == uid);

                    if (calendarEvent == null)
                        continue;

                    BackgroundJob.Delete($"calendar:{uid}:*");

                    updateAction(calendarEvent);

                    var serializer = new CalendarSerializer();
                    File.WriteAllText(file, serializer.SerializeToString(calendar));

                    // 4️⃣ Return updated event (caller schedules)
                    return calendarEvent;
                }

                return null;
            }

            // D
            public static void DeleteEventByUid(string uid)
            {
                BackgroundJob.Delete($"calendar:{uid}:*");

                var directoryPath = Path.Combine(DataPath, "Calendar");

                if (!Directory.Exists(directoryPath))
                    return;

                var files = Directory.GetFiles(directoryPath, "*.ics");
                foreach (var file in files)
                {
                    var calendar = Calendar.Load(File.ReadAllText(file));

                    var matchingEvent = calendar.Events.FirstOrDefault(e => e.Uid == uid);
                    if (matchingEvent != null)
                    {
                        calendar.Events.Remove(matchingEvent);

                        if (calendar.Events.Count == 0)
                            File.Delete(file);
                        else
                            File.WriteAllText(
                                file,
                                new CalendarSerializer().SerializeToString(calendar)
                            );

                        break;
                    }
                }
            }



            //TEMPORARY
            public static class CalendarNotifications
            {
                public static void Notify(string summary)
                {
                    // Do whatever "Hey, it's time!" logic you have here
                    Console.WriteLine($"Reminder: {summary}");
                    // Could also push system notifications, toast, etc.
                }
            }

            public static void ScheduleUpcomingOccurrences(
                IEnumerable<Occurrence> upcomingOccurrences,
                TimeSpan lookaheadWindow)
            {
                var now = DateTime.Now;
                var cutoff = now + lookaheadWindow;

                foreach (var occ in upcomingOccurrences)
                {
                    if (occ.Source is not CalendarEvent calendarEvent)
                        continue;

                    var startLocal = occ.Period.StartTime
                        .ToTimeZone(TimeZoneInfo.Local.Id)
                        .Value;

                    if (startLocal < now || startLocal > cutoff)
                        continue;

                    var delay = startLocal - now;
                    var jobId = $"calendar:{calendarEvent.Uid}:{startLocal:O}";

                    if (delay <= TimeSpan.Zero)
                    {
                        BackgroundJob.Enqueue(
                            jobId,
                            () => CalendarNotifications.Notify(calendarEvent.Summary)
                        );
                    }
                    else
                    {
                        BackgroundJob.Schedule(
                            jobId,
                            () => CalendarNotifications.Notify(calendarEvent.Summary),
                            delay
                        );
                    }
                }
            }






        }



        public static class StopwatchClass
        {

            public record StopwatchRecord
            {
                public int LapCount;
                public int SecondsElapsed;

                public StopwatchRecord() { }

                public StopwatchRecord(int lapCount, int secondsElapsed)
                {
                    LapCount = lapCount;
                    SecondsElapsed = secondsElapsed;
                }

            }

            public static Dictionary<string, (long, List<StopwatchRecord>)> StopWatches = new Dictionary<string, (long, List<StopwatchRecord>)>();

            public static string CreateStopwatch()
            {
                string id = Guid.NewGuid().ToString("N");

                StopWatches.Add(id, (Stopwatch.GetTimestamp(), new List<StopwatchRecord>()));

                return id;
            }

            public static TimeSpan GetTimeElapsed(string id)
            {
                try
                {
                    var val = StopWatches[id].Item1;
                    return Stopwatch.GetElapsedTime(val);
                }
                catch
                {
                    throw new InvalidOperationException("A Stopwatch with this ID does not exist.");
                }
            }

            public static StopwatchRecord CreateLap(string id)
            {
                var elapsedLaps = StopWatches[id].Item2.Count;

                var currentTime = GetTimeElapsed(id).Seconds;

                var newStopwatchRecord = new StopwatchRecord(elapsedLaps, currentTime);

                StopWatches[id].Item2.Add(newStopwatchRecord);

                return newStopwatchRecord;

            }

            public static List<StopwatchRecord> DestroyStopwatch(string id)
            {
                var records = StopWatches[id].Item2;
                StopWatches.Remove(id);
                return records;

            }

            //Create Later
            public static void SaveStopwatchValuesAsSheet(List<StopwatchRecord> Values, DateTime RecordedOn, string FileName)
            {
                var directoryPath = Path.Combine(DataPath, $"{FileName}____RecordedOn_{RecordedOn.ToShortDateString()}_{RecordedOn.ToShortTimeString()}.csv");

                using (var writer = new StreamWriter(directoryPath))
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    csv.WriteRecords(Values);
                }
            }



        }


        public class ClipboardClass
        {
            public class BaseClipboard
            {
                internal static Dictionary<string, string> Clipboard = new Dictionary<string, string>();

                //R
                public Dictionary<string, string> LoadClipboard()
                {
                    return Clipboard;
                }

                public async Task<byte[]> GetClipboardItem(string key)
                {
                    var value = SecureStore.Get<byte[]>("last_session");
                    return value;
                }

                //U
                public void AddToClipboard(byte[] item, string itemName)
                {
                    SecureStore.Set(itemName, item);
                }

                //D
                public void RemoveFromClipboard(string itemName)
                {
                    string path = GetPath(itemName);

                    File.Delete(path);
                }


                private static string BasePath
                {
                    get
                    {
                        // Use per-session temp directory on all platforms
                        string sessionDir = Path.Combine(Path.GetTempPath(), "SECURE_STORE" + Environment.UserName);
                        Directory.CreateDirectory(sessionDir);
                        return sessionDir;
                    }
                }

                private static string GetPath(string key) =>
        Path.Combine(BasePath, $"secstr_{key}.dat");

            }

            public class ClipboardGroups
            {
                private static Dictionary<string, Dictionary<string, byte[]>> ClipboardGroup = new();

                // R
                public Dictionary<string, byte[]> LoadClipboard(string groupName)
                {
                    if (!ClipboardGroup.ContainsKey(groupName))
                        ClipboardGroup[groupName] = new Dictionary<string, byte[]>();

                    return ClipboardGroup[groupName];
                }

                public async Task<byte[]> GetClipboardItem(string groupName, string key)
                {
                    var group = LoadClipboard(groupName);
                    if (group.TryGetValue(key, out var value))
                        return value;

                    // Fallback: load from file if exists
                    string path = GetPath(groupName, key);
                    if (File.Exists(path))
                        return await File.ReadAllBytesAsync(path);

                    return null;
                }

                // U
                public void AddToClipboard(string groupName, byte[] item, string key)
                {
                    var group = LoadClipboard(groupName);
                    group[key] = item;

                    // Save to disk
                    string path = GetPath(groupName, key);
                    File.WriteAllBytes(path, item);
                }

                // D
                public void RemoveFromClipboard(string groupName, string key)
                {
                    var group = LoadClipboard(groupName);
                    if (group.ContainsKey(key))
                        group.Remove(key);

                    string path = GetPath(groupName, key);
                    if (File.Exists(path))
                        File.Delete(path);
                }


                private static string BasePath
                {
                    get
                    {
                        string sessionDir = Path.Combine(Path.GetTempPath(), "SECURE_STORE_" + Environment.UserName);
                        Directory.CreateDirectory(sessionDir);
                        return sessionDir;
                    }
                }

                private static string GetPath(string groupName, string key)
                {
                    string groupDir = Path.Combine(BasePath, groupName);
                    Directory.CreateDirectory(groupDir);
                    return Path.Combine(groupDir, $"secstr_{key}.dat");
                }
            }

        }

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

                public Creator (string name, string description, FileRecord? pfp,  List<FileRecord?> files)
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


        public class MusicPlayerClass
        {

            internal static Songs.SongOverview? CurrentlyPlaying;
            internal static List<Songs.SongOverview> Queue;

            public static class CurrentlyPlayingClass
            {
                //R
                public static SongOverview GetCurrentlyPlaying()
                {
                    return CurrentlyPlaying;
                }

                //U
                public static async Task SetCurrentlyPlaying(string audioFile, string directoryUUID)
                {
                    var overview = await SongClass.GetSongInfo(audioFile, directoryUUID, SongClass.MusicInfoStyle.overview);

                    //Try creating if an overview doesn't exist
                    if (overview.Item1 == null)
                    {
                        await SongClass.CreateSongInfo(audioFile, directoryUUID);
                        overview = await SongClass.GetSongInfo(audioFile, directoryUUID, SongClass.MusicInfoStyle.overview);
                    }

                    if (overview.Item1 == null)
                    {

                        await SongClass.CreateSongInfo(audioFile, directoryUUID);

                        throw new InvalidOperationException("The song overview could not be found or created.");
                    }

                    CurrentlyPlaying = (Songs.SongOverview)overview.Item1;

                    await MusicHistoryClass.AddToPlayHistory(audioFile, directoryUUID);
                }
            
                //D
                public static void ResetCurrentlyPlaying()
                {
                    CurrentlyPlaying = null;
                }
            
            
            }

            public static class MusicQueueClass
            {
                //C
                public static List<SongOverview> GetCurrentlyPlaying()
                {
                    return Queue;
                }

                //R
                public static async Task AddToMusicQueue(string audioFile, string directoryUUID)
                {
                    var overview = await SongClass.GetSongInfo(audioFile, directoryUUID, SongClass.MusicInfoStyle.overview);

                    //Try creating if an overview doesn't exist
                    if (overview.Item1 == null)
                    {
                        await SongClass.CreateSongInfo(audioFile, directoryUUID);
                        overview = await SongClass.GetSongInfo(audioFile, directoryUUID, SongClass.MusicInfoStyle.overview);
                    }

                    if (overview.Item1 == null)
                    {

                        await SongClass.CreateSongInfo(audioFile, directoryUUID);

                        throw new InvalidOperationException("The song overview could not be found or created.");
                    }

                    Queue.Add((SongOverview)overview.Item1);
                }

                //U
                public static async Task ReorderSong(SongOverview item, int ReorderNumber)
                {
                    if (item == null || !Queue.Contains(item) || ReorderNumber < 0 || ReorderNumber >= Queue.Count)
                    {
                        throw new InvalidOperationException("The reorder number is invalid.");
                    }

                    Queue.Remove(item);
                    Queue.Insert(ReorderNumber, item);

                }

                //D
                public static async Task RemoveSong(SongOverview item, int ReorderNumber)
                {
                    Queue.Remove(item);

                }

                public static async Task ResetQueue()
                {
                    Queue = new List<Songs.SongOverview>();
                }

            }
            //Convert musicqueue to playlist
            public static class Random
            {

            }


        }


        public class VolumeClass
        {


            public record SoundEQ
            {
                public string EQName;
                public float Software;
                public float Effects;
                public float Voice;
                public float Music;
                public float Alerts;
                public float UI;
                public float Etc;
                public ExperimentalAudio OtherVol;

                public SoundEQ() { }

                public SoundEQ(string eqname, float software, float effects, float voice, float music, float alerts, float ui, float etc, ExperimentalAudio otherVol)
                {
                    EQName = eqname;
                    Software = software;
                    Effects = effects;
                    Voice = voice;
                    Music = music;
                    Alerts = alerts;
                    UI = ui;
                    Etc = etc;
                    OtherVol = otherVol;

                }
            }

            public record AudioGroup
            {
                public string AudioGroupName;
                public List<string> AppNames;
                public List<float> Volumes;
                public bool IsStereo;

                public AudioGroup() { }


                public AudioGroup(string audiogroupname, List<string> appnames, List<float> volumes, bool isstereo)
                {
                    AudioGroupName = audiogroupname;
                    AppNames = appnames;
                    Volumes = volumes;
                    IsStereo = isstereo;


                }
            }

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
            public static SoundEQ EQ;

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

                public static List<SoundEQ> GetSoundEQDB()
                {

                    //Get the JSON File holding the MusicDirectory object for the user
                    var FileWithSoundEQDB = UniversalSave.Load(UserSoundEQDBPath, DataFormat.JSON);

                    //This file has a lot of things in it, so we are getting the variable called MusicDirectory, aka the object we are looking for
                    List<SoundEQ> target = (List<SoundEQ>)FileWithSoundEQDB.Get("SoundEQDB");

                    List<SoundEQ> ourlist = new List<SoundEQ>();

                    foreach (SoundEQ soundEQ in target)
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
                    foreach (SoundEQ item in ourdb)
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

                        var returnlist = new List<SoundEQ>();

                        foreach (SoundEQ item in ourdb)
                        {
                            returnlist.Add(new SoundEQ(item, UserPassword));
                        }

                        var FileWithSoundEQDB = UniversalSave.Load(UserSoundEQDBPath, DataFormat.JSON);

                        FileWithSoundEQDB.Set("MusicQueue", returnlist);

                        UniversalSave.Save(UserSoundEQDBPath, FileWithSoundEQDB);
                    }


                }

                public static void UpdateFromSoundEQDB(string DBName, SoundEQ input)
                {

                    var ourdb = GetSoundEQDB();


                    bool itemexists = false;
                    int round = -1;
                    foreach (SoundEQ item in ourdb)
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

                        var returnlist = new List<SoundEQ>();

                        foreach (SoundEQ item in ourdb)
                        {
                            returnlist.Add(new SoundEQ(item, UserPassword));
                        }

                        var FileWithSoundEQDB = DataHandler.JSONDataHandler.LoadJsonFile(UserSoundEQDBPath, DataFormat.JSON);

                        FileWithSoundEQDB.Set("MusicQueue", returnlist);

                        UniversalSave.Save(UserSoundEQDBPath, FileWithSoundEQDB);
                    }


                }

                public static void AddToSoundEQDB(SoundEQ input)
                {

                    var ourdb = GetSoundEQDB();


                    ourdb.Add(input);

                    var returnlist = new List<SoundEQ>();

                    foreach (SoundEQ item in ourdb)
                    {
                        returnlist.Add(new SoundEQ(item, UserPassword));
                    }

                    var FileWithSoundEQDB = UniversalSave.Load(UserSoundEQDBPath, DataFormat.JSON);

                    FileWithSoundEQDB.Set("MusicQueue", returnlist);

                    UniversalSave.Save(UserSoundEQDBPath, FileWithSoundEQDB);
                }



                public static SoundEQ GetDefaultSoundEQ()
                {

                    //Get the JSON File holding the MusicDirectory object for the user
                    var FileWithSoundEQDB = UniversalSave.Load(UserSoundEQDBPath, DataFormat.JSON);

                    //This file has a lot of things in it, so we are getting the variable called MusicDirectory, aka the object we are looking for
                    SoundEQ target = (SoundEQ)FileWithSoundEQDB.Get("DefaultSoundEQ");

                    SoundEQ item = DecryptSoundEQ(target);

                    return item;
                }

                public static SoundEQ GetUserDefaultSoundEQ()
                {

                    //Get the JSON File holding the MusicDirectory object for the user
                    var FileWithSoundEQDB = UniversalSave.Load(UserSoundEQDBPath, DataFormat.JSON);

                    //This file has a lot of things in it, so we are getting the variable called MusicDirectory, aka the object we are looking for
                    SoundEQ target = (SoundEQ)FileWithSoundEQDB.Get("UserDefaultSoundEQ");

                    SoundEQ item = DecryptSoundEQ(target);

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

                public static void SetUserDefaultSoundEQ(SoundEQ input)
                {

                    var ourinput = new SoundEQ(input, UserPassword);

                    var FileWithSoundEQDB = UniversalSave.Load(UserSoundEQDBPath, DataFormat.JSON);

                    FileWithSoundEQDB.Set("UserDefaultSoundEQ", ourinput);

                    UniversalSave.Save(UserSoundEQDBPath, FileWithSoundEQDB);


                }

                public static void ResetUserDefaultSoundEQ()
                {

                    var fancyoptions = new ExperimentalAudio(false, false, 0, 0);

                    var input = new SoundEQ(default, 100, 100, 100, 100, 100, 100, 100, fancyoptions);

                    var ourinput = new SoundEQ(input, UserPassword);

                    var FileWithSoundEQDB = UniversalSave.Load(UserSoundEQDBPath, DataFormat.JSON);

                    FileWithSoundEQDB.Set("UserDefaultSoundEQ", ourinput);

                    UniversalSave.Save(UserSoundEQDBPath, FileWithSoundEQDB);


                }

            }

            public class AudioGroups
            {
                string AudioGroupsPath = "caca";


                public List<AudioGroup> GetAllAudioGroups()
                {

                    //Get the JSON File holding the MusicDirectory object for the user
                    var FileWithAudioGroups = UniversalSave.Load(AudioGroupsPath, DataFormat.JSON);

                    //This file has a lot of things in it, so we are getting the variable called MusicDirectory, aka the object we are looking for
                    List<AudioGroup> target = (List<AudioGroup>)FileWithAudioGroups.Get("AudioGroups");

                    List<AudioGroup> ourlist = new List<AudioGroup>();

                    foreach (AudioGroup audioGroup in target)
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
                    foreach (AudioGroup item in ourdb)
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

                        var returnlist = new List<AudioGroup>();

                        foreach (AudioGroup item in ourdb)
                        {
                            returnlist.Add(new AudioGroup(item, UserPassword));
                        }

                        var FileWithAudioGroups = UniversalSave.Load(AudioGroupsPath, DataFormat.JSON);

                        FileWithAudioGroups.Set("AudioGroups", returnlist);

                        UniversalSave.Save(AudioGroupsPath, FileWithAudioGroups);
                    }


                }

                public void UpdateFromAudioGroups(string DBName, AudioGroup input)
                {

                    var ourdb = GetAllAudioGroups();


                    bool itemexists = false;
                    int round = -1;
                    foreach (AudioGroup item in ourdb)
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

                        var returnlist = new List<AudioGroup>();

                        foreach (AudioGroup item in ourdb)
                        {
                            returnlist.Add(new AudioGroup(item, UserPassword));
                        }

                        var FileWithAudioGroups = UniversalSave.Load(AudioGroupsPath, DataFormat.JSON);

                        FileWithAudioGroups.Set("AudioGroups", returnlist);

                        UniversalSave.Save(AudioGroupsPath, FileWithAudioGroups);
                    }


                }

                public void AddToAudioGroups(AudioGroup input)
                {

                    var ourdb = GetAllAudioGroups();


                    ourdb.Add(input);

                    var returnlist = new List<AudioGroup>();

                    foreach (AudioGroup item in ourdb)
                    {
                        returnlist.Add(new AudioGroup(item, UserPassword));
                    }

                    var FileWithAudioGroups = UniversalSave.Load(AudioGroupsPath, DataFormat.JSON);

                    FileWithAudioGroups.Set("AudioGroups", returnlist);

                    UniversalSave.Save(AudioGroupsPath, FileWithAudioGroups);
                }

            }


        }

        
        //Later I should make it so notifications appearing is optional; that way people can use it on the backend

        public class AlarmClass
        {

            public record Alarm
            {
                public string AlarmName;
                public DateTime AlarmTime;
                public bool IsRecurring;
                public List<DayOfWeek> RecurringDays;
                public FileRecord SoundFilePath;
                public int Volume;
                public bool IsEnabled;
                public Alarm() { }
                public Alarm(string alarmName, DateTime alarmTime, bool isRecurring, List<DayOfWeek> recurringDays, FileRecord soundFilePath, int volume, bool isEnabled)
                {
                    AlarmName = alarmName;
                    AlarmTime = alarmTime;
                    IsRecurring = isRecurring;
                    RecurringDays = recurringDays;
                    SoundFilePath = soundFilePath;
                    Volume = volume;
                    IsEnabled = isEnabled;
                }
            }
            public static ObservableCollection<Alarm> Alarms = new ObservableCollection<Alarm>();

            //C
            public static async Task AddAlarm(Alarm newAlarm)
            {
                Alarms.Add(newAlarm);

                var directoryPath = Path.Combine(DataPath, "Alarms");
                var manager = new Yuuko.Bindings.DirectoryManager(directoryPath);

                var alarmsFile = await JSONDataHandler.LoadJsonFile("Alarms", directoryPath);
                var alarmBytes = (byte[])await JSONDataHandler.GetVariable<byte[]>(alarmsFile, "Data", encryptionKey);

                var alarms = (ObservableCollection<Alarm>)await BinaryConverter.NCByteArrayToObjectAsync<ObservableCollection<Alarm>>(alarmBytes);
                alarms.Add(newAlarm);

                var updatedAlarmBytes = await BinaryConverter.NCObjectToByteArrayAsync(alarms);
                var editedJSON = await JSONDataHandler.UpdateJson<byte[]>(alarmsFile, "Data", updatedAlarmBytes, encryptionKey);

                await JSONDataHandler.SaveJson(editedJSON);

                AlarmScheduler.ScheduleAlarm(newAlarm);
            }

            //R
            public static async Task LoadAlarms()
            {
                var directoryPath = Path.Combine(DataPath, "Alarms");
                var manager = new Yuuko.Bindings.DirectoryManager(directoryPath);

                var alarmsFile = await JSONDataHandler.LoadJsonFile("Alarms", directoryPath);
                var alarmBytes = (byte[])await JSONDataHandler.GetVariable<byte[]>(alarmsFile, "Data", encryptionKey);

                var alarms = (ObservableCollection<Alarm>)await BinaryConverter.NCByteArrayToObjectAsync<ObservableCollection<Alarm>>(alarmBytes);
                Alarms = alarms;

                AlarmScheduler.ScheduleAllAlarms();

            }

            //U (Lowk headache trying to do this one)
            public static async Task<Alarm?> UpdateAlarm(Alarm existingAlarm, Action<Alarm> updateAction)
            {
                if (!Alarms.Contains(existingAlarm))
                    return null;

                // Cancel any scheduled jobs
                BackgroundJob.Delete($"alarm:{existingAlarm.AlarmName}:*");

                // Apply caller updates
                updateAction(existingAlarm);

                var directoryPath = Path.Combine(DataPath, "Alarms");
                var manager = new Yuuko.Bindings.DirectoryManager(directoryPath);

                var alarmsFile = await JSONDataHandler.LoadJsonFile("Alarms", directoryPath);
                var alarmBytes = (byte[])await JSONDataHandler.GetVariable<byte[]>(alarmsFile, "Data", encryptionKey);

                var alarms = (ObservableCollection<Alarm>)await BinaryConverter.NCByteArrayToObjectAsync<ObservableCollection<Alarm>>(alarmBytes);

                // Replace the old alarm object with the updated one
                var index = alarms.IndexOf(alarms.First(a => a.AlarmName == existingAlarm.AlarmName && a.AlarmTime == existingAlarm.AlarmTime));
                if (index >= 0)
                    alarms[index] = existingAlarm;

                var updatedAlarmBytes = await BinaryConverter.NCObjectToByteArrayAsync(alarms);
                var editedJSON = await JSONDataHandler.UpdateJson<byte[]>(alarmsFile, "Data", updatedAlarmBytes, encryptionKey);
                await JSONDataHandler.SaveJson(editedJSON);

                // Reschedule the updated alarm
                AlarmScheduler.ScheduleAlarm(existingAlarm);

                return existingAlarm;
            }

            //D I should really be putting more comments LOLLLL
            //Me looking at this in like a month
            public static async Task DeleteAlarm(Alarm alarm)
            {
                // Cancel any scheduled jobs
                BackgroundJob.Delete($"alarm:{alarm.AlarmName}:*");

                // Remove locally
                Alarms.Remove(alarm);

                var directoryPath = Path.Combine(DataPath, "Alarms");
                var manager = new Yuuko.Bindings.DirectoryManager(directoryPath);

                var alarmsFile = await JSONDataHandler.LoadJsonFile("Alarms", directoryPath);
                var alarmBytes = (byte[])await JSONDataHandler.GetVariable<byte[]>(alarmsFile, "Data", encryptionKey);

                var alarms = (ObservableCollection<Alarm>)await BinaryConverter.NCByteArrayToObjectAsync<ObservableCollection<Alarm>>(alarmBytes);

                alarms.Remove(alarm);

                var updatedAlarmBytes = await BinaryConverter.NCObjectToByteArrayAsync(alarms);
                var editedJSON = await JSONDataHandler.UpdateJson<byte[]>(alarmsFile, "Data", updatedAlarmBytes, encryptionKey);

                await JSONDataHandler.SaveJson(editedJSON);
            }

            public static class AlarmScheduler
            {
                public static void ScheduleAlarm(Alarm alarm)
                {
                    // Cancel any existing jobs
                    BackgroundJob.Delete($"alarm:{alarm.AlarmName}:*");

                    if (!alarm.IsEnabled)
                        return;

                    if (alarm.IsRecurring)
                    {
                        // Schedule for the next 7 days from now
                        var now = DateTime.Now;
                        var endWindow = now.AddDays(7);

                        for (var day = now.Date; day <= endWindow.Date; day = day.AddDays(1))
                        {
                            if (!alarm.RecurringDays.Contains(day.DayOfWeek))
                                continue;

                            var alarmTime = new DateTime(
                                day.Year, day.Month, day.Day,
                                alarm.AlarmTime.Hour, alarm.AlarmTime.Minute, alarm.AlarmTime.Second
                            );

                            var delay = alarmTime - now;
                            var jobId = $"alarm:{alarm.AlarmName}:{alarmTime:O}";

                            if (delay <= TimeSpan.Zero)
                                BackgroundJob.Enqueue(jobId, () => FireAlarm(alarm));
                            else
                                BackgroundJob.Schedule(jobId, () => FireAlarm(alarm), delay);
                        }
                    }
                    else
                    {
                        var delay = alarm.AlarmTime - DateTime.Now;
                        var jobId = $"alarm:{alarm.AlarmName}:{alarm.AlarmTime:O}";

                        if (delay <= TimeSpan.Zero)
                            BackgroundJob.Enqueue(jobId, () => FireAlarm(alarm));
                        else
                            BackgroundJob.Schedule(jobId, () => FireAlarm(alarm), delay);
                    }
                }

                private static void FireAlarm(Alarm alarm)
                {
                    // Trigger the alarm (play sound, show notification, etc.)
                    Console.WriteLine($"Alarm Triggered: {alarm.AlarmName} at {DateTime.Now}");
                    // Example: PlaySound(alarm.SoundFilePath, alarm.Volume);

                    // If recurring, reschedule itself for the next occurrence
                    if (alarm.IsRecurring)
                    {
                        ScheduleNextOccurrence(alarm);
                    }
                }

                private static void ScheduleNextOccurrence(Alarm alarm)
                {
                    var now = DateTime.Now;

                    // Find the next recurring day
                    for (int i = 1; i <= 7; i++) // look up to a week ahead
                    {
                        var nextDay = now.AddDays(i);
                        if (alarm.RecurringDays.Contains(nextDay.DayOfWeek))
                        {
                            var nextAlarmTime = new DateTime(
                                nextDay.Year, nextDay.Month, nextDay.Day,
                                alarm.AlarmTime.Hour, alarm.AlarmTime.Minute, alarm.AlarmTime.Second
                            );

                            var delay = nextAlarmTime - now;
                            var jobId = $"alarm:{alarm.AlarmName}:{nextAlarmTime:O}";

                            BackgroundJob.Schedule(jobId, () => FireAlarm(alarm), delay);
                            break;
                        }
                    }
                }

                public static void ScheduleAllAlarms()
                {
                    foreach (var alarm in Alarms)
                    {
                        ScheduleAlarm(alarm);
                    }
                }

            }



        }

        public static class TimerManagerClass
        {
            public record TimerRecord
            {
                public string TimerName;
                public DateTime EndTime;
                public bool IsRunning;
                public TimeSpan Duration;
                public Action? OnFinish;

                public TimerRecord(string timerName, TimeSpan duration, Action? onFinish = null)
                {
                    TimerName = timerName;
                    Duration = duration;
                    EndTime = DateTime.Now + duration;
                    IsRunning = false;
                    OnFinish = onFinish;
                }
            }

            public static Dictionary<string, TimerRecord> Timers = new Dictionary<string, TimerRecord>();

            //C
            public static void StartTimer(TimerRecord timer)
            {
                if (Timers.ContainsKey(timer.TimerName))
                    CancelTimer(timer.TimerName);

                timer.EndTime = DateTime.Now + timer.Duration;
                timer.IsRunning = true;
                Timers[timer.TimerName] = timer;

                ScheduleTimerJob(timer);
            }


            //U
            public static void AddTime(string timerName, TimeSpan extra)
            {
                if (!Timers.TryGetValue(timerName, out var timer)) return;

                if (timer.IsRunning)
                    timer.EndTime += extra;
                else
                    timer.Duration += extra;

                ScheduleTimerJob(timer); 
            }


            //D
            public static void CancelTimer(string timerName)
            {
                BackgroundJob.Delete($"timer:{timerName}:*");
                if (Timers.TryGetValue(timerName, out var timer))
                    timer.IsRunning = false;
            }

            private static void ScheduleTimerJob(TimerRecord timer)
            {
                var delay = timer.EndTime - DateTime.Now;
                if (delay <= TimeSpan.Zero)
                {
                    FireTimer(timer.TimerName);
                    return;
                }

                var jobId = $"timer:{timer.TimerName}:{timer.EndTime:O}";
                BackgroundJob.Schedule(jobId, () => FireTimer(timer.TimerName), delay);
            }

            //Notification TEMPORARY
            public static void FireTimer(string timerName)
            {
                if (!Timers.TryGetValue(timerName, out var timer) || !timer.IsRunning) return;

                timer.IsRunning = false;
                timer.OnFinish?.Invoke();
                Timers.Remove(timerName);
            }
        }


        public static class LoginCustomizer
        {

            public record LoginTheme
            {

            }

        }


        public class ChronoClass
        {
            public static class Date
            {

                private static DateData currentDate;

                public enum TimeFormat { TwelveHour, TwentyFourHour }

                public enum ShortTime { hhdmm, hhpmm, hhdmmds, hhpmmps } // d = :, p = .

                public enum ShortDate { mmzddzyy, ddzmmzyy, mmxddxyy, ddxmmxyy, mmcddcyy, ddcmmcyy } // z = ., x = -, c = /

                public enum LongTime
                {
                    EightThirthy,
                    ThirtyMinutesPastEight,
                    EightThirtyandTwentySeconds,
                    EightMinutesandTwentySecondsPastEight
                }

                public enum LongDate
                {
                    xxdaymmddyyyy,
                    mmddyyyy,
                    mmdd,
                    ddmmyyyy
                }

                public record DateData
                {
                    // Format selections
                    public TimeFormat TimeFormat;
                    public ShortTime ShortTimeFormat;
                    public ShortDate ShortDateFormat;
                    public LongTime LongTimeFormat;
                    public LongDate LongDateFormat;

                    public string TimeZone;

                    public List<string> WorldTimes;


                    public DateData() { }

                    public DateData(
                        TimeFormat timeFormat, ShortTime shortTimeFormat, ShortDate shortDateFormat, LongTime longTimeFormat, LongDate longDateFormat,
                        string timeZone, List<string> worldTimes)

                    {
                        TimeFormat = timeFormat;
                        ShortTimeFormat = shortTimeFormat;
                        ShortDateFormat = shortDateFormat;
                        LongTimeFormat = longTimeFormat;
                        LongDateFormat = longDateFormat;
                        TimeZone = timeZone;
                        WorldTimes = worldTimes;

                    }
                }



                public static async Task SaveDateData()
                {

                    var directoryPath = Path.Combine(DataPath, "Chrono");

                    var loadedJSON = await DataHandler.JSONDataHandler.LoadJsonFile("Chrono", directoryPath);

                    var DateData = (byte[])await BinaryConverter.NCObjectToByteArrayAsync<DateData>(currentDate);

                    var updatedJson = await DataHandler.JSONDataHandler.UpdateJson<byte[]>(loadedJSON, "Data", DateData, encryptionKey);

                    await DataHandler.JSONDataHandler.SaveJson(updatedJson);


                }

                public static async Task LoadDateData()
                {

                    var directoryPath = Path.Combine(DataPath, "Chrono");

                    var loadedJSON = await DataHandler.JSONDataHandler.LoadJsonFile("Chrono", directoryPath);

                    var dataBytes = (byte[])await DataHandler.JSONDataHandler.GetVariable<byte[]>(loadedJSON, "Data", encryptionKey);

                    var DateData = (DateData) await BinaryConverter.NCByteArrayToObjectAsync<DateData>(dataBytes);

                    currentDate = DateData;


                }

                //Getters and Setters

                //Timezone

                //G
                public static string GetTimezone(string Timezone)
                {
                    return currentDate.TimeZone;
                }

                //S
                public static void SetTimezone(string Timezone)
                {
                    TimeZoneInfo easternZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");

                    {
                        try
                        {
                            var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = "tzutil.exe",
                                Arguments = $"/s \"{Timezone}\"", // Wrap ID in quotes in case it has spaces
                                UseShellExecute = false,
                                CreateNoWindow = true
                            });

                            if (process != null)
                            {
                                process.WaitForExit();
                                // Clear the cached data in the current application domain to reflect the change
                                TimeZoneInfo.ClearCachedData();
                            }

                            currentDate.TimeZone = Timezone;

                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error changing time zone: {ex.Message}");
                            // Handle exceptions as needed
                        }
                    }
                }


                //Date

                //G

                public static (string, string) GetDate()
                {

                    // Get the time zone information
                    TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById(currentDate.TimeZone);


                    // Get the current time in the specified time zone
                    DateTime currentTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);

                    int day = currentTime.Day;
                    int month = currentTime.Month;
                    int year = currentTime.Year;

                    string ld = default;
                    string sd = default;


                    switch (currentDate.LongDateFormat)
                    {
                        case LongDate.xxdaymmddyyyy:
                            ld = $"{currentTime.DayOfWeek}, {MonthConverter.ConvertToWordedMonth(month)} {NumberConvert.NumberToWords(day)}, {NumberConvert.NumberToWords(year)}";
                            break;
                        case LongDate.mmddyyyy:
                            ld = $"{MonthConverter.ConvertToWordedMonth(month)} {NumberConvert.NumberToWords(day)}, {NumberConvert.NumberToWords(year)}";
                            break;
                        case LongDate.mmdd:
                            ld = $"{MonthConverter.ConvertToWordedMonth(month)} {NumberConvert.NumberToWords(day)}";
                            break;
                        case LongDate.ddmmyyyy:
                            ld = $"{NumberConvert.NumberToWords(day)} {MonthConverter.ConvertToWordedMonth(month)}, {NumberConvert.NumberToWords(year)}";
                            break;
                        default:
                            ld = $"{MonthConverter.ConvertToWordedMonth(month)} {NumberConvert.NumberToWords(day)}, {NumberConvert.NumberToWords(year)}";
                            break;
                    }

                    switch (currentDate.ShortDateFormat)
                    {
                        case ShortDate.mmzddzyy:
                            ld = $"{month}. {day}. {year}";
                            break;
                        case ShortDate.ddzmmzyy:
                            ld = $"{day}. {month}. {year}";
                            break;
                        case ShortDate.mmxddxyy:
                            ld = $"{month}-{day}-{year}";
                            break;
                        case ShortDate.ddxmmxyy:
                            ld = $"{day}-{month}-{year}";
                            break;
                        case ShortDate.mmcddcyy:
                            ld = $"{month}/{day}/{year}";
                            break;
                        case ShortDate.ddcmmcyy:
                            ld = $"{day}/{month}/{year}";
                            break;
                        default:
                            ld = $"{month}. {day}. {year}";
                            break;
                    }

                    return (ld, sd);
                
                }

                //S 

                public static async void SetDate(ShortDate shortDateFormat, LongDate longDateFormat)
                {

                    currentDate.ShortDateFormat = shortDateFormat;
                    currentDate.LongDateFormat = longDateFormat;

                    await SaveDateData();

                }


                //Time

                //G

                public static (string, string) GetTime()
                {

                    // Get the time zone information
                    TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById(currentDate.TimeZone);


                    // Get the current time in the specified time zone
                    DateTime currentTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);


                    int hour = currentTime.Hour;
                    int minute = currentTime.Minute;
                    int second = currentTime.Second;


                    string lt = default;
                    string st = default;

                    if (currentDate.TimeFormat == TimeFormat.TwelveHour && hour > 13)
                    {
                        hour = hour - 12;
                    }


                    switch (currentDate.LongTimeFormat)
                    {
                        case LongTime.EightThirthy:
                            lt = string.Concat(NumberConvert.NumberToWords(hour), " ", (NumberConvert.NumberToWords(minute)));
                            break;
                        case LongTime.ThirtyMinutesPastEight:
                            lt = string.Concat(NumberConvert.NumberToWords(hour), " Past ", (NumberConvert.NumberToWords(minute)));
                            break;
                        case LongTime.EightThirtyandTwentySeconds:
                            lt = string.Concat(NumberConvert.NumberToWords(hour), " ", (NumberConvert.NumberToWords(minute)), " And ", string.Concat(NumberConvert.NumberToWords(second)), " Seconds");
                            break;
                        case LongTime.EightMinutesandTwentySecondsPastEight:
                            lt = string.Concat(NumberConvert.NumberToWords(minute), " Minutes And ", (NumberConvert.NumberToWords(second)), " Seconds Past ", string.Concat(NumberConvert.NumberToWords(hour)));
                            break;
                        default:
                            lt = string.Concat(NumberConvert.NumberToWords(hour), " ", (NumberConvert.NumberToWords(minute)), " And ", string.Concat(NumberConvert.NumberToWords(second)), " Seconds");
                            break;
                    }

                    switch (currentDate.ShortTimeFormat)
                    {
                        case ShortTime.hhdmm:
                            st = string.Concat(hour, ":", minute);
                            break;
                        case ShortTime.hhpmm:
                            st = string.Concat(hour, ".", minute);
                            break;
                        case ShortTime.hhdmmds:
                            st = string.Concat(hour, ":", minute, ":", second);
                            break;
                        case ShortTime.hhpmmps:
                            st = string.Concat(hour, ".", minute, ".", second);
                            break;
                        default:
                            st = string.Concat(hour, ".", minute);
                            break;
                    }

                    if (currentDate.TimeFormat == TimeFormat.TwelveHour)
                    {
                        string amorpm = "AM";

                        if (hour >= 12)
                        {
                            amorpm = "PM";
                        }
                        st = string.Concat(st, amorpm);
                    }

                    return (lt, st);

                }

                //S
                public static async void SetTime(ShortTime shortTimeFormat, LongTime longTimeFormat)
                {

                    currentDate.ShortTimeFormat = shortTimeFormat;
                    currentDate.LongTimeFormat = longTimeFormat;

                    await SaveDateData();

                }



                //World Time

                //C
                public static void AddWorldTime(string worldTime)
                {
                    currentDate.WorldTimes.Add(worldTime);
                }

                //R
                public static List<string> GetWorldTimezoneCollection()
                {
                    return currentDate.WorldTimes;
                }


                public static List<Dictionary<string, (string, string, string, string)>> GetWorldTimes()
                {

                    var tempDictionary = new List<Dictionary<string, (string, string, string, string)>>();

                    foreach (var item in currentDate.WorldTimes)
                    {
                        tempDictionary.Add<(item, (GetTimeInTimezone(item))>);
                    }

                    return tempDictionary;

                }

                public static (string, string, string, string) GetTimeInTimezone(string timeZoneData)
                {
                    // Get the time zone information
                    TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneData);


                    // Get the current time in the specified time zone
                    DateTime currentTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);


                    int hour = currentTime.Hour;
                    int minute = currentTime.Minute;
                    int second = currentTime.Second;


                    int day = currentTime.Day;
                    int month = currentTime.Month;
                    int year = currentTime.Year;


                    string lt = default;
                    string st = default;
                    string ld = default;
                    string sd = default;



                    if (currentDate.TimeFormat == TimeFormat.TwelveHour && hour > 13)
                    {
                        hour = hour - 12;
                    }


                    switch (currentDate.LongTimeFormat)
                    {
                        case LongTime.EightThirthy:
                            lt = string.Concat(NumberConvert.NumberToWords(hour), " ", (NumberConvert.NumberToWords(minute)));
                            break;
                        case LongTime.ThirtyMinutesPastEight:
                            lt = string.Concat(NumberConvert.NumberToWords(hour), " Past ", (NumberConvert.NumberToWords(minute)));
                            break;
                        case LongTime.EightThirtyandTwentySeconds:
                            lt = string.Concat(NumberConvert.NumberToWords(hour), " ", (NumberConvert.NumberToWords(minute)), " And ", string.Concat(NumberConvert.NumberToWords(second)), " Seconds");
                            break;
                        case LongTime.EightMinutesandTwentySecondsPastEight:
                            lt = string.Concat(NumberConvert.NumberToWords(minute), " Minutes And ", (NumberConvert.NumberToWords(second)), " Seconds Past ", string.Concat(NumberConvert.NumberToWords(hour)));
                            break;
                        default:
                            lt = string.Concat(NumberConvert.NumberToWords(hour), " ", (NumberConvert.NumberToWords(minute)), " And ", string.Concat(NumberConvert.NumberToWords(second)), " Seconds");
                            break;
                    }

                    switch (currentDate.ShortTimeFormat)
                    {
                        case ShortTime.hhdmm:
                            st = string.Concat(hour, ":", minute);
                            break;
                        case ShortTime.hhpmm:
                            st = string.Concat(hour, ".", minute);
                            break;
                        case ShortTime.hhdmmds:
                            st = string.Concat(hour, ":", minute, ":", second);
                            break;
                        case ShortTime.hhpmmps:
                            st = string.Concat(hour, ".", minute, ".", second);
                            break;
                        default:
                            st = string.Concat(hour, ".", minute);
                            break;
                    }

                    if (currentDate.TimeFormat == TimeFormat.TwelveHour)
                    {
                        string amorpm = "AM";

                        if (hour >= 12)
                        {
                            amorpm = "PM";
                        }
                        st = string.Concat(st, amorpm);
                    }



                    switch (currentDate.LongDateFormat)
                    {
                        case LongDate.xxdaymmddyyyy:
                            ld = $"{currentTime.DayOfWeek}, {MonthConverter.ConvertToWordedMonth(month)} {NumberConvert.NumberToWords(day)}, {NumberConvert.NumberToWords(year)}";
                            break;
                        case LongDate.mmddyyyy:
                            ld = $"{MonthConverter.ConvertToWordedMonth(month)} {NumberConvert.NumberToWords(day)}, {NumberConvert.NumberToWords(year)}";
                            break;
                        case LongDate.mmdd:
                            ld = $"{MonthConverter.ConvertToWordedMonth(month)} {NumberConvert.NumberToWords(day)}";
                            break;
                        case LongDate.ddmmyyyy:
                            ld = $"{NumberConvert.NumberToWords(day)} {MonthConverter.ConvertToWordedMonth(month)}, {NumberConvert.NumberToWords(year)}";
                            break;
                        default:
                            ld = $"{MonthConverter.ConvertToWordedMonth(month)} {NumberConvert.NumberToWords(day)}, {NumberConvert.NumberToWords(year)}";
                            break;
                    }

                    switch (currentDate.ShortDateFormat)
                    {
                        case ShortDate.mmzddzyy:
                            ld = $"{month}. {day}. {year}";
                            break;
                        case ShortDate.ddzmmzyy:
                            ld = $"{day}. {month}. {year}";
                            break;
                        case ShortDate.mmxddxyy:
                            ld = $"{month}-{day}-{year}";
                            break;
                        case ShortDate.ddxmmxyy:
                            ld = $"{day}-{month}-{year}";
                            break;
                        case ShortDate.mmcddcyy:
                            ld = $"{month}/{day}/{year}";
                            break;
                        case ShortDate.ddcmmcyy:
                            ld = $"{day}/{month}/{year}";
                            break;
                        default:
                            ld = $"{month}. {day}. {year}";
                            break;
                    }

                    return (lt, st, ld, sd);

                }



                //D
                public static void DeleteWorldTime(string worldTime)
                {
                    currentDate.WorldTimes.Remove(worldTime);
                }













                public class NumberConvert
                {

                    //From https://github.com/ardimh7/unity-convert-number-to-words/blob/master/NumberConvert.cs

                    public static string NumberToWords(long number)
                    {
                        if (number == 0)
                            return "zero";

                        if (number < 0)
                            return "minus " + NumberToWords(Math.Abs(number));

                        string words = "";

                        if ((number / 1000000000) > 0)
                        {
                            words += NumberToWords(number / 1000000000) + " billion ";
                            number %= 1000000000;
                        }

                        if ((number / 1000000) > 0)
                        {
                            words += NumberToWords(number / 1000000) + " million ";
                            number %= 1000000;
                        }

                        if ((number / 1000) > 0)
                        {
                            words += NumberToWords(number / 1000) + " thousand ";
                            number %= 1000;
                        }

                        if ((number / 100) > 0)
                        {
                            words += NumberToWords(number / 100) + " hundred ";
                            number %= 100;
                        }

                        if (number > 0)
                        {
                            if (words != "")
                                words += "and ";

                            var unitsMap = new[] { "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten", "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen" };
                            var tensMap = new[] { "zero", "ten", "twenty", "thirty", "forty", "fifty", "sixty", "seventy", "eighty", "ninety" };

                            if (number < 20)
                                words += unitsMap[number];
                            else
                            {
                                words += tensMap[number / 10];
                                if ((number % 10) > 0)
                                    words += "-" + unitsMap[number % 10];
                            }
                        }

                        return words;
                    }
                }

                public class MonthConverter
                {
                    // Function to convert a numerical month to a worded month
                    public static string ConvertToWordedMonth(int monthNumber)
                    {
                        string[] monthNames = { "January", "February", "March", "April", "May", "June",
                                "July", "August", "September", "October", "November", "December" };

                        if (monthNumber >= 1 && monthNumber <= 12)
                        {
                            return monthNames[monthNumber - 1];
                        }
                        else
                        {
                            Console.WriteLine("Invalid month number. Please provide a number between 1 and 12.");
                            return "Invalid Month";
                        }
                    }
                }











            }

            public class Times

            {
                Timedata currenttime;


                private List<Timedata> timezonenumbers;
                private List<string> timezones;




                public struct Timedata
                {
                    public string longtime;
                    public string shorttime;

                    // Constructor for the struct
                    public Timedata(string longtime, string shorttime)
                    {
                        this.longtime = longtime;
                        this.shorttime = shorttime;
                    }
                }


                private int houroffset = 0;
                private int minuteoffset = 0;
                private int secondoffset = 0;


                private void SetOffset(int hour, int minute, int second)
                {
                    if (hour == -1)
                    {
                        //Do nothing
                    }
                    else
                    {
                        houroffset = hour;
                    }

                    if (minute == -1)
                    {
                        //Do nothing
                    }
                    else
                    {
                        minuteoffset = minute;
                    }

                    if (second == -1)
                    {
                        //Do nothing
                    }
                    else
                    {
                        secondoffset = second;
                    }
                }


                public static bool isTimeMeasurementRunning = false;

                // Example: Start the time measurement
                public void GetCurrentTime()
                {
                    if (!isTimeMeasurementRunning)
                    {
                        isTimeMeasurementRunning = true;

                        var routine = new MonoBehaviour();
                        routine.StartCoroutine(GetTime());
                    }
                }

                // Example: Stop the time measurement
                public void StopGettingCurrentTime()
                {
                    if (isTimeMeasurementRunning)
                    {
                        isTimeMeasurementRunning = false;
                        var routine = new MonoBehaviour();
                        routine.StopCoroutine(GetTime());
                    }
                }

                // Coroutine to measure time
                public IEnumerator GetTime()
                {
                    while (isTimeMeasurementRunning)
                    {
                        // Get the time zone information
                        TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById(currentuser.timeZoneInfo);

                        // Get the current time in the specified time zone
                        DateTime currentTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);

                        int hour = currentTime.Hour + houroffset;
                        int minute = currentTime.Minute + minuteoffset;
                        int second = currentTime.Second + secondoffset;


                        string lt = default;
                        string st = default;

                        if (currentuser.timeFormat == TimeFormat.TwelveHour && hour > 13)
                        {
                            hour = hour - 12;
                        }

                        switch (currentuser.longtime)
                        {
                            case LongTime.EightThirthy:
                                lt = string.Concat(NumberConvert.NumberToWords(hour), " ", (NumberConvert.NumberToWords(minute)));
                                break;
                            case LongTime.ThirtyMinutesPastEight:
                                lt = string.Concat(NumberConvert.NumberToWords(hour), " Past ", (NumberConvert.NumberToWords(minute)));
                                break;
                            case LongTime.EightThirtyandTwentySeconds:
                                lt = string.Concat(NumberConvert.NumberToWords(hour), " ", (NumberConvert.NumberToWords(minute)), " And ", string.Concat(NumberConvert.NumberToWords(second)), " Seconds");
                                break;
                            case LongTime.EightMinutesandTwentySecondsPastEight:
                                lt = string.Concat(NumberConvert.NumberToWords(minute), " Minutes And ", (NumberConvert.NumberToWords(second)), " Seconds Past ", string.Concat(NumberConvert.NumberToWords(hour)));
                                break;
                            default:
                                lt = string.Concat(NumberConvert.NumberToWords(hour), " ", (NumberConvert.NumberToWords(minute)), " And ", string.Concat(NumberConvert.NumberToWords(second)), " Seconds");
                                break;
                        }

                        switch (currentuser.shorttime)
                        {
                            case ShortTime.hhdmm:
                                st = string.Concat(hour, ":", minute);
                                break;
                            case ShortTime.hhpmm:
                                st = string.Concat(hour, ".", minute);
                                break;
                            case ShortTime.hhdmmds:
                                st = string.Concat(hour, ":", minute, ":", second);
                                break;
                            case ShortTime.hhpmmps:
                                st = string.Concat(hour, ".", minute, ".", second);
                                break;
                            default:
                                st = string.Concat(hour, ".", minute);
                                break;
                        }

                        if (currentuser.timeFormat == TimeFormat.TwelveHour)
                        {
                            string amorpm = "AM";

                            if (hour >= 12)
                            {
                                amorpm = "PM";
                            }
                            st = string.Concat(st, amorpm);
                        }

                        currenttime = new Timedata(lt, st);

                        yield return null;


                    }
                }


                public void GetCurrentTimeFromWorldTimes()
                {
                    if (!isTimeMeasurementRunning)
                    {
                        isTimeMeasurementRunning = true;

                        var routine = new MonoBehaviour();
                        routine.StartCoroutine(GetTimeWorldTimes());
                    }
                }

                // Example: Stop the time measurement
                public void StopGettingCurrentTimeWorldTimes()
                {
                    if (isTimeMeasurementRunning)
                    {
                        isTimeMeasurementRunning = false;

                        var routine = new MonoBehaviour();
                        routine.StopCoroutine(GetTimeWorldTimes());
                    }
                }

                // Coroutine to measure time
                private IEnumerator GetTimeWorldTimes()
                {
                    while (isTimeMeasurementRunning)
                    {
                        // Iterate through the list of time zones
                        for (int i = 0; i < timezones.Count; i++)
                        {
                            // Get the time zone information
                            TimeZoneInfo timeZone = TimeZoneInfo.FindSystemTimeZoneById(timezones[i]);

                            // Get the current time in the specified time zone
                            DateTime currentTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);

                            int hour = currentTime.Hour;
                            int minute = currentTime.Minute;
                            int second = currentTime.Second;

                            string lt = default;
                            string st = default;

                            if (currentuser.timeFormat == TimeFormat.TwelveHour && hour > 13)
                            {
                                hour = hour - 12;
                            }

                            switch (currentuser.longtime)
                            {
                                // ... (your existing switch case logic for longtime)
                            }

                            switch (currentuser.shorttime)
                            {
                                // ... (your existing switch case logic for shorttime)
                            }

                            if (currentuser.timeFormat == TimeFormat.TwelveHour)
                            {
                                string amorpm = "AM";

                                if (hour >= 12)
                                {
                                    amorpm = "PM";
                                }
                                st = string.Concat(st, amorpm);
                            }

                            // Update the Timedata object in the list
                            timezonenumbers[i] = new Timedata(lt, st);
                        }

                        yield return null;
                    }
                }







                //Timer stuff




                public float timerduration = 0f;
                private Coroutine timerCoroutine;

                private IEnumerator StartTimer(float duration)
                {
                    timerduration = duration;

                    var routine = new MonoBehaviour();
                    timerCoroutine = routine.StartCoroutine(RunTimer());


                    yield return true;
                }

                // Coroutine for the timer
                private IEnumerator RunTimer()
                {
                    float timer = timerduration;

                    while (timer > 0)
                    {
                        timer -= Time.deltaTime;
                        yield return null;
                    }

                    // Timer reached zero
                    yield return true;
                }

                // Function to stop the timer and reset duration
                private void StopAndResetTimer()
                {
                    if (timerCoroutine != null)
                    {

                        var routine = new MonoBehaviour();
                        routine.StopCoroutine(timerCoroutine);
                        timerduration = 0f;
                    }
                }


                private float elapsedTime = 0f;
                private bool isRunning = false;

                public List<string> times = new List<string>();

                private Coroutine stopwatchCoroutine;


                // Start the stopwatch
                public void StartStopwatch()
                {
                    if (!isRunning)
                    {
                        isRunning = true;

                        var routine = new MonoBehaviour();
                        stopwatchCoroutine = routine.StartCoroutine(UpdateStopwatch());
                    }
                }

                // Stop the stopwatch
                public void StopStopwatch()
                {
                    if (isRunning)
                    {
                        isRunning = false;
                        if (stopwatchCoroutine != null)
                        {

                            var routine = new MonoBehaviour();
                            routine.StopCoroutine(stopwatchCoroutine);
                        }
                    }
                }

                // Reset the stopwatch
                public void ResetStopwatch()
                {
                    StopStopwatch();
                    elapsedTime = 0f;
                    // Clear the recorded times
                    times.Clear();
                }

                // Coroutine to update the stopwatch
                private IEnumerator UpdateStopwatch()
                {
                    while (isRunning)
                    {
                        elapsedTime += Time.deltaTime;

                        // Update UI or perform other actions based on the elapsed time
                        // For example, you could display the elapsed time in a text field.

                        yield return null;
                    }
                }

                // Record the current time and add it to the times list
                public void RecordTime()
                {
                    if (isRunning)
                    {
                        times.Add(GetFormattedTime());
                    }
                }

                // Format the elapsed time as a string (mm:ss)
                private string GetFormattedTime()
                {
                    int minutes = Mathf.FloorToInt(elapsedTime / 60);
                    int seconds = Mathf.FloorToInt(elapsedTime % 60);
                    return string.Format("{0:00}:{1:00}", minutes, seconds);
                }
            }

            public class Location
            {

                private float latitude;
                private float longitude;

                public void GetExactCoordinates()
                {
                    if (Input.location.status == LocationServiceStatus.Running)
                    {
                        LocationInfo location = Input.location.lastData;

                        // Check if location data is valid
                        if (location.latitude != 0 && location.longitude != 0)
                        {
                            LocationPoint locationPoint = new LocationPoint
                            {
                                Timestamp = DateTime.Now,
                                Latitude = location.latitude,
                                Longitude = location.longitude
                            };

                            // Add location point to history or do something with it
                            locationhistory.Add(locationPoint);
                        }
                        else
                        {
                            Debug.LogWarning("Unable to determine location.");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("Location services not running.");
                    }
                }

                private List<LocationPoint> locationhistory;

                public struct LocationPoint
                {
                    public DateTime Timestamp;
                    public double Latitude;
                    public double Longitude;

                    public LocationPoint(DateTime timestamp, double latitude, double longitude)
                    {
                        Timestamp = timestamp;
                        Latitude = latitude;
                        Longitude = longitude;
                    }
                }


                public RelativePoint currentrelativelocation;

                public struct RelativePoint
                {
                    public float latmin;
                    public float latmax;
                    public float longmin;
                    public float longmax;

                    public RelativePoint(float latmin, float latmax, float longmin, float longmax)
                    {
                        this.latmin = latmin;
                        this.latmax = latmax;
                        this.longmin = longmin;
                        this.longmax = longmax;
                    }
                }


                private List<RelativePoint> relativelocationhistory;


                private void GetRelativeCoordinates()
                {
                    try
                    {

                        RelativePoint relativePoint = new RelativePoint();

                        for (int i = 0; i < 5; i++)
                        {
                            relativePoint.latmin += UnityEngine.Random.Range(-0.03f, 0.03f) + latitude;
                        }

                        relativePoint.latmax = UnityEngine.Random.Range(latitude - 0.15f, latitude + 0.15f);

                        for (int i = 0; i < 5; i++)
                        {
                            relativePoint.longmin += UnityEngine.Random.Range(-0.03f, 0.03f) + longitude;
                        }

                        relativePoint.longmax = UnityEngine.Random.Range(longitude - 0.15f, longitude + 0.15f);

                        relativelocationhistory.Add(relativePoint);
                        currentrelativelocation = relativePoint;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError("Error: " + ex.Message);
                    }
                }



            }



        }


        public class PowerOff()
        {

            public void ShutDown()
            {
                System.Diagnostics.Process.Start("shutdown.exe", "/s /t 0");
            }

            public void Sleep()
            {
                Application.SetSuspendState(PowerState.Suspend, true, true);

            }


        }



        public static class ThemeSystem
        {

            public record ThemeColors
            {
                public (string, string) BackgroundPrimary; //Image, color
                public (string, string) BackgroundSecondary;
                public (string, string) Surface;
                public (string, string) AccentPrimary;
                public (string, string) AccentSecondary;
                public string TextPrimary;
                public string TextMuted;
                public string Error;
                public string Warning;
                public string Success;

                public ThemeColors() { }

                public ThemeColors(
                    (string, string) backgroundPrimary,
                    (string, string) backgroundSecondary,
                    (string, string) surface,
                    (string, string) accentPrimary,
                    (string, string) accentSecondary,
                    string textPrimary,
                    string textMuted,
                    string error,
                    string warning,
                    string success)
                {
                    BackgroundPrimary = backgroundPrimary;
                    BackgroundSecondary = backgroundSecondary;
                    Surface = surface;
                    AccentPrimary = accentPrimary;
                    AccentSecondary = accentSecondary;
                    TextPrimary = textPrimary;
                    TextMuted = textMuted;
                    Error = error;
                    Warning = warning;
                    Success = success;
                }
            }

            public record ThemeTypography
            {
                public List<string> PrimaryFont; //h1 = h6, paragraph, caption
                public float FontScale;

                public ThemeTypography() { }

                public ThemeTypography(
                    List<string> primaryFont,
                    float fontScale)
                {
                    PrimaryFont = primaryFont;
                    FontScale = fontScale;
                }
            }

            public record ThemeSpatial
            {
                public float PanelThickness;
                public float CornerRadius;
                public float PanelCurvature;
                public string PhysicalityPreset;
                public bool EnableVolumetricShadows;

                public ThemeSpatial() { }

                public ThemeSpatial(
                    float panelThickness,
                    float cornerRadius,
                    float panelCurvature,
                    string physicalityPreset,
                    bool enableVolumetricShadows)
                {
                    PanelThickness = panelThickness;
                    CornerRadius = cornerRadius;
                    PanelCurvature = panelCurvature;
                    PhysicalityPreset = physicalityPreset;
                    EnableVolumetricShadows = enableVolumetricShadows;
                }
            }

            public sealed class UIAudioRoles
            {
                public string? Navigate;   // focus change, cursor move, gaze hop
                public string? Select;     // confirm / primary action
                public string? Back;       // cancel / escape
                public string? Error;      // invalid action
                public string? Warning;    // “hey idiot, careful”
                public string? Success;    // completion, save, done
                public string? Disabled;   // interaction blocked
                public string? Hover;      // subtle, optional

                // Parameterless constructor
                public UIAudioRoles() { }

                // Full constructor
                public UIAudioRoles(
                    string? navigate,
                    string? select,
                    string? back,
                    string? error,
                    string? warning,
                    string? success,
                    string? disabled,
                    string? hover)
                {
                    Navigate = navigate;
                    Select = select;
                    Back = back;
                    Error = error;
                    Warning = warning;
                    Success = success;
                    Disabled = disabled;
                    Hover = hover;
                }
            }

            public sealed class AppAudioRoles
            {
                public string? Launch;
                public string? Close;
                public string? Crash;       // yes, really
                public string? Background;
                public string? Foreground;

                // Parameterless constructor
                public AppAudioRoles() { }

                // Full constructor
                public AppAudioRoles(
                    string? launch,
                    string? close,
                    string? crash,
                    string? background,
                    string? foreground)
                {
                    Launch = launch;
                    Close = close;
                    Crash = crash;
                    Background = background;
                    Foreground = foreground;
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

            public record DefaultApp
            {
                public string AppID;            // Unique identifier
                public string Role;             // Launcher, Calendar, Browser, etc
                public int LaunchPriority;      // Lower = earlier
                public bool AutoStart;          // Should it start automatically
                public string? CustomPanel;     // Optional: panel ID if tied to a panel
                public List<DefaultAppImage> Images;
                public List<ThemeTypography> FontOverrides;
                public List<DefaultAppSound> SoundOverrides;


                public DefaultApp() { }

                public DefaultApp(string appID, string role, int launchPriority, bool autoStart,
                    List<DefaultAppImage> images, List<ThemeTypography> fontOverrides,
                    List<DefaultAppSound> soundOverrides, string? customPanel = null)
                {
                    AppID = appID;
                    Role = role;
                    LaunchPriority = launchPriority;
                    AutoStart = autoStart;
                    CustomPanel = customPanel;
                    Images = images;
                    FontOverrides = fontOverrides;
                    SoundOverrides = soundOverrides;
                }
            
            }

            public record DefaultAppSound
            {
                public string Role;          // Launcher, Calendar, Notification, etc
                public string Path;          // Path to sound file
                public float Volume;         // 0-1
                public float Pitch;          // Optional pitch modification
                public bool IsDefault;

                public DefaultAppSound() { }

                public DefaultAppSound(string role, string path, float volume = 1f, float pitch = 1f, bool isDefault = false)
                {
                    Role = role;
                    Path = path;
                    Volume = volume;
                    Pitch = pitch;
                    IsDefault = isDefault;
                }
            }

            public record DefaultAppImage
            {
                public string Role;          // Launcher, Calendar, Browser, etc
                public string Path;          // Path to image
                public int Width;            // Pixels
                public int Height;           // Pixels
                public float AspectRatio;    // Width / Height
                public bool IsDefault;       // True = primary image for this role

                public DefaultAppImage() { }

                public DefaultAppImage(string role, string path, int width, int height, bool isDefault = false)
                {
                    Role = role;
                    Path = path;
                    Width = width;
                    Height = height;
                    AspectRatio = width / (float)height;
                    IsDefault = isDefault;
                }
            }


            public record XRUIOSTheme
            {
                public ThemeIdentity Identity;
                public ThemeColors Colors;
                public ThemeTypography Typography;
                public ThemeSpatial Spatial;
                public AppAudioRoles AppAudio;
                public UIAudioRoles Audio;
                public List<DefaultApp> Defaults;

                public XRUIOSTheme() { }

                public XRUIOSTheme(
                    ThemeIdentity identity,
                    ThemeColors colors,
                    ThemeTypography typography,
                    ThemeSpatial spatial,
                    AppAudioRoles appAudio,
                    UIAudioRoles audio,
                    List<DefaultApp> defaults)
                {
                    Identity = identity;
                    Colors = colors;
                    Typography = typography;
                    Spatial = spatial;
                    AppAudio = appAudio;
                    Audio = audio;
                    Defaults = defaults;
                }
            }


            internal static XRUIOSTheme CurrentTheme;
            //Keep in mind a XRUIOS theme might completely ignore the values from the themes or ignore them partly


            //C
            //Remember to create a folder with the same name in the directory containing all the assets!

            public static async Task SaveTheme(XRUIOSTheme theme)
            {
                var directoryPath = Path.Combine(DataPath, "Themes");
                var fileName = $"{theme.Identity.Name} v{theme.Identity.Version} by {theme.Identity.Author}, ID {theme.Identity.ThemeID}";

                var filePath = Path.Combine(DataPath, "Themes", fileName);

                if (File.Exists(filePath))
                {
                    throw new InvalidOperationException("This theme already exists; please change the name.");
                }

                var xruiosFile = await BinaryConverter.NCObjectToByteArrayAsync<XRUIOSTheme>(theme);

                await JSONDataHandler.CreateJsonFile(fileName, directoryPath, new JsonObject());

                var json = await JSONDataHandler.LoadJsonFile(directoryPath, fileName);
                json = await JSONDataHandler.AddToJson<byte[]>(json, "Data", xruiosFile, encryptionKey);
                await JSONDataHandler.SaveJson(json);
            }

            //R
            public static async Task<List<XRUIOSTheme>> GetAllXRUIOSThemes()
            {
                List<XRUIOSTheme> Themes = new List<XRUIOSTheme>();

                var directoryPath = Path.Combine(DataPath, "Themes");

                var themePaths = Directory.EnumerateFiles(directoryPath);

                foreach (var item in themePaths)
                {
                    var json = await JSONDataHandler.LoadJsonFile(directoryPath, (Path.GetFileNameWithoutExtension(item)));

                    var bytes = (byte[])await JSONDataHandler.GetVariable<byte[]>(json, "Data", encryptionKey);

                    var themeFile = (XRUIOSTheme)await BinaryConverter.NCByteArrayToObjectAsync<XRUIOSTheme>(bytes);

                    Themes.Add(themeFile);

                }

                return Themes;
            }

            public static async Task<XRUIOSTheme> GetXRUIOSTheme(string FileName)
            {

                var directoryPath = Path.Combine(DataPath, "Themes");

                    var json = await JSONDataHandler.LoadJsonFile(directoryPath, FileName);

                    var bytes = (byte[])await JSONDataHandler.GetVariable<byte[]>(json, "Data", encryptionKey);

                    var themeFile = await BinaryConverter.NCByteArrayToObjectAsync<XRUIOSTheme>(bytes);

                return themeFile;
            }
            public static async Task<XRUIOSTheme> GetCurrentTheme(string FileName)
            {
                return CurrentTheme;

            }

            //U

            //Remember to put Identity.Version up
            public static async Task UpdateTheme(XRUIOSTheme theme)
            {
                var directoryPath = Path.Combine(DataPath, "Themes");
                var fileName = $"{theme.Identity.Name} v{theme.Identity.Version} by {theme.Identity.Author}, ID {theme.Identity.ThemeID}";

                var filePath = Path.Combine(DataPath, "Themes", fileName);

                if (!File.Exists(filePath))
                {
                    throw new InvalidOperationException("This theme does not exist.");
                }

                var xruiosFile = await BinaryConverter.NCObjectToByteArrayAsync<XRUIOSTheme>(theme);

                var json = await JSONDataHandler.LoadJsonFile(directoryPath, fileName);
                json = await JSONDataHandler.UpdateJson<byte[]>(json, "Data", xruiosFile, encryptionKey);
                await JSONDataHandler.SaveJson(json);
            }

            public static async Task SetTheme(string FileName)
            {
                CurrentTheme = await GetXRUIOSTheme(FileName);

            }


            //D
            public static async Task DeleteXRUIOSTheme(string FileName)
            {
                var directoryPath = Path.Combine(DataPath, "Themes");
                var filePath = Path.Combine(DataPath, "Themes", FileName);

                File.Delete(filePath);
            }


        }

        //Flavors

        public class Color
        {
            public int R = 0;
            public int G = 0;
            public int B = 0;
            public int A = 0;

            public Color(int r, int g, int b, int a)
            {
                this.R = r;
                this.G = g;
                this.B = b;
                this.A = a;
            }

            public static readonly Color Red = new Color(255, 0, 0, 255);
            public static readonly Color Green = new Color(0, 255, 0, 255);
            public static readonly Color Blue = new Color(0, 0, 255, 255);
            public static readonly Color White = new Color(255, 255, 255, 255);
            public static readonly Color Black = new Color(0, 0, 0, 255);
            public static readonly Color Yellow = new Color(255, 255, 0, 255);
            public static readonly Color Cyan = new Color(0, 255, 255, 255);
            public static readonly Color Magenta = new Color(255, 0, 255, 255);
            public static readonly Color Gray = new Color(128, 128, 128, 255);
            public static readonly Color DarkGray = new Color(64, 64, 64, 255);
            public static readonly Color LightGray = new Color(192, 192, 192, 255);
            public static readonly Color Orange = new Color(255, 165, 0, 255);
            public static readonly Color Pink = new Color(255, 192, 203, 255);
            public static readonly Color Purple = new Color(128, 0, 128, 255);
            public static readonly Color Brown = new Color(139, 69, 19, 255);
            public static readonly Color Maroon = new Color(128, 0, 0, 255);
            public static readonly Color Olive = new Color(128, 128, 0, 255);
            public static readonly Color Navy = new Color(0, 0, 128, 255);
            public static readonly Color Teal = new Color(0, 128, 128, 255);
            public static readonly Color Lime = new Color(0, 255, 0, 255);
            public static readonly Color Gold = new Color(255, 215, 0, 255);
            public static readonly Color Silver = new Color(192, 192, 192, 255);
            public static readonly Color Coral = new Color(255, 127, 80, 255);
            public static readonly Color Salmon = new Color(250, 128, 114, 255);
            public static readonly Color Indigo = new Color(75, 0, 130, 255);
            public static readonly Color Violet = new Color(238, 130, 238, 255);
            public static readonly Color SkyBlue = new Color(135, 206, 235, 255);
            public static readonly Color DeepSkyBlue = new Color(0, 191, 255, 255);
            public static readonly Color Transparent = new Color(0, 0, 0, 0);

            public static readonly Color CyberpunkYellow = new Color(255, 230, 0, 255);    // Neon yellow
            public static readonly Color JohnnySilver = new Color(192, 192, 192, 255);  // Metallic silver
            public static readonly Color VergilBlue = new Color(50, 90, 255, 255);    // Devil May Cry Vergil blue
            public static readonly Color GyroGreen = new Color(0, 200, 120, 255);    // JoJo Gyro green
            public static readonly Color ValentinePurple = new Color(150, 0, 200, 255);    // Rell Valentine purple
            public static readonly Color BondrewdBlack = new Color(20, 20, 30, 255);     // Deep black w/ blue tint
            public static readonly Color KingCrimson = new Color(220, 20, 60, 255);    // Crimson red
            public static readonly Color MayanoOrange = new Color(255, 140, 0, 255);    // Bright orange
            public static readonly Color KillerQueenPink = new Color(255, 105, 180, 255);  // Hot pink
            public static readonly Color RiasRed = new Color(200, 0, 50, 255);     // Rias Gremory red
            public static readonly Color GordonOrange = new Color(255, 100, 0, 255);    // Gordon Freeman orange
            public static readonly Color ZaWaurdoYellow = new Color(255, 255, 50, 255);   // Bright yellow
            public static readonly Color ChiefGreen = new Color(0, 100, 0, 255);      // Master Chief armor green
            public static readonly Color UndyneBlue = new Color(0, 90, 255, 255);     // Strong blue
            public static readonly Color SusiePurple = new Color(160, 50, 180, 255);   // Deep purple

            public static readonly Color KazumaBrown = new Color(139, 69, 19, 255);    // Dark brown
            public static readonly Color BatmanBlack = new Color(10, 10, 10, 255);     // Jet black
            public static readonly Color SaberYellow = new Color(255, 215, 0, 255);    // Golden yellow
            public static readonly Color ShiroOrange = new Color(255, 120, 40, 255);   // Warm orange
            public static readonly Color GokuBlack = new Color(60, 0, 60, 255);      // Dark purple-black

            public static readonly Color MakishimaWhite = new Color(240, 240, 240, 255);  // Soft white
            public static readonly Color UltraInstinctWhite = new Color(255, 255, 255, 255);  // Bright white
            public static readonly Color TheDrinkPurple = new Color(128, 0, 255, 255);    // Neon purple




        }












        //This will interface with C++ later, for now it's datatypes
        public class DesktopMirrors
        {
            public class Monitors //For uDesktopDuplication, specifically 
            {
                public record Monitor
                {

                    public int MonitorNumber;

                    public bool InvertedX;

                    public bool InvertedY;

                    public Monitor() { }

                    public Monitor(int monitornumber, bool invertedX, bool invertedY)
                    {
                        this.MonitorNumber = monitornumber;
                        this.InvertedX = invertedX;
                        this.InvertedY = invertedY;
                    }

                }

            }

            public class Apps //For uWindowsCapture, specifically Uwc Window Texture.cs
            {

                public enum RenderingMode { OnlyWhenVisible, AllFrames };
                public enum CapturePriority
                {
                    High = 0,
                    Middle = 1,
                    Low = 2,
                };

                public enum CaptureMode
                {
                    None = -1,
                    PrintWindow = 0,
                    BitBlt = 1,
                    NativeToOS = 2,
                    Auto = 3,
                };

                public record App
                {

                    public string AppPath;

                    public CaptureMode CaptureAPI;

                    public CapturePriority Priority;

                    public int CaptureFPS;

                    //We will draw the cursor whenever the user's raycast is on it

                    public RenderingMode RenderingModeType;

                    //Create Child Windows is always open

                    //We always do searches when the parameter changes

                    public string PartialAppTitle;

                    public App() { }

                    public App(string appPath, CaptureMode captureAPI, CapturePriority priority, int captureFPS, RenderingMode renderingModeType, string partialAppTitle)
                    {
                        this.AppPath = appPath;
                        this.CaptureAPI = captureAPI;
                        this.Priority = priority;
                        this.CaptureFPS = captureFPS;
                        this.RenderingModeType = renderingModeType;
                        this.PartialAppTitle = partialAppTitle;
                    }



                }



            }


        }

#if Test

        public class DataSlotClass
        {
            #region structs

            public record Session
            {
                public byte[] SavedSession; //The actual save data for our session, this can be a gameobj holding an entire scene or a single 3D object
                public string SaveSessionType; //Our type of Save Session, this is for organization and the user can create their own, in realtime we just check the parent object and decide accordingly!
                public bool IsFavorite; //If this is favorited
                public string DateAndTime; //The date and time
                public string Title; //Title
                public string Description; //Description

                public Session() { }


                public Session(byte[] SavedSession, string SaveSessionType, bool IsFavorite, string DateTimeVar,
                    string Title, string Description, string UserPassword)

                {
                    this.SavedSession = SavedSession;
                    this.SaveSessionType = SaveSessionType;
                    this.IsFavorite = IsFavorite;
                    var timenow = DateTime.Now;
                    string temptimenowasstring = default;

                    //If we are using DecryptSession we just give the value decrypted, however if this is a new one we will set the datetime
                    if (DateTimeVar == default)
                    {
                        temptimenowasstring = timenow.ToString("yyyy-MM-dd//THH:mm:ss//Z");
                    }
                    else
                    {
                        temptimenowasstring = DateTimeVar;
                    }
                    var timenowasstring = temptimenowasstring;
                    this.DateAndTime = timenowasstring;
                    this.Title = Title;
                    this.Description = Description;



                }
            }

            public struct DataSlot
            {
                public bool IsFavorite; //If this is favorited
                public string DateAndTime; //The date and time it was made
                public string Title; //Title
                public string Description; //Description
                public string ImgPath; //The path to the img icon
                public string TextureFolder; //2.5D images for previewing, for v2
                public List<WorldPoint> WorldPoints;

                public DataSlot(bool IsFavorite, string DateTimeVar,
                        string Title, string Description, string ImgPath, string UserPassword, List<string> ObjectsAndTransforms, string TextureFolder, Dictionary<string, Color> Categories,
                        List<WorldPoint> structWorldPoints)

                {
                    this.IsFavorite = IsFavorite;
                    var timenow = DateTime.Now;
                    string temptimenowasstring = default;

                    //If we are using DecryptSession we just give the value decrypted, however if this is a new one we will set the datetime
                    if (DateTimeVar == default)
                    {
                        temptimenowasstring = timenow.ToString("yyyy-MM-dd//THH:mm:ss//Z");
                    }
                    else
                    {
                        temptimenowasstring = DateTimeVar;
                    }
                    var timenowasstring = temptimenowasstring;
                    this.DateAndTime = timenowasstring;
                    this.Title = Title;
                    this.Description = Description;
                    this.ImgPath = ImgPath;




                    this.TextureFolder = default; //I'm a dumbass i'll make it later I hate how stupid I am I bet someone else could do this in one sitting, maybe 5 minutes

                    this.WorldPoints = structWorldPoints;


                }
            }

            public struct WorldPoint
            {
                public DesktopMirrors.Apps.RenderingMode RenderingMode;
                public object PointData; // Point data
                public string PointName;
                public string PointDescription;
                public string PointImagePath;
                public bool UserCentric; // Is this a point which is at a fixed point relative to the user
                public List<Objects.DecryptedStaticObject> StaticObjs;
                public List<Objects.App> AppObjs;
                public List<Objects.DesktopScreen> DesktopScreenObjs;
                public List<Objects.StaciaItems> StaciaObjs;

                public WorldPoint(DesktopMirrors.Apps.RenderingMode renderingMode, object pointData, string pointName, string pointDescription, string pointImagePath,
                    bool userCentric, List<Objects.DecryptedStaticObject> staticObjs, List<Objects.App> appObjs,
                    List<Objects.DesktopScreen> desktopScreenObjs, List<Objects.StaciaItems> staciaObjs)
                {
                    RenderingMode = renderingMode;
                    PointData = pointData;
                    PointName = pointName;
                    PointDescription = pointDescription;
                    PointImagePath = pointImagePath;
                    UserCentric = userCentric;
                    StaticObjs = staticObjs;
                    AppObjs = appObjs;
                    DesktopScreenObjs = desktopScreenObjs;
                    StaciaObjs = staciaObjs;
                }
            }

 
            #endregion



 

   
            public struct AppInfo
            {

                //I can make some upgrades

                public Apptype AppType;

                public string AppPath; //In the case of a base app, this is an EXE file or anything that can be ran. In the case of a XRUIOS app, this is a resource.
                public string AppName; //Taken from the name of the EXE or Resource

                public System.Drawing.Icon AppIcon; //Basically the icon set for the resource/EXE file.
                public Dictionary<string, GameObject> WidgetsPath; //A widget for this program. In the case of a XRUIOS app, this is not needed since it should be within the resources as a folder named "Widgets". However, this is an option for EXE apps. You can make a pipeline and showcase information.

                public Dictionary<String, GameObject> Pages; //What should be opened when a specific name type is called, can work for all XRUIOS apps and some EXE apps.

                public Dictionary<string, SceneData.DecryptedSession> SavedSessions;
                //Save session data for an app. Works with all XRUIOS only, in future will find a way to add for EXE files.

                public GameObject BackgroundProcess; //The singular file allowed to run at the back of a XRUIOS runtime. Can't do much about EXE files but the XRUIOS can be ran.

                //Also this file can not be tampered with. It is updated on the start of a user's runtime as well.

                public AppInfo(string AppPath, string AppName, Dictionary<string, GameObject> WidgetsPath, Dictionary<String, GameObject> Pages, Dictionary<string, SceneData.DecryptedSession> SavedSessions, GameObject BackgroundProcess, string UserPassword)
                {
                    //Let's start by setting our first variable, AKA our selected datatype
                    this.AppPath = AppPath;
                    var appextension = Path.GetExtension(AppPath);
                    Apptype temp = default;
                    if (appextension == ".exe")
                    {
                        temp = Apptype.BaseOS;
                    }

                    if (appextension == ".xur")
                    {
                        temp = Apptype.BaseOS;
                    }

                    this.AppType = temp;
                    //Let's make a few variable containers!
                    string tempappname = default;
                    System.Drawing.Icon tempappicon = default;
                    Dictionary<string, GameObject> tempwidgetspath = default;
                    Dictionary<string, GameObject> temppages = default;
                    Dictionary<string, SceneData.DecryptedSession> tempsavedsessions = default;
                    GameObject tempbackgroundprocess = default;






                    //Now we will create a switch, depending on our apptype
                    switch (AppType)
                    {
                        case Apptype.XRUIOS: //If this is a mod, this is the location of a resource
                                             //Get 
                            ModInfo xurapp = ModInfo.Load(AppName); //CHECK THIS LATER

                            //The app name
                            tempappname = xurapp.name;
                            //The resource icon, taken by System.Drawing
                            tempappicon = default; //Do this later
                                                   //The widgets path, you would still need to use the permissions system but you could pull data from an app if you know how to or if you saved it to a file or something
                            tempwidgetspath = WidgetsPath;
                            //Specific pages for a program, as an example the bookmrks or history page of OperaGX. We don't touch for now.
                            temppages = Pages;
                            //Saved Session Details, we don't touch this (yet) and for XRUIOS only
                            tempsavedsessions = SavedSessions;
                            //Background Process, for XRUIOS only. The singular BG process should be linked to this.
                            tempbackgroundprocess = BackgroundProcess;
                            break;



                        case Apptype.BaseOS: //If this is a XRUIOS app
                                             //The app name is the name of the EXE file
                            tempappname = Path.GetFileNameWithoutExtension(AppPath);

                            //The app icon, taken by System.Drawing
                            tempappicon = Icon.ExtractAssociatedIcon(AppPath);
                            //The widgets path, you would still need to use the permissions system but you could pull data from an app if you know how to or if you saved it to a file or something
                            tempwidgetspath = WidgetsPath;
                            //Specific pages for a program, as an example the bookmrks or history page of OperaGX. We don't touch for now.
                            temppages = Pages;
                            //Saved Session Details, we don't touch this (yet) and for XRUIOS only
                            tempsavedsessions = SavedSessions;
                            //Background Process, for XRUIOS only. The singular BG process should be linked to this.
                            tempbackgroundprocess = BackgroundProcess;

                            break;
                    }

                    this.AppName = tempappname; //Gets this automatically
                    this.AppIcon = tempappicon; //Gets this automatically
                    this.WidgetsPath = tempwidgetspath;
                    this.Pages = temppages;
                    this.SavedSessions = tempsavedsessions;
                    this.BackgroundProcess = tempbackgroundprocess;
                }
            }

            public class Objects //Exception to struct rule, this is a chunk of structs
            {
                public enum PositionalTrackingMode { Follow, Anchored, FollowingExternal }
                public enum RotationalTrackingMode { Static, LAM }
                public enum ObjectOSLabel { Default, Software, Objects, Voice, Music, Alerts, Ui, Other }

                //For referncing objects, they are imported using modtool but are at a different list. 

                //Naming = Name.AR/VR


                public struct DecryptedStaticObject
                {
                    public PositionalTrackingMode PTrackingType;
                    public RotationalTrackingMode RTrackingType;
                    public string Name; // Path to the object
                    public Vector3 SpatialData;
                    public ObjectOSLabel ObjectLabel;

                    public DecryptedStaticObject(PositionalTrackingMode pTrackingType, RotationalTrackingMode rTrackingType, string name, Vector3 spatialData, ObjectOSLabel objectLabel)
                    {
                        PTrackingType = pTrackingType;
                        RTrackingType = rTrackingType;
                        Name = name;
                        SpatialData = spatialData;
                        ObjectLabel = objectLabel;
                    }
                }

                public struct App
                {
                    public PositionalTrackingMode PTrackingType;
                    public RotationalTrackingMode RTrackingType;
                    public AppInfo WindowsAppInfo;
                    public DesktopMirrors.Apps.App MainAppData;
                    public Vector3 SpatialData;
                    public ObjectOSLabel ObjectLabel;

                    public App(PositionalTrackingMode pTrackingType, RotationalTrackingMode rTrackingType, AppInfo windowsAppInfo, DesktopMirrors.Apps.App mainAppData, Vector3 spatialData, ObjectOSLabel objectLabel)
                    {
                        PTrackingType = pTrackingType;
                        RTrackingType = rTrackingType;
                        WindowsAppInfo = windowsAppInfo;
                        MainAppData = mainAppData;
                        SpatialData = spatialData;
                        ObjectLabel = objectLabel;
                    }
                }

                public struct DesktopScreen
                {
                    public PositionalTrackingMode PTrackingType;
                    public RotationalTrackingMode RTrackingType;
                    public DesktopMirrors.Monitors.Monitor DesktopData;
                    public Vector3 SpatialData;
                    public ObjectOSLabel ObjectLabel;

                    public DesktopScreen(PositionalTrackingMode pTrackingType, RotationalTrackingMode rTrackingType, DesktopMirrors.Monitors.Monitor desktopData, Vector3 spatialData, ObjectOSLabel objectLabel)
                    {
                        PTrackingType = pTrackingType;
                        RTrackingType = rTrackingType;
                        DesktopData = desktopData;
                        SpatialData = spatialData;
                        ObjectLabel = objectLabel;
                    }
                }

                public struct StaciaItems
                {
                    public PositionalTrackingMode PTrackingType;
                    public RotationalTrackingMode RTrackingType;
                    public string BinaryData;
                    public Vector3 SpatialData;
                    public ObjectOSLabel ObjectLabel;

                    public StaciaItems(PositionalTrackingMode pTrackingType, RotationalTrackingMode rTrackingType, string binaryData, Vector3 spatialData, ObjectOSLabel objectLabel)
                    {
                        PTrackingType = pTrackingType;
                        RTrackingType = rTrackingType;
                        BinaryData = binaryData;
                        SpatialData = spatialData;
                        ObjectLabel = objectLabel;
                    }
                }












            }


        }

#endif
        public class DateClass
        {


        }

        public class Apps
            {

            }



            public class BasicTimeClass
            {
                public DateTime GetCurrentTimeLocal()
                {
                    DateTime localNow = DateTime.Now;
                    return localNow;
                }

                public DateTime GetCurrentTimeUTC()
                {
                    DateTime localNow = DateTime.UtcNow;
                    return localNow;
                }

                public void SetCurrentTime()
                {
                }


            }


















            public async Task InitiateFileSync(List<string> SongDirectory)
            {
                //First, let's get 
            }


            public List<string> SongDirectories { get; set; }


             
   





            public async Task RemoveHashSetFromCollection(string filePath)
            {

                var PathTitle = Path.GetDirectoryName(filePath);

                string SongOverviewFile = Path.Combine(DataPath, "Music", "Overviews", PathTitle);
                string SongDetailedFile = Path.Combine(DataPath, "Music", "Details", PathTitle);
                string HashsetsFile = Path.Combine(DataPath, "Music", "Hashsets", PathTitle);

                Path.GetDirectoryName(SongOverviewFile);




            }

            public async Task AddHashSetToCollection(string filePath)
            {
                string SongOverviewFile = Path.Combine(DataPath, "Music", "Overviews");
                string SongDetailedFile = Path.Combine(DataPath, "Music", "Details");
                string HashsetsFile = Path.Combine(DataPath, "Music", "Hashsets");



            }

            public async Task UpdateCollection(string filePath)
            {
                string SongOverviewFile = Path.Combine(DataPath, "Music", "Overviews");
                string SongDetailedFile = Path.Combine(DataPath, "Music", "Details");
                string HashsetsFile = Path.Combine(DataPath, "Music", "Hashsets");



            }

            public async Task UpdateMedia(string SongPath)
            {
                string SongOverview = Path.Combine(DataPath, "Music", "Overviews");
                string SongDetailed = Path.Combine(DataPath, "Music", "Details");
                string Hashsets = Path.Combine(DataPath, "Music", "Hashsets");
                string Images = Path.Combine(DataPath, "Music", "Images");
            }


            public async Task Update(List<string> SongDirectory)
            {

                foreach (var SongLibrary in SongDirectory)

                {

                    string SongOverview = Path.Combine(DataPath, "Music", "Overviews");
                    string SongDetailed = Path.Combine(DataPath, "Music", "Details");
                    string Hashsets = Path.Combine(DataPath, "Music", "Hashsets");
                    string Images = Path.Combine(DataPath, "Music", "Images");




                }


            }










        }





    }


//From https://gist.github.com/bugshake/d4a6ea578f2f96f07c5c6c2d701141e6
// get an event when a property changes
public class ObservableProperty<T>
{
    T value;

    public delegate void ChangeEvent(T data);
    public event ChangeEvent changed;

    public ObservableProperty(T initialValue)
    {
        value = initialValue;
    }

    public void Set(T v)
    {
        if (!v.Equals(value))
        {
            value = v;
            if (changed != null)
            {
                changed(value);
            }
        }
    }

    public T Get()
    {
        return value;
    }

    public static implicit operator T(ObservableProperty<T> p)
    {
        return p.value;
    }
}
