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