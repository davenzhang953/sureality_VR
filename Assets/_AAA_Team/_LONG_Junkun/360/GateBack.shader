Shader "Custom/GateBack"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Overlay" }

        Pass
        {
            Name "StencilPass"

            Stencil
            {
                Ref 2
                Comp Always
                Pass Replace
            }

            // 关键点：让 Plane 完全透明但仍然生效
            ColorMask 0      // 不写入颜色缓冲区
            ZWrite Off       // 关闭深度写入，避免影响其他物体
            Blend Zero One   // 确保不会影响颜色

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(IN.positionOS);
                OUT.positionHCS = vertexInput.positionCS;
                return OUT;
            }

            half4 frag(Varyings i) : SV_Target
            {
                return half4(0, 0, 0, 0); // 透明输出
            }
            ENDHLSL
        }
    }
}
