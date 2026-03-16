Shader "DearBrave/VertexColor"
{
    Properties
    {
        _Brightness ("Brightness", Range(0.5, 1.5)) = 1.0
        [Header(Wind)]
        _WindSpeed ("Wind Speed", Range(0, 3)) = 1.0
        _WindStrength ("Wind Strength", Range(0, 0.15)) = 0.04
        _WindFrequency ("Wind Frequency", Range(0, 5)) = 2.0
        _WindDirection ("Wind Direction (XZ)", Vector) = (1, 0, 0.5, 0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        Cull Off

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
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
                float3 normalWS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float fogFactor : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float _Brightness;
                float _WindSpeed;
                float _WindStrength;
                float _WindFrequency;
                float4 _WindDirection;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;

                float3 posOS = input.positionOS.xyz;

                // Height factor: y=0 is root (no movement), higher = more sway
                float heightFactor = saturate(posOS.y * 3.0);
                heightFactor *= heightFactor; // quadratic falloff - tips move most

                // World position for spatial variation (so not all stalks move in sync)
                float3 worldPos = TransformObjectToWorld(posOS);
                float phase = worldPos.x * 0.7 + worldPos.z * 0.9;

                // Wind wave
                float time = _Time.y * _WindSpeed;
                float wave = sin(time * _WindFrequency + phase)
                           + sin(time * _WindFrequency * 0.7 + phase * 1.3) * 0.5;
                wave *= 0.67; // normalize roughly to -1..1

                // Apply displacement along wind direction
                float2 windDir = normalize(_WindDirection.xz + float2(0.001, 0.001));
                float displacement = wave * _WindStrength * heightFactor;

                posOS.x += windDir.x * displacement;
                posOS.z += windDir.y * displacement;

                VertexPositionInputs posInputs = GetVertexPositionInputs(posOS);
                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.color = input.color;
                output.fogFactor = ComputeFogFactor(posInputs.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Simple lambert lighting
                Light mainLight = GetMainLight();
                float3 normal = normalize(input.normalWS);
                float NdotL = saturate(dot(normal, mainLight.direction));
                float lighting = NdotL * 0.6 + 0.4; // 0.4 ambient

                half3 color = input.color.rgb * lighting * mainLight.color.rgb * _Brightness;
                color = MixFog(color, input.fogFactor);
                return half4(color, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ColorMask 0
            Cull Off

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
