// Invisible mask: renders the water mesh into the stencil buffer only.
// It writes the value 1 into the stencil wherever the water shape is, without
// touching colour or depth. Fish spot sprites (see FishSpotMasked) then only
// draw where the stencil equals that value, clipping them to the water outline.
Shader "FishSpot/WaterStencilMask"
{
    SubShader
    {
        // Render before the transparent sprites (queue 3000) so the stencil is
        // populated by the time the spots are drawn.
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry-1" }

        Pass
        {
            ColorMask 0   // don't write any colour
            ZWrite Off
            ZTest Always
            Cull Off

            Stencil
            {
                Ref 1
                Comp Always
                Pass Replace
            }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; };
            struct v2f { float4 pos : SV_POSITION; };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return fixed4(0, 0, 0, 0);
            }
            ENDCG
        }
    }
}
