// แสงเรืองทับรูป 2D ตอนผู้เล่นเล็ง (SpriteLighting ใช้) — บวกแสงเพิ่มบนรูปเดิม ทำให้สว่างกว่าสีจริงของรูปได้
// สี/ความแรงมาจาก SpriteRenderer.color (alpha = ความแรง) คูณ _Intensity
Shader "Saharan/SpriteAdditiveGlow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Intensity ("Intensity", Float) = 1.6
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Cull Off
        ZWrite Off
        Lighting Off
        Blend SrcAlpha One

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _Intensity;

            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 t = tex2D(_MainTex, i.uv);
                fixed4 c;
                c.rgb = (t.rgb * 0.5 + 0.5) * i.color.rgb * _Intensity; // ครึ่งหนึ่งตามลายรูป ครึ่งหนึ่งเป็นแสงสีเทียนล้วน
                c.a = t.a * i.color.a;
                return c;
            }
            ENDCG
        }
    }
}
