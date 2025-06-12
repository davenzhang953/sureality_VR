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

            // �ؼ��㣺�� Plane ��ȫ͸������Ȼ��Ч
            ColorMask 0      // ��д����ɫ������
            ZWrite Off       // �ر����д�룬����Ӱ����������
            Blend Zero One   // ȷ������Ӱ����ɫ

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
                return half4(0, 0, 0, 0); // ͸�����
            }
            ENDHLSL
        }
    }
}
