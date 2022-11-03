
Shader "ScreenSpace/Depth"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
    
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                //float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                //float2 uv : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.screenPos = ComputeScreenPos(o.vertex);
                //o.uv = TRANSFORM_TEX(v.uv, _MainTex); // o.uv = (screenPos.xy / screenPos.w);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 textureCoord = i.screenPos.xy / i.screenPos.w;
                //textureCoord.x *= (_ScreenParams.x / _ScreenParams.y); // textureCoord.x * aspect
                
                // sample the texture
                fixed4 col = tex2D(_MainTex, textureCoord);
                
                return float4(0, 1, 0, 1) * col;
            }
            ENDCG
        }
    }
}
