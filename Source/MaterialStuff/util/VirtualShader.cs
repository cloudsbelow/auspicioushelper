



using System.Collections;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

public class VirtualShader{
  Effect shader;
  Effect quiet;
  public bool isnull=>shader==null;
  public VirtualShader(Effect shader, Effect quiet = null){
    this.shader=shader; this.quiet=quiet;
  }
  public void setBaseparams(){
    var p = shader.Parameters;
    p["cpos"]?.SetValue(MaterialPipe.camera.Position);
    p["pscale"]?.SetValue(RenderTargetPool.pixelSize);
    p["time"]?.SetValue(((Engine.Scene as Level)?.TimeActive??0)+2);
    p["quiet"]?.SetValue(Settings.Instance.DisableFlashes? 1f:0f);
    if(quiet!=null){
      p=quiet.Parameters;
      p["cpos"]?.SetValue(MaterialPipe.camera.Position);
      p["pscale"]?.SetValue(RenderTargetPool.pixelSize);
      p["time"]?.SetValue(((Engine.Scene as Level)?.TimeActive??0)+2);
      p["quiet"]?.SetValue(Settings.Instance.DisableFlashes? 1f:0f);
    }
  }
  public void setparamvalex(string key, bool t) {
    shader.Parameters[key]?.SetValue(t);
    if(quiet!=null)quiet.Parameters[key]?.SetValue(t);
  }
  public void setparamvalex(string key, float t) {
    //DebugConsole.Write($"HereFloat {key} {t}");
    if(shader.Parameters[key]!=null){
      shader.Parameters[key].SetValue(t);
      //DebugConsole.Write($" SETTING {key} {t}");
    }
    if(quiet!=null)quiet.Parameters[key]?.SetValue(t);
  }
  public void setparamvalex(string key, int t) {
    shader.Parameters[key]?.SetValue(t);
    if(quiet!=null)quiet.Parameters[key]?.SetValue(t);
  }
  public void setparamvalex(string key, float[] t){
    shader.Parameters[key]?.SetValue(t);
    if(quiet!=null)quiet.Parameters[key]?.SetValue(t);
  }
  public void setparamvalex(string key, Vector4 t){
    shader.Parameters[key]?.SetValue(t);
    if(quiet!=null)quiet.Parameters[key]?.SetValue(t);
  }
  public void setparamvalex(string key, Vector2 t){
    shader.Parameters[key]?.SetValue(t);
    if(quiet!=null)quiet.Parameters[key]?.SetValue(t);
  }
  public static implicit operator Effect(VirtualShader v){
    if(v==null) return null;
    return v.quiet!=null&&auspicioushelperModule.Settings.UseQuietShader?v.quiet:v.shader;
  }
  public static implicit operator VirtualShader(Effect eff)=>new VirtualShader(eff);
}

public class VirtualShaderList:IEnumerable<VirtualShader>{
  List<VirtualShader> shaders = new();
  public void setparamvalex(string key, bool t){
    foreach(var s in shaders) s?.setparamvalex(key, t);
  }
  public void setparamvalex(string key, int t){
    foreach(var s in shaders) s?.setparamvalex(key, t);
  }
  public void setparamvalex(string key, float t){
    foreach(var s in shaders) s?.setparamvalex(key, t);
  }
  public void setparamvalex(string key, float[] t){
    foreach(var s in shaders) s?.setparamvalex(key, t);
  }
  public void setparamvalex(string key, Vector4 t){
    foreach(var s in shaders) s?.setparamvalex(key, t);
  }
  public void setparamvalex(string key, Vector2 t){
    foreach(var s in shaders) s?.setparamvalex(key, t);
  }
  
  public void setparamvalex(string key, bool t, int idx){
    shaders[idx]?.setparamvalex(key, t);
  }
  public void setparamvalex(string key, int t, int idx){
    shaders[idx]?.setparamvalex(key, t);
  }
  public void setparamvalex(string key, float t, int idx){
    shaders[idx]?.setparamvalex(key, t);
  }
  public void setparamvalex(string key, float[] t, int idx){
    shaders[idx]?.setparamvalex(key, t);
  }
  public void setbaseparams(){
    foreach(var s in shaders)s?.setBaseparams();
  }
  public int Count=>shaders.Count;
  public IEnumerator<VirtualShader> GetEnumerator() {
    return shaders.GetEnumerator(); 
  }
  IEnumerator IEnumerable.GetEnumerator() {
    return shaders.GetEnumerator();
  }
  public VirtualShader this[int x]{
    get=>shaders[x];
  }
  public void Add(VirtualShader n)=>shaders.Add(n==null||n.isnull?null:n);
}
