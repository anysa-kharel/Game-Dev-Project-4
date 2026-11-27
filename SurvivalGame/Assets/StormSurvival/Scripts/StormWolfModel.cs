using System.Collections.Generic;
using UnityEngine;

namespace StormSurvival
{
    internal sealed class StormWolfModel : MonoBehaviour
    {
        private readonly List<Mesh> meshes = new List<Mesh>();
        private readonly Transform[] upperLegs = new Transform[4];
        private readonly Transform[] lowerLegs = new Transform[4];
        private Transform torso, skull, tail;
        private float gait, movement, tension, lunge;
        private StormWolfPhase lastPhase;

        internal void Build(StormWorld world)
        {
            var fur = world.MakeMaterial(new Color(.62f, .61f, .57f));
            var shadow = world.MakeMaterial(new Color(.32f, .33f, .34f));
            var chest = world.MakeMaterial(new Color(.74f, .72f, .65f));
            var cream = world.MakeMaterial(new Color(.85f, .81f, .72f));
            var black = world.MakeMaterial(new Color(.10f, .10f, .10f));
            var amber = world.MakeMaterial(new Color(.74f, .45f, .12f));

            torso = Joint("Wolf body", transform, Vector3.zero);
            Segment(torso, "Flanks", At(0, .77f, -.65f), At(0, .81f, .59f), Size(.235f, .26f), Size(.285f, .31f), fur);
            Segment(torso, "Dark coat", At(0, .96f, -.61f), At(0, 1.04f, .55f), Size(.19f, .09f), Size(.21f, .12f), shadow);
            Segment(torso, "Chest", At(0, .65f, .24f), At(0, .91f, .69f), Size(.25f, .21f), Size(.25f, .26f), chest);
            Segment(torso, "Neck ruff", At(0, .90f, .35f), At(0, 1.02f, .80f), Size(.30f, .29f), Size(.17f, .19f), fur);

            skull = Joint("Wolf head", torso, At(0f, 1.03f, .69f));
            Segment(skull, "Head", At(0, .045f, -.06f), At(0, .04f, .325f), Size(.17f, .165f), Size(.142f, .115f), fur);
            Segment(skull, "Muzzle", At(0, -.018f, .24f), At(0, -.063f, .585f), Size(.125f, .091f), Size(.073f, .052f), cream);
            Segment(skull, "Muzzle bridge", At(0, .045f, .23f), At(0, -.02f, .545f), Size(.10f, .046f), Size(.06f, .025f), shadow);
            Segment(skull, "Nose", At(0, -.044f, .555f), At(0, -.050f, .61f), Size(.075f, .046f), Size(.058f, .038f), black);
            for (int side = -1; side <= 1; side += 2)
            {
                Segment(skull, "Pointed ear", At(.127f * side, .13f, .068f), At(.206f * side, .396f, .018f), Size(.096f, .078f), Size(.009f, .012f), shadow);
                Segment(skull, "Ear lining", At(.143f * side, .168f, .135f), At(.19f * side, .334f, .072f), Size(.060f, .030f), Size(.009f, .009f), chest);
                Segment(skull, "Cheek", At(.122f * side, -.036f, .043f), At(.148f * side, -.095f, .26f), Size(.07f, .097f), Size(.041f, .049f), chest);
                Segment(skull, "Eye", At(.146f * side, .071f, .23f), At(.164f * side, .069f, .272f), Size(.033f, .023f), Size(.017f, .014f), black);
                Segment(skull, "Amber iris", At(.157f * side, .070f, .261f), At(.172f * side, .070f, .269f), Size(.018f, .015f), Size(.012f, .011f), amber);
            }

            tail = Joint("Bushy wolf tail", torso, At(0, .86f, -.61f));
            Segment(tail, "Tail base", Vector3.zero, At(0, -.14f, -.29f), Size(.09f, .10f), Size(.14f, .14f), fur);
            Segment(tail, "Tail plume", At(0, -.13f, -.25f), At(-.035f, -.26f, -.58f), Size(.139f, .13f), Size(.095f, .09f), fur);
            Segment(tail, "Tail tip", At(-.035f, -.25f, -.55f), At(-.033f, -.275f, -.8f), Size(.102f, .098f), Size(.021f, .024f), shadow);

            for (int index = 0; index < upperLegs.Length; index++)
            {
                bool front = index < 2;
                int side = index % 2 == 0 ? -1 : 1;
                Vector3 knee = At(0, front ? -.36f : -.34f, front ? .025f : .12f);
                var upper = upperLegs[index] = Joint("Wolf upper leg", torso, At(.21f * side, .69f, front ? .46f : -.48f));
                Segment(upper, "Upper leg", At(0, .065f, 0), knee, Size(.105f, .117f), Size(.075f, .082f), fur);
                var lower = lowerLegs[index] = Joint("Wolf lower leg", upper, knee);
                Vector3 foot = At(0, front ? -.274f : -.285f, front ? -.038f : -.11f);
                Segment(lower, "Lower leg", Vector3.zero, foot, Size(.067f, .075f), Size(.053f, .060f), chest);
                Segment(lower, "Paw", foot + At(0, -.033f, -.046f), foot + At(0, -.033f, .14f), Size(.069f, .043f), Size(.071f, .043f), shadow);
            }
        }

        internal void SetVisible(bool visible) => torso.gameObject.SetActive(visible);

        internal void Animate(float travel, StormWolfPhase phase)
        {
            bool running = phase == StormWolfPhase.Chase || phase == StormWolfPhase.Retreat;
            float weight = Mathf.Clamp01(travel / Mathf.Max(.0001f, Time.deltaTime * (running ? 3.2f : 1.2f)));
            movement = Mathf.MoveTowards(movement, weight, Time.deltaTime * 7f);
            gait += travel * (running ? 5f : 7.8f);

            float lean = phase == StormWolfPhase.Windup ? 1f :
                phase == StormWolfPhase.Warning || phase == StormWolfPhase.Staggered ? .6f : 0f;
            tension = Mathf.MoveTowards(tension, lean, Time.deltaTime * 5.8f);
            if (lastPhase == StormWolfPhase.Windup && phase == StormWolfPhase.Recover) lunge = 1f;
            else lunge = Mathf.MoveTowards(lunge, 0f, Time.deltaTime * 4.5f);
            lastPhase = phase;
            float stepPulse = Mathf.Cos(gait * 2f) * movement;
            torso.localPosition = new Vector3(0f, stepPulse * .026f - tension * .081f + lunge * .07f, lunge * .34f);
            torso.localRotation = Quaternion.Euler(tension * 12f - lunge * 8f, movement * Mathf.Sin(gait) * 2f, 0f);
            skull.localRotation = Quaternion.Euler(-tension * 15f + stepPulse * 3f + lunge * 9f, 0f, 0f);
            tail.localRotation = Quaternion.Euler(running ? -15f : 0f, Mathf.Sin(Time.time * 4.3f) * (5f + movement * 11f), 0f);

            for (int index = 0; index < upperLegs.Length; index++)
            {
                bool diagonal = index == 0 || index == 3;
                float cycle = gait + (diagonal ? 0f : Mathf.PI);
                float legSwing = Mathf.Sin(cycle) * movement * (running ? 38f : 25f);
                float flex = 48f * movement * Mathf.Max(0f, Mathf.Cos(cycle));
                upperLegs[index].localRotation = Quaternion.Euler(legSwing + tension * (index < 2 ? 18f : -15f), 0f, 0f);
                lowerLegs[index].localRotation = Quaternion.Euler(index < 2 ? flex : -flex, 0f, 0f);
            }
        }

        private Transform Joint(string label, Transform parent, Vector3 position)
        {
            var joint = new GameObject(label).transform;
            joint.SetParent(parent, false);
            joint.localPosition = position;
            return joint;
        }

        private void Segment(Transform parent, string label, Vector3 start, Vector3 end,
            Vector2 startRadius, Vector2 endRadius, Material material)
        {
            const int sides = 8;
            var part = Joint(label, parent, start);
            Vector3 axis = end - start;
            Vector3 normal = Mathf.Abs(Vector3.Dot(axis.normalized, Vector3.up)) > .92f ? Vector3.forward : Vector3.up;
            part.localRotation = Quaternion.LookRotation(axis, normal);

            float length = axis.magnitude;
            var vertices = new Vector3[sides * 12];
            var indices = new int[vertices.Length];
            int vertex = 0;
            for (int side = 0; side < sides; side++)
            {
                float angle = (side + .5f) * Mathf.PI * 2f / sides;
                float next = (side + 1.5f) * Mathf.PI * 2f / sides;
                Vector3 a = Rim(angle, startRadius, 0f);
                Vector3 b = Rim(next, startRadius, 0f);
                Vector3 c = Rim(next, endRadius, length);
                Vector3 d = Rim(angle, endRadius, length);
                Triangle(vertices, ref vertex, a, b, c);
                Triangle(vertices, ref vertex, a, c, d);
                Triangle(vertices, ref vertex, Vector3.zero, b, a);
                Triangle(vertices, ref vertex, Vector3.forward * length, d, c);
            }

            for (int index = 0; index < indices.Length; index++) indices[index] = index;
            var mesh = new Mesh { name = label, vertices = vertices, triangles = indices };
            mesh.RecalculateNormals();
            meshes.Add(mesh);
            part.gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            part.gameObject.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static void Triangle(Vector3[] vertices, ref int index, Vector3 a, Vector3 b, Vector3 c)
        {
            vertices[index++] = a;
            vertices[index++] = b;
            vertices[index++] = c;
        }

        private static Vector3 Rim(float angle, Vector2 radius, float z) =>
            new Vector3(Mathf.Cos(angle) * radius.x, Mathf.Sin(angle) * radius.y, z);

        private static Vector3 At(float x, float y, float z) => new Vector3(x, y, z);
        private static Vector2 Size(float x, float y) => new Vector2(x, y);

        private void OnDestroy()
        {
            foreach (var mesh in meshes) Destroy(mesh);
        }
    }
}
