Shader "Unlit/fishFight shader"
{
    Properties
    {
        _GreenColor("Green color", Color) = (0, 1, 0, 1)
        _RedColor("Red color", Color) = (1, 0, 0, 1)
        _Rarity("Rarity", Range(1, 15)) = 1
        _YellowSize("Yellow area size", Range(1, 1000)) = 100
    }
    SubShader
    {
        Tags { "RenderType"="Opaque"}
        ZTest Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

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

            float4 _MainTex_ST;
            float4 _GreenColor;
            float4 _RedColor;
            float _Rarity;
            float _YellowSize;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float inverseLerp(float a, float b, float v) {
                return(v - a) / (b - a);
            }

            float normalize(float x, float min, float max) {
                return(x - min) / (max - min);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                float x = 0;
                float y = 0;
                if (i.uv.x > ((1.0f / 8.0f) * _Rarity + 1.0f / 8.0f) / 2.0f && i.uv.x < 1- (((1.0f / 8.0f)*_Rarity + 1.0f / 8.0f) / 2.0f))  {
                    x = 1;
                }
                else {
                    y = 1;
                }
                if (i.uv.x > (((1.0f / 8.0f) * _Rarity + 1.0f / 8.0f) / 2.0f) - (normalize(_YellowSize, 1, 1000) / 2) && i.uv.x < (((1.0f / 8.0f) * _Rarity + 1.0f / 8.0f) / 2.0f) + (normalize(_YellowSize, 1, 1000) / 2)) {
                    float xMin = (((1.0f / 8.0f) * _Rarity + 1.0f / 8.0f) / 2.0f) - (normalize(_YellowSize, 1, 1000) / 4);
                    float xMax = (((1.0f / 8.0f) * _Rarity + 1.0f / 8.0f) / 2.0f) + (normalize(_YellowSize, 1, 1000) / 4);
                    x = inverseLerp(xMin, xMax, i.uv.x) + 0.5f;
                    y = 1 - inverseLerp(xMin, xMax, i.uv.x) + 0.5f;
                }
                else if (i.uv.x > 1 - (((1.0f / 8.0f) * _Rarity + 1.0f / 8.0f) / 2.0f) - (normalize(_YellowSize, 1, 1000) / 2) && i.uv.x < 1 - (((1.0f / 8.0f) * _Rarity + 1.0f / 8.0f) / 2.0f) + (normalize(_YellowSize, 1, 1000) / 2)) {
                    float xMin = 1 - (((1.0f / 8.0f) * _Rarity + 1.0f / 8.0f) / 2.0f) - (normalize(_YellowSize, 1, 1000) / 4);
                    float xMax = 1 - (((1.0f / 8.0f) * _Rarity + 1.0f / 8.0f) / 2.0f) + (normalize(_YellowSize, 1, 1000) / 4);
                    x = 1 - inverseLerp(xMin, xMax, i.uv.x) + 0.5f;
                    y = inverseLerp(xMin, xMax, i.uv.x) + 0.5f;
                }
                return float4(y , x , 0, 1);
            }
            ENDCG
        }
    }
}
