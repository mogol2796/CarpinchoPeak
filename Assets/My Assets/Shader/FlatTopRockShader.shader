Shader "Custom/LowPolyFlatTop"
{
    Properties
    {
        _MainColor ("Side Color", Color) = (0.5, 0.5, 0.5, 1)
        _TopColor ("Top Color", Color) = (0.2, 0.8, 0.2, 1)
        _Spread ("Top Spread", Range(-1, 1)) = 0.5
        _Sharpness ("Blend Sharpness", Range(1, 20)) = 10
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model
        #pragma surface surf Standard fullforwardshadows

        // Use Shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        struct Input
        {
            float3 worldNormal; // The direction the face is pointing
        };

        fixed4 _MainColor;
        fixed4 _TopColor;
        float _Spread;
        float _Sharpness;

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // 1. Get the direction: Is this face pointing up?
            // Dot Product compares two vectors. 
            // 1 = Exactly same direction. 0 = 90 degrees apart. -1 = Opposite.
            float upDot = dot(normalize(IN.worldNormal), float3(0,1,0));

            // 2. Calculate the mask
            // We take the dot product and adjust it by our "Spread" variable
            float mask = upDot - (1 - _Spread);
            
            // 3. Sharpen the mask
            // This creates the hard edge typical of low poly styles
            mask = saturate(mask * _Sharpness);

            // 4. Blend the colors based on the mask
            o.Albedo = lerp(_MainColor, _TopColor, mask).rgb;
            
            // Optional: Make it matte (rock-like)
            o.Metallic = 0.0;
            o.Smoothness = 0.0; 
        }
        ENDCG
    }
    FallBack "Diffuse"
}