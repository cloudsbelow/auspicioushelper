

sampler2D TextureSampler : register(s0);


uniform float2 pscale;
uniform float time;

float4 valAt(float2 pos, float offsetx, float offsety){
    return tex2D(TextureSampler,pos+float2(offsetx,offsety)*pscale);
}

uniform float sigma;
float4 main(float4 color : COLOR0, float2 pos : TEXCOORD0) : SV_Target {
  float4 sum = float4(0,0,0,0);
  float totalWeight = 0.0;

  float denom = 2.0 * sigma * sigma;
  int radius = 12;

  for (int i = -radius; i <= radius; i++) {
      float x = float(i);
      float weight = exp(-(x * x) / denom);
      sum += valAt(pos, i, 0) * weight;
      totalWeight += weight;
  }
  return sum / totalWeight;
}

technique BasicTech {
    pass Pass0 {
        PixelShader = compile ps_3_0 main();
    }
}
