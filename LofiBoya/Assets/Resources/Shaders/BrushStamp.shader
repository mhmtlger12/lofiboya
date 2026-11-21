// Boya kalemi.
// Pass 0: damgaları "çizgi" katmanına basar (BlendOp Max). Aynı çizgide üst üste binen damgalar
//         birikmez; kâğıt dokusunun çukurları boş kalır, gerçek kuru boya gibi görünür.
//         Kalem yalnızca çizime başladığı bölgeye boyar (_Region), çizgilerin dışına taşmaz.
// Pass 1: parmak kalkınca çizgiyi boya katmanına işler (premultiplied "over").
// Pass 2: silgi çizgisini boya katmanından düşer.
Shader "Hidden/LofiBoya/BrushStamp"
{
    Properties
    {
        _MainTex ("Stroke", 2D) = "black" {}
        _RegionTex ("Regions", 2D) = "black" {}
        _Grain ("Grain", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _Region ("Region", Float) = -1
        _Opacity ("Opacity", Float) = 0.9
        _Hardness ("Hardness", Float) = 0.5
        _GrainScale ("Grain Scale", Vector) = (8,8,0,0)
        _GrainAmount ("Grain Amount", Float) = 0.6
    }

    CGINCLUDE
    #include "UnityCG.cginc"
    sampler2D _MainTex, _RegionTex, _Grain;
    float4 _Color, _GrainScale;
    float _Region, _Opacity, _Hardness, _GrainAmount;
    ENDCG

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        // 0: damga -> çizgi katmanı
        Pass
        {
            BlendOp Max
            Blend One One
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            struct appdata { float4 vertex : POSITION; float2 local : TEXCOORD0; float2 uv : TEXCOORD1; };
            struct v2f { float4 pos : SV_POSITION; float2 local : TEXCOORD0; float2 uv : TEXCOORD1; };
            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.local = v.local;
                o.uv = v.uv;
                return o;
            }
            float4 frag (v2f i) : SV_Target
            {
                float d = length(i.local);
                float a = 1.0 - smoothstep(_Hardness, 1.0, d);
                // kâğıdın pürüzü: iki ölçekli doku + eşik. Kenarlarda daha çok boşluk kalır.
                float g = tex2D(_Grain, i.uv * _GrainScale.xy).r;
                float g2 = tex2D(_Grain, i.uv * _GrainScale.xy * 2.7 + 0.37).r;
                float tooth = smoothstep(0.22 + 0.25 * (1.0 - a), 0.62, g * 0.55 + g2 * 0.6);
                a *= lerp(1.0, tooth, _GrainAmount) * _Opacity;
                if (_Region >= 0)
                {
                    float2 rg = tex2D(_RegionTex, i.uv).rg;
                    float id = round(rg.x * 255.0) + round(rg.y * 255.0) * 256.0;
                    if (abs(id - _Region) > 0.5) a = 0;
                }
                return float4(a, a, a, a);
            }
            ENDCG
        }

        // 1: çizgiyi boya katmanına işle
        Pass
        {
            Blend One OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            float4 frag (v2f_img i) : SV_Target
            {
                float sa = tex2D(_MainTex, i.uv).r;
                return float4(_Color.rgb * sa, sa);
            }
            ENDCG
        }

        // 2: silgi çizgisini uygula
        Pass
        {
            Blend Zero OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            float4 frag (v2f_img i) : SV_Target
            {
                float sa = tex2D(_MainTex, i.uv).r;
                return float4(0, 0, 0, sa);
            }
            ENDCG
        }
    }
}
