
sampler2D TextureSampler : register(s0);
sampler2D sampler1 : register(s1);
sampler2D sampler2 : register(s2);
sampler2D sampler3 : register(s3);
sampler2D sampler4 : register(s4);

uniform float num=1;

float4 main(float4 color : COLOR0, float2 pos : TEXCOORD0) : COLOR0{
  float a = num-floor(num);
  float thing = num+0;
  float4 v0 = tex2D(TextureSampler,pos);
  float4 t1 = tex2D(sampler1, pos);
  if(thing<1){
    return v0+t1*a*(1-v0.a);
  }
  float4 v1 = v0+t1*(1-v0.a);
  float4 t2 = tex2D(sampler2, pos);
  if(thing<2){
    return v1+t2*a*(1-v1.a);
  }
  float4 v2 = v1+t2*(1-v1.a);
  float4 t3 = tex2D(sampler3, pos);
  if(thing<3){
    return v2+t3*a*(1-v2.a);
  }
  float4 v3 = v2+t3*(1-v2.a);
  float4 t4 = tex2D(sampler4, pos);
  if(thing<4){
    return v3+t4*a*(1-v3.a);
  }
  return v3+t4*(1-v3.a);
}


technique BasicTech {
  pass Pass0 {
    PixelShader = compile ps_3_0 main();
  }
}


