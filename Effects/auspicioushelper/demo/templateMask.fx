

sampler2D TextureSampler : register(s0);
texture2D materialTex : register(t1);
sampler2D materialSamp : register(s1);


uniform float2 pscale;
uniform float time;

float4 maskAt(float2 pos, float offsetx, float offsety){
    return tex2D(TextureSampler,pos+float2(offsetx,offsety)*pscale);
}

float4 valAt(float2 pos, float offsetx, float offsety){
    return tex2D(materialSamp,pos+float2(offsetx,offsety)*pscale);
}

float4 main(float4 color : COLOR0, float2 pos : TEXCOORD0) : SV_Target {
	float4 mask = maskAt(pos,0,0);
  if(mask.a<0.5){
    return float4(0,0,0,0);
  }
  if(mask.r>0.9){
    return float4(1,1,1,1);
  }
  float4 other = valAt(pos,0,0);
  float4 composite = other*0.7+float4(0.2,0.2,0.2,0.3);
  return composite;
}

technique BasicTech {
    pass Pass0 {
        PixelShader = compile ps_3_0 main();
    }
}
