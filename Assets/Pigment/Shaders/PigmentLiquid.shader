Shader "Pigment/Liquid" {
Properties { _BaseColor("Pigment",Color)=(1,.4,.1,1) _Smoothness("Smoothness",Range(0,1))=.96 _Clarity("Clarity",Range(0,1))=.7 _Glow("Glow",Range(0,1))=.18 }
SubShader {
Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Geometry" "RenderType"="Opaque"}
Pass {
Cull Off
HLSLPROGRAM
#pragma vertex vert
#pragma fragment frag
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
struct A {float4 positionOS:POSITION;float3 normalOS:NORMAL;};struct V {float4 positionCS:SV_POSITION;float3 pos:TEXCOORD0;float3 normal:TEXCOORD1;};
CBUFFER_START(UnityPerMaterial)
float4 _BaseColor;float _Smoothness,_Clarity,_Glow;
CBUFFER_END
V vert(A i){V o;o.pos=TransformObjectToWorld(i.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.pos);o.normal=TransformObjectToWorldNormal(i.normalOS);return o;}
half4 frag(V i):SV_Target {float3 n=normalize(i.normal),v=normalize(GetWorldSpaceViewDir(i.pos));float top=smoothstep(.75,.99,n.y);float3 l=normalize(float3(-.55,1,.5));float shade=.68+.28*saturate(dot(n,l));shade=lerp(shade,1,top);float fres=pow(1-saturate(abs(dot(n,v))),4);float spec=pow(saturate(dot(n,normalize(l+v))),lerp(15,160,_Smoothness));float ripple=sin(length(i.pos.xz)*35-_Time.y*3)*sin(i.pos.x*19+i.pos.z*13+_Time.y)*.018*top;float3 c=_BaseColor.rgb*(shade+ripple+_Glow*.15);c=lerp(c,float3(1,.98,.93),saturate(spec*.42+fres*.12*_Clarity));return half4(c,1);}
ENDHLSL
}
}
}
