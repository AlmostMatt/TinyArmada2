Shader "TeamColorAlphaMask" {
    Properties{
        _Color("Color", Color) = (1,1,1,1)
        _MainTex("Albedo (RGB)", 2D) = "white" {}
        _Glossiness("Smoothness", Range(0,1)) = 0.5
        _Metallic("Metallic", Range(0,1)) = 0.0
        _TeamColor("Team Color", Color) = (0.5, 0.5, 0.5, 1)
    }
    SubShader{
        Tags{ "RenderType" = "Opaque" }
        LOD 200
 
        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0
 
        sampler2D _MainTex;
 
        struct Input {
            float2 uv_MainTex;
            float4 color : COLOR;
        };
 
        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        fixed4 _TeamColor;
 
        void surf(Input IN, inout SurfaceOutputStandard o) {
            fixed4 sampledColor = tex2D(_MainTex, IN.uv_MainTex);
            fixed4 c = sampledColor * IN.color * _Color;
            // Turns 0-alpha colors into team-color
            o.Albedo = lerp(_TeamColor.rgb, c.rgb, sampledColor.a);
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
 
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}