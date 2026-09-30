using UnityEngine;
using UnityEngine.UI;
namespace Pigment {
// Vector UI symbols avoid depending on optional font glyphs.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class PigmentIcon : MaskableGraphic {
    public bool refresh;
    protected override void OnPopulateMesh(VertexHelper vh){
        vh.Clear();var r=GetPixelAdjustedRect();Vector2 c=r.center;float radius=Mathf.Min(r.width,r.height)*.46f;
        if(!refresh){vh.AddVert(c,color,Vector2.zero);for(int i=0;i<10;i++){float a=(90+i*36)*Mathf.Deg2Rad;vh.AddVert(c+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius*(i%2==0?1:.44f),color,Vector2.zero);}for(int i=0;i<10;i++)vh.AddTriangle(0,1+i,1+(i+1)%10);}
        else {const int count=32;for(int i=0;i<=count;i++){float a=(35+i*285f/count)*Mathf.Deg2Rad;Vector2 d=new Vector2(Mathf.Cos(a),Mathf.Sin(a));vh.AddVert(c+d*radius,color,Vector2.zero);vh.AddVert(c+d*(radius*.7f),color,Vector2.zero);if(i>0){int n=i*2;vh.AddTriangle(n-2,n-1,n);vh.AddTriangle(n,n-1,n+1);}}float end=320*Mathf.Deg2Rad;Vector2 p=c+new Vector2(Mathf.Cos(end),Mathf.Sin(end))*radius*.86f;Vector2 t=new Vector2(-Mathf.Sin(end),Mathf.Cos(end));int k=vh.currentVertCount;vh.AddVert(p+t*radius*.44f,color,Vector2.zero);vh.AddVert(p-t*radius*.2f+new Vector2(t.y,-t.x)*radius*.4f,color,Vector2.zero);vh.AddVert(p-t*radius*.2f-new Vector2(t.y,-t.x)*radius*.4f,color,Vector2.zero);vh.AddTriangle(k,k+1,k+2);}
    }
}
}
