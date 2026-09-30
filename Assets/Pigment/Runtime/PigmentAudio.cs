using UnityEngine;
namespace Pigment {
public sealed class PigmentAudio : MonoBehaviour {
    public PigmentStyle style;
    AudioSource music,pour,fx;AudioClip musicClip,pourClip,pickClip,winClip,loseClip;float level;
    const int Rate=22050;
    void Awake(){music=Source(true);pour=Source(true);fx=Source(false);musicClip=Music();pourClip=Noise();pickClip=Chord(new[]{880f,1320f},.28f);winClip=Chord(new[]{523.25f,659.25f,783.99f},1.2f);loseClip=Chord(new[]{220f,207.65f},.65f);music.clip=musicClip;pour.clip=pourClip;music.Play();pour.Play();}
    AudioSource Source(bool loop){var a=gameObject.AddComponent<AudioSource>();a.loop=loop;a.playOnAwake=false;a.spatialBlend=0;a.volume=0;return a;}
    AudioClip Clip(string name,float[] data){var c=AudioClip.Create(name,data.Length,1,Rate,false);c.SetData(data,0);return c;}
    static float Hz(int midi)=>440*Mathf.Pow(2,(midi-69)/12f);
    AudioClip Music(){float duration=25.6f;var data=new float[(int)(duration*Rate)];int[][] chords={new[]{45,52,57,60},new[]{41,48,53,57},new[]{48,55,60,64},new[]{43,50,55,59}};int[] melody={69,72,74,76,79,81,84};for(int i=0;i<data.Length;i++){float t=i/(float)Rate,bar=t%6.4f;int c=Mathf.Min(3,(int)(t/6.4f));float envelope=Mathf.Min(1,bar/.65f)*Mathf.Min(1,(6.4f-bar)/.9f),s=0;foreach(int note in chords[c])s+=Mathf.Sin(t*Hz(note)*2*Mathf.PI)*.026f*envelope;float beat=t%1.6f;int noteIndex=((int)(t/1.6f)*3)%melody.Length;s+=Mathf.Sin(beat*Hz(melody[noteIndex])*2*Mathf.PI)*Mathf.Exp(-beat*4)*.045f;data[i]=s;}return Clip("A minor workshop music",data);}
    AudioClip Noise(){var data=new float[Rate*2];var rng=new System.Random(21);float low=0;for(int i=0;i<data.Length;i++){float n=(float)rng.NextDouble()*2-1;low=Mathf.Lerp(low,n,.18f);data[i]=(n-low)*.07f+Mathf.Sin(i/(float)Rate*7100)*.006f;}return Clip("Water trickle",data);}
    AudioClip Chord(float[] notes,float duration){var data=new float[(int)(duration*Rate)];for(int i=0;i<data.Length;i++){float t=i/(float)Rate;foreach(float hz in notes)data[i]+=Mathf.Sin(t*hz*Mathf.PI*2)*Mathf.Exp(-t*5/duration)*Mathf.Min(1,t/.005f)*.13f/notes.Length;}return Clip("Glass chime",data);}
    public void Tick(float dt,float strength,float fill){if(!music)return;float enable=style.audioEnabled?1:0;music.volume=style.musicVolume*enable;fx.volume=style.effectsVolume*enable;float target=strength>.01f?.35f+.65f*Mathf.Sqrt(strength):0;level=Mathf.Lerp(level,target,dt*(target>level?18:6));pour.volume=level*style.pourVolume*style.effectsVolume*enable;pour.pitch=.85f+fill*.4f;}
    public void Pick(){if(fx)fx.PlayOneShot(pickClip);}
    public void Result(bool pass){if(fx)fx.PlayOneShot(pass?winClip:loseClip);}
    void OnDestroy(){foreach(var c in new[]{musicClip,pourClip,pickClip,winClip,loseClip})if(c)Destroy(c);}
}
}
