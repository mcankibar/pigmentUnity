using UnityEngine;
namespace Pigment {
[CreateAssetMenu(menuName="Pigment/Vessel Shape")]
public sealed class VesselDefinition : ScriptableObject {
    [Min(.1f)] public float height=1.04f,rBottom=.31f,rTop=.38f;
    [Min(.001f)] public float wall=.022f,baseThickness=.13f;
    public float bulge=0,curve=1;
    public Mesh glassMesh;
    public GameObject prefab;
    public float Outer(float y){float t=Mathf.Clamp01(y/height);return rBottom+(rTop-rBottom)*Mathf.Pow(t,curve)+bulge*Mathf.Sin(Mathf.PI*t);}
    public float Inner(float y)=>Outer(y)-wall-.005f;
}
}
