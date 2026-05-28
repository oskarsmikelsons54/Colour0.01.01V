Shader "Custom/lazer"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _EmissionColor("Emission Color", Color) = (1, 0, 0, 1)
        [Float] _EmissionIntensity("Emission Intensity", Float) = 2.0
        [Float] _EdgeSoftness("Edge Softness", Float) = 0.15
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        // Additive blending so bright pixels bloom with post-processing
        Blend One One
        Cull Off
        ZWrite Off

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float4 _BaseMap_ST;
                half4 _EmissionColor;
                float _EmissionIntensity;
                float _EdgeSoftness;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // sample base texture
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                half4 col = tex * _BaseColor;

                // soft edge across the beam's short axis (assumes UV.y maps across beam thickness)
                float dist = abs(IN.uv.y - 0.5);
                float softness = max(0.0001, _EdgeSoftness);
                // edge factor: 1 at center, 0 at or beyond softness
                float edgeFactor = saturate(1.0 - (dist / softness));

                // emission contribution scaled by alpha so transparent parts don't glow as much
                half3 emission = _EmissionColor.rgb * (_EmissionIntensity * col.a);

                // multiply color by edge factor for soft falloff
                half3 rgbOut = col.rgb * edgeFactor + emission * edgeFactor;
                float alphaOut = col.a * edgeFactor;

                // Return HDR-capable color; additive blending will make it bloom under post-processing
                return half4(rgbOut, alphaOut);
            }
            ENDHLSL
        }
    }
}
