//UNITY_SHADER_NO_UPGRADE
#ifndef MYHLSLINCLUDE_INCLUDED
#define MYHLSLINCLUDE_INCLUDED

float4 hash(float2 p) {
    return frac(sin(float4(
        1.0 + dot(p, float2(37.0, 17.0)),
        2.0 + dot(p, float2(11.0, 47.0)),
        3.0 + dot(p, float2(41.0, 29.0)),
        4.0 + dot(p, float2(23.0, 31.0))
    )) * 103.0);
}

void MyFunction_float(in SamplerState samp, in Texture2D tex, in float2 uv, out float4 Out) {
    int2 iuv = int2(floor(uv));
    float2 fuv = frac(uv);

    // Generate per-tile transform
    float4 ofa = hash(iuv + int2(0, 0));
    float4 ofb = hash(iuv + int2(1, 0));
    float4 ofc = hash(iuv + int2(0, 1));
    float4 ofd = hash(iuv + int2(1, 1));

    float2 dx = ddx(uv);
    float2 dy = ddy(uv);

    // Transform per-tile uvs
    ofa.zw = sign(ofa.zw - 0.5);
    ofb.zw = sign(ofb.zw - 0.5);
    ofc.zw = sign(ofc.zw - 0.5);
    ofd.zw = sign(ofd.zw - 0.5);

    // UVs & Derivatives (for correct mipmapping)
    float2 uva = uv * ofa.zw + ofa.xy,
           ddxa = dx * ofa.zw,
           ddya = dy * ofa.zw;
    float2 uvb = uv * ofb.zw + ofb.xy,
           ddxb = dx * ofb.zw,
           ddyb = dy * ofb.zw;
    float2 uvc = uv * ofc.zw + ofc.xy,
           ddxc = dx * ofc.zw,
           ddyc = dy * ofc.zw;
    float2 uvd = uv * ofd.zw + ofd.xy,
           ddxd = dx * ofd.zw,
           ddyd = dy * ofd.zw;

    // Fetch and blend
    float2 b = smoothstep(0.25, 0.75, fuv);

    // we need to change tex to be a real Texture2D, then use the equivalent of this to make it work
    // in unity, thats the only way

    Out = lerp(lerp(tex.SampleGrad(samp, uva, ddxa, ddya),
                    tex.SampleGrad(samp, uvb, ddxb, ddyb),
                    b.x),
               lerp(tex.SampleGrad(samp, uvc, ddxc, ddyc),
                    tex.SampleGrad(samp, uvd, ddxd, ddyd),
                    b.x),
               b.y);
}

void MyFunctionTriplanar_float(in SamplerState samp, in Texture2D tex, in float3 position, in float3 normal, in float tile, in float blend, out float3 Out) {
    float3 masterUV = position * tile;
    float3 masterBlend = pow(abs(normal), blend);
    masterBlend /= dot(masterBlend, 1.0);

    float2 curUV[3] = { masterUV.zy, masterUV.xz, masterUV.xy };
    float3 outs[3] = { float3(0, 0, 0), float3(0, 0, 0), float3(0, 0, 0) };

    Out = float4(0.0, 0.0, 0.0, 0.0);
    float2 uv = float2(0.0, 0.0);

    int i = 0;
    for (i = 0; i < 3; i++) {
        uv = curUV[i];
        int2 iuv = int2(floor(uv));
        float2 fuv = frac(uv);

        // Generate per-tile transform
        float4 ofa = hash(iuv + int2(0, 0));
        float4 ofb = hash(iuv + int2(1, 0));
        float4 ofc = hash(iuv + int2(0, 1));
        float4 ofd = hash(iuv + int2(1, 1));

        float2 dx = ddx(uv);
        float2 dy = ddy(uv);

        // Transform per-tile uvsd
        ofa.zw = sign(ofa.zw - 0.5);
        ofb.zw = sign(ofb.zw - 0.5);
        ofc.zw = sign(ofc.zw - 0.5);
        ofd.zw = sign(ofd.zw - 0.5);

        // UVs & Derivatives (for correct mipmapping)
        float2 uva = uv * ofa.zw + ofa.xy,
            ddxa = dx * ofa.zw,
            ddya = dy * ofa.zw;
        float2 uvb = uv * ofb.zw + ofb.xy,
            ddxb = dx * ofb.zw,
            ddyb = dy * ofb.zw;
        float2 uvc = uv * ofc.zw + ofc.xy,
            ddxc = dx * ofc.zw,
            ddyc = dy * ofc.zw;
        float2 uvd = uv * ofd.zw + ofd.xy,
            ddxd = dx * ofd.zw,
            ddyd = dy * ofd.zw;

        // Fetch and blend
        float2 b = smoothstep(0.25, 0.75, fuv);

        outs[i] = lerp(lerp(tex.SampleGrad(samp, uva, ddxa, ddya),
                            tex.SampleGrad(samp, uvb, ddxb, ddyb),
                            b.x),
                       lerp(tex.SampleGrad(samp, uvc, ddxc, ddyc),
                            tex.SampleGrad(samp, uvd, ddxd, ddyd),
                            b.x),
                       b.y);
    }

    
    Out = outs[0] * masterBlend.x + outs[1] * masterBlend.y + outs[2] * masterBlend.z;
}


void MyFunctionSuperTriplanar_float(in SamplerState samp, in Texture2D tex, in float3 position, in float3 normal, in float tile, in float blend, out float3 Out) {
    float3 masterUV = position * tile;
    float3 masterBlend = pow(abs(normal), blend);
    masterBlend /= dot(masterBlend, 1.0);

    float2 curUV[3] = { masterUV.zy, masterUV.xz, masterUV.xy };
    float3 outs[3] = { float3(0, 0, 0), float3(0, 0, 0), float3(0, 0, 0) };

    Out = float4(0.0, 0.0, 0.0, 0.0);
    float2 uv = float2(0.0, 0.0);

    int i = 0;
    for (i = 0; i < 3; i++) {
        uv = curUV[i];
        
        float2 p = floor(uv);
        float2 f = frac(uv);

        float2 dx = ddx(uv);
        float2 dy = ddy(uv);


        float4 va = float4(0.0, 0.0, 0.0, 0.0);
        float wt = 0.0;
        for (int j = -1; j <= 1; j++) {
            for (int k = -1; k <= 1; k++) {
                float2 g = float2(float(k), float(j));
                float4 o = hash(p + g);
                float2 r = g - f + o.xy;
                float d = dot(r, r);
                float w = exp(-5.0 * d);
                float4 c = tex.SampleGrad(samp, uv + o.zw, dx, dy);
                va += w * c;
                wt += w;
            }
        }

        outs[i] = va / wt;
    }


    Out = outs[0] * masterBlend.x + outs[1] * masterBlend.y + outs[2] * masterBlend.z;
}


float customSum(float3 v) { return v.x + v.y + v.z; }

void MyFunctionHyperTriplanar_float(in SamplerState samp, in Texture2D tex, in Texture2D noiseTex, in float3 position, in float3 normal, in float tile, in float noiseTile, in float3 offset, in float3 noiseOffset, in float blend, out float3 Out) {
    float3 masterUV = position * tile;
    float3 masterBlend = pow(abs(normal), blend);
    masterBlend /= dot(masterBlend, 1.0);

    float2 curUV[3] = { masterUV.zy, masterUV.xz, masterUV.xy };
    float2 curOffset[3] = { offset.zy, offset.xz, offset.xy };
    float2 curNoiseOffset[3] = { noiseOffset.zy, noiseOffset.xz, noiseOffset.xy };

    float3 outs[3] = { float3(0, 0, 0), float3(0, 0, 0), float3(0, 0, 0) };

    Out = float4(0.0, 0.0, 0.0, 0.0);
    float2 uv = float2(0.0, 0.0);

    int e = 0;
    for (e = 0; e < 3; e++) {
        uv = curUV[e];

        float2 x = uv * 1; //alternate tiling location

        float k = noiseTex.Sample(samp, 0.005 * x * noiseTile + curNoiseOffset[e]).x;

        float l = k * 8.0;
        float f = frac(l);

        /*float ia = floor(l);
        float ib = ia + 1.0;*/

        float ia = floor(l + 0.5);
        float ib = floor(l);
        f = min(f, 1.0 - f) * 2.0;

        float2 offa = sin(float2(3.0, 7.0) * ia);
        float2 offb = sin(float2(3.0, 7.0) * ib);

        float2 dx = ddx(x);
        float2 dy = ddy(x);

        float3 cola = tex.SampleGrad(samp, x + offa + curOffset[e], dx, dy).xyz;
        float3 colb = tex.SampleGrad(samp, x + offb + curOffset[e], dx, dy).xyz;

        outs[e] = lerp(cola, colb, smoothstep(0.2, 0.8, f - 0.1 * customSum(cola - colb)));
    }


    Out = outs[0] * masterBlend.x + outs[1] * masterBlend.y + outs[2] * masterBlend.z;
}

#endif //MYHLSLINCLUDE_INCLUDED