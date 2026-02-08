sampler2D TextureSampler : register(s0);
sampler2D cg1Sampler: register(s1);

float4 cg1(float4 c1){
  float size = 16.0;
  float sqrd = size * size;

  float offX = c1.x * (1.0 / sqrd) * (size - 1.0) + (1.0 / sqrd) * 0.5;
  float offY = c1.y + (1.0 / size) * 0.5;
  float zSlice0 = min(floor(c1.z * size), size - 1.0);
  float zSlice1 = min(zSlice0 + 1.0, size - 1.0);
  float3 sample0 = tex2D(cg1Sampler, float2(offX + zSlice0 / size, offY)).xyz;
  float3 sample1 = tex2D(cg1Sampler, float2(offX + zSlice1 / size, offY)).xyz;
  return float4(lerp(sample0, sample1, fmod(c1.z * size, 1.0)) * c1.a, c1.a);
}

float4 main(float4 color : COLOR0, float2 pos : TEXCOORD0) : SV_Target {
  float4 ov = tex2D(TextureSampler,pos);
  return cg1(ov);
}

technique BasicTech {
  pass Pass0 {
    PixelShader = compile ps_3_0 main();
  }
}