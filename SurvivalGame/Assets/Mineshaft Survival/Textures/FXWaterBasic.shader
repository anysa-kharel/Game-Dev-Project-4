Shader "FX/Water (Basic)"
{
    Properties
    {
        _horizonColor ("Horizon color", Color) = (.172,.463,.435,1)
        _WaveScale ("Wave scale", Range(.02,.15)) = .07
        [NoScaleOffset] _ColorControl ("Water colors", 2D) = "white" {}
        [NoScaleOffset] _BumpMap ("Waves normal map", 2D) = "bump" {}
        WaveSpeed ("Wave speed", Vector) = (19,9,-16,-7)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS:POSITION; };
            struct Varyings { float4 positionCS:SV_POSITION; float3 positionWS:TEXCOORD0; float fog:TEXCOORD1; };
            CBUFFER_START(UnityPerMaterial)
            half4 _horizonColor;
            float _WaveScale;
            float4 WaveSpeed;
            float4 _WaveOffset;
            CBUFFER_END
            TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);
            TEXTURE2D(_ColorControl); SAMPLER(sampler_ColorControl);
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS=TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS=TransformWorldToHClip(output.positionWS);
                output.fog=ComputeFogFactor(output.positionCS.z);
                return output;
            }
            half4 Frag(Varyings input):SV_Target
            {
                float2 uv=input.positionWS.xz*_WaveScale;
                half2 waveA=SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,uv*.45+_WaveOffset.xy).xy*2-1;
                half2 waveB=SAMPLE_TEXTURE2D(_BumpMap,sampler_BumpMap,uv+_WaveOffset.zw).xy*2-1;
                half3 normal=normalize(half3((waveA.x+waveB.x)*.25,1,(waveA.y+waveB.y)*.25));
                half fresnel=pow(1-saturate(dot(normal,GetWorldSpaceNormalizeViewDir(input.positionWS))),3);
                half3 color=lerp(_horizonColor.rgb*.7,_horizonColor.rgb+half3(.12,.16,.17),fresnel);
                return half4(MixFog(color,input.fog),1);
            }
            ENDHLSL
        }
    }
}
