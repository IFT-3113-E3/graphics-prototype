Shader "Effects/SwordSlash"
{
    Properties
    {
        _P0 ("P0", Vector) = (0, 0, 0)
        _P1 ("P1", Vector) = (0, 0, 0)
        _P2 ("P2", Vector) = (0, 0, 0)
        _P3 ("P3", Vector) = (0, 0, 0)
        _Progress ("Progress", Float) = 0.0

        // base color
        _BaseColor("Base Color", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalRenderPipeline"
        }
        Pass
        {
            Name "SWORD SLASH"

            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off

            HLSLPROGRAM
            #pragma vertex  vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"


            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            float3 _P0;
            float3 _P1;
            float3 _P2;
            float3 _P3;

            float4 _BaseColor;

            float _Progress;


            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            float _Bands = 5;
            float _GlowStrength = 1.0;

            float rand(float2 co)
            {
                return frac(sin(dot(co, float2(12.9898, 78.233))) * 43758.5453);
            }

            float computeBandProgress(float globalT, float bandIndex, float totalBands, float delayStrength) {
                float normalizedBand = bandIndex / (totalBands - 1); // 0 (inner) to 1 (outer)
                float delay = normalizedBand * delayStrength;        // delay offset
                float adjustedT = saturate((globalT - delay) / (1.0f - delay));
                return adjustedT;
            }

            float ComputeBandProgress(float globalT, float bandIndex, float totalBands, float easeStrength) {
                float normalizedBand = bandIndex / (totalBands - 1); // 0 (inner) to 1 (outer)
                
                // Ease with exponent: higher = more delay, but still starts at 0
                float exponent = lerp(1.0f, easeStrength, normalizedBand);
                
                return pow(globalT, exponent); // starts at 0, ends at 1
            }
            
            float4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;

                float _Frames = 4;
                float _Bands = 8*3;
                float _Shades = 3;
                float _GlowStrength = 4.0;
                float _MinBrightness = 0.2;
                float _DelayStrength = 0.1;
                
                float _MaxFragments = 3;

                float band = floor(uv.y * _Bands);
                float shade = floor(uv.y * _Shades);

                // // Band has already disappeared
                // if (band / _Bands < _Progress)
                //     discard;

                // Band is in the process of disappearing → full strip before fragmenting
                // if (band == _Frame)
                    // return float4(_BaseColor.rgb * _GlowStrength, _BaseColor.a);

                float shadeRatio = shade / (_Shades - 1.0);
                float brightness = lerp(_MinBrightness, 1.0, shadeRatio);
                float3 color = _BaseColor.rgb * brightness * _GlowStrength;

                float _Frame = floor(_Progress * (_Frames + 1.0)) / _Frames;
                
                // === Ordered Fragment Generation ===
                float bandSeed = rand(float2(band, 2.71828));
                // band progresses overlap with each other so that they are not strictly one after the other
                float bandProgress = computeBandProgress(_Frame, band, _Bands, 0.5);
                // return bandProgress;
                float numFragments = 1 + floor(bandSeed * _MaxFragments);
                float base = 1.0;
                float cursor = base;
                bool visible = false;
                float lastSize = 1- bandProgress;
                for (int i = 0; i < numFragments; i++)
                {
                    float r = rand(float2(bandSeed, i * 17.13));
                    float size = lastSize * (1.0 - (float)i / numFragments); // longer to shorter
                    // randomize a little, still ordered
                    size *= (1-bandProgress) + r * bandProgress;
                    lastSize = size;

                    float start = cursor - size;
                    float end = cursor;

                    if (uv.x >= start && uv.x <= end)
                    {
                        visible = true;
                        break;
                    }

                    cursor = start - r*(1/(lastSize)) * 0.01; // gap between fragments
                }

                if (!visible)
                    discard;

                return float4(color, _BaseColor.a * 0.96);
            }
            ENDHLSL
        }
    }
}