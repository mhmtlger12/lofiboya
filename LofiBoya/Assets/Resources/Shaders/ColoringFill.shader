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

            sampler2D _MainTex, _RegionTex, _ColorTex;
            float _ColorRows, _FillRegion, _FillRadius, _HintRegion;
            float4 _TexSize, _FillCenter, _FillColor, _HintColor, _ClipRect;

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
                    c = lerp(c, fc, k);
                }

                // İpucu: boyanmamış bölge yumuşakça parlar
                if (abs(id - _HintRegion) < 0.5)
                {
                    float p = 0.5 + 0.5 * sin(_Time.y * 6.0);
                    c.rgb = lerp(c.rgb, _HintColor.rgb, 0.25 + 0.45 * p);
                }

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
