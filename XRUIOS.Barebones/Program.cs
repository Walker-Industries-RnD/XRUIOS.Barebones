
using System;
using XRUIOS.Barebones;

class Program
{
    static void Main()
    {
        var testSong = "C:\\Users\\Me\\Music\\Little More (リトルモア)  Muv-Luv Extra Miki Tamase Ending Song.mp3";

        // Use the fully qualified type name
        var (overview, detailed) = XRUIOS.Barebones.XRUIOS.Songs.CreateSongInfo(testSong);

        // Example usage
        Console.WriteLine($"Title: {overview.SongName}");
        Console.WriteLine($"Artist: {overview.TrackArtist}");
        Console.WriteLine($"Album: {overview.AlbumName}");
        Console.WriteLine($"Duration: {overview.Duration}");
        Console.WriteLine($"Genre: {detailed.Genre}");
    }
}
