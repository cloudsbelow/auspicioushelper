


using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

namespace Celeste.Mod.auspicioushelper;

public partial class FancyWater{
  [Tracked]
  class Displacement:Entity{
    [ResetEvents.NullOn(ResetEvents.Times.NewAssets)]
    [Import.SpeedrunToolIop.Static]
    static Displacement me;

    [ResetEvents.NullOn(ResetEvents.Times.LvlCleanup)]
    static Pos2Color[] mesh = null;
    static int meshindex = 0;
    const int NUMQUADS = 4096;
    static Effect effect;
    RenderTargetPool.RenderTargetHandle handle = new(false);
    public static Displacement For(Scene s){
      if(me==null) s.Add(me = new Displacement());
      return me;
    }


    Displacement(){
      effect = auspicioushelperGFX.LoadShader("misc/waterdisplacement");
      if(mesh==null) mesh = new Pos2Color[NUMQUADS*6];
      AddTag(Tags.Global);
      Add(new BeforeRenderHook(Rasterize));
      Add(new DisplacementRenderHook(()=>{
        Draw.SpriteBatch.Draw(handle, (Scene as Level).Camera.position, Color.White);
      }));
      Depth = -13000;
    }
    void Rasterize(){
      Dictionary<int,List<FancyWater>> byDepth = new();
      foreach(FancyWater f in Scene.Tracker.GetEntities<FancyWater>()) if(f.leader==null){
        if(!byDepth.TryGetValue(-f.Depth, out var li)) li = byDepth[-f.Depth] = new();
        li.Add(f); 
      }
      int[] keys = byDepth.Keys.ToArray();
      Array.Sort(keys);
      List<List<Solid>> buckets = new();
      for(int i=0; i<keys.Length; i++) buckets.Add(new());
      if(keys.Length==0){
        me = null;
        RemoveSelf();
        return;
      }

      if(keys.Length<=4){
        foreach(Solid s in Scene.Tracker.GetEntities<Solid>()){
          if(!s.Visible) continue;
          int d = -s.Depth;
          int i=0;
          for(; i<keys.Length && keys[i]<=d; i++);
          if(i==0) continue;
          buckets[i-1].Add(s);
        }
      } else {
        foreach(Solid s in Scene.Tracker.GetEntities<Solid>()){
          if(!s.Visible) continue;
          int d = -s.Depth;
          int left = 0; 
          int right = keys.Length;
          while(right!=left){
            int middle = (left+right)/2;
            if(keys[middle]<=d) left=middle+1;
            else right = middle;
          }
          if(right==0) continue;
          buckets[right-1].Add(s);
        }
      }
      
      Color solidColor = Color.Transparent;
      Color waterColor = new(0.5f,0.5f,0.25f,1);
      var gd = Engine.Instance.GraphicsDevice;
      gd.SetRenderTarget(handle);
      gd.Clear(Color.Transparent);
      var cam = (Scene as Level).Camera;
      Start(cam.Matrix);
      Int2 camsize = ExtendedCameraIop.cameraSize();

      for(int i=0; i<keys.Length; i++){
        foreach(var fw in byDepth[keys[i]]){
          //todo - visible check
          Quads(fw.fills, waterColor, fw.Position);
          foreach(var s in fw.surfaces) QuadsFrom(s.mesh, waterColor, fw.Position, s.rayidx/6);
        }
        foreach(var s in buckets[i]){
          Collider c = s.Collider;
          start:
          if(c is Hitbox hb) Quad(new FloatRect(hb), solidColor);
          else if(c is Grid g){
            var m = MiptileCollider.fromGrid(g);
            float w = m.cellsize.X;
            float h = m.cellsize.Y;
            var tlc = m.tlc;
            var vec = -tlc+cam.position;
            var rtlc = Int2.Max(Int2.Floor(vec/m.cellsize), 0);
            var rbrc = Int2.Min(Int2.Ceil((vec+camsize)/m.cellsize), m.mg.size);
            var l = m.mg.layers[0];
            for(int yy=rtlc.y; yy<rbrc.y; yy++){
              int cur = -1;
              for(int xx=rtlc.x; xx<rbrc.x; xx++){
                if(l.collidePoint(new(xx,yy))){
                  if(cur==-1) cur=xx;
                } else {
                  if(cur!=-1){
                    Quad(new(tlc.X+cur*w, tlc.Y+yy*h, (xx-cur)*w, h), solidColor);
                  }
                  cur=-1;
                }
              }
              if(cur!=-1) Quad(new(tlc.X+cur*w, tlc.Y+yy*h, (rbrc.x-cur)*w, h), solidColor);
            }
            
          }
          else if(c is IColliderWrapper cw){
            c=cw.wrapped;
            goto start;
          }
        }
      } 
      Flush();
    }


    public override void Added(Scene scene) {
      base.Added(scene);
      handle.Claim();
    }
    public override void Removed(Scene scene) {
      base.Removed(scene);
      handle.Free();
      if(me==this) me=null;
    }
    public override void SceneEnd(Scene scene) {
      base.SceneEnd(scene);
      handle.Free();
      if(me==this) me=null;
    }



    [Serializable]
	  [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Pos2Color:IVertexType{
      public Vector2 Position;
      public Color Color;
      public static readonly VertexDeclaration vertexDeclaration = new(
        new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),
        new VertexElement(8, VertexElementFormat.Color, VertexElementUsage.Color, 0)
      );
      VertexDeclaration IVertexType.VertexDeclaration=> vertexDeclaration;
    }
    static void Start(Matrix matrix){
      Vector2 vector = new Vector2(Engine.Graphics.GraphicsDevice.Viewport.Width, Engine.Graphics.GraphicsDevice.Viewport.Height);
      matrix *= Matrix.CreateScale(1f / vector.X * 2f, (0f - 1f / vector.Y) * 2f, 1f);
      matrix *= Matrix.CreateTranslation(-1f, 1f, 0f);
      Engine.Instance.GraphicsDevice.RasterizerState = RasterizerState.CullNone;
      Engine.Instance.GraphicsDevice.BlendState = BlendState.Opaque;
      effect.Parameters["World"].SetValue(matrix);
    }
    static void Flush(){
      effect.CurrentTechnique.Passes[0].Apply();
      Engine.Instance.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleList, mesh, 0, meshindex*2);
      meshindex=0;
    }
    static void QuadsFrom(VertexPositionColor[] verts, Color ncolor, Vector2 translate, int count, int offset=0){
      for(int i=0; i<count; i++){
        var o = i*6+offset;
        var m = meshindex++*6;
        for(int j=0; j<6; j++){
          var vert = verts[o+j].Position;
          mesh[m+j].Position = new(vert.X+translate.X, vert.Y+translate.Y);
          mesh[m+j].Color = ncolor;
        }
        if(m==NUMQUADS) Flush();
      }
    }
    static void Quads(List<FloatRect> rects, Color ncolor, Vector2 translate){
      foreach(var r in rects){
        var m = meshindex++*6;
        for(int j=0; j<6; j++) mesh[m+j].Color = ncolor;
        mesh[m+0].Position = r.tlc+translate;
        mesh[m+4].Position = mesh[m+1].Position = r.blc+translate;
        mesh[m+3].Position = mesh[m+2].Position = r.trc+translate;
        mesh[m+5].Position = r.brc+translate;
        if(m==NUMQUADS) Flush();
      }
    }
    static void Quad(FloatRect r, Color ncolor){
      var m = meshindex++*6;
      for(int j=0; j<6; j++) mesh[m+j].Color = ncolor;
      mesh[m+0].Position = r.tlc;
      mesh[m+4].Position = mesh[m+1].Position = r.blc;
      mesh[m+3].Position = mesh[m+2].Position = r.trc;
      mesh[m+5].Position = r.brc;
      if(m==NUMQUADS) Flush();
    }
  }
}