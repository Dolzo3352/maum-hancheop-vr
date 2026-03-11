// BirdRevealSphere.shader
// 카메라를 감싸는 뒤집힌 구체에 적용
// Renderer Feature 없이 동작 - Unity 6 URP 호환

Shader "Custom/BirdRevealSphere"
{
    Properties
    {
        _RevealProgress     ("Reveal Progress",   Range(0, 1))      = 0
        _BirdDir            ("Bird Direction",    Vector)           = (1, 0, 0, 0)
        _RevealEdgeSoftness ("Edge Softness",     Range(0.02, 0.3)) = 0.08
        _NoiseScale         ("Noise Scale",       Range(1, 15))     = 6.0
        _NoiseStrength      ("Noise Strength",    Range(0, 0.08))   = 0.03
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue"          = "Geometry-1"   // 스카이박스보다 먼저 그려짐
        }

        Pass
        {
            Name "RevealSpherePass"
            Cull  Front          // 구체 안쪽 면을 렌더 (뒤집힌 노멀)
            ZTest Always
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float  _RevealProgress;
            float4 _BirdDir;
            float  _RevealEdgeSoftness;
            float  _NoiseScale;
            float  _NoiseStrength;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;   // 월드 노멀로 방향 판단
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // ── 간단한 노이즈 ────────────────────────────
            float2 Hash2(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)),
                           dot(p, float2(269.5, 183.3)));
                return -1.0 + 2.0 * frac(sin(p) * 43758.5453123);
            }

            float GradNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(
                    lerp(dot(Hash2(i),              f),
                         dot(Hash2(i + float2(1,0)), f - float2(1,0)), u.x),
                    lerp(dot(Hash2(i + float2(0,1)), f - float2(0,1)),
                         dot(Hash2(i + float2(1,1)), f - float2(1,1)), u.x),
                    u.y
                );
            }

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            float4 Frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                // 구체 표면의 방향 벡터 (중심에서 바깥 방향)
                float3 sphereDir = normalize(IN.normalWS);

                // 새 이동 방향 (수평 XZ)
                float3 birdDir3D = normalize(float3(_BirdDir.x, 0, _BirdDir.z)
                                            + float3(0.0001, 0, 0));

                // 새 방향과 현재 픽셀 방향의 유사도
                float alignment = dot(sphereDir, birdDir3D);

                // 구체 표면 위치를 2D로 매핑 (노이즈용)
                float2 sphereUV = float2(
                    atan2(sphereDir.z, sphereDir.x) / (2.0 * 3.14159),
                    (sphereDir.y + 1.0) * 0.5
                );

                // 노이즈로 경계 불규칙하게
                float noise = GradNoise(sphereUV * _NoiseScale + _RevealProgress * 1.5);

                // 중심각 기반 거리 계산
                // 새 방향으로 편향된 원형 reveal
                float centerDot  = dot(sphereDir, birdDir3D) * 0.15 * _RevealProgress;
                float3 revealDir = normalize(birdDir3D * centerDot + float3(0, 1, 0) * (1.0 - centerDot));
                float  dist      = 1.0 - dot(sphereDir, revealDir) * 0.5 - 0.5;
                       dist     += noise * _NoiseStrength;

                // Reveal 반경
                // Progress 0 → 전체 검정
                // Progress 1 → 완전 열림
                float radius = (1.0 - _RevealProgress) * 1.0;

                float mask = 1.0 - smoothstep(
                    radius - _RevealEdgeSoftness,
                    radius + _RevealEdgeSoftness,
                    dist
                );

                return float4(0.0, 0.0, 0.0, mask);
            }
            ENDHLSL
        }
    }
}
