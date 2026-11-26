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
            AddHarvestSound("chop", HarvestSurface.Wood, false);
            AddHarvestSound("wood split", HarvestSurface.Wood, true);
            AddHarvestSound("stone strike", HarvestSurface.Stone, false);
            AddHarvestSound("stone crumble", HarvestSurface.Stone, true);
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
        private void AddHarvestSound(string name, HarvestSurface surface, bool breaks)
        {
            bool wood = surface == HarvestSurface.Wood;
            const int rate = 22050;
            float duration = breaks ? .52f : .25f;
            var data = new float[Mathf.CeilToInt(rate * duration)];
            var random = new System.Random((wood ? 121 : 365) + (breaks ? 20 : 0));
            float softened = 0f;
            for (int i = 0; i < data.Length; i++)
            {
                float time = (float)i / rate;
                float cycle = time * 2f * Mathf.PI;
                float noise = (float)random.NextDouble() * 2f - 1f;
                softened = softened * .86f + noise * .14f;
                float bright = noise - softened;
                float impact = wood
                    ? (.40f * Mathf.Sin(cycle * 103f) + .17f * Mathf.Sin(cycle * 287f)) * Mathf.Exp(-42f * time)
                        + (.81f * softened + .36f * bright) * Mathf.Exp(-93f * time)
                    : (.23f * Mathf.Sin(cycle * 970f) + .16f * Mathf.Sin(cycle * 1580f)) * Mathf.Exp(-42f * time)
                        + (.18f * Mathf.Sin(cycle * 188f) + .62f * bright) * Mathf.Exp(-88f * time);
                if (breaks)
                {
                    impact += softened * .65f * Tail(time, .040f, 12f);
                    impact += bright * (wood ? .54f : .33f) * Tail(time, .082f, 38f);
                    impact += softened * .58f * Tail(time, .153f, 22f);
                }
                data[i] = Mathf.Clamp(impact, -.95f, .95f) * Mathf.Clamp01(time * 4000f)
                    * Mathf.Clamp01((duration - time) * 35f);
            }
            var clip = AudioClip.Create("Storm / " + name, data.Length, 1, rate, false);
            clip.SetData(data, 0);
            clips[name] = clip;
        }

        private static float Tail(float time, float onset, float decay)
        {
            float elapsed = time - onset;
            return elapsed < 0f ? 0f : Mathf.Min(1f, elapsed * 260f) * Mathf.Exp(-elapsed * decay);
        }

        public void PlayHarvest(HarvestSurface surface, bool breaks, Vector3 position)
        {
            string name = surface == HarvestSurface.Wood
                ? (breaks ? "wood split" : "chop")
                : (breaks ? "stone crumble" : "stone strike");
            PlayClip(name, position, breaks ? .92f : .78f, .85f);
        }

        public void Play(string name, Vector3 position, float volume)
        {
            PlayClip(name, position, volume, 0f);
        }

        private void PlayClip(string name, Vector3 position, float volume, float spatialBlend)
        {
            if (!clips.TryGetValue(name, out AudioClip clip)) return;
            var sound = new GameObject("Sound / " + name);
            sound.transform.SetParent(transform, false);
            sound.transform.position = position;
            var source = sound.AddComponent<AudioSource>();
            source.clip = clip;
            source.volume = volume;
            source.spatialBlend = spatialBlend;
            source.minDistance = 2.5f;
            source.maxDistance = 18f;
            source.dopplerLevel = 0f;
            source.Play();
            Destroy(sound, clip.length + .1f);
        }
        private void Update(){if(game!=null)AudioListener.volume=game.volume;}
        private void OnDestroy(){foreach(var clip in clips.Values)if(clip!=null)Destroy(clip);AudioListener.volume=1f;}
    }
}
