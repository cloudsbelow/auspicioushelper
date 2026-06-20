




sampler2D TextureSampler : register(s0);

uniform float2 pscale;
uniform float time;
uniform float2 cpos;
uniform float4 low=float4(0,0,0,1);
uniform float4 high=float4(1,1,1,1);

float4 orig(float2 pos, float offsetx, float offsety){
    return tex2D(TextureSampler,pos+float2(offsetx,offsety)*pscale);
}
float hash(float2 pos, float time){
  float3 p = float3(pos.x, pos.y, time) + float3(13.52, 91.24, 42.11);
  float h1 = frac(sin(dot(p, float3(12.9898, 78.233, 37.719))) * 43758.5453);
  float h2 = frac(sin(dot(p + h1, float3(39.346, 11.135, 83.155))) * 28613.1234);
  return frac(cos(dot(p + h2, float3(73.156, 52.235, 19.344))) * 35842.1645);
}
float centerWithinRange(float x){
  return x-64*floor(x/32+0.5);
}


float4 main(float4 color : COLOR0, float2 pos : TEXCOORD0) : SV_Target {
	float mask = orig(pos,0,0).a;
  float val = hash(pos,centerWithinRange(time));
  return (val*high+(1-val)*low)*mask;
}

technique BasicTech {
    pass Pass0 {
        PixelShader = compile ps_3_0 main();
    }
}
