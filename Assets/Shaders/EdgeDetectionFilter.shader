Shader "Hidden/EdgeDetectionFilter"
{
    Properties
    {
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Opaque"
        }
        Pass
        {
            Name "EdgeDetectionFilterPass"

            ZTest Always
            Cull Off
            ZWrite Off

            HLSLPROGRAM

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"

            #pragma vertex vert
            #pragma fragment frag

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                
                // This line is required for VR SPI to work.
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS     : SV_POSITION;
                float3 positionWS      : TEXCOORD1;
                float3 normalWS        : TEXCOORD2;
                float3 viewDirectionWS : TEXCOORD3;
                
                // This line is required for VR SPI to work.
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            Varyings vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                
                // These macros are required for VR SPI compatibility
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                
                // Set up each field of the Varyings struct, then return it.
                OUT.positionWS = mul(unity_ObjectToWorld, IN.positionOS).xyz;
                OUT.normalWS = NormalizeNormalPerPixel(TransformObjectToWorldNormal(IN.normalOS));
                OUT.positionHCS = TransformWorldToHClip(OUT.positionWS);
                OUT.viewDirectionWS = normalize(GetWorldSpaceViewDir(OUT.positionWS));
                
                return OUT;
            }
            
            float4 frag(Varyings i) : SV_Target
            {
                return 1;
            }
            ENDHLSL
        }
    }
}