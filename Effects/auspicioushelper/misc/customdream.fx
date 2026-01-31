

sampler2D TextureSampler : register(s0);

uniform float4 color0;
uniform float4 color1;
uniform float4 color2;
uniform float4 color3;
uniform float4 color4;
uniform float4 color5;

uniform float4 edge;
uniform float4 inside;

uniform float3 density;
uniform float thru;

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



float4 main(float4 color : COLOR0, float2 pos : TEXCOORD0) : SV_Target {
	float4 mask = orig(pos,0,0);
	float2 wpos = worldpos(pos);
  if(mask.a<0.5){
    return float4(0,0,0,0);
  }
  //normal edge stuff
  if(orig(pos,1,0).a*orig(pos,-1,0).a*orig(pos,0,1).a*orig(pos,0,-1).a<0.5){
    return edge;
  }
  if(true){
    //for largest stars
    //calculate cell and offset in cell
    float2 hpos = floor(pos/pscale+cpos*0.65)/10;
    float2 hcell = floor(hpos);
    float2 hoffset = (hpos-hcell)*10;
    //calculate the location of the star in the cell and l1 distance to it
    float2 seed = floor(float2(2+6*hash(hcell,0),2+6*hash(hcell,1)));
    float2 del = hoffset-seed;
    float l1 = abs(del.x)+abs(del.y);
    //calculate a hash used to decide whether to make the star and what color to give it
    float chash = hash(seed,2);
    float fchash = floor(chash*16);
    //if you wanted to replace this with an arbitrary sprite, you'd want to replace the abs(...) check
    //with whether your sample point lies inside the sprite. So you'd want to sample from the center of the sprite on your sheet
    //at an offset of 'del' which is the offset from the center of the star in the cell. If the sample lies in a positive-alpha
    //region of the sprite, that means it should be drawn. In essence, the below would become
    // if(tex2D(spriteSheet,spriteCenter+del).a>0.5 && fchash<3*density.x){...}
    //one note is that del is in pixels, so ofc you need to divide by your spritesheet size
    if(abs(l1-sin(time*10+chash*6.283*16/3)*0.75-0.5)<1 && fchash<3*density.x){
      //pick a shared color from the palette
      return saturate(1-fchash)*color0+saturate(1-abs(fchash-1))*color1+saturate(fchash-1)*color2;
    }
  }
  if(true){
    //the same exact thing with slightly tweaked numbers for smaller stars
    float2 hpos = floor(pos/pscale+cpos*0.32)/7;
    float2 hcell = floor(hpos);
    float2 hoffset = (hpos-hcell)*7;
    float2 seed = floor(float2(1+5*hash(hcell,3),1+5*hash(hcell,4)));
    float2 del = hoffset-seed;
    float l1 = abs(del.x)+abs(del.y);
    float chash = hash(seed,5);
    float fchash = floor(chash*25);
    if(l1<sin(time*10+chash*6.283*35/3)*0.75+1 && fchash<3*density.y){
      return saturate(1-fchash)*color3+saturate(1-abs(fchash-1))*color5+saturate(fchash-1)*color4;
    }
  }
  if(true){
    //for tiny stars that are just pixels
    float2 hpos = floor(pos/pscale+cpos*0.17);
    float fchash = floor(hash(hpos,6)*3);
    //we use the minimum of multiple hashes instead of just one to improve the uniformity
    float item = max(max(hash(hpos,7),hash(hpos+float2(100,100),8)),hash(hpos+float2(200,300),9));
    if(item<0.15*pow(density.z,1/3)){
      return saturate(1-fchash)*color0+saturate(1-abs(fchash-1))*color1+saturate(fchash-1)*color4;
    }
  }
  return inside*(1-thru)+mask*thru;
}

technique BasicTech {
    pass Pass0 {
        PixelShader = compile ps_3_0 main();
    }
}
