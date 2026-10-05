Shader "Custom/MobileSingleColorGlossy"
{
    Properties
    {
        // Main Texture & Base Tint
        _MainTex ("Albedo (RGB) & Specular Amount (A)", 2D) = "white" {}
        _Color ("Main Color", Color) = (1,1,1,1)

        // Normal Map Properties
        [Header(Normal Mapping)]
        [Toggle(_NORMALMAP_ON)] _NormalMapToggle ("Enable Normal Map", Float) = 0
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _NormalStrength ("Normal Strength", Range(0, 2)) = 1.0

        // Occlusion Map Properties
        [Header(Occlusion)]
        [Toggle(_OCCLUSIONMAP_ON)] _OcclusionMapToggle ("Enable Occlusion Map", Float) = 0
        _OcclusionMap ("Occlusion Map (R)", 2D) = "white" {}
        _OcclusionStrength ("Occlusion Strength", Range(0, 1)) = 1.0

        // Specular Properties
        [Header(Specular)]
        _SpecularColor ("Specular Color", Color) = (1,1,1,1)
        _SpecularSize ("Specular Size", Range(0, 1)) = 0.5
        _SpecularFocus ("Specular Focus", Range(0.01, 1)) = 0.5

        // Fresnel Emission Properties
        [Header(Fresnel)]
        _FresnelColor ("Fresnel Color", Color) = (0,0,0,1)
        _FresnelStrength ("Fresnel Strength", Range(0, 5)) = 0.0
        _FresnelPower ("Fresnel Power (Width)", Range(0.1, 10.0)) = 1.0
        _FresnelDirection ("Fresnel Direction (View Space)", Vector) = (0,1,0,0)
        _FresnelDirectionInfluence ("Fresnel Direction Influence", Range(0, 1)) = 0.0

        // Reflection Map
        [Header(Reflection)]
        [Toggle(_REFLECTION_ON)] [NoScaleOffset] _ReflectionToggle("Enable Reflection", Float) = 0
        _ReflectionColor("Reflection Tint", Color) = (1,1,1,1)
        [NoScaleOffset] _ReflectionCube("Reflection Cubemap", Cube) = "_Skybox" {}
        _ReflectionStrength("Reflection Strength", Range(0, 1)) = 0.5
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200
        Pass
        {
            Name "ForwardBase"
            Tags { "LightMode" = "ForwardBase" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase
            
            #pragma shader_feature _NORMALMAP_ON
            #pragma shader_feature _OCCLUSIONMAP_ON
            #pragma shader_feature _REFLECTION_ON
            
            #include "UnityCG.cginc"
            #include "Lighting.cginc"
            #include "AutoLight.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                half3 normal : NORMAL;
                half4 tangent : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                half3 worldNormal : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
                half3 viewSpaceNormal : TEXCOORD6;
                #ifdef _NORMALMAP_ON
                    half3 tspace0 : TEXCOORD3;
                    half3 tspace1 : TEXCOORD4;
                    half3 tspace2 : TEXCOORD5;
                #endif
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            half4 _Color;

            #ifdef _NORMALMAP_ON
                sampler2D _NormalMap;
                half _NormalStrength;
            #endif
            #ifdef _OCCLUSIONMAP_ON
                sampler2D _OcclusionMap;
                half _OcclusionStrength;
            #endif
            half4 _SpecularColor;
            half _SpecularSize;
            half _SpecularFocus;
            half4 _FresnelColor;
            half _FresnelStrength;
            half _FresnelPower;
            half3 _FresnelDirection;
            half _FresnelDirectionInfluence;
            #ifdef _REFLECTION_ON
                samplerCUBE _ReflectionCube;
                half4 _ReflectionColor;
                half _ReflectionStrength;
            #endif
            
            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                
                half3 worldNormal = UnityObjectToWorldNormal(v.normal);
                o.worldNormal = worldNormal;
                o.viewSpaceNormal = mul((half3x3)UNITY_MATRIX_V, worldNormal);
                #ifdef _NORMALMAP_ON
                    half3 worldTangent = UnityObjectToWorldDir(v.tangent.xyz);
                    half tangentSign = v.tangent.w * unity_WorldTransformParams.w;
                    half3 worldBitangent = cross(worldNormal, worldTangent) * tangentSign;
                    o.tspace0 = half3(worldTangent.x, worldBitangent.x, worldNormal.x);
                    o.tspace1 = half3(worldTangent.y, worldBitangent.y, worldNormal.y);
                    o.tspace2 = half3(worldTangent.z, worldBitangent.z, worldNormal.z);
                #endif
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                // Sample main texture (RGB for color and A for specular)
                half4 tex = tex2D(_MainTex, i.uv);
                half4 albedo = tex * _Color;
                half specularAmount = tex.a;

                // Normal calculation
                half3 worldNormal;
                #ifdef _NORMALMAP_ON
                    half3 normalTex = UnpackNormal(tex2D(_NormalMap, i.uv));
                    half3 blendedNormal = lerp(half3(0,0,1), normalTex, _NormalStrength);
                    worldNormal = normalize(half3(
                        dot(i.tspace0, blendedNormal),
                        dot(i.tspace1, blendedNormal),
                        dot(i.tspace2, blendedNormal)
                    ));
                #else
                    worldNormal = normalize(i.worldNormal);
                #endif

                // Occlusion
                half occlusion = 1.0;
                #ifdef _OCCLUSIONMAP_ON
                    occlusion = tex2D(_OcclusionMap, i.uv).r;
                    occlusion = lerp(1.0, occlusion, _OcclusionStrength);
                #endif

                half3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                half3 viewDir = normalize(_WorldSpaceCameraPos.xyz - i.worldPos);
                half3 halfVector = normalize(lightDir + viewDir);

                // Diffuse
                half NdotL = saturate(dot(worldNormal, lightDir));
                half3 diffuse = albedo.rgb * _LightColor0.rgb * NdotL;
                half3 ambient = UNITY_LIGHTMODEL_AMBIENT.rgb * albedo.rgb;

                // Specular (Toon-like Focus)
                half NdotH = saturate(dot(worldNormal, halfVector));
                half specularPower = exp2(_SpecularSize * 10.0 + 1.0);
                half specularRaw = pow(NdotH, specularPower);
                half focusThreshold = 1.0 - _SpecularFocus;
                half specularFocused = smoothstep(focusThreshold, 1.0, specularRaw);
                half3 specular = _SpecularColor.rgb * _LightColor0.rgb * specularFocused * specularAmount;

                // Fresnel
                half NdotV = saturate(dot(worldNormal, viewDir));
                half fresnelFactor = pow(1.0 - NdotV, _FresnelPower);
                half directionalMask = 1.0;
                if (_FresnelDirectionInfluence > 0.0)
                {
                    half3 viewSpaceNormal = normalize(i.viewSpaceNormal);
                    half3 fresnelDir = normalize(_FresnelDirection);
                    directionalMask = lerp(1.0, saturate(dot(viewSpaceNormal, fresnelDir)), _FresnelDirectionInfluence);
                }
                half3 fresnelEmission = _FresnelColor.rgb * fresnelFactor * _FresnelStrength * directionalMask;

                // Reflection
                half3 reflection = half3(0,0,0);
                #ifdef _REFLECTION_ON
                    half3 reflectVec = reflect(-viewDir, worldNormal);
                    half4 reflectColor = texCUBE(_ReflectionCube, reflectVec);
                    half3 finalReflectColor = lerp(_ReflectionColor.rgb, reflectColor.rgb * _ReflectionColor.rgb, reflectColor.a);
                    reflection = finalReflectColor * _ReflectionStrength * albedo.rgb * specularAmount;
                #endif
                
                half3 finalColor = (ambient + diffuse + specular + fresnelEmission + reflection) * occlusion;
                return half4(finalColor, albedo.a);
            }
            ENDCG
        }

        // Shadow Caster Pass
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            Cull Back
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_shadowcaster
            #include "UnityCG.cginc"
            struct v2f { V2F_SHADOW_CASTER; };
            v2f vert(appdata_base v) { v2f o; TRANSFER_SHADOW_CASTER_NORMALOFFSET(o) return o; }
            float4 frag(v2f i) : SV_Target { SHADOW_CASTER_FRAGMENT(i) }
            ENDCG
        }
    }
    FallBack "Mobile/Diffuse"
}