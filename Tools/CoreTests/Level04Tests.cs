using System; using System.Collections.Generic; using System.IO;
using PixelShooting.Core.Board; using PixelShooting.Core.Conveyor; using PixelShooting.Core.Holder;
using PixelShooting.Core.Levels; using PixelShooting.Core.Queue; using PixelShooting.Core.Shooting;

// Level04 is the "order matters" level: it must be winnable, and random tapping must often lose.
// Plays whole games with the real Core models and the scene's geometry (path, board area, speed).
static class Level04Tests { static int fails=0; static void Check(bool c,string m){Console.WriteLine($"[{(c?"PASS":"FAIL")}] {m}"); if(!c)fails++;}

  enum Result { Won, Lost, Stuck }

  // One game. 'choose' picks a source when the player may tap: >=0 queue column, <0 holder slot (-1-slot).
  // sequential = wait until the conveyor is empty before each tap.
  static Result Play(LevelData l, Func<Game,int> choose, bool sequential, out int taps){
    var g=new Game(l); taps=0;
    for(int frame=0;frame<60*600;frame++){
      if(g.Board.IsCleared) return Result.Won;
      if(g.C.Pending==0 && g.C.Free>0 && (!sequential || g.C.RiderCount==0)){
        int src=choose(g);
        if(src!=int.MinValue){ g.Tap(src); taps++; }
      }
      if(!g.Step()) return Result.Lost;
    }
    return Result.Stuck;
  }

  class Game {
    public BoardModel Board=new BoardModel(); public QueueModel Q=new QueueModel(); public HolderModel H=new HolderModel();
    public ConveyorModel C; ShootingSystem S; Shot[] shots=new Shot[32]; Shooter[] ex=new Shooter[5]; int pendingSrc; public Random Rng;
    public Game(LevelData l){
      // Scene values: path (-4.75, 4.75, -1.77, 8.26), lapEndGap 1.6, speed 5, spacing 1.2, hop 0.4; board area 7.8 at y 3.26.
      var path=new ConveyorPath(-4.75f,4.75f,-1.77f,8.26f); Board.Reset(l); Q.Reset(l);
      float cell=Math.Min(7.8f/l.Width,7.8f/l.Height);
      var F=new FiringPoints(); F.Build(path,-(l.Width-1)*cell*0.5f,3.26f-(l.Height-1)*cell*0.5f,cell,l.Width,l.Height);
      C=new ConveyorModel(path,path.Length-1.6f,5f,1.2f,0.4f); S=new ShootingSystem(C,Board,F); }
    public void Tap(int src){ C.TryReserve(); pendingSrc=src; }
    public bool Step(){
      if(C.Pending>0 && C.CanEnter){ C.Enter(pendingSrc>=0 ? Q.TakeTop(pendingSrc) : H.Take(-1-pendingSrc)); }
      C.Advance(1f/60f); S.Fire(shots); int n=C.CollectExits(ex);
      for(int i=0;i<n;i++) if(!H.TryAdd(ex[i],out _)) return false;
      return true; }
    public void Sources(List<int> list){ list.Clear();
      for(int c=0;c<Q.ColumnCount;c++) if(!Q.IsEmpty(c)) list.Add(c);
      for(int s=0;s<HolderModel.Capacity;s++) if(H.Get(s)!=null) list.Add(-1-s); }
  }

  public static int Run(){
    var e=new List<string>(); var w=new List<string>();
    bool ok=LevelLoader.TryLoad(File.ReadAllText(Paths.Levels+"Level04.json"),"Level04",out var l,e,w);
    Check(ok,"Level04 loads and validates (bullets == cells per color)"+(ok?"":": "+string.Join(";",e)));
    if(!ok) return fails;

    // Winnable: depth-first search over sequential tap orders (replaying from the start each time).
    var best=new List<int>(); bool won=Search(l,new List<int>(),best,0);
    Check(won,"Level04 is winnable; one winning order: "+Describe(l,best));

    // Random tapping (taps as soon as there's room, random source): should often lose.
    int lost=0, wins=0, runs=300; var rng=new Random(1); var list=new List<int>();
    for(int r=0;r<runs;r++){
      var res=Play(l,g=>{ g.Sources(list); return list.Count==0?int.MinValue:list[rng.Next(list.Count)]; },false,out _);
      if(res==Result.Lost) lost++; else if(res==Result.Won) wins++; }
    Console.WriteLine($"  random tapping: {lost}/{runs} lost, {wins} won, {runs-lost-wins} stuck");
    Check(lost>=runs/3,"Level04: random tapping loses at least a third of the time");

    // The first 3 existing levels stay forgiving: random tapping never loses there.
    for(int k=1;k<=3;k++){
      LevelLoader.TryLoad(File.ReadAllText(Paths.Levels+$"Level0{k}.json"),"L",out var lk,e,w); int lk_lost=0;
      for(int r=0;r<50;r++) if(Play(lk,g=>{ g.Sources(list); return list.Count==0?int.MinValue:list[rng.Next(list.Count)]; },false,out _)==Result.Lost) lk_lost++;
      Console.WriteLine($"  Level0{k}: random tapping lost {lk_lost}/50"); }
    Console.WriteLine(fails==0?"ALL PASSED":$"{fails} FAILED"); return fails; }

  static bool Search(LevelData l,List<int> prefix,List<int> found,int depth){
    if(depth>24) return false;
    int i=0; var res=Play(l,g=>{ if(i<prefix.Count) return prefix[i++]; return int.MinValue; },true,out _);
    // Replays the prefix; when it runs out the game idles until cleared/stuck.
    var g2=Replay(l,prefix,out bool lost); if(lost) return false; if(g2.Board.IsCleared){ found.AddRange(prefix); return true; }
    var list=new List<int>(); g2.Sources(list);
    foreach(int src in list){ prefix.Add(src); if(Search(l,prefix,found,depth+1)) return true; prefix.RemoveAt(prefix.Count-1); }
    return false; }

  static Game Replay(LevelData l,List<int> seq,out bool lost){
    var g=new Game(l); lost=false; int next=0;
    for(int frame=0;frame<60*600;frame++){
      if(g.C.RiderCount==0 && g.C.Pending==0){ if(next==seq.Count) return g; g.Tap(seq[next++]); }
      if(!g.Step()){ lost=true; return g; } }
    return g; }

  static string Describe(LevelData l,List<int> seq){ var parts=new List<string>(); foreach(int s in seq) parts.Add(s>=0?$"Q{s}":$"H{-1-s}"); return string.Join(" ",parts); }
}
