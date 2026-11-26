Shader "StormSurvival/Soft Weather Particles"
{
    Properties { _BaseColor("Tint", Color) = (1,1,1,1) _BaseMap("Texture", 2D) = "white" {} }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Input { float4 positionOS : POSITION; float2 uv : TEXCOORD0; half4 color : COLOR; };
            struct Output { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float viewDepth : TEXCOORD1; half4 color : COLOR; };
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            float4 _BaseMap_ST;
            CBUFFER_END
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            Output Vert(Input input)
            {
                Output output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.viewDepth = -TransformWorldToView(TransformObjectToWorld(input.positionOS.xyz)).z;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.color = input.color * _BaseColor;
                return output;
            }
            half4 Frag(Output input) : SV_Target
            {
                float distance = length((input.uv - 0.5) * 2.0);
                half4 textureColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                // Particles crossing the camera must fade rather than form an opaque overlay.
                half nearFade = saturate((input.viewDepth - 0.12) / 0.30);
                half alpha = saturate(1.0 - distance) * input.color.a * textureColor.a * nearFade;
                return half4(input.color.rgb * textureColor.rgb, alpha);
            }
            ENDHLSL
        }
    }
}
