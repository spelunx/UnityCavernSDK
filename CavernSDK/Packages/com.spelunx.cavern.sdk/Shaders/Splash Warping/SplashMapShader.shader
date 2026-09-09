Shader "Custom/SplashMapShader"
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
            Name "SplashMapShader"

            HLSLPROGRAM
            
            #pragma vertex Vertex
            #pragma fragment Frag

            #include "SmoothBlend.hlsl"

        layout(location = 0) in float4 _vertex;
        layout(location = 1) in float2 _texCoord;
        layout(location = 2) in float4 _normal;
        layout(location = 3) in float4 _annexe;

        uniform float4x4 _modelViewProjectionMatrix;
        uniform float4x4 _modelViewMatrix;
        uniform float4x4 _normalMatrix;
        uniform float4 _cameraAttributes; // blendWidth, brightness, saturation, contrast

    #ifdef VERTEXBLENDING
        uniform float _farthestVertex;
    #endif

        struct VertexData
        {
            float4 position;
            float2 texCoord;
            float4 normal;
            float4 annexe;
            float blendingValue;
        };

        VertexData Vertex(CustomAttributes input) {
        {
            VertexData vertexOut;
            vertexOut.position = float4(_vertex.xyz, 1.0);
            vertexOut.position = _modelViewProjectionMatrix * vertexOut.position;
            gl_Position = vertexOut.position;
            vertexOut.normal = normalize(_normalMatrix * _normal);
            vertexOut.texCoord = _texCoord;
            vertexOut.annexe = _annexe;

            float4 projectedVertex = vertexOut.position / vertexOut.position.w;
            if (projectedVertex.z >= 0.0)
            {
    #ifdef VERTEXBLENDING
                // Compute the distance to the camera, then compare it to the farthest
                // vertex distance and adjust blending value based on this
                float vertexDistToCam = abs(_modelViewMatrix * float4(_vertex.xyz, 1.0)).z;
                // The luminance diminishes with the square of the distance
                // luminanceRatio should always be less than 1.0, as
                // _farthestVertex is by definition the highest possible distance
                float luminanceRatio = 1.0;
                if (_farthestVertex != 0.0)
                {
                    luminanceRatio = vertexDistToCam / _farthestVertex;
                    luminanceRatio = luminanceRatio * luminanceRatio;
                }
                vertexOut.blendingValue = luminanceRatio * min(1.0, getSmoothBlendFromVertex(projectedVertex, _cameraAttributes.x) / _annexe.y);
    #else
                vertexOut.blendingValue = 1.0;
    #endif
            }
        }

    }
}
