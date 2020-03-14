Shader "James/grid"
{
    Properties
    {
        _Cell ("cell ", Float) = 10
        _WColor("w color", Color) = (1, 1, 1, 1)
        _BColor ("b color", Color) = (0, 0, 0, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

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
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Cell;
            fixed4 _WColor, _BColor;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                //o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.uv = v.uv;
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float x = i.uv.x * _Cell;
                float y = i.uv.y * _Cell;

                if (fmod(x, 1) >= 0.5 && fmod(y, 1) >= 0.5)
                    return _WColor;
                else if (fmod(x, 1) <= 0.5 && fmod(y, 1) <= 0.5)
                    return _WColor;
                else
                    return _BColor;
            }
            ENDCG
        }
    }

    FallBack "Diffuse"
}
