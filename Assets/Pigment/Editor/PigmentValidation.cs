using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
namespace Pigment.Editor {
public static class PigmentValidation {
    [Serializable] public class MixFixture {public Vector4 mix,recipe;public Color rgb;public int score;}
    [Serializable] public class VolumeFixture {public string shape;public float angle,capacity,tiltedCapacity,halfLevel;}
    [Serializable] public class SourceFixtures {public MixFixture[] mixes;public VolumeFixture[] volumes;}
    public static string SourceParity(){var data=JsonUtility.FromJson<SourceFixtures>(Resources.Load<TextAsset>("SourceParity").text);foreach(var f in data.mixes){Color c=PigmentMath.MixColor(f.mix);Check(Mathf.Abs(c.r-f.rgb.r)+Mathf.Abs(c.g-f.rgb.g)+Mathf.Abs(c.b-f.rgb.b)<.00001f,"source RGB parity");Check(PigmentMath.Match(f.recipe,f.mix)==f.score,"source OKLab score parity");}foreach(var f in data.volumes){var d=AssetDatabase.LoadAssetAtPath<VesselDefinition>("Assets/Pigment/Config/Vessels/"+f.shape+".asset");var cavity=new PigmentMath.Cavity(d);float a=f.angle*Mathf.Deg2Rad;Vector3 up=new Vector3(Mathf.Sin(a),Mathf.Cos(a),0);Check(Mathf.Abs(cavity.capacity-f.capacity)<.00001f,"source cavity capacity");Check(Mathf.Abs(cavity.CapacityAt(up)-f.tiltedCapacity)<.00001f,"source tilted capacity");Check(Mathf.Abs(cavity.Solve(up,cavity.capacity*.5f)-f.halfLevel)<.00001f,"source half level");}return "PASS: "+data.mixes.Length+" original JS color/score fixtures and "+data.volumes.Length+" original JS vessel/tilt fixtures.";}

    static void Check(bool condition,string message){if(!condition)throw new Exception("Pigment validation: "+message);}
    [MenuItem("Pigment/Validate Math and Levels")]
    public static string Run(){
        var red=new Vector4(1,0,0,0);var yellow=new Vector4(0,1,0,0);var blue=new Vector4(0,0,1,0);var white=new Vector4(0,0,0,1);
        Color green=PigmentMath.MixColor(yellow+blue);Check(green.g>green.r&&green.g>green.b,"yellow + blue must be green");
        Check(PigmentMath.MixColor(red+white*2).g>PigmentMath.MixColor(red).g+.3f,"white dilution");
        Check(PigmentMath.Match(red+blue,(red+blue)*.4f)==100,"ratio invariance");Check(PigmentMath.Match(red+blue,red*.5f+blue*.45f)>=85,"10% slip");Check(PigmentMath.Match(red+blue,red*.7f+blue*.3f)<85,"70/30 should fail");Check(PigmentMath.Match(red,Vector4.zero)==0,"empty result");
        int levelCount=0;foreach(string guid in AssetDatabase.FindAssets("t:PigmentLevel",new[]{"Assets/Pigment"})){var l=AssetDatabase.LoadAssetAtPath<PigmentLevel>(AssetDatabase.GUIDToAssetPath(guid));float total=PigmentMath.Total(l.recipe);Check(total>0,l.name+" positive recipe");for(int i=0;i<4;i++){float share=l.recipe[i]/total;Check(share<=.66f,l.name+" supply");if(share>0)Check(Array.IndexOf(l.sources,(PigmentId)i)>=0,l.name+" source availability");}Check(PigmentMath.Match(l.recipe,l.recipe*.173f)==100,l.name+" exact target");levelCount++;}
        Check(levelCount==7,"seven levels");int shapes=0;
        foreach(string guid in AssetDatabase.FindAssets("t:VesselDefinition",new[]{"Assets/Pigment"})){var d=AssetDatabase.LoadAssetAtPath<VesselDefinition>(AssetDatabase.GUIDToAssetPath(guid));var c=new PigmentMath.Cavity(d);float half=c.capacity*.5f;Check(Mathf.Abs(c.Below(Vector3.up,c.Solve(Vector3.up,half))-half)<1e-5f,d.name+" upright volume");float last=float.MaxValue;foreach(float deg in new[]{0f,30,60,90,120}){float a=deg*Mathf.Deg2Rad;Vector3 up=new Vector3(Mathf.Sin(a),Mathf.Cos(a),0);float cap=c.CapacityAt(up);Check(cap<=last+1e-5f,d.name+" monotonically decreasing capacity");last=cap;float level=c.Solve(up,half);Check(Mathf.Abs(c.Below(up,level)-half)<1e-5f,d.name+" tilted conservation");}float scale=new PigmentMath.Cavity(d,1.3f).capacity/c.capacity;Check(Mathf.Abs(scale-1.3f*1.3f*1.3f)<.08f,d.name+" cubic scaling");Check(d.prefab&&d.glassMesh,d.name+" prefab");shapes++;}
        string result="PASS: RYB/OKLab scoring, empty mix, reachability of "+levelCount+" levels, capacity/tilt conservation of "+shapes+" vessel shapes, prefab references.";Debug.Log(result);return result;
    }
    static PigmentGame game;static int pigmentIndex;static double started,releaseAt;static bool released;static readonly List<string> results=new List<string>();
    public static string Playtest(){if(!EditorApplication.isPlaying)return "Enter Play mode first";game=UnityEngine.Object.FindAnyObjectByType<PigmentGame>();if(!game)return "No Pigment game";results.Clear();game.Again();pigmentIndex=0;released=false;started=EditorApplication.timeSinceStartup;EditorApplication.update-=Tick;EditorApplication.update+=Tick;return "Started seven-level pour test";}
    public static string Report()=>string.Join("\n",results);
    static void Tick(){if(!game||!EditorApplication.isPlaying){EditorApplication.update-=Tick;return;}if(EditorApplication.timeSinceStartup-started>160){results.Add("FAIL: timed out at level "+(game.LevelIndex+1));EditorApplication.update-=Tick;return;}
        if(game.Phase==GamePhase.Result||game.Phase==GamePhase.Celebration){results.Add("Level "+(game.LevelIndex+1)+" "+game.levels[game.LevelIndex].displayName+": "+game.Score+"%, "+game.rules.Stars(game.Score)+" stars; lost volume="+game.LostVolume.ToString("F6"));if(game.Score<85||game.Phase==GamePhase.Celebration){EditorApplication.update-=Tick;Debug.Log("PIGMENT_PLAYTEST_DONE\n"+Report());return;}game.Advance();pigmentIndex=0;released=false;return;}
        if(game.Phase!=GamePhase.Playing)return;
        var recipe=game.levels[game.LevelIndex].recipe;float total=PigmentMath.Total(recipe);while(pigmentIndex<4&&recipe[pigmentIndex]<=0)pigmentIndex++;if(pigmentIndex>=4)return;
        var source=game.Sources.Find(v=>(int)v.pigment==pigmentIndex);if(!source)return;
        if(released){if(EditorApplication.timeSinceStartup-releaseAt<.8)return;released=false;pigmentIndex++;return;}
        if(game.Active!=source||!game.Held)game.Press(source);
        bool last=true;for(int i=pigmentIndex+1;i<4;i++)if(recipe[i]>0)last=false;
        float delivered=game.Mix[pigmentIndex]+source.stream.FallingVolume;
        if(!last&&delivered>=game.GoalVolume*(recipe[pigmentIndex]/total-.013f)){game.ReleaseHold();released=true;releaseAt=EditorApplication.timeSinceStartup;}
    }
}
}
