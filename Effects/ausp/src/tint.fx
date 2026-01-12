sampler2D TextureSampler : register(s0);

uniform float4 high=float4(0.5,1,1,1);
uniform float4 low=float4(0.2,0.4,0.4,1);
uniform float sat=0.2;

float4 main(float4 color : COLOR0, float2 texCoord : TEXCOORD0) : COLOR0{
    float4 orig = tex2D(TextureSampler, texCoord)*color;
    if(orig.a<0.1) return float4(0,0,0,0);
    float3 unpremult = orig.rgb/orig.a;
    float3 remapped = unpremult*(high.rgb-low.rgb)+low.rgb;
    float lum = dot(unpremult, float3(0.3,0.55,0.15));
    float3 bnw = high*lum+low*(1-lum);
    return float4(sat*remapped+(1-sat)*bnw, 1)*orig.a;
}
technique BasicTech {
    pass Pass0 {
        PixelShader = compile ps_3_0 main();
    }
}
