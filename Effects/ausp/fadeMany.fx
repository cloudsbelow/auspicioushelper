

sampler2D sampler1 : register(s1);
sampler2D sampler2 : register(s2);
sampler2D sampler3 : register(s3);
sampler2D sampler4 : register(s4);

uniform float choose;

float4 main(float4 color : COLOR0, float2 pos : TEXCOORD0) : COLOR0{
  float a = choose-floor(choose);
  float ia = 1-a;
  float4 t1 = tex2D(sampler1, pos);
  float4 t2 = tex2D(sampler2, pos);
  float4 t3 = tex2D(sampler3, pos);
  float4 t4 = tex2D(sampler4, pos);
  float thing = choose+0;
  if(thing<1){
    return t1*ia+t2*a;
  }
  if(choose<2){
    return t2*ia+t3*a;
  }
  if(choose<3){
    return t3*ia+t4*a;
  }
  return t4;
}


technique BasicTech {
  pass Pass0 {
    PixelShader = compile ps_3_0 main();
  }
}


