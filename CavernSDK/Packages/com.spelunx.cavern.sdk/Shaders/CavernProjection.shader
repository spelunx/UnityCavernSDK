Shader "Spelunx/CavernProjection" {
    // Properties are options set per material, exposed by the material inspector.
    // By convention, properties' names are declared _PropertyName("Label In Inspector", Type) = Value.
    // List of property types: https://docs.unity3d.com/Manual/SL-Properties.html
    Properties { }
    

    SubShader {
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
        ENDHLSL

        Tags {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType"="Opaque"
        }
        LOD 100
        ZWrite Off Cull Off
        Pass {
            Name "Cavern Projection Mapping"

            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM // Begin HLSL code.

            // Tell the shader to find the function named "Vertex" in our HLSL code, and use it as the vertex function.
		    #pragma vertex Vertex
		    // Tell the shader to find the function named "Fragment" in our HLSL code, and use it as the fragment function.
		    #pragma fragment Fragment

            // Include our HLSL code. Seperating it out makes the code reusable.
            #include "HLSL/CavernProjectionMapping.hlsl"

            ENDHLSL // End HLSL code.
        }
    }
}