sampler2D TextureSampler : register(s0);

uniform float4 lerpcol;
uniform float red;
uniform float green;
uniform float blue;

float4 main(float4 color : COLOR0, float2 texCoord : TEXCOORD0) : COLOR0{
    float4 orig = tex2D(TextureSampler, texCoord)*color;
    float3 mask = float3(red, green, blue);
    return float4(lerpcol.rgb*mask+orig*(1-mask),orig.a);
}
technique BasicTech {
    pass Pass0 {
        PixelShader = compile ps_3_0 main();
    }
}
