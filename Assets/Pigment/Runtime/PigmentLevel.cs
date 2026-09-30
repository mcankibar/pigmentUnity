using UnityEngine;
namespace Pigment {
public enum PigmentId { Red, Yellow, Blue, White }
[CreateAssetMenu(menuName="Pigment/Level")]
public sealed class PigmentLevel : ScriptableObject {
    public int id=1;
    public string displayName="Turuncu";
    [Tooltip("X: red, Y: yellow, Z: blue, W: white. Ratios, not fixed amounts.")]
    public Vector4 recipe=new Vector4(1,1,0,0);
    public PigmentId[] sources={PigmentId.Red,PigmentId.Yellow};
    public Color TargetColor=>PigmentMath.MixColor(recipe);
}
}
