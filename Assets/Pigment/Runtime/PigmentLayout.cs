using System;
using UnityEngine;
namespace Pigment {
[CreateAssetMenu(menuName="Pigment/Bench Layout")]
public sealed class PigmentLayout : ScriptableObject {
    [Serializable] public class VesselPlacement { public Vector3 position=new Vector3(0,0,-1.9f); [Range(.3f,3)] public float scale=1.35f; }
    [Serializable] public class SourceLayout {
        public Vector3 position=new Vector3(0,0,.1f);
        [Min(.1f)] public float arcWidth=2.66f,arcDepth=2;
        [Range(0,80)] public float spread;
        [Range(.3f,.85f)] public float startFill=.72f;
        [Tooltip("Optional per-slot offsets and scale multipliers, applied after automatic arc layout.")]
        public Vector3[] slotOffsets=new Vector3[4];
        public float[] slotScales={1,1,1,1};
    }
    [Serializable] public class ShelfLayout {
        public bool autoPlace=true;
        public Vector3 position=new Vector3(0,2.6f,-3.45f);
        public float distanceFromWall=.75f,portraitScale=1.5f,landscapeScale=1.1f,width=2.2f,thickness=.07f;
        [Range(0,40)] public float tilt;
        public Color color=new Color(.478f,.306f,.165f);
    }
    [Serializable] public class CameraLayout {
        public bool autoFrame=true;
        public Vector3 portraitDirection=new Vector3(0,.6f,1.46f),landscapeDirection=new Vector3(0,1.05f,1.46f);
        public Vector3 lookAt=new Vector3(0,1,.3f),position=new Vector3(0,6.5f,6);
        [Range(20,75)] public float fieldOfView=42;
        public Vector3 portraitMargins=new Vector3(.3f,-.8f,.94f),landscapeMargins=new Vector3(.26f,-.84f,.8f);
    }
    public VesselPlacement mixer=new VesselPlacement();
    public SourceLayout sources=new SourceLayout();
    public ShelfLayout goalShelf=new ShelfLayout();
    public CameraLayout camera=new CameraLayout();
    [HideInInspector] public int revision;
    void OnValidate(){revision++;}
}
}
