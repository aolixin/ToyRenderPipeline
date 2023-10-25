Shader "Unlit/UnlitPassTransparent"
{
    Properties
    {
        _BaseMap ("Texture", 2D) = "white" {}
        _BaseColor("Color", Color) = (1.0, 1.0, 1.0, 1.0)
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 1
		[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 0
        [Enum(Off, 0, On, 1)] _ZWrite ("Z Write", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" }
        LOD 100

        Pass
        {
            // 透明度混合模式 
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            HLSLPROGRAM

            #pragma multi_compile_instancing
			#pragma vertex UnlitPassVertex
			#pragma fragment UnlitPassFragment

            #include "UnlitPassTransparent.hlsl"

			ENDHLSL
        }
    }
}
