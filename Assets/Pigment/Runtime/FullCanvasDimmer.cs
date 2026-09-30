using UnityEngine;
namespace Pigment {
/// <summary>Fits a background to the entire root Canvas, even inside a safe-area container.</summary>
[ExecuteAlways]
[RequireComponent(typeof(RectTransform), typeof(UnityEngine.UI.Image))]
public sealed class FullCanvasDimmer : MonoBehaviour {
    readonly Vector3[] corners=new Vector3[4];
    void OnEnable(){Fit();}
    void LateUpdate(){Fit();}
    public void Fit(){
        var rect=transform as RectTransform;
        var parent=rect.parent as RectTransform;
        var canvas=GetComponentInParent<Canvas>();
        if(!parent||!canvas)return;
        var canvasRect=canvas.rootCanvas.transform as RectTransform;
        if(!canvasRect||canvasRect==rect)return;
        canvasRect.GetWorldCorners(corners);
        Vector2 min=Vector2.positiveInfinity,max=Vector2.negativeInfinity;
        for(int i=0;i<4;i++){Vector2 point=parent.InverseTransformPoint(corners[i]);min=Vector2.Min(min,point);max=Vector2.Max(max,point);}
        rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(.5f,.5f);
        rect.localRotation=Quaternion.identity;rect.localScale=Vector3.one;
        rect.sizeDelta=max-min;
        rect.anchoredPosition=(min+max)*.5f-parent.rect.center;
    }
}
}
