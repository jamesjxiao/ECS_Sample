Shader "James/grid_instance"
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
            #pragma multi_compile_instancing

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(fixed4, _WColor)
                UNITY_DEFINE_INSTANCED_PROP(fixed4, _BColor)
            UNITY_INSTANCING_BUFFER_END(Props)

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Cell;

            v2f vert (appdata v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                o.vertex = UnityObjectToClipPos(v.vertex);
                //o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.uv = v.uv;
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float4 wColor = UNITY_ACCESS_INSTANCED_PROP(Props, _WColor);
                float4 bColor = UNITY_ACCESS_INSTANCED_PROP(Props, _BColor);

                float x = i.uv.x * _Cell;
                float y = i.uv.y * _Cell;

                if (fmod(x, 1) >= 0.5 && fmod(y, 1) >= 0.5)
                    return wColor;
                else if (fmod(x, 1) <= 0.5 && fmod(y, 1) <= 0.5)
                    return wColor;
                else
                    return bColor;
            }
            ENDCG
        }
    }

}