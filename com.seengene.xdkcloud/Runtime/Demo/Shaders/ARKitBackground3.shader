Shader "Unlit/ARKitBackground3"
{
    Properties
    {
        _textureY ("TextureY", 2D) = "white" {}
        _textureCbCr ("TextureCbCr", 2D) = "black" {}
        _HumanStencil ("HumanStencil", 2D) = "black" {}
        _HumanDepth ("HumanDepth", 2D) = "black" {}
        _HueMul("HueMul", Range (0, 10)) = 1
        _HueAdd("HueAdd", Range (-1, 1)) = 0
        _Saturation("Saturation", Range (0, 10)) = 1
        _Brightness("Brightness", Range (0, 10)) = 1
        _Contrast("Contrast", Range (0, 1)) = 1
        _Temperature("Temperature", Range (-1, 1)) = 0
        _Tint("Tint", Range (-2, 2)) = 0
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


            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_local __ ARKIT_BACKGROUND_URP ARKIT_BACKGROUND_LWRP
            #pragma multi_compile_local __ ARKIT_HUMAN_SEGMENTATION_ENABLED


            #include "UnityCG.cginc"

            #define real4 half4
            #define real4x4 half4x4
            #define TransformObjectToHClip UnityObjectToClipPos
            #define FastSRGBToLinear GammaToLinearSpace

            #define ARKIT_TEXTURE2D_HALF(texture) UNITY_DECLARE_TEX2D_HALF(texture)
            #define ARKIT_SAMPLER_HALF(sampler)
            #define ARKIT_TEXTURE2D_FLOAT(texture) UNITY_DECLARE_TEX2D_FLOAT(texture)
            #define ARKIT_SAMPLER_FLOAT(sampler)
            #define ARKIT_SAMPLE_TEXTURE2D(texture,sampler,texcoord) UNITY_SAMPLE_TEX2D(texture,texcoord)

            float _HueMul;
            float _HueAdd;
            float _Saturation;
            float _Brightness;
            float _Contrast;
            float _Temperature;
            float _Tint;

            float3 MyHsvToRgb( float3 c )
            {
                 float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
                 float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
                 return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
            }

            float3 MyRgbToHsv( float3 c )
            {
                float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
                float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
                float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));

                float d = q.x - min(q.w, q.y);
                float e = 1.0e-10;
                return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
            }

            float3 Unity_WhiteBalance_float(float3 In, float Temperature, float Tint)
            {
                // Range ~[-1.67;1.67] works best
                float t1 = Temperature * 10 / 6;
                float t2 = Tint * 10 / 6;

                // Get the CIE xy chromaticity of the reference white point.
                // Note: 0.31271 = x value on the D65 white point
                float x = 0.31271 - t1 * (t1 < 0 ? 0.1 : 0.05);
                float standardIlluminantY = 2.87 * x - 3 * x * x - 0.27509507;
                float y = standardIlluminantY + t2 * 0.05;

                // Calculate the coefficients in the LMS space.
                float3 w1 = float3(0.949237, 1.03542, 1.08728); // D65 white point

                // CIExyToLMS
                float Y = 1;
                float X = Y * x / y;
                float Z = Y * (1 - x - y) / y;
                float L = 0.7328 * X + 0.4296 * Y - 0.1624 * Z;
                float M = -0.7036 * X + 1.6975 * Y + 0.0061 * Z;
                float S = 0.0030 * X + 0.0136 * Y + 0.9834 * Z;
                float3 w2 = float3(L, M, S);

                float3 balance = float3(w1.x / w2.x, w1.y / w2.y, w1.z / w2.z);

                float3x3 LIN_2_LMS_MAT = {
                    3.90405e-1, 5.49941e-1, 8.92632e-3,
                    7.08416e-2, 9.63172e-1, 1.35775e-3,
                    2.31082e-2, 1.28021e-1, 9.36245e-1
                };

                float3x3 LMS_2_LIN_MAT = {
                    2.85847e+0, -1.62879e+0, -2.48910e-2,
                    -2.10182e-1,  1.15820e+0,  3.24281e-4,
                    -4.18120e-2, -1.18169e-1,  1.06867e+0
                };

                float3 lms = mul(LIN_2_LMS_MAT, In);
                lms *= balance;
                return mul(LMS_2_LIN_MAT, lms);
            }

            float3 Unity_Contrast_float(float3 In, float Contrast)
            {
                float midpoint = pow(0.5, 2.2);
                return (In - midpoint) * Contrast + midpoint;
            }

            struct appdata
            {
                float3 position : POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 position : SV_POSITION;
                float2 texcoord : TEXCOORD0;
            };

            struct fragment_output
            {
                real4 color : SV_Target;
                float depth : SV_Depth;
            };


            CBUFFER_START(UnityARFoundationPerFrame)
            // Device display transform is provided by the AR Foundation camera background renderer.
            float4x4 _UnityDisplayTransform;
            CBUFFER_END


            v2f vert (appdata v)
            {
                // Transform the position from object space to clip space.
                float4 position = TransformObjectToHClip(v.position);

                // Remap the texture coordinates based on the device rotation.
                float2 texcoord = mul(float3(v.texcoord, 1.0f), _UnityDisplayTransform).xy;

                v2f o;
                o.position = position;
                o.texcoord = texcoord;
                return o;
            }


            CBUFFER_START(ARKitColorTransformations)
            static const real4x4 s_YCbCrToSRGB = real4x4(
                real4(1.0h,  0.0000h,  1.4020h, -0.7010h),
                real4(1.0h, -0.3441h, -0.7141h,  0.5291h),
                real4(1.0h,  1.7720h,  0.0000h, -0.8860h),
                real4(0.0h,  0.0000h,  0.0000h,  1.0000h)
            );
            CBUFFER_END


            inline float ConvertDistanceToDepth(float d)
            {
                // Clip any distances smaller than the near clip plane, and compute the depth value from the distance.
                return (d < _ProjectionParams.y) ? 0.0f : ((0.5f / _ZBufferParams.z) * ((1.0f / d) - _ZBufferParams.w));
            }


            ARKIT_TEXTURE2D_HALF(_textureY);
            ARKIT_SAMPLER_HALF(sampler_textureY);
            ARKIT_TEXTURE2D_HALF(_textureCbCr);
            ARKIT_SAMPLER_HALF(sampler_textureCbCr);
#if ARKIT_HUMAN_SEGMENTATION_ENABLED
            ARKIT_TEXTURE2D_HALF(_HumanStencil);
            ARKIT_SAMPLER_HALF(sampler_HumanStencil);
            ARKIT_TEXTURE2D_FLOAT(_HumanDepth);
            ARKIT_SAMPLER_FLOAT(sampler_HumanDepth);
#endif // ARKIT_HUMAN_SEGMENTATION_ENABLED


            fragment_output frag (v2f i)
            {
                // Sample the video textures (in YCbCr).
                real4 ycbcr = real4(ARKIT_SAMPLE_TEXTURE2D(_textureY, sampler_textureY, i.texcoord).r,
                                    ARKIT_SAMPLE_TEXTURE2D(_textureCbCr, sampler_textureCbCr, i.texcoord).rg,
                                    1.0h);

                // Convert from YCbCr to sRGB.
                real4 videoColor = mul(s_YCbCrToSRGB, ycbcr);

#if !UNITY_COLORSPACE_GAMMA
                // If rendering in linear color space, convert from sRGB to RGB.
                videoColor.xyz = FastSRGBToLinear(videoColor.xyz);
#endif // !UNITY_COLORSPACE_GAMMA

                // Assume the background depth is the back of the depth clipping volume.
                float depthValue = 0.0f;

#if ARKIT_HUMAN_SEGMENTATION_ENABLED
                // Check the human stencil, and skip non-human pixels.
                if (ARKIT_SAMPLE_TEXTURE2D(_HumanStencil, sampler_HumanStencil, i.texcoord).r > 0.5h)
                {
                    // Sample the human depth (in meters).
                    float humanDistance = ARKIT_SAMPLE_TEXTURE2D(_HumanDepth, sampler_HumanDepth, i.texcoord).r;

                    // Convert the distance to depth.
                    depthValue = ConvertDistanceToDepth(humanDistance);
                }
#endif // ARKIT_HUMAN_SEGMENTATION_ENABLED

                float3 hsv = MyRgbToHsv(videoColor.xyz);
                hsv.x *= _HueMul + _HueAdd;
                hsv.y *= _Saturation;
                hsv.z *= _Brightness;
                float3 outColor = MyHsvToRgb(hsv);

                outColor = Unity_WhiteBalance_float(outColor, _Temperature, _Tint);
                outColor = Unity_Contrast_float(outColor, _Contrast);

                fragment_output o;
                o.color = float4(outColor, 1);
                o.depth = depthValue;
                return o;
            }

            ENDHLSL
        }
    }
}
