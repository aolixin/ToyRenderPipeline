Shader "ToyRP/LitPass"
{
    Properties
    {
        _BaseMap ("Texture", 2D) = "white" {}
        _BaseColor("Color", Color) = (1.0, 1.0, 1.0, 1.0)

        _Cutoff ("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        [Toggle(_CLIPPING)] _Clipping ("Alpha Clipping", Float) = 0
        _Metallic ("Metallic", Range(0, 1)) = 0
		_Smoothness ("Smoothness", Range(0, 1)) = 0.5
        [Toggle(_PREMULTIPLY_ALPHA)] _PremulAlpha ("Premultiply Alpha", Float) = 0
        
    }
    SubShader
    {
        Tags { "RenderType"="Opaque"
              "LightMode" = "ToyRPLit"
               }

        LOD 100

        Pass
        {
            HLSLPROGRAM

            #pragma target 3.5
            #pragma shader_feature _CLIPPING
            #pragma shader_feature _PREMULTIPLY_ALPHA
            #pragma multi_compile _ _DIRECTIONAL_PCF3 _DIRECTIONAL_PCF5 _DIRECTIONAL_PCF7
            #pragma multi_compile_instancing
			#pragma vertex LitPassVertex
			#pragma fragment LitPassFragment

            #include "LitPass.hlsl"

			ENDHLSL
        }



		Pass {


			Tags {
				"LightMode" = "ShadowCaster"
			}

			ColorMask 0

			HLSLPROGRAM
			#pragma target 3.5
			#pragma shader_feature _CLIPPING
			#pragma multi_compile_instancing
			#pragma vertex ShadowCasterPassVertex
			#pragma fragment ShadowCasterPassFragment
			#include "ShadowCasterPass.hlsl"
			ENDHLSL
		}
    }

    CustomEditor "CustomShaderGUI"
}
