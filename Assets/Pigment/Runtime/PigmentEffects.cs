using UnityEngine;
namespace Pigment {
public sealed class PigmentEffects : MonoBehaviour {
    public ParticleSystem droplets,mist,sparks,dust;
    public void Impact(Vector3 position,Color color,PigmentStyle style,float amount){
        if(style.splash&&Random.value<amount*70)Emit(droplets,position,new Vector3(Random.Range(-.4f,.4f),Random.Range(.7f,1.8f),Random.Range(-.4f,.4f)),color,Random.Range(.014f,.032f),.6f);
        if(style.bubbles&&Random.value<amount*90)Emit(droplets,position+Vector3.up*.02f,Vector3.up*.08f,Color.Lerp(color,Color.white,.55f),.026f,.45f);
        if(style.vapor&&Random.value<amount*25*style.vaporRate)Emit(mist,position,Vector3.up*.35f,new Color(color.r,color.g,color.b,.18f),.13f,1.5f);
    }
    public void Celebrate(Vector3 center){for(int i=0;i<80;i++)Emit(sparks,center,new Vector3(Random.Range(-2,2),Random.Range(1,4),Random.Range(-1,1)),Color.HSVToRGB(Random.value,.7f,1),.04f,2);}
    static void Emit(ParticleSystem ps,Vector3 p,Vector3 v,Color c,float size,float life){if(!ps)return;ps.Emit(new ParticleSystem.EmitParams{position=p,velocity=v,startColor=c,startSize=size,startLifetime=life},1);}
}
}
