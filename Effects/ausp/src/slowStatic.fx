

sampler2D layerSamp : register(s0);


uniform float2 pscale;
uniform float time;
uniform float2 cpos;
uniform float4 low=float4(0,0,0,1);
uniform float4 high=float4(1,1,1,1);
uniform float speed = 1;
uniform float scale = 1;

float2 worldpos(float2 pos){
    return floor(pos/pscale+cpos);
}

////////////////// K.jpg's Smooth Re-oriented 8-Point BCC Noise //////////////////
//////////////////// Output: float4(dF/dx, dF/dy, dF/dz, value) ////////////////////

// Borrowed from Stefan Gustavson's noise code
float4 permute(float4 t) {
	return t * (t * 34.0 + 133.0);
}

float mod(float x, float y){
	return x - y * floor(x / y);
}

float2 mod(float2 x, float2 y){
	return x - y * floor(x / y);
}

float3 mod(float3 x, float3 y){
	return x - y * floor(x / y);
}

float4 mod(float4 x, float4 y){
	return x - y * floor(x / y);
}

float drand(float3 co) {
    co *= 1000.0;
		co=mod(co,219.23);
    float3 a = mod(co * float3(0.1031, 0.1030, 0.0973),1);
    a += dot(a, a.yzx + 33.33);
    return mod((a.x + a.y) * a.z,1);
}
float hash(float2 pos, float time){
  float3 p = float3(pos.x, pos.y, time) + float3(13.52, 91.24, 42.11);
  float h1 = frac(sin(dot(p, float3(12.9898, 78.233, 37.719))) * 43758.5453);
  float h2 = frac(sin(dot(p + h1, float3(39.346, 11.135, 83.155))) * 28613.1234);
  return frac(cos(dot(p + h2, float3(73.156, 52.235, 19.344))) * 35842.1645);
}

// Gradient set is a normalized expanded rhombic dodecahedron
float3 grad(float hash) {
	// Random vertex of a cube, +/- 1 each
	float3 cube = mod(floor(hash / float3(1.0, 2.0, 4.0)), 2.0) * 2.0 - 1.0;

	// Random edge of the three edges connected to that vertex
	// Also a cuboctahedral vertex
	// And corresponds to the face of its dual, the rhombic dodecahedron
	float3 cuboct = cube;

	int index = int(hash / 16.0);
	if (index == 0)
		cuboct.x = 0.0;
	else if (index == 1)
		cuboct.y = 0.0;
	else
		cuboct.z = 0.0;

	// In a funky way, pick one of the four points on the rhombic face
	float type = mod(floor(hash / 8.0), 2.0);
	float3 rhomb = (1.0 - type) * cube + type * (cuboct + cross(cube, cuboct));

	// Expand it so that the new edges are the same length
	// as the existing ones
	float3 grad = cuboct * 1.22474487139 + rhomb;

	// To make all gradients the same length, we only need to shorten the
	// second type of vector. We also put in the whole noise scale constant.
	// The compiler should reduce it into the existing floats. I think.
	grad *= (1.0 - 0.042942436724648037 * type) * 3.5946317686139184;

	return grad;
}

// BCC lattice split up into 2 cube lattices
float os2deriv(float3 X) {
	float3 b = floor(X);
	float4 i4 = float4(X - b, 2.5);

	// Pick between each pair of oppposite corners in the cube.
	float3 v1 = b + floor(dot(i4, float4(.25, .25, .25, .25)));
	float3 v2 = b + float3(1, 0, 0) + float3(-1, 1, 1) * floor(dot(i4, float4(-.25, .25, .25, .35)));
	float3 v3 = b + float3(0, 1, 0) + float3(1, -1, 1) * floor(dot(i4, float4(.25, -.25, .25, .35)));
	float3 v4 = b + float3(0, 0, 1) + float3(1, 1, -1) * floor(dot(i4, float4(.25, .25, -.25, .35)));

	// Gradient hashes for the four vertices in this half-lattice.
	float4 hashes = permute(mod(float4(v1.x, v2.x, v3.x, v4.x), 289.0));
	hashes = permute(mod(hashes + float4(v1.y, v2.y, v3.y, v4.y), 289.0));
	hashes = mod(permute(mod(hashes + float4(v1.z, v2.z, v3.z, v4.z), 289.0)), 48.0);

	// Gradient extrapolations & kernel function
	float3 d1 = X - v1; float3 d2 = X - v2; float3 d3 = X - v3; float3 d4 = X - v4;
	float4 a = max(0.75 - float4(dot(d1, d1), dot(d2, d2), dot(d3, d3), dot(d4, d4)), 0.0);
	float4 aa = a * a; float4 aaaa = aa * aa;
	float3 g1 = grad(hashes.x); float3 g2 = grad(hashes.y);
	float3 g3 = grad(hashes.z); float3 g4 = grad(hashes.w);
	float4 extrapolations = float4(dot(d1, g1), dot(d2, g2), dot(d3, g3), dot(d4, g4));

	float4x3 derivativeMatrix = { d1, d2, d3, d4 };
	float4x3 gradientMatrix = { g1, g2, g3, g4 };

	// Derivatives of the noise
	//float3 derivative = -8.0 * mul(aa * a * extrapolations, derivativeMatrix)
	//	+ mul(aaaa, gradientMatrix);

	// Return it all as a float4
	return dot(aaaa, extrapolations);
}

// Use this if you want to show X and Y in a plane, then use Z for time, vertical, etc.
float os2noderivskew(float3 X) {
	// Not a skew transform.
	float3x3 orthonormalMap = {
		0.788675134594813, -0.211324865405187, -0.577350269189626,
		-0.211324865405187, 0.788675134594813, -0.577350269189626,
		0.577350269189626, 0.577350269189626, 0.577350269189626 };

	X = mul(X, orthonormalMap);
	float4 result = os2deriv(X) + os2deriv(X + 144.5);

	return result;
}

//////////////////////////////// End noise code ////////////////////////////////



float4 main(float4 color : COLOR0, float2 pos : TEXCOORD0) : SV_Target {
	float orig = tex2D(layerSamp,pos).a;
	float2 wpos = worldpos(pos);
	
	if(orig == 0){
    return float4(0,0,0,0);
  }

	float val = os2noderivskew(float3(wpos.x*scale,wpos.y*scale,time*speed)*2)*0.7+0.5;
	return (val*high+(1-val)*low)*orig;
}

technique BasicTech {
    pass Pass0 {
        PixelShader = compile ps_3_0 main();
    }
}

