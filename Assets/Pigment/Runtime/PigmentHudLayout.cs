using System;
using UnityEngine;
namespace Pigment {
[CreateAssetMenu(menuName="Pigment/HUD Layout")]
public sealed class PigmentHudLayout : ScriptableObject {
    [Serializable] public class Placement {
        public Vector2 anchorMin=new Vector2(.5f,.5f),anchorMax=new Vector2(.5f,.5f),pivot=new Vector2(.5f,.5f);
        [Tooltip("Canvas units. For world-following labels this is an offset from their tracked object.")]
        public Vector2 position;
        public Vector2 size;
        public Vector3 scale=Vector3.one;
        [Tooltip("For Target Tag, Hint and Tutorial: follow the glass and use Position as an offset. Disable for a fixed screen position.")]
        public bool followWorld;
        public void Apply(RectTransform rect){rect.anchorMin=anchorMin;rect.anchorMax=anchorMax;rect.pivot=pivot;rect.sizeDelta=size;rect.anchoredPosition=position;rect.localScale=scale;}
    }
    [Serializable] public class Element {
        public string name;
        [Tooltip("Path relative to Pigment HUD. Keep this aligned with the Canvas hierarchy.")]
        public string path;
        public Placement portrait=new Placement(),landscape=new Placement();
    }
    [Header("Canvas reference sizes")]
    public Vector2 portraitResolution=new Vector2(450,800),landscapeResolution=new Vector2(800,450);
    [Range(0,1)] public float portraitMatch=.5f,landscapeMatch=.5f;
    public Element[] elements=Array.Empty<Element>();
}
}
