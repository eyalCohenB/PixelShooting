using System; using System.Collections.Generic; using System.IO;
using PixelShooting.Core.Board; using PixelShooting.Core.Conveyor; using PixelShooting.Core.Holder;
using PixelShooting.Core.Levels; using PixelShooting.Core.Queue; using PixelShooting.Core.Shooting;

// Part 3 (loop finale): conveyor loop mode, and whole games where the finale must always win.
static class LoopTests { static int fails=0; static void Check(bool c,string m){Console.WriteLine($"[{(c?"PASS":"FAIL")}] {m}"); if(!c)fails++;}

  public static int Run(){
    // ---- ConveyorModel loop mode: path length 28, lap ends at 25, speed 1, spacing 2, hop 0.5 ----
    var p=new ConveyorPath(-5,5,0,4); var m=new ConveyorModel(p,25f,1f,2f,0.5f); var ex=new Shooter[5];
    var a=new Shooter(new ShooterSpec(0,3)); m.TryReserve(); m.Enter(a);
    m.StartLooping(2f);
    Check(m.Looping && Math.Abs(m.HopDuration-0.25f)<1e-5f && m.FireLimit==28f,"StartLooping: faster, shorter hop, fire limit = full path");
    m.Advance(13f); // -0.5 + 26 = 25.5: past the normal lap end
    Check(m.CollectExits(ex)==0 && m.RiderCount==1,"loop mode: no exit at LapLength");
    m.RiderRef(0).NextPoint=7; m.Advance(1.5f); // 28.5
    int n=m.CollectExits(ex);
    Check(n==0 && Math.Abs(m.GetRider(0).Distance-0.5f)<1e-4f && m.GetRider(0).NextPoint==0,"loop mode: wraps to a new lap, firing points start over");
    m.Reset(); Check(!m.Looping && Math.Abs(m.HopDuration-0.5f)<1e-5f,"Reset leaves loop mode and restores the base speed");

    // Random loop-mode sim: circular gaps (incl. across the wrap) always >= minSpacing, nobody exits.
    m=new ConveyorModel(p,25f,1f,2f,0.5f); m.StartLooping(3f); var rng=new Random(3); bool gapsOk=true, noExit=true;
    var pool=new Shooter[8]; for(int i=0;i<8;i++) pool[i]=new Shooter(new ShooterSpec(0,1)); int next=0;
    for(int step=0;step<20000;step++){
      if(rng.Next(3)==0) m.TryReserve();
      if(m.CanEnter) m.Enter(pool[next++%8]);
      m.Advance((float)rng.NextDouble()*0.3f); if(m.CollectExits(ex)!=0) noExit=false;
      for(int i=0;i<m.RiderCount;i++) for(int j=i+1;j<m.RiderCount;j++){
        float d=Math.Abs(m.GetRider(i).Distance-m.GetRider(j).Distance)%28f; d=Math.Min(d,28f-d); if(d<2f-1e-3f) gapsOk=false; }
    }
    Check(gapsOk,"loop mode random sim: every pair of riders >= minSpacing apart around the loop");
    Check(noExit && m.RiderCount==5,"loop mode random sim: nobody exits; the conveyor fills to 5");

    // ---- Whole games (scene geometry): random tapping until <= 5 shooters are alive, then the finale ----
    var list=new List<int>(); var r2=new Random(11);
    for(int k=1;k<=4;k++){
      var e=new List<string>(); LevelLoader.TryLoad(File.ReadAllText(Paths.Levels+$"Level0{k}.json"),"L",out var l,e,new List<string>());
      int finales=0, finaleWins=0, finaleLosses=0, finaleStuck=0, lostBefore=0, maxFinaleSeconds=0;
      for(int run=0;run<150;run++){
        var g=new Game(l); string res=g.Play(r2,list,out bool reached,out float finaleSeconds);
        if(!reached){ if(res=="lost") lostBefore++; continue; }
        finales++; if(res=="won") finaleWins++; else if(res=="lost") finaleLosses++; else finaleStuck++;
        maxFinaleSeconds=Math.Max(maxFinaleSeconds,(int)Math.Ceiling(finaleSeconds));
      }
      Console.WriteLine($"  Level0{k}: {finales}/150 reached the finale (lost before it: {lostBefore}); finale max {maxFinaleSeconds}s");
      Check(finaleWins==finales,$"Level0{k}: every finale wins ({finaleWins}/{finales}; lost {finaleLosses}, stuck {finaleStuck})");
    }
    Console.WriteLine(fails==0?"ALL PASSED":$"{fails} FAILED"); return fails; }

  // Mirrors GameController: pending FIFO (queue column or holder slot), holder returns, finale trigger.
  class Game {
    BoardModel B=new BoardModel(); QueueModel Q=new QueueModel(); HolderModel H=new HolderModel();
    ConveyorModel C; ShootingSystem S; Shot[] shots=new Shot[32]; Shooter[] ex=new Shooter[5];
    List<(bool holder,int index)> pending=new List<(bool,int)>(); int alive; bool armed, looping;
    public Game(LevelData l){
      var path=new ConveyorPath(-4.75f,4.75f,-1.77f,8.26f); B.Reset(l); Q.Reset(l);
      float cell=Math.Min(7.8f/l.Width,7.8f/l.Height);
      var F=new FiringPoints(); F.Build(path,-(l.Width-1)*cell*0.5f,3.26f-(l.Height-1)*cell*0.5f,cell,l.Width,l.Height);
      C=new ConveyorModel(path,path.Length-1.6f,5f,1.2f,0.4f); S=new ShootingSystem(C,B,F);
      for(int c=0;c<Q.ColumnCount;c++) alive+=Q.Remaining(c); armed=alive>5; }

    public string Play(Random rng,List<int> list,out bool reached,out float finaleSeconds){
      reached=false; finaleSeconds=0f; const float dt=1f/60f;
      for(int frame=0;frame<60*900;frame++){
        if(B.IsCleared) return "won";
        if(looping) finaleSeconds+=dt;
        // Random player: taps a random source when there's room (not during the finale).
        if(!looping && C.Free>0 && rng.Next(10)==0){
          list.Clear();
          for(int c=0;c<Q.ColumnCount;c++){ int queued=0; foreach(var t in pending) if(!t.holder&&t.index==c) queued++; if(Q.Remaining(c)>queued) list.Add(c); }
          for(int s=0;s<5;s++) if(H.Get(s)!=null && !pending.Contains((true,s))) list.Add(-1-s);
          if(list.Count>0){ int src=list[rng.Next(list.Count)]; C.TryReserve(); pending.Add(src>=0?(false,src):(true,-1-src)); }
        }
        while(pending.Count>0 && C.CanEnter){ var t=pending[0]; pending.RemoveAt(0); C.Enter(t.holder?H.Take(t.index):Q.TakeTop(t.index)); }
        C.Advance(dt); int n=S.Fire(shots); for(int i=0;i<n;i++) if(shots[i].ShooterEmptied) alive--;
        int e=C.CollectExits(ex); for(int i=0;i<e;i++) if(!H.TryAdd(ex[i],out _)) return "lost";
        if(armed && !looping && alive<=5){
          looping=true; reached=true; C.StartLooping(15f);
          for(int s=0;s<5;s++) if(H.Get(s)!=null && !pending.Contains((true,s))){ C.TryReserve(); pending.Add((true,s)); }
          for(int c=0;c<Q.ColumnCount;c++){ int queued=0; foreach(var t in pending) if(!t.holder&&t.index==c) queued++;
            for(int k=0;k<Q.Remaining(c)-queued;k++){ if(!C.TryReserve()) return "reserve-failed"; pending.Add((false,c)); } }
        }
      }
      return "stuck"; }
  }
}
