using System; using System.Collections.Generic; using System.IO; using PixelShooting.Core.Levels;
static class LevelTests { static int fails=0;
  static void Run(string name,string json,bool expectOk,string expectText=null){
    var e=new List<string>(); var w=new List<string>();
    bool ok=LevelLoader.TryLoad(json,name,out var lvl,e,w);
    bool pass= ok==expectOk && (expectText==null || string.Join("|",e).Contains(expectText) || string.Join("|",w).Contains(expectText));
    if(!pass) fails++;
    Console.WriteLine($"[{(pass?"PASS":"FAIL")}] {name}: ok={ok}");
    foreach(var x in e) Console.WriteLine("    error: "+x); foreach(var x in w) Console.WriteLine("    warn:  "+x);
    if(ok){ Console.WriteLine($"    {lvl.Width}x{lvl.Height}, {lvl.PaletteCount} colors, {lvl.QueueCount} queues");
      for(int y=lvl.Height-1;y>=0;y--){ var r="    "; for(int x=0;x<lvl.Width;x++){var c=lvl.GetCell(x,y); r+=c<0?" .":" "+c;} Console.WriteLine(r);} } }
  public static int Run(){
    string dir=Paths.Levels;
    Run("Level01",File.ReadAllText(dir+"Level01.json"),true);
    Run("Level02",File.ReadAllText(dir+"Level02.json"),true);
    Run("Level03",File.ReadAllText(dir+"Level03.json"),true);
    Run("Invalid_BulletMismatch",File.ReadAllText(dir+"Invalid_BulletMismatch.json"),false,"color 0 has 8 cells but 7 bullets");
    // Extra edge cases
    Run("empty","",false,"empty");
    Run("brokenJson","{ \"width\": 2, ",false,"invalid JSON");
    Run("noCells","{\"width\":2,\"height\":1,\"palette\":[\"#FF0000\"],\"queues\":[{\"shooters\":[{\"color\":0,\"bullets\":2}]}]}",false,"expected 2 cells");
    Run("badHex","{\"width\":1,\"height\":1,\"palette\":[\"red\"],\"cells\":[0],\"queues\":[{\"shooters\":[{\"color\":0,\"bullets\":1}]}]}",false,"not a valid hex");
    Run("badCell","{\"width\":2,\"height\":1,\"palette\":[\"#FF0000\"],\"cells\":[0,5],\"queues\":[{\"shooters\":[{\"color\":0,\"bullets\":1}]}]}",false,"invalid color 5");
    Run("noQueues","{\"width\":1,\"height\":1,\"palette\":[\"#FF0000\"],\"cells\":[0]}",false,"at least one queue");
    Run("zeroBullets","{\"width\":1,\"height\":1,\"palette\":[\"#FF0000\"],\"cells\":[0],\"queues\":[{\"shooters\":[{\"color\":0,\"bullets\":0},{\"color\":0,\"bullets\":1}]}]}",false,"at least 1 bullet");
    Run("emptyQueueWarn","{\"width\":1,\"height\":1,\"palette\":[\"#FF0000\",\"#00FF00\"],\"cells\":[0],\"queues\":[{\"shooters\":[{\"color\":0,\"bullets\":1}]},{\"shooters\":[]}]}",true,"queue 1 has no shooters");
    Run("allEmptyCells","{\"width\":2,\"height\":1,\"palette\":[\"#FF0000\"],\"cells\":[-1,-1],\"queues\":[{\"shooters\":[{\"color\":0,\"bullets\":1}]}]}",false,"0 cells but 1 bullets");
    var pre=new List<string>{"old error"}; bool r=LevelLoader.TryLoad(File.ReadAllText(dir+"Level01.json"),"preexisting",out _,pre,new List<string>());
    Console.WriteLine($"[{(r?"PASS":"FAIL")}] pre-existing errors in list don't fail a valid level"); if(!r) fails++;
    Console.WriteLine(fails==0?"ALL PASSED":$"{fails} FAILED"); return fails; } }
