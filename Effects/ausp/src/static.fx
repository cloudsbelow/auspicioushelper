




sampler2D TextureSampler : register(s0);

uniform float2 pscale;
uniform float time;
uniform float2 cpos;
uniform float4 low=float4(0,0,0,1);
uniform float4 high=float4(1,1,1,1);

float4 orig(float2 pos, float offsetx, float offsety){
    return tex2D(TextureSampler,pos+float2(offsetx,offsety)*pscale);
}
float hash(float2 p, float o){
  return frac(sin(dot(float3(p,o), float3(12.9898, 78.233, 45.164))) * 43758.5453);
}


float4 main(float4 color : COLOR0, float2 pos : TEXCOORD0) : SV_Target {
	float mask = orig(pos,0,0).a;
  float val = hash(pos,time);
  return (val*high+(1-val)*low)*mask;
}

technique BasicTech {
    pass Pass0 {
        PixelShader = compile ps_3_0 main();
    }
}
