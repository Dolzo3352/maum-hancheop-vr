Shader "DearBrave/RicePaddyField"
{
    Properties
    {
        [Header(Base)]
        _MainTex ("벼 텍스쳐 (없어도 OK)", 2D) = "white" {}
        _UseTexture ("텍스쳐 사용", Float) = 0
        _Color ("벼 색 1 (밝은)", Color) = (0.62, 0.52, 0.18, 1)
        _Color2 ("벼 색 2 (어두운)", Color) = (0.40, 0.38, 0.12, 1)
        _SoilColor ("흙/그림자 색", Color) = (0.30, 0.22, 0.10, 1)
        _Brightness ("밝기", Range(0.5, 1.5)) = 0.9

        [Header(Rice Row Pattern)]
        _RowScale ("이랑 밀도", Range(1, 80)) = 8.0
        _RowAngle ("이랑 각도", Range(0, 360)) = 0
        _RowSharpness ("이랑 선명도", Range(1, 10)) = 3.0
        _RowSoilWidth ("이랑 사이 흙 비율", Range(0, 0.5)) = 0.12
        _CrossRowScale ("가로줄 밀도", Range(0, 80)) = 10.0
        _CrossRowStrength ("가로줄 강도", Range(0, 1)) = 0.25

        [Header(Color Variation)]
        _ColorNoiseScale ("색상 변화 크기", Range(0.1, 10)) = 1.5
        _ColorMix ("색상 혼합 강도", Range(0, 1)) = 0.5

        [Header(Wind Animation)]
        _WindSpeed ("바람 속도", Range(0, 3)) = 0.8
        _WindStrength ("바람 세기 (UV)", Range(0, 0.1)) = 0.02
        _WindFrequency ("바람 주파수", Range(0, 5)) = 1.5
        _WindDirection ("바람 방향 (XZ)", Vector) = (1, 0, 0.5, 0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        Cull Back

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float fogFactor : TEXCOORD2;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _Color;
                half4 _Color2;
                half4 _SoilColor;
                float _UseTexture;
                float _Brightness;
                float _RowScale;
                float _RowAngle;
                float _RowSharpness;
                float _RowSoilWidth;
                float _CrossRowScale;
                float _CrossRowStrength;
                float _ColorNoiseScale;
                float _ColorMix;
                float _WindSpeed;
                float _WindStrength;
                float _WindFrequency;
                float4 _WindDirection;
            CBUFFER_END

            // Hash for noise
            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            // Smooth 2D noise
            float noise2D(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                float a = hash21(i);
                float b = hash21(i + float2(1, 0));
                float c = hash21(i + float2(0, 1));
                float d = hash21(i + float2(1, 1));

                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.fogFactor = ComputeFogFactor(posInputs.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // --- World XZ UV ---
                float2 worldXZ = input.positionWS.xz;

                // Rotate for row angle
                float rad = _RowAngle * 0.01745329;
                float cosA = cos(rad);
                float sinA = sin(rad);
                float2 rotUV;
                rotUV.x = worldXZ.x * cosA - worldXZ.y * sinA;
                rotUV.y = worldXZ.x * sinA + worldXZ.y * cosA;

                // --- Wind ---
                float2 windDir = normalize(_WindDirection.xz + float2(0.001, 0.001));
                float time = _Time.y * _WindSpeed;
                float phase = worldXZ.x * 0.5 + worldXZ.y * 0.7;
                float wave = sin(time * _WindFrequency + phase)
                           + sin(time * _WindFrequency * 0.6 + phase * 1.4) * 0.5;
                wave *= 0.67;
                rotUV += windDir * wave * _WindStrength;

                // === PROCEDURAL RICE ROW PATTERN ===

                // Main rows (vertical stripes)
                float rowVal = rotUV.x * _RowScale;
                float rowFrac = frac(rowVal);
                // Create rice stalk shape: narrow soil gaps between rows
                float rowPattern = saturate((rowFrac - _RowSoilWidth) / (1.0 - _RowSoilWidth * 2.0));
                rowPattern = pow(abs(sin(rowPattern * 3.14159)), 1.0 / _RowSharpness);

                // Cross rows (horizontal variation for individual stalk feel)
                float crossVal = rotUV.y * _CrossRowScale;
                float crossPattern = pow(abs(sin(crossVal * 3.14159)), 2.0);
                rowPattern *= lerp(1.0, 0.7 + 0.3 * crossPattern, _CrossRowStrength);

                // Small noise for organic feel
                float smallNoise = noise2D(rotUV * _RowScale * 0.5);
                rowPattern *= lerp(0.85, 1.0, smallNoise);

                // Soil between rows
                float isSoil = 1.0 - step(_RowSoilWidth, rowFrac) * step(rowFrac, 1.0 - _RowSoilWidth);

                // --- Color ---
                // Large-scale color variation
                float colorNoise = noise2D(worldXZ * _ColorNoiseScale);
                half3 riceColor = lerp(_Color.rgb, _Color2.rgb, colorNoise * _ColorMix);

                // Mix rice and soil
                half3 baseColor = lerp(riceColor, _SoilColor.rgb, isSoil * 0.7);

                // Apply row pattern darkness
                baseColor *= lerp(0.7, 1.0, rowPattern);

                // Optional texture overlay
                if (_UseTexture > 0.5)
                {
                    half4 texCol = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, rotUV * 0.1);
                    baseColor *= texCol.rgb;
                }

                // --- Lighting ---
                Light mainLight = GetMainLight();
                float3 normal = normalize(input.normalWS);
                float NdotL = saturate(dot(normal, mainLight.direction));
                float lighting = NdotL * 0.5 + 0.5; // half-lambert

                half3 finalColor = baseColor * lighting * mainLight.color.rgb * _Brightness;
                finalColor = MixFog(finalColor, input.fogFactor);

                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; };

            Varyings ShadowVert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 ShadowFrag(Varyings input) : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
