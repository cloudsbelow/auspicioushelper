

sampler2D TextureSampler : register(s0);

uniform float2 pscale;
uniform float time;
uniform float2 cpos;

float4 orig(float2 pos, float offsetx, float offsety){
    return tex2D(TextureSampler,pos+float2(offsetx,offsety)*pscale);
}
float2 worldpos(float2 pos){
    return floor(pos/pscale+cpos);
}
float hash(float3 p) {
  return frac(sin(dot(p, float3(12.9898, 78.233, 45.164))) * 43758.5453);
}
float hash(float2 p, float o){
  return frac(sin(dot(float3(p,o), float3(12.9898, 78.233, 45.164))) * 43758.5453);
}

const float4 color0 = float4(0.95,0.4,0.4,1);
const float4 color1 = float4(0.7,0.3,1,1);
const float4 color2 = float4(0.2,0.8,0.3,1);
const float4 color3 = float4(0,0.2,1,1);
const float4 color4 = float4(0.9,0.75,0,1);
const float4 color5 = float4(0.2,0.8,1,1);

float4 main(float4 color : COLOR0, float2 pos : TEXCOORD0) : SV_Target {
	float4 mask = orig(pos,0,0);
	float2 wpos = worldpos(pos);
  if(mask.a<0.5){
    return float4(0,0,0,0);
  }
  if(orig(pos,1,0).a*orig(pos,-1,0).a*orig(pos,0,1).a*orig(pos,0,-1).a<0.5){
    return float4(1,1,1,1);
  }
  if(true){
    float2 hpos = floor(pos/pscale+cpos*0.65)/10;
    float2 hcell = floor(hpos);
    float2 hoffset = (hpos-hcell)*10;
    float2 seed = floor(float2(2+6*hash(hcell,0),2+6*hash(hcell,1)));
    float2 del = hoffset-seed;
    float l1 = abs(del.x)+abs(del.y);
    float chash = hash(seed,2);
    float fchash = floor(chash*16);
    if(abs(l1-sin(time*10+chash*6.283*16/3)*0.75-0.5)<1 && fchash<3){
      return saturate(1-fchash)*color0+saturate(1-abs(fchash-1))*color1+saturate(fchash-1)*color2;
    }
  }
  if(true){
    float2 hpos = floor(pos/pscale+cpos*0.32)/7;
    float2 hcell = floor(hpos);
    float2 hoffset = (hpos-hcell)*7;
    float2 seed = floor(float2(1+5*hash(hcell,3),1+5*hash(hcell,4)));
    float2 del = hoffset-seed;
    float l1 = abs(del.x)+abs(del.y);
    float chash = hash(seed,5);
    float fchash = floor(chash*25);
    if(l1<sin(time*10+chash*6.283*35/3)*0.75+1 && fchash<3){
      return saturate(1-fchash)*color3+saturate(1-abs(fchash-1))*color5+saturate(fchash-1)*color4;
    }
  }
  if(true){
    float2 hpos = floor(pos/pscale+cpos*0.17);
    float fchash = floor(hash(hpos,6)*3);
    float item = max(max(hash(hpos,7),hash(hpos+float2(100,100),8)),hash(hpos+float2(200,300),9));
    if(item<0.15){
      return saturate(1-fchash)*color0+saturate(1-abs(fchash-1))*color1+saturate(fchash-1)*color4;
    }
  }
  return float4(mask.rgb*0.15,1);
}

technique BasicTech {
    pass Pass0 {
        PixelShader = compile ps_3_0 main();
    }
}
