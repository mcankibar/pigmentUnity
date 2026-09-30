using UnityEngine;
namespace Pigment {
[CreateAssetMenu(menuName="Pigment/Pour and Scoring Rules")]
public sealed class PigmentRules : ScriptableObject {
    [Header("Volume & scoring (original game)")]
    [Range(.1f,.92f)] public float fillLevel=.6f;
    [Range(.1f,1)] public float supply=.66f;
    [Range(0,100)] public int passPercent=85,twoStars=90,threeStars=95;
    [Header("Pour angles in degrees")]
    public float hoverTilt=16,maxTilt=122,tiltSpeed=75,pourTiltSpeed=24;
    [Min(.05f)] public float travelTime=.42f;
    [Min(.1f)] public float flowGain=3.2f,maxFlow=.5f,tiltSpring=150,tiltDamping=21;
    [Header("Stream")]
    [Min(1)] public float gravity=22;
    [Range(.3f,3)] public float streamThickness=1;
    public float settleTime=.45f;
    public int Stars(int score)=>score>=threeStars?3:score>=twoStars?2:score>=passPercent?1:0;
}
}
