using System; using UnityEngine; using PixelShooting.Core.Conveyor; using PixelShooting.Core.Queue; using PixelShooting.Core.Levels;
static class ConveyorTests { static int fails=0; static void Check(bool c,string m){Console.WriteLine($"[{(c?"PASS":"FAIL")}] {m}"); if(!c)fails++;}
  static bool At(Vector2 v,float x,float y)=>Math.Abs(v.x-x)<1e-4f && Math.Abs(v.y-y)<1e-4f;
  public static int Run(){
    // 10 wide, 4 tall: left=-5 right=5 bottom=0 top=4 -> length 28
    var p=new ConveyorPath(-5,5,0,4);
    Check(Math.Abs(p.Length-28)<1e-4f,"length = perimeter (28)");
    Check(At(p.Evaluate(0),-5,0),"start = bottom-left");
    Check(At(p.Evaluate(3),-2,0),"bottom side goes right");
    Check(At(p.Evaluate(10),5,0),"bottom-right corner");
    Check(At(p.Evaluate(12),5,2),"right side goes up");
    Check(At(p.Evaluate(14),5,4),"top-right corner");
    Check(At(p.Evaluate(20),-1,4),"top side goes left");
    Check(At(p.Evaluate(24),-5,4),"top-left corner");
    Check(At(p.Evaluate(26),-5,2),"left side goes down");
    Check(At(p.Evaluate(28),-5,0),"full lap = back at start");
    Check(At(p.Evaluate(30),-5,0) && At(p.Evaluate(-1),-5,0),"out of range clamps to start");

    // ---- ConveyorModel: path length 28, speed 1, minSpacing 2, hopTime 0.5 -> approach 0.5 ----
    var m=new ConveyorModel(p,28f,1f,2f,0.5f); var ex=new Shooter[ConveyorModel.Capacity];
    Shooter S()=>new Shooter(new ShooterSpec(0,1));
    Check(m.Free==5 && !m.CanEnter,"starts with 5 free, nothing to enter");
    Check(Math.Abs(m.HopDuration-0.5f)<1e-5f,"hop duration = approach / speed");
    bool ok=true; for(int i=0;i<5;i++) ok&=m.TryReserve();
    Check(ok && m.Free==0 && !m.TryReserve() && m.Pending==5,"5 reserves succeed, 6th fails, all pending");
    var a=S(); var b=S();
    Check(m.CanEnter,"gate open on an empty conveyor");
    m.Enter(a); Check(m.RiderCount==1 && m.GetRider(0).Distance==-0.5f && m.Pending==4,"enter starts at -approach (hop is part of the ride)");
    Check(!m.CanEnter,"gate closed right after an entry");
    bool threw=false; try{ m.Enter(b);}catch(InvalidOperationException){threw=true;}
    Check(threw && m.RiderCount==1,"Enter while closed throws");
    m.Update(1f,ex); Check(!m.CanEnter,"gate closed while gap < minSpacing");
    m.Update(1f,ex); Check(m.CanEnter,"gate opens when gap reaches minSpacing");
    m.Enter(b); Check(m.GetRider(1).Shooter==b,"FIFO order");
    int n=0; for(int i=0;i<26;i++) n+=m.Update(1f,ex);
    Check(n==0 && m.GetRider(0).Distance==27.5f,"no exit before Length");
    n=m.Update(1f,ex); Check(n==1 && ex[0]==a && m.RiderCount==1 && m.Free==1,"exit at Length frees a place");
    m.Reset(); Check(m.Free==5 && m.RiderCount==0 && m.Pending==0,"Reset clears everything");

    // Behind check: a rider finishing its lap must not meet a new rider at the start corner.
    m.TryReserve(); m.TryReserve(); m.Enter(a);
    m.Update(25.9f,ex); // a at 25.4: 28 - 25.4 - 0.5 = 2.1 >= 2
    Check(m.CanEnter,"behind gap 2.1: open");
    m.Update(0.2f,ex);  // a at 25.6: gap 1.9
    Check(!m.CanEnter,"behind gap 1.9: closed (would meet at the corner)");
    m.Update(2.5f,ex);  // a exits
    Check(m.RiderCount==0 && m.CanEnter,"open again once the old rider has left");

    // Shortened lap: exits at LapLength (25), not at the end of the path (28)
    var ms=new ConveyorModel(p,25f,1f,2f,0.5f); ms.TryReserve(); ms.Enter(a);
    n=ms.Update(25.4f,ex); Check(n==0,"short lap: no exit before LapLength");
    n=ms.Update(0.2f,ex); Check(n==1 && ms.Free==5,"short lap: exits at LapLength");
    Check(new ConveyorModel(p,0f,1f,2f,0.5f).LapLength==28f && new ConveyorModel(p,99f,1f,2f,0.5f).LapLength==28f,"invalid LapLength falls back to path length");

    // Random sim: every gap (incl. wrap-around) >= minSpacing, capacity respected, 0 alloc
    m.Reset(); var rng=new Random(7); int exitedTotal=0; bool gapsOk=true, countOk=true;
    var pool=new Shooter[64]; for(int i=0;i<pool.Length;i++) pool[i]=S(); int next=0;
    _=m.CanEnter; m.Update(0f,ex); long a0=GC.GetAllocatedBytesForCurrentThread();
    for(int step=0;step<20000;step++){
      if(rng.Next(3)==0) m.TryReserve();
      if(m.CanEnter) m.Enter(pool[next++%pool.Length]);
      exitedTotal+=m.Update((float)rng.NextDouble()*0.5f,ex);
      int c=m.RiderCount;
      for(int i=1;i<c;i++) if(m.GetRider(i-1).Distance-m.GetRider(i).Distance<2f-1e-3f) gapsOk=false;
      if(c>=2 && 28f-m.GetRider(0).Distance+m.GetRider(c-1).Distance<2f-1e-3f) gapsOk=false;
      if(c>5 || m.Free<0 || m.Pending<0) countOk=false;
    }
    long a1=GC.GetAllocatedBytesForCurrentThread();
    Check(gapsOk,"random sim: all gaps incl. wrap-around >= minSpacing");
    Check(countOk && exitedTotal>100,$"random sim: capacity respected ({exitedTotal} laps)");
    Check(a1-a0==0,$"TryReserve/CanEnter/Enter/Update allocate 0 bytes ({a1-a0})");
    Console.WriteLine(fails==0?"ALL PASSED":$"{fails} FAILED"); return fails; } }
