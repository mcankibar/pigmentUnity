using UnityEngine;
using TMPro;
namespace Pigment {
[CreateAssetMenu(menuName="Pigment/Visuals and Audio")]
public sealed class PigmentStyle : ScriptableObject {
    public Material glass,liquid,wood,stone,parchment,iron;
    public TMP_FontAsset font;
    public Sprite hand,roundedPanel;
    [Header("Lighting")]
    public Color keyColor=new Color(1,.965f,.925f),ambientSky=Color.white,ambientGround=new Color(.54f,.52f,.5f);
    [Range(0,5)] public float keyIntensity=1.8f,ambientIntensity=.8f;
    [Header("Liquid")]
    [Range(0,1)] public float clarity=.7f,roughness=.04f,glow=.18f;
    [Range(0,3)] public float slosh=1;
    public bool bubbles=true,vapor=true,sparks=true,splash=true,dust=true;
    [Range(0,3)] public float vaporRate=.7f;
    [Header("Audio")]
    public bool audioEnabled=true;
    [Range(0,1)] public float musicVolume=.45f,effectsVolume=.9f,pourVolume=.8f;
    [Header("Tutorial")]
    public bool tutorial=true;
    public float handSize=115,pressDuration=1.3f;
}
}
