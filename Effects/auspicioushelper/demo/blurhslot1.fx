

sampler2D TextureSampler : register(s0);
texture2D materialTex : register(t1);
sampler2D materialSamp : register(s1);


uniform float2 pscale;
uniform float time;

float4 valAt(float2 pos, float offsetx, float offsety){
    return tex2D(layerSamp,pos+float2(offsetx,offsety)*pscale);
}