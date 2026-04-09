
sampler2D TextureSampler : register(s0);


uniform float2 pscale;
uniform float4 color = float4(1,1,1,1);

float4 orig(float2 pos, float offsetx, float offsety){
    return tex2D(TextureSampler,pos+float2(offsetx,offsety)*pscale);
}


float4 main(float4 inTint : COLOR0, float2 pos : TEXCOORD0) : SV_Target {
	float4 o = orig(pos,0,0)*inTint;
  int num = 0;
  if(orig(pos,1,0).a<0.5){num = num+1;}
  if(orig(pos,0,1).a<0.5){num = num+1;}
  if(orig(pos,-1,0).a<0.5){num = num+1;}
  if(orig(pos,0,-1).a<0.5){num = num+1;}
  if(num>=2){
    return float4(0,0,0,0);
  }
  return o;
}

technique BasicTech {
    pass Pass0 {
        PixelShader = compile ps_3_0 main();
    }
}
