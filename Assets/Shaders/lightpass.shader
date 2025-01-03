Shader "ToyRP/lightpass"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Cull Off ZWrite On ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "BRDF.cginc"
            #include "globaluniform.cginc"
            #include "shadows.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }


            #define PI 3.14159265358
            float4x4 _vpMatrix;
            float4x4 _vpMatrixInv;
            fixed4 frag(v2f i, out float depthOut:SV_Depth) : SV_Target
            {
                float2 uv = i.uv;
                float4 GT2 = tex2D(_GT2, uv);
                float4 GT3 = tex2D(_GT3, uv);

                // 从 Gbuffer 解码数据
                float3 albedo = tex2D(_GT0, uv).rgb;
                float3 normal = tex2D(_GT1, uv).rgb * 2 - 1;
                float2 motionVec = GT2.rg;
                float roughness = GT2.b;
                float metallic = GT2.a;
                float3 emission = GT3.rgb;
                float occlusion = GT3.a;

                float d = UNITY_SAMPLE_DEPTH(tex2D(_gdepth, uv));
                float d_lin = Linear01Depth(d);
                depthOut = d;

                float4 ndcPos = float4(uv * 2 - 1, d, 1);
                float4 worldPos = mul(_vpMatrixInv, ndcPos);
                worldPos /= worldPos.w;

                // 计算参数
                float3 color = float3(0, 0, 0);
                float3 N = normalize(normal);
                float3 L = normalize(_WorldSpaceLightPos0.xyz);
                float3 V = normalize(_WorldSpaceCameraPos.xyz - worldPos.xyz);
                float3 radiance = _LightColor0.rgb;

                // 计算光照
                float3 direct = PBR(N, V, L, albedo, radiance, roughness, metallic);

                // 向着法线偏移采样点
                float4 worldPosOffset = worldPos;
                worldPosOffset.xyz += normal * 0.01;

                float shadow = 1.0;
                if (d_lin < _split0)
                    shadow *= ShadowMap01(worldPosOffset, _shadowtex0, _shadowVpMatrix0);
                    // shadow *= PCF3x3(worldPosOffset, _shadowtex0, _shadowVpMatrix0,_shadowMapResolution,0);
                else if (d_lin < _split0 + _split1)
                    shadow *= ShadowMap01(worldPosOffset, _shadowtex1, _shadowVpMatrix1);
                    // shadow *= PCF3x3(worldPosOffset, _shadowtex1, _shadowVpMatrix1,_shadowMapResolution,0);
                else if (d_lin < _split0 + _split1 + _split2)
                    shadow *=  ShadowMap01(worldPosOffset, _shadowtex2, _shadowVpMatrix2);
                    // shadow *=  PCF3x3(worldPosOffset, _shadowtex2, _shadowVpMatrix2,_shadowMapResolution,0);
                else if (d_lin < _split0 + _split1 + _split2 + _split3)
                    shadow *=  ShadowMap01(worldPosOffset, _shadowtex3, _shadowVpMatrix3);
                    // shadow *=  PCF3x3(worldPosOffset, _shadowtex3, _shadowVpMatrix3,_shadowMapResolution,0);

                // 计算环境光照
                float3 ambient = IBL(
                    N, V,
                    albedo, roughness, metallic,
                    _diffuseIBL, _specularIBL, _brdfLut
                );

                color += ambient * occlusion;
                color += direct * shadow;
                color += emission;

                return float4(color, 1);
            }
            ENDCG
        }
    }
}