using System.Collections.Generic;
using UnityEngine;
namespace Pigment {
/// <summary>Each ballistic packet owns a measured pigment volume until it lands.</summary>
public sealed class PourStream {
    public sealed class Packet {public Vector3 position,velocity;public float volume,flow,age;public bool connected;}
    public readonly List<Packet> packets=new List<Packet>(180);readonly Stack<Packet> pool=new Stack<Packet>();
    readonly Mesh mesh;readonly GameObject go;readonly MeshRenderer renderer;readonly MaterialPropertyBlock props=new MaterialPropertyBlock();
    readonly List<Vector3> vertices=new List<Vector3>(1800),normals=new List<Vector3>(1800);readonly List<int> indices=new List<int>(11000);
    public bool flowing;Vector3 lip;float lipFlow;
    public bool Active=>packets.Count>0;
    public float FallingVolume {get {float t=0;foreach(var p in packets)t+=p.volume;return t;}}
    public PourStream(Transform parent,Material material,Color color){go=new GameObject("Ballistic pour stream",typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(parent,false);mesh=new Mesh{name="Pour stream tube"};mesh.MarkDynamic();go.GetComponent<MeshFilter>().sharedMesh=mesh;renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;props.SetColor("_BaseColor",color);renderer.SetPropertyBlock(props);renderer.enabled=false;}
    public void Emit(Vector3 p,Vector3 v,float flow,float volume){if(packets.Count>=150){var old=packets[0];packets.RemoveAt(0);packets[0].volume+=old.volume;packets[0].connected=false;pool.Push(old);}var q=pool.Count>0?pool.Pop():new Packet();q.position=p;q.velocity=v;q.flow=flow;q.volume=volume;q.age=0;q.connected=flowing;packets.Add(q);flowing=true;lip=p;lipFlow=flow;}
    public void Step(float dt,float gravity,System.Func<Packet,bool> absorb){for(int i=packets.Count-1;i>=0;i--){var p=packets[i];p.velocity.y-=gravity*dt;p.position+=p.velocity*dt;p.age+=dt;if(absorb(p)||p.age>2.5f){if(i+1<packets.Count)packets[i+1].connected=false;packets.RemoveAt(i);pool.Push(p);}}}
    public void Draw(Vector3 currentLip,float thickness){lip=currentLip;vertices.Clear();normals.Clear();indices.Clear();int count=packets.Count+(flowing&&packets.Count>0?1:0);renderer.enabled=count>=2;if(count<2)return;const int radial=10;
        for(int i=0;i<count;i++){bool attached=i==packets.Count;Vector3 p=attached?lip:packets[i].position;Vector3 vel=attached?Vector3.down:packets[i].velocity;float flow=attached?lipFlow:packets[i].flow;Vector3 tangent=vel.normalized;if(i+1<count)tangent=((i+1==packets.Count?lip:packets[i+1].position)-p).normalized;if(tangent.sqrMagnitude<.1f)tangent=Vector3.down;Vector3 u=Vector3.Cross(tangent,Mathf.Abs(tangent.y)<.9f?Vector3.up:Vector3.right).normalized,w=Vector3.Cross(tangent,u);float radius=Mathf.Clamp((.012f+.066f*Mathf.Sqrt(flow))*Mathf.Sqrt(.7f/Mathf.Max(vel.magnitude,.65f))*thickness,.0035f,.095f);for(int j=0;j<radial;j++){float a=j*Mathf.PI*2/radial;Vector3 n=u*Mathf.Cos(a)+w*Mathf.Sin(a);vertices.Add(p+n*radius);normals.Add(n);}if(i>0&&(attached||packets[i].connected)){for(int j=0;j<radial;j++){int a=(i-1)*radial+j,b=(i-1)*radial+(j+1)%radial;indices.Add(a);indices.Add(b);indices.Add(a+radial);indices.Add(b);indices.Add(b+radial);indices.Add(a+radial);}}}
        mesh.Clear();mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetTriangles(indices,0);mesh.RecalculateBounds();
    }
    public void Dispose(){VesselView.Release(mesh);VesselView.Release(go);}
}
}
