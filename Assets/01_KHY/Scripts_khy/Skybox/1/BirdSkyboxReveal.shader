// BirdSkyboxReveal.shader
// URP Fullscreen Blit 용 셰이더
// 새가 날아가는 방향으로 검은 마스크가 열리는 효과
//
// [사용법]
// BirdRevealRendererFeature의 overrideMaterial에 이 셰이더로 만든 Material 할당

Shader "Custom/BirdSkyboxReveal"
{
    Properties
    {
        // 코드에서 Shader.SetGlobal로 제어 (Inspector 직접 수정 불필요)
        _RevealProgress      ("Reveal Progress",    Range(0, 1)) = 0
        _BirdWorldPos        ("Bird World Pos",     Vector)      = (0, 0, 0, 0)
        _BirdDir             ("Bird Direction",     Vector)      = (1, 0, 0, 0)
        _RevealEdgeSoftness  ("Edge Softness",      Range(0.01, 0.3)) = 0.08
        _NoiseScale          ("Noise Scale",        Range(1, 20))     = 8.0
        _NoiseStrength       ("Noise Strength",     Range(0, 0.15))   = 0.05
    }

    SubShader
    {
        Tags
        {
            "RenderType"  = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Overlay"
        }

        Pass
        {
            Name "BirdRevealPass"
            ZTest  Always
            ZWrite Off
            Cull   Off
            Blend  SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex   Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // ── Shader Global 변수 ──────────────────────
            float  _RevealProgress;
            float4 _BirdWorldPos;
            float4 _BirdDir;
            float  _RevealEdgeSoftness;
            float  _NoiseScale;
            float  _NoiseStrength;

            // ── 구조체 ──────────────────────────────────
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
            };

            // ── 간단한 2D 노이즈 (경계 불규칙하게) ──────
            float2 Hash2(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)),
                           dot(p, float2(269.5, 183.3)));
                return -1.0 + 2.0 * frac(sin(p) * 43758.5453123);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f); // smoothstep

                return lerp(
                    lerp(dot(Hash2(i + float2(0,0)), f - float2(0,0)),
                         dot(Hash2(i + float2(1,0)), f - float2(1,0)), u.x),
                    lerp(dot(Hash2(i + float2(0,1)), f - float2(0,1)),
                         dot(Hash2(i + float2(1,1)), f - float2(1,1)), u.x),
                    u.y
                );
            }

            // ── Vertex ──────────────────────────────────
            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            // ── Fragment ────────────────────────────────
            float4 Frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;
                float2 center = float2(0.5, 0.5);

                // ── 새 이동 방향을 스크린 공간으로 투영 ──
                // XZ 방향만 사용 (수평 이동 기반)
                float2 birdScreenDir = normalize(_BirdDir.xz + float2(0.0001, 0.0001));

                // ── Reveal 중심: 새 방향으로 살짝 편향 ──
                float2 revealCenter = center + birdScreenDir * 0.15 * _RevealProgress;

                // ── 화면 비율 보정 ──────────────────────
                float aspectRatio = _ScreenParams.x / _ScreenParams.y;
                float2 diff = uv - revealCenter;
                diff.x *= aspectRatio;

                // ── 새 방향으로 살짝 늘어진 타원 ─────────
                float alongBird = dot(diff, birdScreenDir * float2(aspectRatio, 1.0));
                float perpBird  = length(diff - alongBird * birdScreenDir * float2(1.0 / aspectRatio, 1.0));

                // 새 방향으로 길쭉하게 (타원형 reveal)
                float dist = length(float2(alongBird * 0.7, perpBird * 1.2));

                // ── 노이즈로 경계 불규칙하게 (붓질 느낌) ─
                float noise = ValueNoise(uv * _NoiseScale + _RevealProgress * 2.0);
                dist += noise * _NoiseStrength;

                // ── Reveal 반경 계산 ─────────────────────
                // Progress 0 → 원이 화면 전체 덮음(검정)
                // Progress 1 → 원이 사라짐(스카이박스 완전 노출)
                float revealRadius = (1.0 - _RevealProgress) * 1.2;

                // ── 마스크: 원 안은 투명, 밖은 검정 ─────
                float mask = 1.0 - smoothstep(
                    revealRadius - _RevealEdgeSoftness,
                    revealRadius + _RevealEdgeSoftness,
                    dist
                );

                // mask = 1 → 검정 불투명 / mask = 0 → 투명(스카이박스 보임)
                return float4(0.0, 0.0, 0.0, mask);
            }

            ENDHLSL
        }
    }
}
