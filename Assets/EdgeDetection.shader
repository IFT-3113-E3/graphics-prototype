//Shader "Hidden/EdgeDetection3D"
//{
//    Properties
//    {
//        _OutlineThickness ("Outline Thickness", Float) = 1
//        _OutlineColor ("Outline Color", Color) = (0, 0, 0, 1)
//    }
//
//    SubShader
//    {
//        Tags
//        {
//            "RenderPipeline" = "UniversalPipeline"
//            "RenderType"="Opaque"
//        }
//
//        ZWrite Off
//        Cull Off
//        Blend SrcAlpha OneMinusSrcAlpha
//
//        Pass
//        {
//            Name "EDGE DETECTION OUTLINE"
//
//            HLSLPROGRAM
//            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
//            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
//            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl" // needed to sample scene depth
//
//
//            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl" // needed to sample scene normals
//
//
//            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl" // needed to sample scene color/luminance
//
//
//
//            float _OutlineThickness;
//            float4 _OutlineColor;
//
//            TEXTURE2D(_ScreenSpaceOcclusionTexture);
//            SAMPLER(sampler_ScreenSpaceOcclusionTexture);
//
//            #pragma vertex Vert // vertex shader is provided by the Blit.hlsl include
//            #pragma fragment frag
//
//            // Edge detection kernel that works by taking the sum of the squares of the differences between diagonally adjacent pixels (Roberts Cross).
//            float RobertsCross(float3 samples[4])
//            {
//                const float3 difference_1 = samples[1] - samples[2];
//                const float3 difference_2 = samples[0] - samples[3];
//                return sqrt(dot(difference_1, difference_1) + dot(difference_2, difference_2));
//            }
//
//            // The same kernel logic as above, but for a single-value instead of a vector3.
//            float RobertsCross(float samples[4])
//            {
//                const float difference_1 = samples[1] - samples[2];
//                const float difference_2 = samples[0] - samples[3];
//                return sqrt(difference_1 * difference_1 + difference_2 * difference_2);
//            }
//            
//            float SampleSceneSSAO(float2 uv)
//            {
//                float4 color = SAMPLE_TEXTURE2D(_ScreenSpaceOcclusionTexture, sampler_ScreenSpaceOcclusionTexture, uv);
//                return color.x;
//            }
//
//            // Helper function to sample scene normals remapped from [-1, 1] range to [0, 1].
//            float3 SampleSceneNormalsRemapped(float2 uv)
//            {
//                return SampleSceneNormals(uv) * 0.5 + 0.5;
//            }
//
//            // Helper function to sample scene luminance.
//            float SampleSceneLuminance(float2 uv)
//            {
//                float3 color = SampleSceneColor(uv);
//                return color.r * 0.3 + color.g * 0.59 + color.b * 0.11;
//            }
//
//            float3 RGBtoHSV(float3 c)
//            {
//                float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
//                float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
//                float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));
//
//                float d = q.x - min(q.w, q.y);
//                float e = 1.0e-10;
//                return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
//            }
//
//            float3 HSVtoRGB(float3 hsv)
//            {
//                float3 p = abs(frac(hsv.xxx + float3(0.0, 2.0 / 3.0, 1.0 / 3.0)) * 6.0 - 3.0);
//                return hsv.z * lerp(float3(1.0, 1.0, 1.0), saturate(p - 1.0), hsv.y);
//            }
//
//            float3 ShadeColor(float3 color, float shadeFactor)
//            {
//                float3 hsv = RGBtoHSV(color);
//
//                // Adjust saturation and value
//                hsv.y *= (1.0 - shadeFactor * 0.2); // Reduce saturation slightly
//                hsv.z += shadeFactor * 0.2; // Increase brightness slightly
//                hsv.z = saturate(hsv.z); // Clamp value to valid range
//
//                return HSVtoRGB(hsv);
//            }
//
//
//            half4 frag(Varyings IN) : SV_TARGET
//            {
//                float2 uv = IN.texcoord;
//
//                float2 texel_size = float2(1.0 / _ScreenParams.x, 1.0 / _ScreenParams.y);
//                // float2 texel_size2 = float2(2.0 / _ScreenParams.x, 2.0 / _ScreenParams.y);
//
//                const float half_width_f = floor(_OutlineThickness * 0.5);
//                const float half_width_c = ceil(_OutlineThickness * 0.5);
//
//                float2 uvs[4];
//                uvs[0] = uv + texel_size * float2(half_width_f, half_width_c) * float2(-1, 0);
//                uvs[1] = uv + texel_size * float2(half_width_c, half_width_c) * float2(1, 0);
//                uvs[2] = uv + texel_size * float2(half_width_f, half_width_f) * float2(0, -1);
//                uvs[3] = uv + texel_size * float2(half_width_c, half_width_f) * float2(0, -1);
//
//                // float2 uvs2[4];
//                // uvs2[0] = uv + texel_size2 * float2(half_width_f, half_width_c) * float2(-1, 1);
//                // uvs2[1] = uv + texel_size2 * float2(half_width_c, half_width_c) * float2(1, 1);
//                // uvs2[2] = uv + texel_size2 * float2(half_width_f, half_width_f) * float2(-1, -1);
//                // uvs2[3] = uv + texel_size2 * float2(half_width_c, half_width_f) * float2(1, -1);
//
//                float3 normal_samples[4];
//                float depth_samples[4], luminance_samples[4], ssao_samples[4];
//
//                for (int i = 0; i < 4; i++)
//                {
//                    depth_samples[i] = SampleSceneDepth(uvs[i]);
//                    normal_samples[i] = SampleSceneNormalsRemapped(uvs[i]);
//                    luminance_samples[i] = SampleSceneLuminance(uvs[i]);
//                    ssao_samples[i] = SampleSceneSSAO(uvs[i]);
//                }
//
//                // Compute edges
//                float edge_depth = RobertsCross(depth_samples);
//                float edge_normal = RobertsCross(normal_samples);
//                float edge_luminance = RobertsCross(luminance_samples);
//                float edge_ssao = SampleSceneSSAO(uv);
//
//                // Dynamic depth threshold adjustment
//                float avgDepth = (depth_samples[0] + depth_samples[1] + depth_samples[2] +
//                    depth_samples[3]) * 0.25;
//                float currentDepth = SampleSceneDepth(uv);
//                float relativeDepthDiff = abs(currentDepth - avgDepth);
//
//                float depth_threshold = 1 / 200.0f;
//                //lerp(1 / 200.0f, 1 / 50.0f, saturate(relativeDepthDiff * 50.0));
//                float normal_threshold = 1 / 4.0f;
//                float luminance_threshold = 1 / 0.5f;
//                float ssao_threshold = 1 / 10.0f;
//                
//                // Identify edges
//                bool is_external = edge_depth > depth_threshold;
//                bool is_internal = (edge_normal > normal_threshold);
//                bool is_internal_concave = is_internal && ((1 - edge_ssao) > ssao_threshold);
//                // || edge_luminance > luminance_threshold);
//
//                float edge = max(edge_depth, max(edge_normal, edge_luminance));
//                //
//                // if (is_external) return float4(1,0,0,1);
//                // if (is_internal_concave) return float4(0,1,0,1);
//                // if (is_internal) return float4(0,0,1,1);
//                // return float4(0, 0, 0, 1);
//
//                // Sample scene color for shading effect
//                float3 originalColor = SampleSceneColor(uv).rgb;
//
//                // Approximate lighting by using normal's Y component (assuming light from above)
//                float lightIntensity = saturate(normal_samples[1].y * 0.5 + 0.5);
//                // Remap from [-1,1] to [0,1]
//
//                // Darken outer edges by multiplying with a shadow factor
//                float3 outerEdgeColor = originalColor * 0.5;
//                // 50% darker for silhouettes
//                // float3 outerEdgeColor = float3(1,0,0);
//                // Adjust inner edge color based on lighting
//                // float3 innerEdgeColor = float3(0,1,0);
//                float3 innerEdgeColor = originalColor * 1.5;
//                // * lerp(0.5, 1.5, lightIntensity);
//                // Darken if not lit, lighten if lit
//
//                // Determine final edge color
//                if (is_external) return float4(outerEdgeColor, 1.0);
//                if (is_internal_concave) return float4(outerEdgeColor, 1.0);
//                // Darker outline for external edges
//                if (is_internal) return float4(innerEdgeColor, 1.0);
//
//                // Adjusted shading for inner edges
//                return float4(0, 0, 0, 0); // No edge
//            }
//            ENDHLSL
//        }
//    }
//}
//
Shader "Hidden/EdgeDetection3D"
{
    Properties
    {
        // Tweak these in your script or inspector
        _DepthScale ("Depth Scale Multiplier", Float) = 1.0
        _DepthThresholdMin ("Depth Edge Min Threshold", Float) = 0.01
        _DepthThresholdMax ("Depth Edge Max Threshold", Float) = 0.02

        _NormalSharpnessThreshold ("Normal Sharpness Threshold", Float) = 0.1
        _NormalDirectionThreshMin ("Normal Dir Min Threshold", Float) = -0.01
        _NormalDirectionThreshMax ("Normal Dir Max Threshold", Float) = 0.01

        _DepthEdgeStrength ("Depth Edge Strength", Float) = 1.0
        _NormalEdgeStrength ("Normal Edge Strength", Float) = 1.0
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




            // OR, if you store normals in a separate RT, do:
            // sampler2D _CameraNormalsTexture;
            // in that case you'd sample it similarly.

            // Properties
            float _DepthScale;
            float _DepthThresholdMin;
            float _DepthThresholdMax;

            float _NormalSharpnessThreshold;
            float _NormalDirectionThreshMin;
            float _NormalDirectionThreshMax;

            float _DepthEdgeStrength;
            float _NormalEdgeStrength;

            #pragma vertex Vert // vertex shader is provided by the Blit.hlsl include
            #pragma fragment frag
            
            float3 SampleSceneNormalsRemapped(float2 uv)
            {
                return normalize(SampleSceneNormals(uv) * 2.0 - 1.0);
            }
            // // Utility: sample the normal from a normal+depth RT
            // // By default (Built-In) the normal is stored in [0..1], we map it back to [-1..1].
            // inline float3 SampleNormal(sampler2D normTex, float2 uv)
            // {
            //     float4 enc = tex2D(normTex, uv);
            //     float3 normEncoded = enc.xyz * 2.0 - 1.0; // map [0..1] -> [-1..1]
            //     // It's in view space for CameraDepthNormalsTexture by default in the built-in pipeline.
            //     // If you made a custom normal pass, adapt as needed.
            //     return normalize(normEncoded);
            // }

                // Helper function to accumulate from one neighbor
            // void AccumulateNormalEdge(float3 nNeighbor, float dNeighbor,
            //     float3 normalCenter,
            //     float3 directionVec,
            //     inout float normalEdgeSum, inout float depthBiasSum, float depthCenter)
            // {
            //     float3 bias = (nNeighbor - normalCenter);
            //     float dirDot = dot(bias, directionVec);
            //
            //     // smoothstep around dirDot (this culls one side)
            //     float directionIndicator = smoothstep(_NormalDirectionThreshMin,
            //        _NormalDirectionThreshMax,
            //        dirDot);
            //
            //     // measure face sharpness
            //     float faceSharpness = (1.0 - dot(normalCenter, nNeighbor));
            //
            //     // combined factor from neighbor
            //     float neighborEdge = faceSharpness * directionIndicator;
            //
            //     normalEdgeSum += neighborEdge;
            //
            //     // accumulate depth bias
            //     depthBiasSum += (dNeighbor - depthCenter);
            // };

            TEXTURE2D(_ScreenSpaceOcclusionTexture);
            SAMPLER(sampler_ScreenSpaceOcclusionTexture);
            
            float getSSAO(float2 vUv, int x, int y)
            {
                float2 offset = float2(x, y) * _ScreenSize.zw;
                return SAMPLE_TEXTURE2D(_ScreenSpaceOcclusionTexture, sampler_ScreenSpaceOcclusionTexture, vUv + offset).r;
            }

            // Returns depth value at a given offset.
            float getDepth(float2 vUv, int x, int y) {
                float2 offset = float2(x, y) * _ScreenSize.zw;
                return SampleSceneDepth(vUv + offset).r;
            }

            // Returns the normal vector at a given offset.
            float3 getNormal(float2 vUv, int x, int y) {
                float2 offset = float2(x, y) * _ScreenSize.zw;
                return SampleSceneNormals(vUv + offset).rgb * 2.0 - 1.0;
            }

            // Computes the edge indicator from one neighbor.
            float neighborNormalEdgeIndicator(float2 vUv, int x, int y, float depth, float3 normal) {
                float depthDiff = getDepth(vUv, x, y) - depth;
                float3 normalEdgeBias = float3(1.0, 1.0, 1.0); // Can be parameterized if needed.
                float normalDiff = dot(normal - getNormal(vUv, x, y), normalEdgeBias);
                float normalIndicator = clamp(smoothstep(-0.01, 0.01, normalDiff), 0.0, 1.0);
                float depthIndicator = clamp(sign(depthDiff * 0.25 + 0.0025), 0.0, 1.0);
                return distance(normal, getNormal(vUv, x, y)) * depthIndicator * normalIndicator;
            }

            // Computes the depth-based edge indicator.
            float depthEdgeIndicator(float2 vUv) {
                float depth = getDepth(vUv, 0, 0);
                float diff = 0.0;
                diff += clamp(getDepth(vUv, 1, 0) - depth, 0.0, 1.0);
                diff += clamp(getDepth(vUv, -1, 0) - depth, 0.0, 1.0);
                diff += clamp(getDepth(vUv, 0, 1) - depth, 0.0, 1.0);
                diff += clamp(getDepth(vUv, 0, -1) - depth, 0.0, 1.0);
                
                float threshold = 1 / 200.0f;
                return floor(smoothstep(threshold/2, threshold, diff) * 2.0) / 2.0;
            }

            float ssaoEdgeIndicator(float2 vUv) {
                float depth = getSSAO(vUv, 0, 0);
                float diff = 0.0;
                diff += clamp(getSSAO(vUv, 1, 0) - depth, 0.0, 1.0);
                diff += clamp(getSSAO(vUv, -1, 0) - depth, 0.0, 1.0);
                diff += clamp(getSSAO(vUv, 0, 1) - depth, 0.0, 1.0);
                diff += clamp(getSSAO(vUv, 0, -1) - depth, 0.0, 1.0);

                float threshold = 0.02;
                
                return floor(smoothstep(threshold/2, threshold, diff) * 2.0) / 2.0;
            }


            struct DepthInfo {
                float edgeValue;
                float winningDepth;
            };

            float3 findBestEdgeColor(float2 vUv, float winningDepth) {
                float minDepthDiff = 1e6;
                float3 bestColor = SampleSceneColor(vUv);

                int2 offsets[8] = {
                    int2(0, 1), int2(1, 0), int2(-1, 0),
                    int2(-1, -1), int2(1, -1), int2(-1, 1), int2(1, 1), int2(0, -1)
                };

                for (int i = 0; i < 8; i++) {
                    float2 sampleUv = vUv + float2(offsets[i]) * _ScreenSize.zw;
                    float depth = SampleSceneDepth(sampleUv);
                    float depthDiff = abs(depth - winningDepth);
                    
                    if (depthDiff < minDepthDiff) {
                        minDepthDiff = depthDiff;
                        bestColor = SampleSceneColor(sampleUv);
                    }
                }
                return bestColor;
            }

            float2 findClosestDepthNeighbor(float2 vUv, out float minDepth) {
                float depth = getDepth(vUv, 0, 0);
                minDepth = depth;
                float2 closestUv = vUv;

                float2 offsets[4] = { float2(1, 0), float2(-1, 0), float2(0, 1), float2(0, -1) };

                for (int i = 0; i < 4; i++) {
                    float2 neighborUv = vUv + offsets[i] * _ScreenSize.zw;
                    float neighborDepth = getDepth(vUv, offsets[i].x, offsets[i].y);

                    if (neighborDepth > minDepth) {
                        minDepth = neighborDepth;
                        closestUv = neighborUv;
                    }
                }
                return closestUv;
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
            

            float SampleSceneLuminance(float2 uv)
            {
                float3 color = SampleSceneColor(uv);
                return color.r * 0.3 + color.g * 0.59 + color.b * 0.11;
            }

            // Returns a smooth sign function.
            float smoothSign(float x, float radius) {
                return smoothstep(-radius, radius, x) * 2.0 - 1.0;
            }
            
            float4 frag(Varyings i) : SV_Target
            {
                float2 uv = i.texcoord;
                float4 texel = float4(SampleSceneColor(uv), 1);
                float tLum = Luminance(texel);
                // return tLum;
                // Coefficients (the commented-out lines show how you might parameterize them with tLum).
                // float normalEdgeCoefficient = (smoothSign(tLum - 0.3, 0.1) + 0.7) * 0.6;
                // float depthEdgeCoefficient = (smoothSign(tLum - 0.3, 0.1) + 0.7) * 0.6;
                float normalEdgeCoefficient = 0.5;
                float depthEdgeCoefficient = 0.5;

                float dei = depthEdgeIndicator(uv);
                float nei = normalEdgeIndicator(uv);
                float ssaoei = ssaoEdgeIndicator(uv);

                // return texel * dei + nei;

                // return 1- ssaoei;
                // if (dei > 0.0) return texel;
                // if (nei >= 0.0) return float4(1,0,0,1) * nei;
                
                float coefficient = (dei > 0.0) ? (1.0 - depthEdgeCoefficient * dei)
                                                : (1.0 + normalEdgeCoefficient * nei);

                float minDepth;
                float2 closestUv = findClosestDepthNeighbor(uv, minDepth);
                float4 edgeColor = (dei > 0.0) ? float4(SampleSceneColor(closestUv), 1) : float4(SampleSceneColor(uv), 1);
            
                // if (nei > 0 && ssaoei > 0.5)
                // {
                //     coefficient = 0.5;
                // }
                
                return edgeColor * coefficient;
            }
            ENDHLSL
        }
    }
}