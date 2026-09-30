using System.Collections.Generic;
using UnityEngine;
namespace Pigment {
public static class VesselMesh {
    public static Mesh Lathe(List<Vector2> profile,int radial=64){
        var v=new List<Vector3>();var uv=new List<Vector2>();var ix=new List<int>();
        for(int j=0;j<profile.Count;j++)for(int i=0;i<=radial;i++){float a=i*2*Mathf.PI/radial;v.Add(new Vector3(profile[j].x*Mathf.Cos(a),profile[j].y,profile[j].x*Mathf.Sin(a)));uv.Add(new Vector2((float)i/radial,profile[j].y));}
        for(int j=0;j<profile.Count-1;j++)for(int i=0;i<radial;i++){int a=j*(radial+1)+i,b=a+radial+1;ix.Add(a);ix.Add(b);ix.Add(a+1);ix.Add(a+1);ix.Add(b);ix.Add(b+1);}
        var m=new Mesh{name="Revolved vessel"};m.SetVertices(v);m.SetUVs(0,uv);m.SetTriangles(ix,0);m.RecalculateNormals();m.RecalculateBounds();return m;
    }
    public static Mesh Glass(VesselDefinition d){
        var p=new List<Vector2>{new Vector2(0,.018f),new Vector2(d.rBottom*.78f,.002f),new Vector2(d.rBottom,.03f)};
        for(int i=1;i<=24;i++){float y=Mathf.Lerp(.03f,d.height-d.wall*.5f,i/24f);p.Add(new Vector2(d.Outer(y),y));}
        float lip=d.wall*.5f;for(int i=1;i<=8;i++){float a=i*Mathf.PI/8;p.Add(new Vector2(d.rTop-lip+Mathf.Cos(a)*lip,d.height-lip+Mathf.Sin(a)*lip));}
        for(int i=1;i<=24;i++){float y=Mathf.Lerp(d.height-lip,d.baseThickness+.025f,i/24f);p.Add(new Vector2(d.Outer(y)-d.wall,y));}
        p.Add(new Vector2(d.Outer(d.baseThickness)-d.wall-.025f,d.baseThickness));p.Add(new Vector2(0,d.baseThickness));return Lathe(p);
    }
    public static Mesh LiquidHull(VesselDefinition d){var p=new List<Vector2>{new Vector2(0,d.baseThickness+.005f)};for(int i=0;i<=24;i++){float y=Mathf.Lerp(d.baseThickness+.005f,d.height-.005f,i/24f);p.Add(new Vector2(d.Inner(y),y));}p.Add(new Vector2(0,d.height-.005f));return Lathe(p,48);}
}
/// <summary>Clips the vessel cavity against a gravity plane and closes the cut with a flat surface.</summary>
public sealed class LiquidSurface {
    struct Vertex {public Vector3 p,n;public Vertex(Vector3 p,Vector3 n){this.p=p;this.n=n;} }
    readonly Vector3[] baseV,baseN;readonly int[] baseT;
    readonly List<Vector3> vertices=new List<Vector3>(16000),normals=new List<Vector3>(16000),cut=new List<Vector3>(400);
    readonly List<int> triangles=new List<int>(24000);readonly Vertex[] input=new Vertex[4],output=new Vertex[4];
    public readonly Mesh mesh;
    public LiquidSurface(VesselDefinition d){Mesh hull=VesselMesh.LiquidHull(d);baseV=hull.vertices;baseN=hull.normals;baseT=hull.triangles;VesselView.Release(hull);mesh=new Mesh{name="Dynamic horizontal liquid"};mesh.MarkDynamic();}
    void Tri(Vertex a,Vertex b,Vertex c){int i=vertices.Count;vertices.Add(a.p);vertices.Add(b.p);vertices.Add(c.p);normals.Add(a.n);normals.Add(b.n);normals.Add(c.n);triangles.Add(i);triangles.Add(i+1);triangles.Add(i+2);}
    public void Update(Vector3 up,float plane){
        vertices.Clear();normals.Clear();triangles.Clear();cut.Clear();
        for(int t=0;t<baseT.Length;t+=3){for(int k=0;k<3;k++){int id=baseT[t+k];input[k]=new Vertex(baseV[id],baseN[id]);}int n=0;
            for(int k=0;k<3;k++){var a=input[k];var b=input[(k+1)%3];float da=Vector3.Dot(up,a.p)-plane,db=Vector3.Dot(up,b.p)-plane;bool ina=da<=0,inb=db<=0;if(ina)output[n++]=a;if(ina!=inb){float f=da/(da-db);var q=new Vertex(Vector3.Lerp(a.p,b.p,f),Vector3.Lerp(a.n,b.n,f).normalized);output[n++]=q;cut.Add(q.p);}}
            for(int k=1;k<n-1;k++)Tri(output[0],output[k],output[k+1]);
        }
        if(cut.Count>2){Vector3 center=Vector3.zero;foreach(var p in cut)center+=p;center/=cut.Count;Vector3 u=Vector3.Cross(up,Mathf.Abs(up.z)<.9f?Vector3.forward:Vector3.right).normalized,v=Vector3.Cross(up,u);cut.Sort((a,b)=>Mathf.Atan2(Vector3.Dot(a-center,v),Vector3.Dot(a-center,u)).CompareTo(Mathf.Atan2(Vector3.Dot(b-center,v),Vector3.Dot(b-center,u))));for(int i=0;i<cut.Count;i++){Vector3 a=cut[i],b=cut[(i+1)%cut.Count];if((a-b).sqrMagnitude>1e-10)Tri(new Vertex(center,up),new Vertex(a,up),new Vertex(b,up));}}
        mesh.Clear();mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();
    }
}
}
