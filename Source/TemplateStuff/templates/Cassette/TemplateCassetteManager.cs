


using System;
using System.Collections.Generic;
using System.Linq;
using Celeste.Editor;
using Celeste.Mod.Entities;
using Celeste.Mod.auspicioushelper;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;

namespace Celeste.Mod.auspicioushelper;

[CustomEntity("auspicioushelper/TemplateCassetteManager","auspicioushelper/TemplateCassetteManagerSimple")]
[Tracked]
public class TemplateCassetteManager:Entity, IDeclareLayers{
  Dictionary<string, string> material = null;
  static Dictionary<string,string> nameTrans = new Dictionary<string, string>{
    {"small","event:/game/general/cassette_block_switch_1"},
    {"big","event:/game/general/cassette_block_switch_2"},
  };
  class timingDesc{
    public List<Tuple<string,int>> ch = new List<Tuple<string, int>>();
    public List<string> sound = new List<string>();
    public timingDesc fromDict(Dictionary<string,string> d){
      foreach(var pair in d){
        switch(pair.Key){
          case "channel": case "ch": case "c": case "channels":
            foreach(var chpair in Util.kvparseflat(Util.stripEnclosure(pair.Value))){
              ch.Add(new Tuple<string, int>(chpair.Key,int.Parse(chpair.Value)));
            } 
            break;
          case "sound": case "s":
            if(nameTrans.TryGetValue(pair.Value, out var sname)){
              sound.Add(sname);
            } else sound.Add(pair.Value); 
            break;
        }
      }
      return this;
    }
  }
  List<Tuple<float,timingDesc>> timings = new List<Tuple<float, timingDesc>>();
  timingDesc ini;
  timingDesc deini;
  float beatsPerMeasure;
  float beatsPerSecond;
  float lastBeatLoc = 0;
  int curIndex = 0;
  public string channel {get;set;}
  bool useChannel;
  bool active = true;
  bool correct;
  bool subordinate = false;
  bool visualOnly = false;
  List<string> channels = new List<string>{"","","",""};
  public TemplateCassetteManager(EntityData d, Vector2 offset):base(d.Position+offset){
    Depth=-10;
    if(d.Name == "auspicioushelper/TemplateCassetteManager"){
      material = Util.kvparseflat(d.Attr("materials",""));
      var timingDict = Util.kvparseflat(d.Attr("timings",""));
      beatsPerMeasure = d.Float("beatsPerMeasure",4);
      beatsPerSecond = d.Float("beatsPerMinute",90)/60;
      channel = d.Attr("channel","");
      useChannel = d.Bool("useChannel",false);
      correct = d.Bool("correct",true);
      lastBeatLoc = (d.Float("offset",0)*beatsPerSecond)%beatsPerMeasure;
      Dictionary<float, timingDesc> added = new Dictionary<float, timingDesc>();
      if(timingDict != null && timingDict.Count>0)foreach(var pair in timingDict){
        float timing = float.Parse(pair.Key);
        if(timing>beatsPerMeasure) continue;
        timingDesc desc = null;
        if(!added.TryGetValue(timing, out desc)){
          desc = new timingDesc();
          timings.Add(new Tuple<float, timingDesc>(timing,desc));
          added.Add(timing,desc);
        }
        desc.fromDict(Util.kvparseflat(Util.stripEnclosure(pair.Value)));
      }
      timings.Sort((a, b) => a.Item1.CompareTo(b.Item1));
      for(int i=0; i<timings.Count; i++){
        if(timings[i].Item1>lastBeatLoc){
          curIndex = i; break;
        }
      }
      ini = new timingDesc().fromDict(Util.kvparseflat(d.Attr("onactivate","")));
      deini = new timingDesc().fromDict(Util.kvparseflat(d.Attr("ondeactivate")));
      hooks.enable();
    } else {
      subordinate = true;
      material = new();
      bool usevanilla = !d.Bool("simple_style",false);
      bool opaque = !d.Bool("translucent",false);
      bool tint = d.Bool("tintActive",false);
      for(int i=0; i<4; i++){
        string ch = d.Attr("channel_"+(i+1).ToString());
        if(string.IsNullOrWhiteSpace(ch)) continue;
        if(!d.Bool("no_visuals",false))material[ch] = i switch {
          0=>$"{{color:#1D539B, x:1.57079632, y:1.57079632, stripecutoff:-0.01, phase:{(usevanilla?"1, style:vanilla":"1, style:simple")}{(opaque?", innercolor:0E3463":"")}{(tint?",fg:1, fgsat:0.5, fghigh: 49aaf0, fglow:248":"")}}}",
          1=>$"{{color:#60247B, x:1.57079632, y:-1.57079632, stripecutoff:-0.01, phase:{(usevanilla?"1, style:vanilla":"1, style:simple")}{(opaque?", innercolor:2F164E":"")}{(tint?",fg:1, fgsat:0.5, fghigh: f049be, fglow:825":"")}}}",
          2=>$"{{color:#656C26, x:1.57079632, y:0, stripecutoff:-0.01, phase:{(usevanilla?"3.141592653, style:vanilla":"0, style:simple")}{(opaque?", innercolor:314318":"")}{(tint?",fg:1, fgsat:0.5, fghigh: fcdc3a, fglow:871":"")}}}",
          3=>$"{{color:#166E32, x:0, y:1.57079632, stripecutoff:-0.01, phase:{(usevanilla?"3.141592653, style:vanilla":"0, style:simple")}{(opaque?", innercolor:0B4520":"")}{(tint?",fg:1, fgsat:0.5, fghigh: 38e04e, fglow:172":"")}}}",
          _=>"{color:#fff, x:0.3,y:1.2}"
        };
        channels[i] = ch;
      }
      visualOnly = d.Bool("visual_only",false);
      if(d.Bool("useVanillaMistiming", false)) Depth = 10;
    }
    inimaterials();
  }
  public void inimaterials(){
    if(material != null) foreach(var pair in material){
      var format = CassetteMaterialLayer.CassetteMaterialFormat.fromDict(Util.kvparseflat(pair.Value,true));
      CassetteMaterialLayer other;
      if(CassetteMaterialLayer.layers.TryGetValue(pair.Key, out other)){
        if(other.format.gethash() != format.gethash()){
          MaterialPipe.removeLayer(other);
          other = null;
        } 
      }
      if(other == null) other = new CassetteMaterialLayer(format, pair.Key);
      CassetteMaterialLayer.layers[pair.Key]=other;
      MaterialPipe.addLayer(other);
    }
  }
  public void declareLayers(){
    inimaterials();
  }
  public void setChVal(double val){
    if(!useChannel) return;
    bool nactive = val!=0;
    if(nactive != active){
      active = nactive;
      if(active){
        if(correct)runToCurrent();
        else{
          curIndex = 0;
          lastBeatLoc = lastBeatLoc % beatsPerMeasure;
          for(int i=0; i<timings.Count; i++){
            if(timings[i].Item1>lastBeatLoc){
              curIndex = i; break;
            }
          }
          Run(ini);
        }
      } 
      else Run(deini);
    }
  }
  public override void Added(Scene scene){
    base.Added(scene);
    if(subordinate){
      return;
    }
    if(useChannel) Add(new ChannelTracker(channel, setChVal, true));
    if(!useChannel && active){
      runToCurrent();
    }
  }
  static void Run(timingDesc t){
    foreach(string sound in t.sound){
      Audio.Play(sound);
    }
    foreach(var pair in t.ch){
      ChannelState.SetChannel(pair.Item1,pair.Item2);
    }
  }
  void runToCurrent(){
    Dictionary<string,int> toset = new Dictionary<string, int>();
    foreach(string sound in ini.sound) Audio.Play(sound);
    foreach(var pair in ini.ch) toset[pair.Item1] = pair.Item2;
    lastBeatLoc = lastBeatLoc % beatsPerMeasure;
    curIndex = 0;
    for(int i=0; i<timings.Count; i++){
      if(timings[i].Item1>lastBeatLoc){
        curIndex = i; break;
      }
      foreach(var pair in timings[i].Item2.ch){
        toset[pair.Item1] = pair.Item2;
      }
    }
    foreach(var pair in toset){
      ChannelState.SetChannel(pair.Key,pair.Value);
    }
  }
  void Advance(float time){
    if(subordinate) return;
    float cbeatloc = lastBeatLoc+time*beatsPerSecond;
    if(timings.Count>0 && active)while(timings[curIndex].Item1<=cbeatloc){
      Run(timings[curIndex].Item2);
      curIndex++;
      if(curIndex == timings.Count){
        curIndex = 0;
        cbeatloc -= beatsPerMeasure;
      }
    }
    lastBeatLoc = cbeatloc;
  }
  public override void Update(){
    base.Update();
    if(subordinate){
      if(visualOnly) return;
      CassetteBlockManager cbm = Scene.Tracker.GetEntity<CassetteBlockManager>();
      int cbeatloc = cbm?.currentIndex??-1;
      if(cbeatloc != lastBeatLoc){
        for(int i=0; i<4; i++){
          if(!string.IsNullOrWhiteSpace(channels[i])){
            ChannelState.SetChannel(channels[i],i==cbeatloc?1:0);
          }
        }
      }
      lastBeatLoc = cbeatloc;
      return;  
    }
    Advance(Engine.DeltaTime);
  }
  static void onFreeze(On.Celeste.Celeste.orig_Freeze orig, float amount){
    if(Engine.FreezeTimer<amount){
      foreach(TemplateCassetteManager m in Engine.Instance.scene.Tracker.GetEntities<TemplateCassetteManager>()){
        m.Advance(amount);
      }
    }
    orig(amount);
  }
  static HookManager hooks = new(()=>{
    On.Celeste.Celeste.Freeze += onFreeze;
  }, ()=>{
    On.Celeste.Celeste.Freeze -= onFreeze;
  }, auspicioushelperModule.OnEnterMap);
}