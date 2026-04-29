Shader "Custom/URP_BarTableWood"
{
    //Template URP_WOODRENDERINGSRXBTP
    Properties
    {
        _MainTex("Base Map", 2D) = "white" {}
        _BaseColor("Couleur du Bois", Color) = (0.5, 0.3, 0.15, 1)
        
        [Normal] _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Intensité Relief", Range(0, 2)) = 1.0
        
        _Glossiness("Vernis (Smoothness)", Range(0, 1)) = 0.8
        _Metallic("Métallique", Range(0, 1)) = 0.0
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 normalWS   : TEXCOORD3;
                float3 viewDirWS  : TEXCOORD4;
                float4 tangentWS  : TEXCOORD5;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_BumpMap);
            SAMPLER(sampler_BumpMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                half4 _BaseColor;
                half _Glossiness;
                half _Metallic;
                half _BumpScale;
            CBUFFER_END

            Varyings vert (Attributes input)
            {
                Varyings output;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                
                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                output.normalWS = normalInput.normalWS;
                output.tangentWS = float4(normalInput.tangentWS, input.tangentOS.w);
                output.viewDirWS = GetWorldSpaceViewDir(vertexInput.positionWS);
                
                return output;
            }

            half4 frag (Varyings input) : SV_Target
            {
                // Couleur de base
                half4 albedo = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * _BaseColor;
                
                // Normales (Calcul TBN pour le relief)
                float3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv), _BumpScale);
                float3 sNormalWS = input.normalWS;
                float3 sTangentWS = input.tangentWS.xyz;
                float3 sBitangentWS = cross(sNormalWS, sTangentWS) * input.tangentWS.w;
                float3 normalWS = TransformTangentToWorld(normalTS, float3x3(sTangentWS, sBitangentWS, sNormalWS));

                // Données de surface pour le moteur de rendu URP
                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = albedo.rgb;
                surfaceData.metallic = _Metallic;
                surfaceData.specular = 0;
                surfaceData.smoothness = _Glossiness;
                surfaceData.normalTS = normalTS; 
                surfaceData.occlusion = 1.0;
                surfaceData.alpha = albedo.a;

                // Données d'éclairage
                InputData inputData = (InputData)0;
                inputData.positionCS = input.positionCS;
                inputData.normalWS = normalize(normalWS);
                inputData.viewDirectionWS = normalize(input.viewDirWS);
                inputData.shadowCoord = 0;
                inputData.bakedGI = SampleSH(inputData.normalWS);
                return UniversalFragmentPBR(inputData, surfaceData);
            }
            ENDHLSL
        }
    }
}