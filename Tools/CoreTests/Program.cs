using System;
using System.IO;

// Runs the Core (pure C#) tests outside Unity: `dotnet run` from this folder.
// UnityShim.cs stands in for the few UnityEngine APIs the Core code uses.
static class Paths
{
    // bin/Debug/net10.0 -> CoreTests -> Tools -> project root -> Assets/_Project/Levels/
    public static readonly string Levels =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../Assets/_Project/Levels/"))
            .Replace(Path.DirectorySeparatorChar, '/');
}

static class Program
{
    static int Main()
    {
        int fails = 0;
        Console.WriteLine("== Level loading/validation =="); fails += LevelTests.Run();
        Console.WriteLine("== Board model ==");            fails += BoardTests.Run();
        Console.WriteLine("== Queue model ==");            fails += QueueTests.Run();
        Console.WriteLine("== Conveyor ==");               fails += ConveyorTests.Run();
        Console.WriteLine("== Shooting ==");               fails += ShootingTests.Run();
        Console.WriteLine("== Holder ==");                 fails += HolderTests.Run();
        Console.WriteLine(fails == 0 ? "\nALL SUITES PASSED" : $"\n{fails} FAILURES");
        return fails == 0 ? 0 : 1;
    }
}
