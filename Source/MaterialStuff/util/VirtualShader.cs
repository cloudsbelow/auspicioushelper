



using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

public class VirtualShader{
  internal Effect shader;
  public bool isnull=>shader==null;
  internal int cacheNum;
  internal string path;
  Dictionary<string,object> grrr = new();
  public VirtualShader(string path){
    cacheNum = -1;
    this.path=path;
  }
  public void setBaseparams(){
    var p = shader.Parameters;
    p["cpos"]?.SetValue(MaterialPipe.camera.Position);
    p["pscale"]?.SetValue(RenderTargetPool.pixelSize);
    p["time"]?.SetValue(((Engine.Scene as Level)?.TimeActive??0)+2);
    p["quiet"]?.SetValue(Settings.Instance.DisableFlashes? 1f:0f);
  }
  public void setparamvalex(string key, bool t) {
    shader.Parameters[key]?.SetValue(t);
    grrr[key] = t;
  }
  public void setparamvalex(string key, float t) {
    shader.Parameters[key]?.SetValue(t);
    grrr[key] = t;
  }
  public void setparamvalex(string key, int t) {
    shader.Parameters[key]?.SetValue(t);
    grrr[key] = t;
  }
  public void setparamvalex(string key, float[] t){
    shader.Parameters[key]?.SetValue(t);
    grrr[key] = t;
  }
  public void setparamvalex(string key, Vector4 t){
    shader.Parameters[key]?.SetValue(t);
    grrr[key] = t;
  }
  public void setparamvalex(string key, Vector2 t){
    shader.Parameters[key]?.SetValue(t);
    grrr[key] = t;
  }
  public static implicit operator Effect(VirtualShader v){
    if(v==null || v.shader == null) return null;
    if(v.cacheNum != auspicioushelperModule.CACHENUM) {
      auspicioushelperGFX.Fill(v);
      foreach(var (k,p) in v.grrr){
        switch(p){
          case bool b: v.setparamvalex(k,b); break;
          case float f: v.setparamvalex(k,f); break;
          case int i: v.setparamvalex(k,i); break;
          case float[] fa: v.setparamvalex(k,fa); break;
          case Vector4 v4: v.setparamvalex(k,v4); break;
          case Vector2 v2: v.setparamvalex(k,v2); break;
        }
      }
    }
    return v.shader;
  }
  public void Clear()=>shader=null;
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
