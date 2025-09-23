Shader "UI/FishFightShader"
{
    Properties
    {
        _GreenColor("Green color", Color) = (0,1,0,1)
        _RedColor("Red color", Color) = (1,0,0,1)
        _Rarity("Rarity", Range(1,15)) = 1
        _YellowSize("Yellow area size", Range(1,1000)) = 100
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        ZTest Always

        Pass
        {
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
                float4 vertex : SV_POSITION;
            };

            float4 _GreenColor;
            float4 _RedColor;
            float _Rarity;
            float _YellowSize;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float inverseLerp(float a, float b, float v) { return (v - a) / (b - a); }
            float normalize(float x, float min, float max) { return (x - min) / (max - min); }

            fixed4 frag(v2f i) : SV_Target
            {
                float x = 0;
                float y = 0;

                if (i.uv.x > ((1.0/8.0) * _Rarity + 1.0/8.0)/2.0 && i.uv.x < 1 - (((1.0/8.0) * _Rarity + 1.0/8.0)/2.0))
                    x = 1;
                else
                    y = 1;

                float yellowNorm = normalize(_YellowSize, 1, 1000);
                float centerLeft = ((1.0/8.0)*_Rarity + 1.0/8.0)/2.0;
                float centerRight = 1 - centerLeft;

                if (i.uv.x > centerLeft - yellowNorm/2 && i.uv.x < centerLeft + yellowNorm/2)
                {
                    float xMin = centerLeft - yellowNorm/4;
                    float xMax = centerLeft + yellowNorm/4;
                    x = inverseLerp(xMin, xMax, i.uv.x) + 0.5;
                    y = 1 - inverseLerp(xMin, xMax, i.uv.x) + 0.5;
                }
                else if (i.uv.x > centerRight - yellowNorm/2 && i.uv.x < centerRight + yellowNorm/2)
                {
                    float xMin = centerRight - yellowNorm/4;
                    float xMax = centerRight + yellowNorm/4;
                    x = 1 - inverseLerp(xMin, xMax, i.uv.x) + 0.5;
                    y = inverseLerp(xMin, xMax, i.uv.x) + 0.5;
                }

                return float4(y, x, 0, 1); // alpha=1 for UI
            }
            ENDCG
        }
    }
}
