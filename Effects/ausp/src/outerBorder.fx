
sampler2D TextureSampler : register(s0);


uniform float2 pscale;
uniform float4 color = float4(1,1,1,1);

float4 orig(float2 pos, float offsetx, float offsety){
    return tex2D(TextureSampler,pos+float2(offsetx,offsety)*pscale);
}


float4 main(float4 inTint : COLOR0, float2 pos : TEXCOORD0) : SV_Target {
	float4 o = orig(pos,0,0)*inTint;
  if(o.a>0.5){
    return o;
  }
  if(max(max(max(orig(pos,1,0).a,orig(pos,-1,0).a),orig(pos,0,1).a),orig(pos,0,-1).a)>0.5){
    return color;
  }
  return o;
}

technique BasicTech {
    pass Pass0 {
        PixelShader = compile ps_3_0 main();
    }
}
