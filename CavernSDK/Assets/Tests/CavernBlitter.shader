Shader "Custom/CavernBlitter"
{   
    SubShader
    {
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
        ENDHLSL

        Tags { "RenderType"="Opaque" }
        LOD 100
        ZWrite Off Cull Off
        Pass
        {
            Name "CavernBlitter"

            HLSLPROGRAM
            
            #pragma vertex Vert
            #pragma fragment Frag

            float4 _Region;
            TEXTURE2D(_BaseTex);

            float4 Frag (Varyings input) : SV_Target
            {
                // float4 color = SAMPLE_TEXTURE2D(_BaseTex, sampler_LinearClamp, input.texcoord).rgba;
                // return color;
                float2 subUV = input.texcoord * _Region.zw + _Region.xy;
                return SAMPLE_TEXTURE2D(_BaseTex, sampler_LinearClamp, subUV);
            }
            
            ENDHLSL
        }
    }
}
