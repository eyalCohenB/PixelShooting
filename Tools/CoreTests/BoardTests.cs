using System; using System.Collections.Generic; using System.IO; using PixelShooting.Core.Levels; using PixelShooting.Core.Board;
static class BoardTests { static int fails=0;
  static void Check(bool c,string m){ Console.WriteLine($"[{(c?"PASS":"FAIL")}] {m}"); if(!c) fails++; }
  static LevelData Load(string json){ var e=new List<string>(); LevelLoader.TryLoad(json,"t",out var l,e,new List<string>()); if(l==null) throw new Exception(string.Join(";",e)); return l; }
  static string Dir=Paths.Levels;
  // Brute force oracle: scan the line from the side.
  static bool Oracle(BoardModel b,BoardSide s,int line,out int x,out int y){
    int len=(s==BoardSide.Bottom||s==BoardSide.Top)?b.Height:b.Width;
    for(int d=0;d<len;d++){ switch(s){case BoardSide.Bottom:x=line;y=d;break;case BoardSide.Top:x=line;y=b.Height-1-d;break;case BoardSide.Left:x=d;y=line;break;default:x=b.Width-1-d;y=line;break;}
      if(b.GetCell(x,y)!=LevelData.EmptyCell) return true; } x=y=-1; return false; }
  public static int Run(){
    var b=new BoardModel();
    var l1=Load(File.ReadAllText(Dir+"Level01.json")); b.Reset(l1);
    Check(b.RemainingCells==16 && !b.IsCleared,"Level01 remaining=16");
    var l2=Load(File.ReadAllText(Dir+"Level02.json")); b.Reset(l2);
    Check(b.RemainingCells==32,"Level02 remaining=32 (4 holes not counted)");
    Check(b.TryGetExposed(BoardSide.Bottom,0,out int x,out int y)&&x==0&&y==1,"Level02 bottom col0 skips hole -> (0,1)");
    Check(b.TryGetExposed(BoardSide.Left,5,out x,out y)&&x==1&&y==5,"Level02 left top row skips hole -> (1,5)");
    Check(b.TryGetExposed(BoardSide.Top,0,out x,out y)&&x==0&&y==4,"Level02 top col0 skips hole -> (0,4)");
    // Worked example column: [Empty, Red, Blue, Empty, Red] from y=0 up
    var ex=Load("{\"width\":1,\"height\":5,\"palette\":[\"#FF0000\",\"#0000FF\"],\"cells\":[0,-1,1,0,-1],\"queues\":[{\"shooters\":[{\"color\":0,\"bullets\":2},{\"color\":1,\"bullets\":1}]}]}");
    b.Reset(ex);
    Check(b.TryFire(BoardSide.Bottom,0,0,out x,out y)&&y==1,"example: red from bottom hits y=1");
    Check(!b.TryFire(BoardSide.Bottom,0,0,out x,out y),"example: red from bottom now blocked by blue");
    Check(b.TryFire(BoardSide.Top,0,0,out x,out y)&&y==4,"example: red from top hits y=4");
    Check(b.TryFire(BoardSide.Bottom,0,1,out x,out y)&&y==2,"example: blue from bottom hits y=2");
    Check(b.IsCleared && !b.TryGetExposed(BoardSide.Top,0,out x,out y),"example: cleared, column empty from top");
    // Cross-side: shooting from bottom removes the Left frontier cell of a row
    b.Reset(l1); // col0,1 red(0); col2,3 blue(1)
    Check(b.TryFire(BoardSide.Bottom,0,0,out x,out y)&&x==0&&y==0,"cross: bottom takes (0,0)");
    Check(b.TryGetExposed(BoardSide.Left,0,out x,out y)&&x==1&&y==0,"cross: left row0 now exposes (1,0)");
    Check(!b.TryFire(BoardSide.Right,0,0,out x,out y),"cross: red from right blocked by blue");
    // Reset restores everything
    b.Reset(l1); Check(b.RemainingCells==16 && b.GetCell(0,0)==0 && b.TryGetExposed(BoardSide.Left,0,out x,out y)&&x==0,"reset restores cells and depths");
    // Randomized: compare with oracle until cleared, all levels
    var rnd=new Random(1234); string[] files={"Level01.json","Level02.json","Level03.json"}; int queries=0; bool allOk=true;
    foreach(var f in files){ var lv=Load(File.ReadAllText(Dir+f));
      for(int run=0;run<200;run++){ b.Reset(lv); int guard=0;
        while(!b.IsCleared && guard++<100000){ var s=(BoardSide)rnd.Next(4); int line=rnd.Next(b.LineCount(s));
          bool o=Oracle(b,s,line,out int ox,out int oy); bool m=b.TryGetExposed(s,line,out int mx,out int my); queries++;
          if(o!=m||ox!=mx||oy!=my){allOk=false;break;}
          if(m){ byte c=(byte)b.GetCell(mx,my); int before=b.RemainingCells; if(!b.TryFire(s,line,c,out int fx,out int fy)||fx!=mx||fy!=my||b.RemainingCells!=before-1){allOk=false;break;} } }
        if(!b.IsCleared) allOk=false; } }
    Check(allOk,$"randomized: {queries} queries match brute-force scan; every run clears the board");
    // Allocation check
    b.Reset(Load(File.ReadAllText(Dir+"Level03.json")));
    long a0=GC.GetAllocatedBytesForCurrentThread();
    for(int i=0;i<10000;i++){ var s=(BoardSide)(i&3); b.TryGetExposed(s,i%10,out x,out y); b.TryFire(s,i%10,(byte)(i%4),out x,out y);} 
    b.Reset(Load(File.ReadAllText(Dir+"Level03.json"))); long a1=GC.GetAllocatedBytesForCurrentThread();
    var lv3=Load(File.ReadAllText(Dir+"Level03.json")); b.Reset(lv3); long a2=GC.GetAllocatedBytesForCurrentThread(); b.Reset(lv3); long a3=GC.GetAllocatedBytesForCurrentThread();
    Check(a3-a2==0,$"Reset with same size allocates 0 bytes (got {a3-a2})");
    long q0=GC.GetAllocatedBytesForCurrentThread(); for(int i=0;i<10000;i++){ b.TryGetExposed((BoardSide)(i&3),i%10,out x,out y);} long q1=GC.GetAllocatedBytesForCurrentThread();
    Check(q1-q0==0,$"10000 queries allocate 0 bytes (got {q1-q0})");
    Console.WriteLine(fails==0?"ALL PASSED":$"{fails} FAILED"); return fails; } }
