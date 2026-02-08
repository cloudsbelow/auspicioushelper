sampler2D TextureSampler : register(s0);
sampler2D cg1Sampler: register(s1);
sampler2D cg2Sampler: register(s2);

float4 cgMulti(float4 c1,float f){
  float size = 16.0;
  float sqrd = size * size;

  float offX = c1.x * (1.0 / sqrd) * (size - 1.0) + (1.0 / sqrd) * 0.5;
  float offY = c1.y + (1.0 / size) * 0.5;
  float zSlice0 = min(floor(c1.z * size), size - 1.0);
  float zSlice1 = min(zSlice0 + 1.0, size - 1.0);
  float3 sample00 = tex2D(cg1Sampler, float2(offX + zSlice0 / size, offY)).xyz;
  float3 sample01 = tex2D(cg1Sampler, float2(offX + zSlice1 / size, offY)).xyz;
  float4 val0 = float4(lerp(sample00, sample01, fmod(c1.z * size, 1.0)) * c1.a, c1.a);

  float3 sample10 = tex2D(cg2Sampler, float2(offX + zSlice0 / size, offY)).xyz;
  float3 sample11 = tex2D(cg2Sampler, float2(offX + zSlice1 / size, offY)).xyz;
  float4 val1 = float4(lerp(sample10, sample11, fmod(c1.z * size, 1.0)) * c1.a, c1.a);
  return val0*(1-f)+val1*f;
}

uniform float fade=0;

float4 main(float4 color : COLOR0, float2 pos : TEXCOORD0) : SV_Target {
  float4 ov = tex2D(TextureSampler,pos);
  return cgMulti(ov, fade);
}

technique BasicTech {
  pass Pass0 {
    PixelShader = compile ps_3_0 main();
  }
}