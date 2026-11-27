using System.Collections.Generic;
using UnityEngine;

namespace StormSurvival
{
    public sealed class StormAudio : MonoBehaviour
    {
        private StormGame game;
        private AudioSource ridgeWind;
        private float ridgeWindMix;
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
            Add("sled release", .48f, 135f, .82f, .72f);
            Add("spotlight turn", .38f, 185f, .46f, .55f);
            Add("signal reply", 1.15f, 705f, .06f, .62f);
            AddWolfSound("wolf warning", 1.45f, 380f, .17f);
            AddWolfSound("wolf bite", .25f, 110f, .62f);
            AddWolfSound("wolf stagger", .48f, 165f, .34f);
            var random=new System.Random(526);const int rate=22050;var values=new float[rate*8];float smooth=0;
            for(int i=0;i<values.Length;i++)
            {
                float t=(float)i/rate;smooth=smooth*.94f+((float)random.NextDouble()*2-1)*.06f;
                float fade=Mathf.Clamp01(Mathf.Min(t,8-t)*3);
                values[i]=(smooth*.30f+((float)random.NextDouble()*2-1)*.025f)*fade;
            }
            var ambient=AudioClip.Create("Original wind and rain",values.Length,1,rate,false);ambient.SetData(values,0);clips["ambience"]=ambient;
            var source=gameObject.AddComponent<AudioSource>();source.clip=ambient;source.volume=.75f;source.loop=true;source.Play();
            ridgeWind = gameObject.AddComponent<AudioSource>();
            ridgeWind.clip = ambient;
            ridgeWind.loop = true;
            ridgeWind.pitch = 1.35f;
            ridgeWind.volume = 0f;
            ridgeWind.Play();
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
                if (name == "signal reply")
                {
                    float beat = t < .28f ? t : t - .28f;
                    tone = Mathf.Sin(beat * Mathf.PI * 2f * frequency * (t < .28f ? 1f : 1.5f)) *
                        Mathf.Clamp01(beat * 75f) * Mathf.Exp(-7f * beat);
                }
                data[i]=(tone*(1-noise)+((float)random.NextDouble()*2-1)*noise)*fade*volume;
            }
            var clip=AudioClip.Create("Storm / "+name,data.Length,1,rate,false);clip.SetData(data,0);clips[name]=clip;
        }
        private void AddWolfSound(string name, float seconds, float frequency, float grit)
        {
            const int sampleRate = 22050;
            var samples = new float[Mathf.CeilToInt(sampleRate * seconds)];
            var random = new System.Random((int)frequency);
            float phase = 0f;
            float breath = 0f;
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (float)i / samples.Length;
                float pitch = frequency * (.87f + .36f * Mathf.Sin(t * Mathf.PI));
                phase += pitch * (2f * Mathf.PI / sampleRate);
                breath = Mathf.Lerp(breath, (float)random.NextDouble() * 2f - 1f, .24f);
                float rasp = Mathf.Sin(phase) + .25f * Mathf.Sin(phase * 2f) + .13f * Mathf.Sin(phase * 3f);
                float envelope = Mathf.Clamp01(t * 14f) * Mathf.Pow(1f - t, .8f);
                samples[i] = (rasp * (.46f - grit * .36f) + breath * grit) * envelope;
            }

            var clip = AudioClip.Create("Storm / " + name, samples.Length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            clips[name] = clip;
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
            float spatialBlend = name.StartsWith("wolf ", System.StringComparison.Ordinal) ? .78f : 0f;
            PlayClip(name, position, volume, spatialBlend);
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
        public void SetRidgeWind(float strength)
        {
            ridgeWindMix = Mathf.Clamp01(strength);
        }

        private void Update()
        {
            if (game == null) return;
            AudioListener.volume = game.volume;
            float mix = game.Running ? ridgeWindMix : 0f;
            ridgeWind.volume = Mathf.MoveTowards(ridgeWind.volume, mix * .93f, Time.unscaledDeltaTime * 1.65f);
        }
        private void OnDestroy(){foreach(var clip in clips.Values)if(clip!=null)Destroy(clip);AudioListener.volume=1f;}
    }
}
