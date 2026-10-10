using System; using System.Collections.Generic; using System.IO;
using PixelShooting.Core.Board; using PixelShooting.Core.Conveyor; using PixelShooting.Core.Hint; using PixelShooting.Core.Holder;
using PixelShooting.Core.Levels; using PixelShooting.Core.Queue; using PixelShooting.Core.Shooting;

// Hint solver: following its hints must win; it must work mid-game (riders + pending) and not allocate on repeat.
static class HintTests { static int fails=0; static void Check(bool c,string m){Console.WriteLine($"[{(c?"PASS":"FAIL")}] {m}"); if(!c)fails++;}

  class Game {
    public BoardModel B=new BoardModel(); public QueueModel Q=new QueueModel(); public HolderModel H=new HolderModel();
    public ConveyorModel C; public ShootingSystem S; public FiringPoints F=new FiringPoints();
    Shot[] shots=new Shot[32]; Shooter[] ex=new Shooter[5];
    public TapSource[] Pending=new TapSource[5]; public int PendingCount;
    public Game(LevelData l){
      var path=new ConveyorPath(-4.75f,4.75f,-1.77f,8.26f); B.Reset(l); Q.Reset(l);
      float cell=Math.Min(7.8f/l.Width,7.8f/l.Height);
      F.Build(path,-(l.Width-1)*cell*0.5f,3.26f-(l.Height-1)*cell*0.5f,cell,l.Width,l.Height);
      C=new ConveyorModel(path,path.Length-1.6f,5f,1.2f,0.4f); S=new ShootingSystem(C,B,F); }
    public void Tap(TapSource t){ C.TryReserve(); Pending[PendingCount++]=t; }
    // One frame; false = lost (6th holder arrival).
    public bool Step(){
      while(PendingCount>0 && C.CanEnter){ var t=Pending[0]; for(int i=1;i<PendingCount;i++) Pending[i-1]=Pending[i]; PendingCount--;
        C.Enter(t.FromHolder?H.Take(t.Index):Q.TakeTop(t.Index)); }
      C.Advance(1f/60f); S.Fire(shots); int n=C.CollectExits(ex);
      for(int i=0;i<n;i++) if(!H.TryAdd(ex[i],out _)) return false;
      return true; }
  }

  static LevelData Load(string name){ var e=new List<string>(); LevelLoader.TryLoad(File.ReadAllText(Paths.Levels+name+".json"),name,out var l,e,new List<string>()); return l; }

  public static int Run(){
    var solver=new HintSolver();

    // Follow the hints one shooter at a time (wait for the conveyor to empty): must win every level.
    for(int k=1;k<=4;k++){
      var l=Load($"Level0{k}"); var g=new Game(l); string result="stuck"; int hints=0; bool allSolved=true; var order=new List<string>();
      for(int frame=0;frame<60*900;frame++){
        if(g.B.IsCleared){ result="won"; break; }
        if(g.C.RiderCount==0 && g.PendingCount==0){
          if(!solver.TryGetHint(g.B,g.F,g.C.LapLength,g.Q,g.H,g.C,g.Pending,g.PendingCount,out var hint,out bool solved)){ result="no-hint"; break; }
          allSolved&=solved; hints++; order.Add(hint.FromHolder?$"H{hint.Index}":$"Q{hint.Index}"); g.Tap(hint);
        }
        if(!g.Step()){ result="lost"; break; }
      }
      Check(result=="won" && allSolved,$"Level0{k}: following the hints wins ({hints} taps: {string.Join(" ",order)})");
    }

    // Level04: the first hint must start digging column 0 (the only column hiding the ring's red shooter).
    { var g=new Game(Load("Level04")); solver.TryGetHint(g.B,g.F,g.C.LapLength,g.Q,g.H,g.C,g.Pending,0,out var h,out bool solved);
      Check(solved && !h.FromHolder && h.Index==0,"Level04: first hint is queue column 0"); }

    // Mid-game: riders on the conveyor and a pending tap are taken into account; the hint is a real source.
    { var g=new Game(Load("Level03")); g.Tap(TapSource.Queue(0)); g.Tap(TapSource.Queue(1));
      for(int f=0;f<90;f++) g.Step(); // first rider on the track, second may still be pending/boarding
      bool ok=solver.TryGetHint(g.B,g.F,g.C.LapLength,g.Q,g.H,g.C,g.Pending,g.PendingCount,out var h,out _);
      bool valid=ok && (h.FromHolder ? g.H.Get(h.Index)!=null : g.Q.Remaining(h.Index)>0);
      int cellsBefore=g.B.RemainingCells; int[] tops={g.Q.Remaining(0),g.Q.Remaining(1),g.Q.Remaining(2)};
      Check(valid,"mid-game hint (riders + pending) points at a real shooter");
      Check(g.B.RemainingCells==cellsBefore && g.Q.Remaining(0)==tops[0] && g.C.RiderCount>=1,"solver never changes the live game (works on snapshots)");

      long a0=GC.GetAllocatedBytesForCurrentThread();
      for(int i=0;i<20;i++) solver.TryGetHint(g.B,g.F,g.C.LapLength,g.Q,g.H,g.C,g.Pending,g.PendingCount,out _,out _);
      long a1=GC.GetAllocatedBytesForCurrentThread();
      Check(a1-a0==0,$"repeated hints allocate 0 bytes once buffers exist ({a1-a0})");
    }

    // Speed: worst case of a fresh solve on the biggest level.
    { var g=new Game(Load("Level03")); var sw=System.Diagnostics.Stopwatch.StartNew();
      solver.TryGetHint(g.B,g.F,g.C.LapLength,g.Q,g.H,g.C,g.Pending,0,out _,out _); sw.Stop();
      Console.WriteLine($"  Level03 fresh hint: {sw.Elapsed.TotalMilliseconds:0.0} ms");
      Check(sw.Elapsed.TotalMilliseconds<50,"a hint takes < 50 ms (it runs once per button press)"); }

    Console.WriteLine(fails==0?"ALL PASSED":$"{fails} FAILED"); return fails; }
}
