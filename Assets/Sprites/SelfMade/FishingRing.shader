Shader "Unlit/FishingRing"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _CircleColor ("Circle Color", Color) = (0.5, 0.5, 0.5, 0.15)
        _RingColor ("Ring Color", Color) = (0,0,0,0.15)

        _Radius ("Circle Radius", Range(0,0.5)) = 0.4
        _RingThickness ("Ring Thickness", Range(0,0.5)) = 0.05
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "CanUseSpriteAtlas"="True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        Lighting Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 uv       : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 uv       : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;

            fixed4 _CircleColor;
            fixed4 _RingColor;

            float _Radius;
            float _RingThickness;

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv) * i.color;

                float2 centeredUV = i.uv - 0.5;

                float dist = length(centeredUV);

                float smooth = 0.005;

                float circle = smoothstep(_Radius, _Radius - smooth, dist);

                float outer = smoothstep(_Radius + _RingThickness,
                                         _Radius + _RingThickness - smooth,
                                         dist);

                float inner = smoothstep(_Radius,
                                         _Radius - smooth,
                                         dist);

                float ring = saturate(outer - inner);

                fixed4 result = 0;
                result += _CircleColor * circle;
                result += _RingColor * ring;

                result.a *= tex.a;

                return result;
            }
            ENDCG
        }
    }
}
