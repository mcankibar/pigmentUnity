using UnityEngine;
using UnityEditor;
namespace Pigment.Editor {
public static class PigmentHudGrouping {
    public static RectTransform GroupTargetCard(PigmentHud hud){
        var card=hud.header;
        if(card.name=="Target Card Group")return card;
        var groupObject=new GameObject("Target Card Group",typeof(RectTransform));
        var group=(RectTransform)groupObject.transform;group.SetParent(card.parent,false);group.SetSiblingIndex(card.GetSiblingIndex());
        group.anchorMin=card.anchorMin;group.anchorMax=card.anchorMax;group.pivot=card.pivot;group.sizeDelta=card.sizeDelta;group.anchoredPosition=card.anchoredPosition;group.localScale=card.localScale;
        card.SetParent(group,false);card.anchorMin=Vector2.zero;card.anchorMax=Vector2.one;card.pivot=new Vector2(.5f,.5f);card.offsetMin=card.offsetMax=Vector2.zero;card.localScale=Vector3.one;
        hud.header=group;
        if(hud.hudLayout){
            const string oldPath="Safe Area/Target Card";const string newPath="Safe Area/Target Card Group";
            foreach(var e in hud.hudLayout.elements){
                if(e.path==oldPath){e.name="Target Card Group";e.path=newPath;}
                else if(e.path.StartsWith(oldPath+"/",System.StringComparison.Ordinal))e.path=newPath+"/Target Card"+e.path.Substring(oldPath.Length);
            }
            EditorUtility.SetDirty(hud.hudLayout);
        }
        // Rebind after the hierarchy changed; serialized layout values are preserved.
        typeof(PigmentHud).GetField("boundCount",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(hud,-1);
        EditorUtility.SetDirty(hud);return group;
    }
}
}
