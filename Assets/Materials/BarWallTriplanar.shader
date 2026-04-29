Shader "Custom/URP_BarWall_Fixed"
{
    Properties
    {
        [MainTexture] _MainTex("Texture (Albedo)", 2D) = "white" {}
        [MainColor] _BaseColor("Teinte du Mur", Color) = (1, 1, 1, 1)
        _MapScale("Échelle", Float) = 1.0
        _Smoothness("Smoothness", Range(0, 1)) = 0.2
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings {
                float4 positionCS : SV_POSITION;
                float3 worldPos   : TEXCOORD0;
                float3 worldNormal: TEXCOORD1;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float _MapScale;
                float _Smoothness;
            CBUFFER_END

            Varyings vert (Attributes input) {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.worldPos = TransformObjectToWorld(input.positionOS.xyz);
                output.worldNormal = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 frag (Varyings input) : SV_Target {
                float3 blending = abs(normalize(input.worldNormal));
                blending /= (blending.x + blending.y + blending.z);

                float3 colX = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.worldPos.zy * _MapScale).rgb;
                float3 colY = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.worldPos.xz * _MapScale).rgb;
                float3 colZ = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.worldPos.xy * _MapScale).rgb;
                
                float3 finalAlbedo = (colX * blending.x + colY * blending.y + colZ * blending.z) * _BaseColor.rgb;

                // Calcul minimal de lumière pour éviter le noir total
                Light mainLight = GetMainLight();
                half3 lightColor = mainLight.color * saturate(dot(input.worldNormal, mainLight.direction));
                half3 ambient = SampleSH(input.worldNormal);

                return half4(finalAlbedo * (lightColor + ambient), 1.0);
            }
            ENDHLSL
        }
    }
}