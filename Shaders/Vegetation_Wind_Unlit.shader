Shader "2DCloth/Vegetation_Wind_Unlit"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _WindSpeed ("WindSpeed", Range(0, 2)) = 1
        _WindDirection ("WindDirection", Vector) = (1, 0.2, 0, 0)
        _WindScale ("WindScale", Float) = 0.02
        _WindStrength ("WindStrength", Float) = 0.1
        _WindInfluenceMask ("WindInfluenceMask", Float) = 4
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 200
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "Cloth2DShaderUtils.cginc"

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

            sampler2D _MainTex;
            float4 _MainTex_ST;
            half _WindSpeed;
            half _WindStrength;
            half4 _WindDirection;
            half _WindScale;
            half _WindInfluenceMask;

            v2f vert (appdata v)
            {
                v2f o;
                float4 vertex = UnityObjectToClipPos(v.vertex);
                half2 windDirection = half2(_WindDirection.x, _WindDirection.y);
                half2 wind = windDirection * _WindSpeed * _Time.y;
                half2 noiseUV = 0.f;
                half noise = 0.f;

                Unity_TilingAndOffset_float(float2(vertex.x, vertex.y), 1.f, wind, noiseUV);
                Unity_GradientNoise_float(noiseUV, _WindScale, noise);
                noise -= 0.5f;
                half2 windOffset = noise * _WindStrength * windDirection;
                windOffset *= clamp(pow(abs(v.uv.y), _WindInfluenceMask), 0.f, 1.f);

                vertex += half4(windOffset.x, windOffset.y, 0.f, 0.f);

                o.vertex = vertex;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv) * 1.f;
                return col;
            }
            ENDCG
        }
    }
}
