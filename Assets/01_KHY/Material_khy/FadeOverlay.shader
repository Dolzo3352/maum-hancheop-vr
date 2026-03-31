Shader "Custom/FadeOverlay"
{
    Properties
    {
        _Color ("Fade Color", Color) = (0, 0, 0, 1)
    }

    SubShader
    {
        // Overlay 큐 = 모든 오브젝트보다 나중에 렌더
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Overlay"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            ZWrite Off           // Depth buffer에 쓰지 않음
            ZTest Always         // Depth 무시 → 항상 맨 위에 그려짐
            Cull Off             // 앞뒤 면 모두 렌더
            Blend SrcAlpha OneMinusSrcAlpha  // 알파 블렌딩

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
                return half4(_Color.rgb, _Color.a);
            }
            ENDHLSL
        }
    }
}
