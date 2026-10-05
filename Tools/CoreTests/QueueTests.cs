using System; using System.Collections.Generic; using System.IO; using PixelShooting.Core.Levels; using PixelShooting.Core.Queue;
static class QueueTests { static int fails=0; static void Check(bool c,string m){Console.WriteLine($"[{(c?"PASS":"FAIL")}] {m}"); if(!c)fails++;}
  public static int Run(){ var e=new List<string>();
    LevelLoader.TryLoad(File.ReadAllText(Paths.Levels+"Level03.json"),"L3",out var l,e,new List<string>());
    var q=new QueueModel(); q.Reset(l);
    Check(q.ColumnCount==3,"3 columns");
    Check(q.Remaining(1)==4 && q.PeekTop(1).ColorIndex==3 && q.PeekTop(1).BulletsLeft==7,"col1 top = green x7, 4 shooters");
    var s=q.TakeTop(1); Check(s.Location==ShooterLocation.Conveyor && q.Remaining(1)==3 && q.PeekTop(1).ColorIndex==0,"take top: moved to conveyor, next (red) is top");
    Check(q.GetAt(1,0)==q.PeekTop(1) && q.GetAt(1,2)!=null && q.GetAt(1,3)==null,"GetAt counts from current top");
    q.TakeTop(1);q.TakeTop(1);q.TakeTop(1); Check(q.IsEmpty(1) && q.PeekTop(1)==null && q.TakeTop(1)==null,"empty column returns null");
    Check(q.Remaining(0)==5,"other columns untouched");
    q.Reset(l); Check(q.Remaining(1)==4 && q.PeekTop(1).Location==ShooterLocation.Queue,"reset restores fresh shooters");
    Console.WriteLine(fails==0?"ALL PASSED":$"{fails} FAILED"); return fails; } }
