Shader "Unlit/ARCoreBackground2"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}        
        _LUT("LUT", 2D) = "white" {}
        _Contribution("Contribution", Range(0, 1)) = 0
        _UvTopLeftRight ("UV of top corners", Vector) = (0, 1, 1, 1)
        _UvBottomLeftRight ("UV of bottom corners", Vector) = (0 , 0, 1, 0)
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Background"
            "RenderType" = "Background"
            "ForceNoShadowCasting" = "True"
        }

        Pass
        {
            Cull Off
            ZTest Always
            ZWrite On
            Lighting Off
            LOD 100
            Tags
            {
                "LightMode" = "Always"
            }

            GLSLPROGRAM

            #define COLORS 32.0
            


//#pragma only_renderers gles3

#ifdef SHADER_API_GLES3
            #extension GL_OES_EGL_image_external_essl3 : require
#endif // SHADER_API_GLES3

            // Device display transform is provided by the AR Foundation camera background renderer.

            uniform mat4 _UnityDisplayTransform;

#ifdef VERTEX
            varying vec2 textureCoord;

            void main()
            {
#ifdef SHADER_API_GLES3
                // Transform the position from object space to clip space.
                gl_Position = gl_ModelViewProjectionMatrix * gl_Vertex;

                // Remap the texture coordinates based on the device rotation.
                textureCoord = (_UnityDisplayTransform * vec4(gl_MultiTexCoord0.x, 1.0f - gl_MultiTexCoord0.y, 1.0f, 0.0f)).xy;
#endif // SHADER_API_GLES3
            }
#endif // VERTEX

#ifdef FRAGMENT
            varying vec2 textureCoord;
            uniform samplerExternalOES _MainTex;
            uniform sampler2D _LUT;
            uniform vec4 _LUT_TexelSize;
            uniform float _Contribution;
            uniform vec4 _UvTopLeftRight;
            uniform vec4 _UvBottomLeftRight;

#if defined(SHADER_API_GLES3) && !defined(UNITY_COLORSPACE_GAMMA)
            float GammaToLinearSpaceExact (float value)
            {
                if (value <= 0.04045F)
                    return value / 12.92F;
                else if (value < 1.0F)
                    return pow((value + 0.055F)/1.055F, 2.4F);
                else
                    return pow(value, 2.2F);
            }

            vec3 GammaToLinearSpace (vec3 sRGB)
            {
                // Approximate version from http://chilliant.blogspot.com.au/2012/08/srgb-approximations-for-hlsl.html?m=1
                return sRGB * (sRGB * (sRGB * 0.305306011F + 0.682171111F) + 0.012522878F);

                // Precise version, useful for debugging, but the pow() function is too slow.
                // return vec3(GammaToLinearSpaceExact(sRGB.r), GammaToLinearSpaceExact(sRGB.g), GammaToLinearSpaceExact(sRGB.b));
            }
#endif // SHADER_API_GLES3 && !UNITY_COLORSPACE_GAMMA

            void main()
            {
#ifdef SHADER_API_GLES3
                vec3 videoColor = texture(_MainTex, textureCoord).xyz;

#ifndef UNITY_COLORSPACE_GAMMA
                videoColor = GammaToLinearSpace(videoColor);
#endif // !UNITY_COLORSPACE_GAMMA


                float maxColor = COLORS - 1.0;

                //fixed4 col = saturate(tex2D(_MainTex, i.uv));

                float halfColX = 0.5 / _LUT_TexelSize.z;
                float halfColY = 0.5 / _LUT_TexelSize.w;
                float threshold = maxColor / COLORS;
 
                float xOffset = halfColX + videoColor.r * threshold / COLORS;
                float yOffset = halfColY + videoColor.g * threshold;

                float cell = floor(videoColor.b * maxColor);
                vec2 lutPos = vec2(cell / COLORS + xOffset, yOffset);

                vec4 gradedCol = texture(_LUT, lutPos);
                 
                float rr = videoColor.r - (videoColor.r - gradedCol.r) * _Contribution;
                float gg = videoColor.g - (videoColor.g - gradedCol.g) * _Contribution;
                float bb = videoColor.b - (videoColor.b - gradedCol.b) * _Contribution;

 /**/

                gl_FragColor = vec4(rr, gg, bb, 1);
                gl_FragDepth = 1.0f;
#endif // SHADER_API_GLES3
            }

#endif // FRAGMENT
            ENDGLSL
        }
    }



    Subshader
    {
        Pass
        {
            ZWrite Off
 
            CGPROGRAM
 
            #pragma exclude_renderers gles3
            #pragma vertex vert
            #pragma fragment frag
 
            #include "UnityCG.cginc"
 
            uniform float4 _UvTopLeftRight;
            uniform float4 _UvBottomLeftRight;

            #define COLORS 32.0
            sampler2D _LUT;
            float4 _LUT_TexelSize;
            float _Contribution;
 
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };
 
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };
 
            v2f vert(appdata v)
            {
                float2 uvTop = lerp(_UvTopLeftRight.xy, _UvTopLeftRight.zw, v.uv.x);
                float2 uvBottom = lerp(_UvBottomLeftRight.xy, _UvBottomLeftRight.zw, v.uv.x);
 
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = lerp(uvTop, uvBottom, v.uv.y);
 
                // Instant preview's texture is transformed differently.
                o.uv = o.uv.yx;
                o.uv.x = 1.0 - o.uv.x;
 
                return o;
            }
 
            sampler2D _MainTex;
 
            fixed4 frag(v2f i) : SV_Target
            {

                fixed4 videoColor = tex2D(_MainTex, i.uv);

                float maxColor = COLORS - 1.0;

                //fixed4 col = saturate(tex2D(_MainTex, i.uv));

                float halfColX = 0.5 / _LUT_TexelSize.z;
                float halfColY = 0.5 / _LUT_TexelSize.w;
                float threshold = maxColor / COLORS;
 
                float xOffset = halfColX + videoColor.r * threshold / COLORS;
                float yOffset = halfColY + videoColor.g * threshold;
                float cell = floor(videoColor.b * maxColor);
 
                float2 lutPos = float2(cell / COLORS + xOffset, yOffset);
                float4 gradedCol = tex2D(_LUT, lutPos);
                 
                float4 outColor = lerp(videoColor, gradedCol, _Contribution);

                videoColor.rgb = outColor.rgb;
                return videoColor;
            }
            ENDCG
        }
    }


    FallBack Off
}
