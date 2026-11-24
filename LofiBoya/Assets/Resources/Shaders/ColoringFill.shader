// LofiBoya boyama shader'ı (UI / RawImage için)
// _MainTex   : çizgi resmi (gri ton). Son renk = bölge rengi * çizgi parlaklığı
// _RegionTex : bölge haritası, id = R*255 + G*255*256 (point, linear)
// _ColorTex  : bölge renkleri, 256 x satır (point, sRGB)
Shader "LofiBoya/ColoringFill"
{
    Properties
    {
        [PerRendererData] _MainTex ("Lines", 2D) = "white" {}
        _RegionTex ("Regions", 2D) = "black" {}
        _ColorTex ("Colors", 2D) = "white" {}
        _PaintTex ("Paint", 2D) = "black" {}
        _StrokeTex ("Stroke", 2D) = "black" {}
        _StrokeColor ("Stroke Color", Color) = (1,1,1,1)
        _StrokeMode ("Stroke Mode", Float) = 0
        _ColorRows ("Color Rows", Float) = 1
        _TexSize ("Texture Size", Vector) = (1,1,0,0)
        _FillRegion ("Fill Region", Float) = -1
        _FillCenter ("Fill Center", Vector) = (0,0,0,0)
        _FillRadius ("Fill Radius", Float) = 0
        _FillColor ("Fill Color", Color) = (1,1,1,1)
        _HintRegion ("Hint Region", Float) = -1
        _HintColor ("Hint Color", Color) = (1,0.7,0.75,1)

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT

            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; float4 worldPos : TEXCOORD1; };

            sampler2D _MainTex, _RegionTex, _ColorTex, _PaintTex, _StrokeTex;
            float _ColorRows, _FillRegion, _FillRadius, _HintRegion, _StrokeMode;
            float _LofiCalm;   // genel değer (Motion): 1 = sakin mod, simli/gökkuşağı/ipucu kıpırdamaz
            float4 _TexSize, _FillCenter, _FillColor, _HintColor, _ClipRect, _StrokeColor;

            // ---- Sihirli boyalar (MagicPaint.cs ile aynı hesap) ----
            float Hash(float2 p) { return frac(sin(p.x * 127.1 + p.y * 311.7) * 43758.5453); }
            float3 Hsv(float h, float s, float v)
            {
                float3 k = saturate(abs(frac(h + float3(0, 2.0 / 3.0, 1.0 / 3.0)) * 6.0 - 3.0) - 1.0);
                return v * lerp(float3(1, 1, 1), k, s);
            }
            // Sabit renkler sRGB yazıldı; proje Linear renk uzayında: renk dokusu okunurken doğrusala çevrilir,
            // sabitler de çevrilmeli (yoksa gökkuşağı/yıldız tuvalde küçük resimdekinden soluk görünür).
            float3 SrgbConst(float3 c)
            {
            #ifdef UNITY_COLORSPACE_GAMMA
                return c;
            #else
                return GammaToLinearSpace(c);
            #endif
            }

            float3 Magic(float3 c, float style, float2 px)
            {
                float t = _Time.y * (1.0 - _LofiCalm);
                if (style < 1.5) // simli
                {
                    float2 cell = floor(px / 6.0);
                    float h = Hash(cell);
                    float tw = 0.5 + 0.5 * sin(t * 3.0 + h * 40.0);
                    float3 col = c * (0.9 + 0.2 * Hash(cell + 3.1));
                    col = lerp(col, float3(1, 1, 1), step(0.86, h) * tw * 0.85);
                    float2 big = floor(px / 34.0);
                    float hb = Hash(big + 7.7);
                    float2 f = frac(px / 34.0) - 0.5;
                    float star4 = max(0.0, 1.0 - (abs(f.x) * abs(f.y) * 600.0 + length(f) * 6.0));
                    col += star4 * step(0.7, hb) * (0.5 + 0.5 * sin(t * 2.0 + hb * 30.0));
                    return col;
                }
                if (style < 2.5) // gökkuşağı (yavaşça akar)
                    return lerp(c, SrgbConst(Hsv((px.x + px.y) / 900.0 + t * 0.03, 0.5, 1.0)), 0.75);
                if (style < 3.5) // puantiye
                {
                    float gy = px.y / 40.0;
                    float gx = px.x / 40.0 + 0.5 * fmod(floor(gy), 2.0);
                    float2 f = frac(float2(gx, gy)) - 0.5;
                    float inside = 1.0 - smoothstep(0.17, 0.21, length(f));
                    return lerp(c, lerp(c, float3(1, 1, 1), 0.65), inside);
                }
                // yıldızlı
                {
                    float gy = px.y / 56.0;
                    float gx = px.x / 56.0 + 0.5 * fmod(floor(gy), 2.0);
                    float2 f = frac(float2(gx, gy)) - 0.5;
                    float a = atan2(f.x, f.y + 1e-6);   // hücre ortasında atan2(0,0) tanımsız (bazı mobil GPU'larda NaN)
                    float rr = 0.30 * (0.45 + 0.55 * pow(abs(cos(a * 2.5)), 3.0));
                    float inside = 1.0 - smoothstep(rr - 0.02, rr + 0.02, length(f));
                    return lerp(c, lerp(c, SrgbConst(float3(1, 0.95, 0.7)), 0.75), inside);
                }
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.worldPos = v.vertex;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float lum = tex2D(_MainTex, i.uv).r;
                float2 rg = tex2D(_RegionTex, i.uv).rg;
                float id = round(rg.x * 255.0) + round(rg.y * 255.0) * 256.0;
                float2 cuv = float2((fmod(id, 256.0) + 0.5) / 256.0, (floor(id / 256.0) + 0.5) / _ColorRows);
                float4 c = tex2D(_ColorTex, cuv);

                // Dokunulan noktadan yayılan boya dalgası
                if (abs(id - _FillRegion) < 0.5)
                {
                    float d = length((i.uv - _FillCenter.xy) * _TexSize.xy);
                    float k = 1.0 - smoothstep(_FillRadius - 4.0, _FillRadius, d);
                    float rim = smoothstep(_FillRadius - 18.0, _FillRadius - 4.0, d) * k;
                    float4 fc = _FillColor;
                    fc.rgb = lerp(fc.rgb, fc.rgb * 1.12 + 0.04, rim);
                    c.rgb = lerp(c.rgb, fc.rgb, k);
                    c.a = k > 0.5 ? fc.a : c.a;
                }

                // Sihirli boya: stil renk dokusunun alfa kanalında (alfa = 1 - stil/255)
                float style = round((1.0 - c.a) * 255.0);
                if (style > 0.5) c.rgb = Magic(c.rgb, style, i.uv * _TexSize.xy);

                // İpucu: boyanmamış bölge yumuşakça parlar
                if (abs(id - _HintRegion) < 0.5)
                {
                    float p = lerp(0.5 + 0.5 * sin(_Time.y * 6.0), 0.7, _LofiCalm);
                    c.rgb = lerp(c.rgb, _HintColor.rgb, 0.25 + 0.45 * p);
                }

                // Boya kalemi katmanı (premultiplied)
                float4 paint = tex2D(_PaintTex, i.uv);
                // şu an çizilmekte olan çizgi (parmak kalkınca katmana işlenir)
                if (_StrokeMode > 0.5)
                {
                    float sa = tex2D(_StrokeTex, i.uv).r;
                    if (_StrokeMode < 1.5) paint = float4(_StrokeColor.rgb * sa, sa) + paint * (1.0 - sa);
                    else paint *= (1.0 - sa);
                }
                c.rgb = c.rgb * (1.0 - paint.a) + paint.rgb;

                c.rgb *= lum;
                c.a = i.color.a;
                #ifdef UNITY_UI_CLIP_RECT
                c.a *= UnityGet2DClipping(i.worldPos.xy, _ClipRect);
                #endif
                return c;
            }
            ENDCG
        }
    }
}
