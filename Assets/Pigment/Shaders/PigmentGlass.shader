Shader "Pigment/Glass" {
Properties { _BaseColor("Glass tint",Color)=(0.8,0.91,0.88,0.12) }
SubShader {
Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+10" "RenderType"="Transparent" }
Pass {
Blend SrcAlpha OneMinusSrcAlpha
ZWrite Off
Cull Back
HLSLPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
struct A {float4 positionOS:POSITION;float3 normalOS:NORMAL;};
struct V {float4 positionCS:SV_POSITION;float3 pos:TEXCOORD0;float3 normal:TEXCOORD1;};
CBUFFER_START(UnityPerMaterial)
float4 _BaseColor;
CBUFFER_END
V vert(A i){V o;o.pos=TransformObjectToWorld(i.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.pos);o.normal=TransformObjectToWorldNormal(i.normalOS);return o;}
half4 frag(V i):SV_Target {float3 n=normalize(i.normal),v=normalize(GetWorldSpaceViewDir(i.pos));float fres=pow(1-saturate(abs(dot(n,v))),3);float key=pow(saturate(dot(reflect(-v,n),normalize(float3(-.5,1,.8)))),48);float strip=pow(saturate(dot(n,normalize(float3(-.85,.03,.45)))),90);float strip2=pow(saturate(dot(n,normalize(float3(.95,.03,.3)))),130);float glint=saturate(key*.8+strip*.7+strip2*.5);return half4(lerp(_BaseColor.rgb,float3(1,.95,.83),saturate(fres+glint)),saturate(.035+fres*.42+glint*.65));}
ENDHLSL
}
}
}
