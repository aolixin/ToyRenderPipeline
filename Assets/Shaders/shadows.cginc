float ShadowMap01(float4 worldPos, sampler2D _shadowtex, float4x4 _shadowVpMatrix)
{
    float4 shadowNdc = mul(_shadowVpMatrix, worldPos);
    shadowNdc /= shadowNdc.w;
    float2 uv = shadowNdc.xy * 0.5 + 0.5;

    if(uv.x<0 || uv.x>1 || uv.y<0 || uv.y>1) return 1.0f;

    float d = shadowNdc.z;
    float d_sample = tex2D(_shadowtex, uv).r;

    #if defined (UNITY_REVERSED_Z)
    if(d_sample>d) return 0.0f;
    #else
    if(d_sample<d) return 0.0f;
    #endif

    return 1.0f;
}

float PCF3x3(float4 worldPos, sampler2D _shadowtex, float4x4 _shadowVpMatrix, float shadowMapResolution, float bias)
{
    float4 shadowNdc = mul(_shadowVpMatrix, worldPos);
    shadowNdc /= shadowNdc.w;
    float2 uv = shadowNdc.xy * 0.5 + 0.5;

    //if(uv.x<0 || uv.x>1 || uv.y<0 || uv.y>1) return 1.0f;

    float d_shadingPoint = shadowNdc.z;
    float shadow = 0.0;

    for(int i=-1; i<=1; i++)
    {
        for(int j=-1; j<=1; j++)
        {
            float2 offset = float2(i, j) / shadowMapResolution;
            float d_sample = tex2D(_shadowtex, uv+offset).r;

            #if defined (UNITY_REVERSED_Z)
            if(d_sample-bias>d_shadingPoint)
                #else
            if(d_sample<d_shadingPoint)
                #endif
            shadow += 1.0;
        }
    }

    return 1.0 - (shadow / 9.0);
}