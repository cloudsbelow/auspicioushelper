sampler2D sampler1:register(s0);

uniform bool flipv;
uniform bool fliph;

float4 main(float4 color : COLOR0, float2 pos : TEXCOORD0) : COLOR0{
  float2 loc = float2(lerp(pos.x,1-pos.x,float(fliph)),lerp(pos.y,1-pos.y,float(flipv)));
  return tex2D(sampler1, loc);
}


technique BasicTech {
  pass Pass0 {
    PixelShader = compile ps_3_0 main();
  }
}