using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace Pigment.Editor {
public static class PigmentAuthoring {
    [MenuItem("Pigment/Apply Layout to Scene Preview")]
    public static void Preview(){var g=Object.FindAnyObjectByType<PigmentGame>();if(!g){Debug.LogWarning("Open the Pigment scene first.");return;}if(Application.isPlaying){g.Retry();return;}Undo.RecordObject(g,"Apply Pigment layout");g.LoadLevel(0);g.RefreshVisuals(0);int i=0;foreach(var v in g.vesselsRoot.GetComponentsInChildren<VesselView>()){string path="Assets/Pigment/Art/PreviewLiquid"+(i++)+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(saved){EditorUtility.CopySerialized(v.liquidFilter.sharedMesh,saved);v.liquidFilter.sharedMesh=saved;EditorUtility.SetDirty(saved);}else {var mesh=Object.Instantiate(v.liquidFilter.sharedMesh);AssetDatabase.CreateAsset(mesh,path);v.liquidFilter.sharedMesh=mesh;}}EditorSceneManager.MarkSceneDirty(g.gameObject.scene);AssetDatabase.SaveAssets();}
    [MenuItem("Pigment/Open Game Scene")]
    public static void Open(){if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())EditorSceneManager.OpenScene("Assets/Pigment/Scenes/Pigment.unity");}
}
}
