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

            sampler2D _gdepth;
            sampler2D _GT0;
            sampler2D _GT1;
            sampler2D _GT2;
            sampler2D _GT3;

            #define PI 3.14159265358
            // D
            float Trowbridge_Reitz_GGX(float NdotH, float a)
            {
                float a2 = a * a;
                float NdotH2 = NdotH * NdotH;

                float nom = a2;
                float denom = (NdotH2 * (a2 - 1.0) + 1.0);
                denom = PI * denom * denom;

                return nom / denom;
            }

            // F
            float3 SchlickFresnel(float HdotV, float3 F0)
            {
                float m = clamp(1 - HdotV, 0, 1);
                float m2 = m * m;
                float m5 = m2 * m2 * m; // pow(m,5)
                return F0 + (1.0 - F0) * m5;
            }

            // G
            float SchlickGGX(float NdotV, float k)
            {
                float nom = NdotV;
                float denom = NdotV * (1.0 - k) + k;

                return nom / denom;
            }

            // 直接光照
            float3 PBR(float3 N, float3 V, float3 L, float3 albedo, float3 radiance, float roughness, float metallic)
            {
                roughness = max(roughness, 0.05); // 保证光滑物体也有高光

                float3 H = normalize(L + V);
                float NdotL = max(dot(N, L), 0);
                float NdotV = max(dot(N, V), 0);
                float NdotH = max(dot(N, H), 0);
                float HdotV = max(dot(H, V), 0);
                float alpha = roughness * roughness;
                float k = ((alpha + 1) * (alpha + 1)) / 8.0;
                float3 F0 = lerp(float3(0.04, 0.04, 0.04), albedo, metallic);

                float D = Trowbridge_Reitz_GGX(NdotH, alpha);
                float3 F = SchlickFresnel(HdotV, F0);
                float G = SchlickGGX(NdotV, k) * SchlickGGX(NdotL, k);

                float3 k_s = F;
                float3 k_d = (1.0 - k_s) * (1.0 - metallic);
                float3 f_diffuse = albedo / PI;
                float3 f_specular = (D * F * G) / (4.0 * NdotV * NdotL + 0.0001);

                f_diffuse *= PI;
                f_specular *= PI;

                float3 color = (k_d * f_diffuse + f_specular) * radiance * NdotL;

                return color;
            }

            float4x4 _vpMatrix;
            float4x4 _vpMatrixInv;
            fixed4 frag(v2f i,out float depthOut:SV_Depth) : SV_Target
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
                float3 N = normalize(normal);
                float3 L = normalize(_WorldSpaceLightPos0.xyz);
                float3 V = normalize(_WorldSpaceCameraPos.xyz - worldPos.xyz);
                float3 radiance = _LightColor0.rgb;

                // 计算光照
                float3 color = PBR(N, V, L, albedo, radiance, roughness, metallic);
                color += emission;

                return float4(color, 1);
            }
            ENDCG
        }
    }
}