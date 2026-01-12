

sampler2D maskSampler : register(s0);

float4 main(float4 color : COLOR0, float2 pos : TEXCOORD0) : COLOR0{
  float4 orig = tex2D(maskSampler, pos)*color;
  return float4(orig.rgb, 1-orig.a);
}


technique BasicTech {
  pass Pass0 {
    PixelShader = compile ps_3_0 main();
  }
}