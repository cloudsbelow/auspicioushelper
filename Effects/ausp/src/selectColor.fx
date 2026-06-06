

sampler2D TextureSampler : register(s0);

uniform float4 color=float4(0.5,1,1,1);
uniform float4 trueColor=float4(1,1,1,1);
uniform float4 falseColor=float4(0,0,0,0);
uniform float range=0.01;

float4 main(float4 bcol : COLOR0, float2 texCoord : TEXCOORD0) : COLOR0{
    float4 orig = tex2D(TextureSampler, texCoord)*bcol;
    float4 delta = orig-color;
    if(abs(delta.r)>range || abs(delta.g)>range || abs(delta.a)>range){
        return falseColor*orig.a;
    } else {
        return trueColor*orig.a;
    }
}
technique BasicTech {
    pass Pass0 {
        PixelShader = compile ps_3_0 main();
    }
}
