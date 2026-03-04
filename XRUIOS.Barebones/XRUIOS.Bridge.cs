using EclipseProject;

public class XRUIOS_Bridge
{
    public static void Initialize()
    {
        EclipseServer.RunServer();
        Console.WriteLine("[ECLIPSE] Server started.");
    }
}