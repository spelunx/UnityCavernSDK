float getSmoothBlendFromVertex(float4 v, float blendDist)
            {
                float2 screenPos = v.xy * 0.5 + float2(0.5);
                float2 dist = float2(min(screenPos.x, 1.0 - screenPos.x), min(screenPos.y, 1.0 - screenPos.y));

                // See Lancelle et al. 2011 for explanations about the various weighting functions
                // d1
                //float weight = min(dist.x / blendDist, dist.y / blendDist);

                // d2
                dist = clamp(dist / blendDist, float2(0.0), float2(1.0));
                float weight = 2.0 / (1.0 / dist.x + 1.0 / dist.y);
                weight = max(0.0, min(1.0, weight));
                weight = weight * weight;

                // d4 (and d3 if pow(x, 1.0))
                //float weight = pow(abs(dist.x / blendDist * dist.y / blendDist), 1.5);
                
                return weight;
            }