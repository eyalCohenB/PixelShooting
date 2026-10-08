using System; using System.Collections.Generic; using System.IO;
using PixelShooting.Core.Board; using PixelShooting.Core.Conveyor; using PixelShooting.Core.Levels;
using PixelShooting.Core.Queue; using PixelShooting.Core.Shooting;
static class ShootingTests { static int fails=0; static void Check(bool c,string m){Console.WriteLine($"[{(c?"PASS":"FAIL")}] {m}"); if(!c)fails++;}
  static LevelData Load(string json){ var e=new List<string>(); if(!LevelLoader.TryLoad(json,"t",out var l,e,new List<string>())) throw new Exception(string.Join(";",e)); return l; }
  const string Red2x2="{\"width\":2,\"height\":2,\"palette\":[\"#FF0000\"],\"cells\":[0,0,0,0],\"queues\":[{\"shooters\":[{\"color\":0,\"bullets\":4}]}]}";

  // path (-5,5,0,4): length 28. 2x2 board, cell 1, centres x -0.5/0.5, y 1.5/2.5.
  class Rig { public ConveyorPath P=new ConveyorPath(-5,5,0,4); public BoardModel B=new BoardModel(); public FiringPoints F=new FiringPoints();
    public ConveyorModel C; public ShootingSystem S; public QueueModel Q=new QueueModel(); public Shot[] Shots=new Shot[16]; public Shooter[] Ex=new Shooter[5];
    public Rig(LevelData l,float lap,float firstX=-0.5f,float firstY=1.5f){ B.Reset(l); Q.Reset(l); F.Build(P,firstX,firstY,1f,l.Width,l.Height);
      C=new ConveyorModel(P,lap,1f,2f,0.5f); S=new ShootingSystem(C,B,F); }
    public Shooter Ride(int column){ C.TryReserve(); var s=Q.TakeTop(column); C.Enter(s); return s; }
    public int Step(float dt,List<Shot> log,out int exits){ C.Advance(dt); int n=S.Fire(Shots); for(int i=0;i<n;i++) log?.Add(Shots[i]); exits=C.CollectExits(Ex); return n; } }

  public static int Run(){
    var r=new Rig(Load(Red2x2),28f);
    float[] want={4.5f,5.5f,11.5f,12.5f,18.5f,19.5f,25.5f,26.5f};
    bool ok=r.F.Count==8; for(int i=0;i<8&&ok;i++) ok=Math.Abs(r.F[i].Distance-want[i])<1e-4f;
    Check(ok,"firing points: one per line per side, at the cell centres, sorted");
    Check(r.F[0].Side==BoardSide.Bottom&&r.F[0].Line==0 && r.F[2].Side==BoardSide.Right&&r.F[2].Line==0
       && r.F[4].Side==BoardSide.Top&&r.F[4].Line==1 && r.F[6].Side==BoardSide.Left&&r.F[6].Line==1,"firing points: sides and line order follow the CCW lap");

    // Full clear: shots at (0,0),(1,0) from the bottom, (1,1) from the right, (0,1) from the top.
    var log=new List<Shot>(); var s=r.Ride(0); int exits=0, totalExits=0;
    for(int k=0;k<400 && r.C.RiderCount>0;k++){ r.Step(0.1f,log,out exits); totalExits+=exits; }
    Check(log.Count==4 && log[0].X==0&&log[0].Y==0 && log[1].X==1&&log[1].Y==0 && log[2].X==1&&log[2].Y==1 && log[3].X==0&&log[3].Y==1,"shots hit the exposed cells in lap order");
    Check(Math.Abs(log[0].Distance-4.5f)<1e-4f,"shot remembers where it was fired");
    Check(log[3].ShooterEmptied && !log[2].ShooterEmptied && s.BulletsLeft==0 && s.Location==ShooterLocation.Destroyed,"last bullet marks the shooter destroyed");
    Check(r.C.RiderCount==0 && r.C.Free==5 && totalExits==0,"out of bullets: removed at once (no lap exit), place freed");
    Check(r.B.IsCleared,"board cleared");

    // Blocked: 1 wide, 2 tall; top red, bottom blue. Red shooter is blocked from below, fires from the right at row 1.
    var lb=Load("{\"width\":1,\"height\":2,\"palette\":[\"#FF0000\",\"#0000FF\"],\"cells\":[0,1],\"queues\":[{\"shooters\":[{\"color\":0,\"bullets\":1},{\"color\":1,\"bullets\":1}]}]}");
    r=new Rig(lb,28f,0f,1.5f); log.Clear(); r.Ride(0);
    for(int k=0;k<140;k++) r.Step(0.1f,log,out _);
    Check(log.Count==1 && log[0].X==0 && log[0].Y==1 && log[0].Distance>10f,"blocked by another color from below; fires at the match from the right");

    // Lap ends at 19 (before the top-left column point at 19.5): 3 shots, then exits with 1 bullet.
    r=new Rig(Load(Red2x2),19f); log.Clear(); s=r.Ride(0); totalExits=0;
    for(int k=0;k<400 && r.C.RiderCount>0;k++){ r.Step(0.1f,log,out exits); totalExits+=exits; }
    Check(log.Count==3 && totalExits==1 && s.BulletsLeft==1 && r.B.RemainingCells==1,"points past LapLength never fire; shooter exits with bullets left");

    // Small buffer: one big step crosses 2 points; buffer of 1 -> 1 shot now, the other next frame.
    r=new Rig(Load(Red2x2),28f); r.Shots=new Shot[1]; r.Ride(0);
    int n1=r.Step(6.5f,null,out _); int n2=r.Step(0f,null,out _);
    Check(n1==1 && n2==1,"full shot buffer: remaining points fire next frame");

    // Level03 simulation: shooters re-enter after their lap (stand-in for the holder).
    // Invariant every frame: bullets left on all shooters == cells left on the board.
    var l3=Load(File.ReadAllText(Paths.Levels+"Level03.json"));
    var path=new ConveyorPath(-6,6,0,12); var B=new BoardModel(); B.Reset(l3); var F=new FiringPoints(); F.Build(path,-4.5f,1.5f,1f,l3.Width,l3.Height);
    var C=new ConveyorModel(path,path.Length-1.5f,4f,1.2f,0.4f); var S=new ShootingSystem(C,B,F); var Q=new QueueModel(); Q.Reset(l3);
    var all=new List<Shooter>(); var waiting=new Queue<Shooter>(); var shots=new Shot[64]; var ex=new Shooter[5];
    for(int c=0;c<Q.ColumnCount;c++) while(!Q.IsEmpty(c)){ var sh=Q.TakeTop(c); all.Add(sh); waiting.Enqueue(sh); }
    bool inv=true; int frames=0;
    var waitArr=waiting.ToArray(); int head=0, tail=waitArr.Length; var ring=new Shooter[all.Count+5]; Array.Copy(waitArr,ring,waitArr.Length);
    S.Fire(shots); C.Advance(0); long a0=GC.GetAllocatedBytesForCurrentThread();
    for(frames=0;frames<200000 && !B.IsCleared;frames++){
      if(head!=tail && C.Pending==0) C.TryReserve();           // like a tap: reserve, then wait in place
      if(C.CanEnter){ C.Enter(ring[head]); head=(head+1)%ring.Length; }
      C.Advance(1f/60f); S.Fire(shots); int e=C.CollectExits(ex); for(int i=0;i<e;i++){ ring[tail]=ex[i]; tail=(tail+1)%ring.Length; }
      int bl=0; for(int i=0;i<all.Count;i++) bl+=all[i].BulletsLeft; if(bl!=B.RemainingCells) inv=false;
    }
    long a1=GC.GetAllocatedBytesForCurrentThread();
    Check(inv,"Level03 sim: bullets left == cells left, every frame");
    Check(B.IsCleared,$"Level03 sim: board cleared ({frames} frames)");
    Check(a1-a0==0,$"Advance/Fire/CollectExits allocate 0 bytes ({a1-a0})");
    Console.WriteLine(fails==0?"ALL PASSED":$"{fails} FAILED"); return fails; } }
