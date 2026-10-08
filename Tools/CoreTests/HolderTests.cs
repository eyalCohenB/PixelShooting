using System; using PixelShooting.Core.Holder; using PixelShooting.Core.Levels; using PixelShooting.Core.Queue;
static class HolderTests { static int fails=0; static void Check(bool c,string m){Console.WriteLine($"[{(c?"PASS":"FAIL")}] {m}"); if(!c)fails++;}
  public static int Run(){
    Shooter S()=>new Shooter(new ShooterSpec(0,3));
    var h=new HolderModel(); var s=new Shooter[7]; for(int i=0;i<7;i++) s[i]=S();
    Check(h.Count==0 && !h.IsFull,"starts empty");
    bool ok=true; for(int i=0;i<5;i++){ ok&=h.TryAdd(s[i],out int slot) && slot==i; }
    Check(ok && h.Count==5 && h.IsFull,"5 arrivals fill slots 0..4 left to right");
    Check(s[0].Location==ShooterLocation.Holder,"arrival sets Location = Holder");
    Check(!h.TryAdd(s[5],out int lostSlot) && lostSlot==-1 && h.Count==5,"6th arrival fails (lose) and changes nothing");
    var t=h.Take(2); Check(t==s[2] && h.Get(2)==null && h.Count==4 && t.Location==ShooterLocation.Conveyor,"Take frees the slot and sets Location = Conveyor");
    Check(h.Get(1)==s[1] && h.Get(3)==s[3],"no sliding: other shooters keep their slots");
    h.Take(0); Check(h.TryAdd(s[5],out int sl) && sl==0,"next arrival gets the leftmost free slot (0, not 2)");
    Check(h.TryAdd(s[6],out sl) && sl==2,"then the next free one (2)");
    bool threw=false; h.Take(4); try{ h.Take(4);}catch(InvalidOperationException){threw=true;}
    Check(threw,"taking an empty slot throws");
    long a0=GC.GetAllocatedBytesForCurrentThread(); for(int k=0;k<1000;k++){ h.TryAdd(s[4],out _); h.Take(4);} long a1=GC.GetAllocatedBytesForCurrentThread();
    Check(a1-a0==0,$"TryAdd/Take allocate 0 bytes ({a1-a0})");
    h.Reset(); Check(h.Count==0 && h.Get(0)==null,"Reset empties every slot");
    Console.WriteLine(fails==0?"ALL PASSED":$"{fails} FAILED"); return fails; } }
