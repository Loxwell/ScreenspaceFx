
// github Unity-Technologies/Graphics (hlsl)
// https://github.com/Unity-Technologies/Graphics/tree/master/Packages/com.unity.render-pipelines.universal/ShaderLibrary

// Gaussian DoF Depth texture 활용 방법 분석
// TEXTURE2D_X ??
// https://github.com/Unity-Technologies/Graphics/blob/6fdc7996098aa184495c0a3c0da10135bfe18340/Packages/com.unity.render-pipelines.universal/Shaders/PostProcessing/GaussianDepthOfField.shader

// example
// https://www.cyanilux.com/tutorials/depth/

Shader "ScreenSpace/Depth"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    
    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
    #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
    #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
    ENDHLSL

    SubShader
    {
        Pass
        {
            Name "DEPTH-PASS"
        
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            //#include "UnityCG.cginc"
                       
            float4 ComputeScreenPos(float4 positionHS, out float4 positionNDC)
            {
                positionNDC = positionHS * 0.5;
                positionNDC.xy = float2(positionNDC.x, positionNDC.y * _ProjectionParams.x) + positionNDC.w;
                positionNDC.zw = positionHS.zw;
                return positionNDC;
            }
            
            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 positionCS    : VS_POSITION;
                float4 screenPos     : TEXCOORD0;
                float4 positionVS    : TEXCOORD1;
                float3 viewDirVector : TEXCOORD2;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert(appdata i)
            {
                v2f o = (v2f)0;
                // TransformObjectToWorld(), TransformWorldToView(), ransformWorldToHClip() // SpaceTransform.hlsl
                
                float4 positionWS = mul(UNITY_MATRIX_M, i.vertex);
                float4 positionVS = mul(UNITY_MATRIX_V, positionWS);
                float4 positionHS = mul(UNITY_MATRIX_P, positionVS);
                
                o.positionVS = positionVS;
                o.positionCS = positionHS;
                o.viewDirVector = _WorldSpaceCameraPos - positionWS;

                // ComputeScreenPos : Clip space to NDC (Normalized Device Coordinates)
                // positionNDC 
                ComputeScreenPos(positionVS, o.screenPos);
                
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                half fragmentEyeDepth = - i.positionVS.z;
                float rawDepth = SampleSceneDepth(i.screenPos.xy / i.screenPos.w);
                float sceneEyeDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
                float3 worldPos = _WorldSpaceCameraPos - ((i.viewDirVector / fragmentEyeDepth) * sceneEyeDepth);

                return float4(1,0,0,1);// float4(frac(worldPos), 1);
            }

            ENDHLSL
        }

        Pass
        {
            Name "TEST-PASS"

            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag
    
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                
                // Tranforms position from object to homogenous space
                o.vertex = UnityObjectToClipPos(v.vertex); // mul(UNITY_MATRIX_VP, mul(unity_ObjectToWorld, float4(pos, 1.0))) // More efficient than computing M*VP matrix product
                o.screenPos = ComputeScreenPos(o.vertex); 
                o.uv = TRANSFORM_TEX(v.uv, _MainTex); // o.uv = (o.screenPos.xy / o.screenPos.w); // also equivalent to

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 textureCoord = i.screenPos.xy / i.screenPos.w;
                //textureCoord.x *= (_ScreenParams.x / _ScreenParams.y); // textureCoord.x * aspect // 정사각형
                
                // sample the texture
                // also equivalent to
                // fixed4 col = tex2D(_MainTex, i.uv);
                fixed4 col = tex2D(_MainTex, textureCoord); 
                return float4(0, 1, 0, 1) * col;
            }
            ENDCG
        }
    }
}
