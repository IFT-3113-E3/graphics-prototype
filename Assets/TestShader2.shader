Shader "Custom/ToonURP"
{
    Properties
    {
        _Color("Color", Color) = (1,1,1,1)
        _MainTex("Main Texture", 2D) = "white" {}
        
        [HDR]
        _AmbientColor("Ambient Color", Color) = (0.4,0.4,0.4,1)
        [HDR]
        _SpecularColor("Specular Color", Color) = (0.9,0.9,0.9,1)
        _Glossiness("Glossiness", Float) = 32

        [HDR]
        _RimColor("Rim Color", Color) = (1,1,1,1)
        _RimAmount("Rim Amount", Range(0, 1)) = 0.716
        _RimThreshold("Rim Threshold", Range(0, 1)) = 0.1        
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalRenderPipeline" "Queue" = "Geometry" "RenderType" = "Opaque" }
        Pass
        {
            Name "ToonShadingPass"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float2 uv          : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float3 viewDirWS   : TEXCOORD2;
                float4 positionCS  : SV_POSITION;
                float3 positionWS  : TEXCOORD3;
                float4 shadowCoord : TEXCOORD4;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;

            float4 _Color;
            float4 _AmbientColor;
            float4 _SpecularColor;
            float _Glossiness;
            float4 _RimColor;
            float _RimAmount;
            float _RimThreshold;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(IN.normalOS);

                OUT.positionCS = posInputs.positionCS;
                OUT.positionWS = posInputs.positionWS;
                OUT.normalWS = normalInputs.normalWS;
                OUT.viewDirWS = normalize(_WorldSpaceCameraPos - posInputs.positionWS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.shadowCoord = TransformWorldToShadowCoord(posInputs.positionWS);

                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                // Normalize vectors
                float3 normalWS = normalize(IN.normalWS);
                float3 viewDir = normalize(IN.viewDirWS);

                // Fetch main directional light
                Light mainLight = GetMainLight(IN.shadowCoord);
                float3 lightDir = normalize(mainLight.direction);
                float NdotL = dot(normalWS, lightDir);

                // Compute toon lighting
                float shadow = mainLight.shadowAttenuation;
                float lightIntensity = smoothstep(0, 0.01, NdotL * shadow);
                float4 light = float4(mainLight.color, 1.0) * lightIntensity;
                
                // Specular reflection (Blinn-Phong)
                float3 halfVector = normalize(lightDir + viewDir);
                float NdotH = dot(normalWS, halfVector);
                float specularIntensity = pow(NdotH * lightIntensity, _Glossiness * _Glossiness);
                float specularSmooth = smoothstep(0.005, 0.01, specularIntensity);
                float4 specular = specularSmooth * _SpecularColor;

                // Rim lighting effect
                float rimDot = 1 - dot(viewDir, normalWS);
                float rimIntensity = rimDot * pow(NdotL, _RimThreshold);
                rimIntensity = smoothstep(_RimAmount - 0.01, _RimAmount + 0.01, rimIntensity);
                float4 rim = rimIntensity * _RimColor;

                // Sample texture
                float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);

                // Final toon shading result
                return (light + _AmbientColor + specular + rim) * _Color * texColor;
            }
            ENDHLSL
        }
    }
}
