using EclipseProject;
using System.Reflection;

public class XRUIOS_Bridge
{
    public static void Initialize()
    {
        EclipseServer.RunServer(Assembly.GetExecutingAssembly());
        Console.WriteLine("[ECLIPSE] Server started.");
    }
}