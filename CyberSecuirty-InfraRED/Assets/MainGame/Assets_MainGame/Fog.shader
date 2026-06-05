Shader "Custom/FogSphereDepth_URP"
{
    Properties
    {
        _FogColor ("Fog Color", Color) = (0.75, 0.78, 0.82, 1)
        _Density  ("Density", Range(0, 4)) = 1

        _FogStart ("Fog Start (meters)", Float) = 40
        _FogEnd   ("Fog End (meters)",   Float) = 180

        _BottomY  ("Bottom Y (world)", Float) = 0
        _TopY     ("Top Y (world)",    Float) = 30

        _NoiseTex ("Noise Texture (R)", 2D) = "white" {}
        _NoiseScale ("Noise Scale", Range(0.001, 1)) = 0.05
        _NoiseStrength ("Noise Strength", Range(0, 2)) = 1
        _NoiseSpeed ("Noise Speed (XZ)", Vector) = (0.02, 0.01, 0, 0)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "Queue"="Transparent"
            "RenderType"="Transparent"
        }

        Pass
        {
            Name "FogSphere"
            Tags { "LightMode"="UniversalForward" }

            // Camera is inside sphere => render backfaces (cull front faces).
            Cull Front

            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _FogColor;
                half  _Density;

                float _FogStart;
                float _FogEnd;

                float _BottomY;
                float _TopY;

                float4 _NoiseTex_ST;
                half   _NoiseScale;
                half   _NoiseStrength;
                float4 _NoiseSpeed;
            CBUFFER_END

            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 worldPos    : TEXCOORD0;
                float4 screenPos   : TEXCOORD1;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 worldPos = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.worldPos = worldPos;

                float4 hclip = TransformWorldToHClip(worldPos);
                OUT.positionHCS = hclip;
                OUT.screenPos = ComputeScreenPos(hclip);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // ----- Depth fog (distance fog) -----
                float2 uv = IN.screenPos.xy / max(IN.screenPos.w, 1e-6);
                float rawDepth = SampleSceneDepth(uv);
                float eyeDepth = LinearEyeDepth(rawDepth, _ZBufferParams);

                // 0 at FogStart, 1 at FogEnd
                half depthFog = smoothstep(_FogStart, _FogEnd, eyeDepth);

                // ----- Height fog (denser near ground) -----
                float heightT = saturate((IN.worldPos.y - _BottomY) / max(1e-4, (_TopY - _BottomY)));
                half heightFog = (half)(1.0 - heightT);

                // ----- Noise breakup (world XZ) -----
                float2 worldXZ = IN.worldPos.xz * _NoiseScale + _Time.y * _NoiseSpeed.xy;
                half n = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, worldXZ).r;
                half noise = lerp(1.0h, n, saturate(_NoiseStrength));

                // Final alpha
                half alpha = saturate(_Density * depthFog * heightFog * noise);

                return half4(_FogColor.rgb, alpha);
            }
            ENDHLSL
        }
    }
}