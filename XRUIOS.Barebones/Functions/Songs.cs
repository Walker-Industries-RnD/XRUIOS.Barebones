using ATL;
using Pariah_Cybersecurity;
using System.Text.Json.Nodes;
using static Pariah_Cybersecurity.DataHandler;
using static XRUIOS.Barebones.XRUIOS;

namespace XRUIOS.Barebones
{
    public class Songs
    {

        //Add music playlist support later










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

                // Optional: force full re-parse from audio file (tags changed externally)
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

        public class Playlists()
        {

            //We simply use ATLDOTNET for this and metadata for songs, i'm too lazy to do it any other way

            //C



            //R


            //U


            //D


        }


    }


}
