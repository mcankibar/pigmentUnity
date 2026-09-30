using UnityEngine;
namespace Pigment {
public enum VesselMotion { Home, Going, Pour, Returning }
public sealed class VesselView : MonoBehaviour {
    public VesselDefinition definition;
    public PigmentId pigment;
    public MeshFilter shell,liquidFilter;
    public MeshRenderer liquidRenderer;
    [System.NonSerialized] public PigmentMath.Cavity cavity;
    [System.NonSerialized] public float volume,scale=1,tilt,tiltVelocity,tiltTarget,hover,clearance,angle,travel,fromAngle,side=1;
    [System.NonSerialized] public Vector3 home,fromPosition;
    [System.NonSerialized] public VesselMotion motion;
    [System.NonSerialized] public PourStream stream;
    public float Height=>definition.height*scale;
    public float Rim=>definition.rTop*scale;
    LiquidSurface surface;MaterialPropertyBlock props;float lastPlane=-999;Vector3 lastUp;Color liquidColor;
    float wobble,wobbleVelocity;
    public void Initialize(float size,Material liquidMaterial){
        scale=size;transform.localScale=Vector3.one*size;cavity=new PigmentMath.Cavity(definition,size);
        if(surface!=null)Release(surface.mesh);surface=new LiquidSurface(definition);liquidFilter.sharedMesh=surface.mesh;
        liquidRenderer.sharedMaterial=liquidMaterial;props=new MaterialPropertyBlock();lastPlane=-999;
    }
    public void Impulse(float amount){wobbleVelocity+=amount;}
    public void UpdateLiquid(float dt,Color color,PigmentStyle style){
        if(surface==null)return;
        liquidRenderer.enabled=volume>1e-7f;if(!liquidRenderer.enabled)return;
        wobbleVelocity+=(-130*wobble-3*wobbleVelocity)*dt;wobble=Mathf.Clamp(wobble+wobbleVelocity*dt,-.2f,.2f);
        Vector3 worldUp=new Vector3(wobble*style.slosh,1,Mathf.Sin(Time.time*2)*wobble*.3f).normalized;
        Vector3 up=transform.InverseTransformDirection(worldUp).normalized;
        // Cavity solver uses world dimensions; geometry is in prefab-local units.
        float plane=cavity.Solve(up,volume)/scale;
        if(Mathf.Abs(plane-lastPlane)>1e-5f||(up-lastUp).sqrMagnitude>1e-8f){surface.Update(up,plane);lastPlane=plane;lastUp=up;}
        liquidColor=color;props.SetColor("_BaseColor",color);props.SetFloat("_Smoothness",1-style.roughness);props.SetFloat("_Clarity",style.clarity);props.SetFloat("_Glow",style.glow);liquidRenderer.SetPropertyBlock(props);
    }
    void OnDestroy(){if(surface!=null)Release(surface.mesh);}
    public static void Release(Object o){if(!o)return;if(Application.isPlaying)Destroy(o);else DestroyImmediate(o);}
}
}
