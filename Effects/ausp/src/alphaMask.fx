

sampler2D maskSampler : register(s1);
sampler2D maskedSampler : register(s2);

float4 main(float4 color : COLOR0, float2 pos : TEXCOORD0) : COLOR0{
  float mask = tex2D(maskSampler, pos).a;
  float4 orig = tex2D(maskedSampler, pos);
  return orig*mask;
}


technique BasicTech {
  pass Pass0 {
    PixelShader = compile ps_3_0 main();
  }
}