Shader "Hidden/EdgeDetection3D"
{
    Properties
    {
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType"="Opaque"
        }
        Pass
        {
            Name "EDGE DETECTION OUTLINE"

            ZTest Always
            Cull Off
            ZWrite Off

            HLSLPROGRAM

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl" // needed to sample scene depth
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl" // needed to sample scene normals
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl" // needed to sample scene color/luminance

            #include "Assets/Curvature.hlsl"

            #pragma vertex Vert // vertex shader is provided by the Blit.hlsl include
            #pragma fragment frag
            
            float3 SampleSceneNormalsRemapped(float2 uv)
            {
                return normalize(SampleSceneNormals(uv) * 2.0 - 1.0);
            }

            TEXTURE2D(_EdgeDetectionFilterTexture);
            SAMPLER(sampler_EdgeDetectionFilterTexture);
            
            TEXTURE2D(_ScreenSpaceOcclusionTexture);
            SAMPLER(sampler_ScreenSpaceOcclusionTexture);

            TEXTURE2D(_ScreenSpaceShadowmapTexture);
            SAMPLER(sampler_ScreenSpaceShadowmapTexture);
            
            float getSSAO(float2 vUv, int x, int y)
            {
                float2 offset = float2(x, y) * _ScreenSize.zw;
                return 1 - SAMPLE_TEXTURE2D(_ScreenSpaceOcclusionTexture, sampler_ScreenSpaceOcclusionTexture, vUv + offset).r;
            }

            float getShadowmap(float2 vUv, int x, int y)
            {
                float2 offset = float2(x, y) * _ScreenSize.zw;
                return 1 - SAMPLE_TEXTURE2D(_ScreenSpaceShadowmapTexture, sampler_ScreenSpaceShadowmapTexture, vUv + offset).r;
            }

            float getDepth(float2 vUv, int x, int y) {
                float2 offset = float2(x, y) * _ScreenSize.zw;
                return SampleSceneDepth(vUv + offset).r;
            }

            float3 getNormal(float2 vUv, int x, int y) {
                float2 offset = float2(x, y) * _ScreenSize.zw;
                return SampleSceneNormals(vUv + offset).rgb * 2.0 - 1.0;
            }

            float getEdgeDrawInfo(float2 vUv, int x, int y)
            {
                float2 offset = float2(x, y) * _ScreenSize.zw;
                return SAMPLE_TEXTURE2D(_EdgeDetectionFilterTexture, sampler_EdgeDetectionFilterTexture, vUv + offset).r;
            }

            float getCurvature(float2 vUv, int x, int y)
            {
                float2 offset = float2(x, y) * _ScreenSize.zw;
                float radius = 1;
                float exponent = 1;
                float multiplier = 1;
                float sharpness = 1;
                float curvature;
                GetAverageCurvature_float(vUv + offset, radius, exponent, multiplier, sharpness, curvature);
                return curvature;
            }

            // Computes the edge indicator from one neighbor.
            float neighborNormalEdgeIndicator(float2 vUv, int x, int y, float depth, float3 normal) {
                float depthDiff = depth - getDepth(vUv, x, y);
                float3 normalEdgeBias = float3(1.0, 1.0, 1.0); // Can be parameterized if needed.
                float normalDiff = dot(normal - getNormal(vUv, x, y), normalEdgeBias);
                float normalIndicator = clamp(smoothstep(-0.01, 0.01, normalDiff), 0.0, 1.0);
                float depthIndicator = clamp(sign(depthDiff * 0.25 + 0.0025), 0.0, 1.0);
                return distance(normal, getNormal(vUv, x, y)) * depthIndicator * normalIndicator;
            }

            float neighborNormalEdgeIndicator2(float2 vUv, int x, int y, float depth, float3 normal) {
                float neighborDepth = getDepth(vUv, x, y);
                float3 neighborNormal = getNormal(vUv, x, y);

                // Compute depth difference
                float depthDiff = neighborDepth - depth;

                // Only consider edges where the neighbor is farther away (prevents outward bleeding)
                float depthIndicator = step(0.0005, depthDiff); 

                // Compute normal difference but reduce sensitivity if depth is too similar
                float depthThreshold = 0.0025; // Adjust this threshold for better results
                float normalFactor = smoothstep(depthThreshold, depthThreshold * 2.0, abs(depthDiff));

                float normalDiff = dot(normal - neighborNormal, float3(1.0, 1.0, 1.0));
                float normalIndicator = clamp(smoothstep(-0.02, 0.02, normalDiff), 0.0, 1.0);

                return normalDiff * normalIndicator * depthIndicator * normalFactor;
            }

            // Computes the normal-based edge indicator.
            float normalEdgeIndicator(float2 vUv) {
                float depth = getDepth(vUv, 0, 0);
                float3 normal = getNormal(vUv, 0, 0);
                float indicator = 0.0;
                indicator += neighborNormalEdgeIndicator(vUv, 0, -1, depth, normal);
                indicator += neighborNormalEdgeIndicator(vUv, 0,  1, depth, normal);
                indicator += neighborNormalEdgeIndicator(vUv, -1, 0, depth, normal);
                indicator += neighborNormalEdgeIndicator(vUv, 1,  0, depth, normal);
                return step(0.1, indicator);
            }

            // Computes the normal-based edge indicator.
            float normalEdgeIndicator2(float2 vUv) {
                float depth = getDepth(vUv, 0, 0);
                float3 normal = getNormal(vUv, 0, 0);
                float indicator = 0.0;
                indicator += neighborNormalEdgeIndicator2(vUv, 0, -1, depth, normal);
                indicator += neighborNormalEdgeIndicator2(vUv, 0,  1, depth, normal);
                indicator += neighborNormalEdgeIndicator2(vUv, -1, 0, depth, normal);
                indicator += neighborNormalEdgeIndicator2(vUv, 1,  0, depth, normal);
                return step(0.01, indicator);
            }

            // Computes the depth-based edge indicator.
            float depthEdgeIndicator(float2 vUv) {
                float depth = getDepth(vUv, 0, 0);
                float diff = 0.0;
                diff += clamp(depth - getDepth(vUv, 1, 0), 0.0, 1.0);
                diff += clamp(depth - getDepth(vUv, -1, 0), 0.0, 1.0);
                diff += clamp(depth - getDepth(vUv, 0, 1), 0.0, 1.0);
                diff += clamp(depth - getDepth(vUv, 0, -1), 0.0, 1.0);

                
                float threshold = 1 / 200.0f;
                return floor(smoothstep(threshold/2, threshold, diff) * 2.0) / 2.0;
            }

            float depthOuterEdgeIndicator(float2 vUv) {
                float depth = getDepth(vUv, 0, 0);
                float diff = 0.0;
                diff += clamp(getDepth(vUv, 1, 0) - depth, 0.0, 1.0);
                diff += clamp(getDepth(vUv, -1, 0) - depth, 0.0, 1.0);
                diff += clamp(getDepth(vUv, 0, 1) - depth, 0.0, 1.0);
                diff += clamp(getDepth(vUv, 0, -1) - depth, 0.0, 1.0);
                
                float threshold = 1 / 200.0f;
                return floor(smoothstep(threshold/2, threshold, diff) * 2.0) / 2.0;
            }

            struct DepthInfo {
                float edgeValue;
                float winningDepth;
            };

            float SampleSceneLuminance(float2 uv)
            {
                float3 color = SampleSceneColor(uv);
                return color.r * 0.3 + color.g * 0.59 + color.b * 0.11;
            }
            
            float4 frag(Varyings i) : SV_Target
            {
                float2 uv = i.texcoord;
                float4 texel = float4(SampleSceneColor(uv), 1);

                // Edge detection coefficients
                float normalEdgeCoefficient = 0.5;
                float depthEdgeCoefficient = 0.5;

                // Edge indicators
                float dei = depthEdgeIndicator(uv);
                float dei_outer = depthOuterEdgeIndicator(uv);
                float nei = normalEdgeIndicator(uv);
                float nei2 = normalEdgeIndicator2(uv);
                float curvature = getCurvature(uv, 0, 0);

                // Compute edge color
                float4 edgeColor = float4(SampleSceneColor(uv), 1);
                
                // Compute depth-based coefficient
                float coefficient = (dei > 0.0) 
                                    ? (1.0 - depthEdgeCoefficient * dei)
                                    : (1.0 + normalEdgeCoefficient * nei);

                // If nei2 is significant, return the original texel
                if (nei2 > 0.0) 
                    return texel;

                // Depth edge handling
                if (dei > 0.0)
                {
                    coefficient = (1.0 - depthEdgeCoefficient * dei);
                    return edgeColor * coefficient;
                }

                // Curvature-based handling (suppress edges where curvature is low)
                float curvatureThreshold = 0.02;
                if (dei <= 0 && nei <= 0 && curvature < curvatureThreshold && dei_outer <= 0)
                {
                    coefficient = (1.0 - depthEdgeCoefficient);
                    return edgeColor * coefficient;
                }

                // If curvature is below the threshold, return the original texel
                if (curvature < curvatureThreshold)
                    return texel;

                // The edges detected by normals are more invasive, so we are only going to apply them last.
                if (nei > 0 && curvature < curvatureThreshold)
                {
                    coefficient = (1.0 - depthEdgeCoefficient * nei);
                    return edgeColor * coefficient;
                }

                return edgeColor * coefficient;
            }

            ENDHLSL
        }
    }
}