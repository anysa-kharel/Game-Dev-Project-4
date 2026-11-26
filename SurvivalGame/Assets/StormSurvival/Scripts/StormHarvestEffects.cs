using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace StormSurvival
{
    public enum HarvestSurface { Wood, Stone }

    public sealed class StormHarvestEffects : MonoBehaviour
    {
        private Vector3 restPosition;
        private Quaternion restRotation;
        private Vector3 restScale;
        private Coroutine motion;

        private void Awake()
        {
            restPosition = transform.localPosition;
            restRotation = transform.localRotation;
            restScale = transform.localScale;
        }

        internal static void Play(StormGame game, GameObject model, HarvestSurface surface,
            Vector3 contact, Vector3 normal, bool breaks)
        {
            if (normal.sqrMagnitude < .01f) normal = Vector3.up;
            normal.Normalize();

            game.Audio.PlayHarvest(surface, breaks, contact);
            game.Player.Shake(breaks ? .09f : .045f);
            Debris(game, surface, contact, normal, breaks);
            if (model == null || !model.activeInHierarchy) return;

            if (!model.TryGetComponent<StormHarvestEffects>(out var effects))
                effects = model.AddComponent<StormHarvestEffects>();
            effects.Animate(surface, normal, breaks);
        }

        private void Animate(HarvestSurface surface, Vector3 normal, bool breaks)
        {
            if (motion != null) StopCoroutine(motion);
            transform.SetLocalPositionAndRotation(restPosition, restRotation);
            transform.localScale = restScale;
            if (!breaks) motion = StartCoroutine(Recoil(normal));
            else if (surface == HarvestSurface.Wood) motion = StartCoroutine(ScatterTimber());
            else motion = StartCoroutine(CrumbleRock(normal));
        }

        private IEnumerator Recoil(Vector3 normal)
        {
            var push = -normal;
            var lean = Vector3.Cross(Vector3.up, push).normalized;
            const float duration = .18f;
            for (float elapsed = 0; elapsed < duration; elapsed += Time.deltaTime)
            {
                float t = elapsed / duration;
                float recoil = Mathf.Sin(t * 2f * Mathf.PI) * (1f - t);
                transform.localPosition = restPosition + LocalOffset(push * (.055f * recoil));
                transform.localRotation = Quaternion.AngleAxis(4f * recoil, lean) * restRotation;
                yield return null;
            }
            transform.SetLocalPositionAndRotation(restPosition, restRotation);
            motion = null;
        }

        private IEnumerator ScatterTimber()
        {
            var colliders = GetComponentsInChildren<Collider>();
            var wasEnabled = new bool[colliders.Length];
            for (int i = 0; i < colliders.Length; i++)
            {
                wasEnabled[i] = colliders[i].enabled;
                colliders[i].enabled = false;
            }

            int count = transform.childCount;
            var branches = new Transform[count];
            var positions = new Vector3[count];
            var rotations = new Quaternion[count];
            var outward = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                var branch = branches[i] = transform.GetChild(i);
                positions[i] = branch.localPosition;
                rotations[i] = branch.localRotation;
                outward[i] = new Vector3(positions[i].x, 0, positions[i].z - .10f).normalized;
                if (outward[i].sqrMagnitude < .01f) outward[i] = Vector3.back;
            }

            const float duration = .42f;
            for (float elapsed = 0; elapsed < duration; elapsed += Time.deltaTime)
            {
                float t = elapsed / duration;
                float lift = 1.75f * elapsed - 4.5f * elapsed * elapsed;
                for (int i = 0; i < count; i++)
                {
                    branches[i].localPosition = positions[i] + outward[i] * (.52f * t) + Vector3.up * lift;
                    branches[i].localRotation = Quaternion.Euler(44f * t, (i % 2 == 0 ? 36f : -36f) * t, 13f * t) * rotations[i];
                }
                yield return null;
            }

            motion = null;
            gameObject.SetActive(false);
            for (int i = 0; i < count; i++)
                branches[i].SetLocalPositionAndRotation(positions[i], rotations[i]);
            for (int i = 0; i < colliders.Length; i++)
                if (colliders[i] != null) colliders[i].enabled = wasEnabled[i];
        }

        private IEnumerator CrumbleRock(Vector3 normal)
        {
            foreach (var collider in GetComponentsInChildren<Collider>())
                collider.enabled = false;

            var fragments = BreakIntoFragments(out var center);
            const float duration = .42f;
            for (float elapsed = 0; elapsed < duration; elapsed += Time.deltaTime)
            {
                float t = elapsed / duration;
                float collapse = 1f - (1f - t) * (1f - t);
                transform.localPosition = restPosition + LocalOffset(Vector3.down * (.30f * collapse));
                transform.localRotation = Quaternion.AngleAxis(15f * collapse, Vector3.forward) * restRotation;
                transform.localScale = restScale * Mathf.Lerp(1f, .06f, collapse);
                for (int i = 0; i < fragments.Length; i++)
                {
                    float angle = i * (2f * Mathf.PI / fragments.Length);
                    var outward = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                    var arc = Vector3.up * (1.85f * elapsed - 4.5f * elapsed * elapsed);
                    fragments[i].position = center + (outward * (.75f + .05f * i) + normal * .14f) * t + arc;
                    fragments[i].Rotate(outward, Time.deltaTime * (195f + 40f * i), Space.World);
                }
                yield return null;
            }
            Destroy(gameObject);
        }

        private Transform[] BreakIntoFragments(out Vector3 center)
        {
            center = transform.position;
            var source = GetComponentInChildren<MeshFilter>();
            if (source == null || source.sharedMesh == null || !source.TryGetComponent<MeshRenderer>(out var sourceRenderer))
                return System.Array.Empty<Transform>();

            center = sourceRenderer.bounds.center;
            var root = new GameObject("Rock fragments");
            root.transform.SetParent(StormGame.Instance.transform, false);
            var span = source.sharedMesh.bounds.size;
            float diameter = Mathf.Max(span.x, Mathf.Max(span.y, span.z));
            var fragments = new Transform[5];
            for (int i = 0; i < fragments.Length; i++)
            {
                var fragment = new GameObject("Stone fragment", typeof(MeshFilter), typeof(MeshRenderer));
                var part = fragments[i] = fragment.transform;
                part.SetParent(root.transform, false);
                part.SetPositionAndRotation(center, Random.rotation);
                part.localScale = Vector3.one * ((.12f + .03f * i) / Mathf.Max(diameter, .001f));
                fragment.GetComponent<MeshFilter>().sharedMesh = source.sharedMesh;
                var renderer = fragment.GetComponent<MeshRenderer>();
                renderer.sharedMaterials = sourceRenderer.sharedMaterials;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
            }
            Destroy(root, .43f);
            return fragments;
        }

        private Vector3 LocalOffset(Vector3 worldOffset)
        {
            return transform.parent == null ? worldOffset : transform.parent.InverseTransformVector(worldOffset);
        }

        private static void Debris(StormGame game, HarvestSurface surface, Vector3 contact, Vector3 normal, bool breaks)
        {
            bool wood = surface == HarvestSurface.Wood;
            var burst = new GameObject(wood ? "Timber chips" : "Stone chips");
            burst.transform.SetParent(game.transform, false);
            burst.transform.position = contact + normal * .04f;
            var particles = burst.AddComponent<ParticleSystem>();
            particles.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = particles.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = .10f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = .8f;
            int count = breaks ? 32 : 14;
            main.maxParticles = count;
            var emission = particles.emission;
            emission.enabled = false;
            var shape = particles.shape;
            shape.enabled = false;
            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = game.particleMaterial;
            renderer.minParticleSize = 0f;
            renderer.maxParticleSize = .04f;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(.9f, .45f), new GradientAlphaKey(0f, 1f) });
            var tint = particles.colorOverLifetime;
            tint.enabled = true;
            tint.color = gradient;

            Color chipColor = wood ? new Color(.84f, .53f, .25f) : new Color(.78f, .80f, .74f);
            Color dustColor = wood ? new Color(.67f, .48f, .26f, .50f) : new Color(.72f, .74f, .71f, .53f);
            particles.Play();
            for (int i = 0; i < count; i++)
            {
                bool dust = i % 3 == 0;
                Vector3 direction = (normal * .65f + Random.insideUnitSphere * .9f + Vector3.up * .6f).normalized;
                var chip = new ParticleSystem.EmitParams
                {
                    position = burst.transform.position,
                    velocity = direction * (dust ? Random.Range(.5f, 1.4f) : Random.Range(1.6f, breaks ? 4.3f : 2.9f)),
                    startColor = dust ? dustColor : chipColor,
                    startLifetime = dust ? Random.Range(.26f, .46f) : Random.Range(.28f, .72f),
                    startSize = dust ? Random.Range(.12f, .26f) : Random.Range(.035f, .095f)
                };
                particles.Emit(chip, 1);
            }
            Destroy(burst, .85f);
        }
    }
}
