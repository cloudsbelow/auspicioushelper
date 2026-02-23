
sampler2D TextureSampler : register(s0);

uniform float2 cpos;
uniform float2 pscale;
uniform float time;

uniform float axis = float2(0.01,0.003);
uniform float speed = 0.2;
uniform float base = 0.1;

float2 worldpos(float2 pos){
    return floor(pos/pscale+cpos);
}

float3 HSVtoRGB(float3 hsv){
    float3 rgb = saturate(abs(frac(hsv.x + float3(0.0, 2.0/3.0, 1.0/3.0)) * 6.0 - 3.0) - 1.0);
    return hsv.z * lerp(float3(1.0, 1.0, 1.0), rgb, hsv.y);
}


float4 main(float4 inTint : COLOR0, float2 pos : TEXCOORD0) : SV_Target {
	float4 o = tex2D(TextureSampler,pos)*inTint;
  return o;
  if(o.a==0){
    return o;
  }
  float3 rainbowBase = HSVtoRGB(float3(time*speed+dot(worldpos(pos),axis),0.4,0.9));
  float lum = dot(o.rgb, float3(0.3,0.55,0.15));
  return float4(rainbowBase*(lum*(1-base)+base*o.a),o.a);
}

technique BasicTech {
    pass Pass0 {
        PixelShader = compile ps_3_0 main();
    }
}
