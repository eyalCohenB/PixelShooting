// Minimal stand-ins for the UnityEngine APIs the level code uses (test harness only).
using System; using System.Text.Json; using System.Globalization;
namespace UnityEngine {
  public struct Vector2 { public float x,y; public Vector2(float x,float y){this.x=x;this.y=y;} public static Vector2 zero=>new Vector2(0,0); }
  public static class Mathf { public static float Clamp(float v,float a,float b)=>v<a?a:(v>b?b:v); }
  public struct Color { public float r,g,b,a; }
  public struct Color32 { public byte r,g,b,a;
    public static implicit operator Color32(Color c)=>new Color32{r=(byte)(c.r*255),g=(byte)(c.g*255),b=(byte)(c.b*255),a=(byte)(c.a*255)}; }
  public static class ColorUtility {
    public static bool TryParseHtmlString(string s, out Color c){ c=default;
      if(s==null||s.Length!=7||s[0]!='#') return false;
      if(!uint.TryParse(s.Substring(1),NumberStyles.HexNumber,null,out uint v)) return false;
      c=new Color{r=((v>>16)&255)/255f,g=((v>>8)&255)/255f,b=(v&255)/255f,a=1}; return true; } }
  public static class JsonUtility {
    public static T FromJson<T>(string json){
      try { return JsonSerializer.Deserialize<T>(json,new JsonSerializerOptions{IncludeFields=true}); }
      catch(JsonException e){ throw new ArgumentException(e.Message); } } }
}
