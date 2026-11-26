using System.Collections.Generic;
using UnityEngine;

namespace StormSurvival
{
    public sealed class StormAudio : MonoBehaviour
    {
        private StormGame game;
        private readonly Dictionary<string,AudioClip> clips = new Dictionary<string,AudioClip>();
        public void Initialize(StormGame owner)
        {
            game=owner;
            Add("step",.14f,90,.65f,.6f);Add("collect",.3f,620,.06f,.5f);
            Add("chop",.30f,140,.75f,.7f);Add("timber",1.3f,60,.7f,.75f);
            Add("water",.75f,120,.85f,.6f);Add("fire",.65f,150,.8f,.4f);
            Add("craft",.55f,480,.15f,.5f);Add("checkpoint",.7f,650,.01f,.5f);
            Add("thunder",2f,40,.5f,.7f);Add("failure",1f,65,.2f,.6f);Add("success",2f,440,.01f,.65f);
            var random=new System.Random(526);const int rate=22050;var values=new float[rate*8];float smooth=0;
            for(int i=0;i<values.Length;i++)
            {
                float t=(float)i/rate;smooth=smooth*.94f+((float)random.NextDouble()*2-1)*.06f;
                float fade=Mathf.Clamp01(Mathf.Min(t,8-t)*3);
                values[i]=(smooth*.30f+((float)random.NextDouble()*2-1)*.025f)*fade;
            }
            var ambient=AudioClip.Create("Original wind and rain",values.Length,1,rate,false);ambient.SetData(values,0);clips["ambience"]=ambient;
            var source=gameObject.AddComponent<AudioSource>();source.clip=ambient;source.volume=.75f;source.loop=true;source.Play();
            if(game.templateMusic!=null){var music=gameObject.AddComponent<AudioSource>();music.clip=game.templateMusic;music.volume=.075f;music.loop=true;music.Play();}
        }
        private void Add(string name,float duration,float frequency,float noise,float volume)
        {
            const int rate=22050;var data=new float[Mathf.CeilToInt(duration*rate)];var random=new System.Random((int)frequency);
            for(int i=0;i<data.Length;i++)
            {
                float t=(float)i/rate;float fade=Mathf.Min(1,t*80)*Mathf.Pow(1-t/duration,2f);
                float tone=Mathf.Sin(t*Mathf.PI*2*frequency);
                if(name=="success")tone=(tone+Mathf.Sin(t*Mathf.PI*2*frequency*1.25f)+Mathf.Sin(t*Mathf.PI*2*frequency*1.5f))/3;
                if(name=="checkpoint")tone=Mathf.Sin(t*Mathf.PI*2*frequency*(t>.25f?1.5f:1));
                data[i]=(tone*(1-noise)+((float)random.NextDouble()*2-1)*noise)*fade*volume;
            }
            var clip=AudioClip.Create("Storm / "+name,data.Length,1,rate,false);clip.SetData(data,0);clips[name]=clip;
        }
        public void Play(string name,Vector3 position,float volume)
        {
            if(!clips.TryGetValue(name,out AudioClip clip))return;
            var obj=new GameObject("Sound / "+name);obj.transform.position=position;
            var source=obj.AddComponent<AudioSource>();source.clip=clip;source.volume=volume;source.spatialBlend=0f;source.Play();Destroy(obj,clip.length+.1f);
        }
        private void Update(){if(game!=null)AudioListener.volume=game.volume;}
        private void OnDestroy(){foreach(var clip in clips.Values)if(clip!=null)Destroy(clip);AudioListener.volume=1f;}
    }
}
