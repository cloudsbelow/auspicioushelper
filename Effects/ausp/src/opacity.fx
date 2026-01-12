sampler2D TextureSampler : register(s0);

uniform float opacity=0.5;

float4 main(float4 color : COLOR0, float2 texCoord : TEXCOORD0) : COLOR0{
    float4 orig = tex2D(TextureSampler, texCoord)*color;
    return orig*opacity;
}
technique BasicTech {
    pass Pass0 {
        PixelShader = compile ps_3_0 main();
    }
}
