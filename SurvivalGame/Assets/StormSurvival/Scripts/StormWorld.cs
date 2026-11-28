using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.AI;

namespace StormSurvival
{
    public sealed class StormWorld : MonoBehaviour
    {
        public Material Wood { get; private set; }
        public Material Metal { get; private set; }
        public Material Amber { get; private set; }
        public Light Sun { get; private set; }
        public StormSearchlight Searchlight { get; private set; }
        public StormLookoutGate LookoutGate { get; private set; }
        public IReadOnlyList<StormWolf> Wolves => wolves;
        private StormGame game;
        private StormRunLayout runLayout;
        private Transform root, fallingTrunk;
        private GameObject bridge;
        private bool isPreview;
        private Material soil, grass, rock, plaster, roof, dark;
        private readonly List<Material> ownedMaterials = new List<Material>();
        private readonly List<Mesh> ownedMeshes = new List<Mesh>();
        private readonly List<Bounds> shelters = new List<Bounds>();
        private readonly List<(Transform Tree, Quaternion BaseRotation)> ridgeTrees = new List<(Transform, Quaternion)>();
        private readonly List<StormInteractable> fires = new List<StormInteractable>();

        private readonly List<StormWolf> wolves = new List<StormWolf>();
        private readonly List<(NavMeshData Data, NavMeshDataInstance Instance)> wolfNavigation =
            new List<(NavMeshData, NavMeshDataInstance)>();
        private Mesh terrainMesh;

        // Gaps between regions leave the bridge, far-bank camp, and lookout outside wolf territory.
        private static readonly (string Name, Vector2 Span, (Vector2 Home, Vector2 Crossing)[] Sites)[] WolfTerritories =
        {
            ("Logging trail", new Vector2(59f, 127f), new[]
            {
                (new Vector2(-3.2f, 73.5f), new Vector2(2.75f, 75f)),
                (new Vector2(3.2f, 101.5f), new Vector2(-2.65f, 103.3f)),
                (new Vector2(-3.2f, 116.7f), new Vector2(2.65f, 118.4f))
            }),
            ("Far bank", new Vector2(159.5f, 184f), new[]
            {
                (new Vector2(3.25f, 169.5f), new Vector2(-2.6f, 171.5f)),
                (new Vector2(-3.15f, 172.5f), new Vector2(2.55f, 174.5f)),
                (new Vector2(3.25f, 175.5f), new Vector2(-2.55f, 177.4f))
            }),
            ("Ridge", new Vector2(197f, 232.5f), new[]
            {
                (new Vector2(3.45f, 205.5f), new Vector2(-4.55f, 208f)),
                (new Vector2(3.5f, 215.7f), new Vector2(-4.2f, 218.3f)),
                (new Vector2(3.2f, 224.4f), new Vector2(-5f, 227.2f))
            })
        };

        internal const int WolfAgentType = 0;
        internal const float WolfAreaHalfWidth = 29f;
        private const float ForestBoundaryX = 40f;
        private const float CrouchBarrierZ = 57f;

        public static float Height(float z)
        {
            if (z > 128f && z < 130f) return -(z - 128f) * 1.5f;
            if (z >= 130f && z <= 142f) return -3f;
            if (z > 142f && z < 144f) return (z - 144f) * 1.5f;
            return z > 175f ? Mathf.Min(20f, (z - 175f) * 0.22f) : 0f;
        }

        public void Build(StormGame owner, Transform previewRoot = null)
        {
            game = owner;
            isPreview = previewRoot != null;
            runLayout = owner.RunLayout ?? new StormRunLayout(StormRunLayout.PreviewSeed);
            root = isPreview ? previewRoot : new GameObject("The Big Storm / Environment").transform;
            Wood = MakeMaterial(new Color(0.35f, 0.23f, 0.13f));
            if (game.woodTexture != null) Wood.SetTexture("_BaseMap", game.woodTexture);
            Metal = MakeMaterial(new Color(0.38f, 0.43f, 0.43f));
            Amber = MakeMaterial(new Color(1f, 0.68f, 0.25f), true);
            grass = MakeMaterial(new Color(0.30f, 0.37f, 0.25f));
            if (game.groundTexture != null) grass.SetTexture("_BaseMap", game.groundTexture);
            soil = MakeMaterial(new Color(0.35f, 0.28f, 0.18f));
            rock = MakeMaterial(new Color(0.25f, 0.31f, 0.30f));
            plaster = MakeMaterial(new Color(0.75f, 0.70f, 0.54f));
            roof = MakeMaterial(new Color(0.27f, 0.18f, 0.14f));
            dark = MakeMaterial(new Color(0.13f, 0.15f, 0.11f));
            BuildGround(); BuildForest(); BuildLoggingCamp(); BuildRiver(); BuildLookout();
            RingFire(new Vector3(-5, 0, 24), "Trailhead camp");
            RingFire(new Vector3(-8, 0, 82), "Logging camp");
            RingFire(new Vector3(-5, 0, 153), "Far-bank shelter");
            RingFire(new Vector3(6, Height(238), 238), "Lookout camp");
            Shelter(new Vector3(-5, 0, 24), 8f, 8f);
            Shelter(new Vector3(-5, 0, 153), 9f, 10f);
            StormRidgeRoute.Build(this, root, shelters.Add, PlaceTemplateSupplies);
            Shelter(new Vector3(6, Height(238), 238), 8f, 7f);
            PlaceTemplateSupplies(new Vector3(-2,0,19));
            PlaceTemplateSupplies(runLayout.StashFoodSite, runLayout.StashWaterSite);
            Marker(runLayout.StashMarkerSite);
            PlaceTemplateSupplies(new Vector3(5,0,153));
            PlaceTemplateSupplies(new Vector3(5,Height(239),239));
            PlaceMine(new Vector3(-4,0,16), false);
            PlaceMine(new Vector3(2,0,124), true);
            PlaceMine(new Vector3(7,0,161), false);
            PlaceMine(new Vector3(-5.4f, Height(242.8f), 242.8f), false);
            PlaceLookoutVein(new Vector3(7.2f, Height(243.8f), 243.8f), true);
            Material gateStone = game.crawlspaceStoneMaterial != null ? game.crawlspaceStoneMaterial : rock;
            LookoutGate = StormLookoutGate.Build(game, this, root, ForestBoundaryX, gateStone);
            AddTrailLog("Early jump lesson / fallen branch", new Vector3(0f, .27f, 47f), 2.9f, .65f);
            BuildCrouchBarrier();
            foreach (var p in new[] { new Vector3(4,0,17), new Vector3(-7,0,34), new Vector3(5,0,51), new Vector3(-6,0,67),
                new Vector3(7,0,92),new Vector3(-5,0,110),new Vector3(5,0,120),new Vector3(6,0,149),
                new Vector3(-6,0,169),new Vector3(5,0,190),new Vector3(-6,0,219),new Vector3(-6,0,245),new Vector3(5,0,254) }) WoodPile(p);

            for (int z = 10; z < 264; z += 14)
                if ((z < 125 || z > 145) && Mathf.Abs(z - StormLookoutGate.GateZ) >= 2f)
                    Marker(new Vector3(3f, Height(z), z));
            PlaceSpareFuel();
            if (!isPreview) BuildWolves();
            LightScene();
        }

        private void BuildWolves()
        {
            var sources = CollectWolfNavigationSources();
            var settings = NavMesh.GetSettingsByID(WolfAgentType);
            settings.agentRadius = .48f;
            settings.agentHeight = 1.45f;
            settings.agentClimb = .20f;

            var random = new System.Random(runLayout.WolfSeed);
            foreach (var territory in WolfTerritories)
            {
                int firstSite = random.Next(territory.Sites.Length);
                int patrolSeed = random.Next();
                var data = NavMeshBuilder.BuildNavMeshData(settings, sources, WolfNavigationBounds(territory.Span),
                    Vector3.zero, Quaternion.identity);
                var instance = data != null ? NavMesh.AddNavMeshData(data) : default;
                if (!instance.valid)
                {
                    if (data != null) DestroyGeneratedAsset(data);
                    Debug.LogError($"Could not build wolf navigation for {territory.Name}.");
                    continue;
                }
                wolfNavigation.Add((data, instance));

                StormWolf wolf = null;
                for (int offset = 0; offset < territory.Sites.Length && wolf == null; offset++)
                {
                    var site = territory.Sites[(firstSite + offset) % territory.Sites.Length];
                    wolf = StormWolf.Build(game, this, root, territory.Name, territory.Span, site, patrolSeed);
                }
                if (wolf != null) wolves.Add(wolf);
                else Debug.LogError($"Could not place a wolf patrol for {territory.Name}.");
            }
        }

        private static Bounds WolfNavigationBounds(Vector2 span)
        {
            float bottom = Height(span.x);
            float top = Height(span.y);
            return new Bounds(new Vector3(0f, (bottom + top) * .5f, (span.x + span.y) * .5f),
                new Vector3(WolfAreaHalfWidth * 2f, top - bottom + 12f, span.y - span.x));
        }

        private List<NavMeshBuildSource> CollectWolfNavigationSources()
        {
            var bounds = WolfNavigationBounds(new Vector2(WolfTerritories[0].Span.x, WolfTerritories[^1].Span.y));
            Physics.SyncTransforms();
            var sources = new List<NavMeshBuildSource>();
            NavMeshBuilder.CollectSources(root, Physics.DefaultRaycastLayers, NavMeshCollectGeometry.PhysicsColliders,
                NavMesh.GetAreaFromName("Walkable"), new List<NavMeshBuildMarkup>(), sources);
            int blocked = NavMesh.GetAreaFromName("Not Walkable");
            for (int index = sources.Count - 1; index >= 0; index--)
            {
                var source = sources[index];
                if (source.sourceObject == terrainMesh) continue;
                if (source.component is Collider collider)
                {
                    if (!collider.enabled || collider.isTrigger || !bounds.Intersects(collider.bounds) ||
                        collider.TryGetComponent<NavMeshObstacle>(out _))
                    {
                        sources.RemoveAt(index);
                        continue;
                    }

                    // Imported collider meshes may lack CPU-readable vertices in player builds.
                    if (source.sourceObject is Mesh mesh && !mesh.isReadable)
                    {
                        source.shape = NavMeshBuildSourceShape.Box;
                        source.size = collider.bounds.size;
                        source.transform = Matrix4x4.Translate(collider.bounds.center);
                        source.sourceObject = null;
                    }
                }
                source.area = blocked;
                sources[index] = source;
            }

            return sources;
        }

        private void AddTrailLog(string label, Vector3 center, float width, float diameter)
        {
            var scale = new Vector3(diameter, width * .5f, diameter);
            var log = Shape(label, PrimitiveType.Cylinder, root, center, scale, Wood, false);
            log.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);

            // Use the visible cylinder instead of the default capsule. The 2.9 m log
            // leaves .95 m of the 4.8 m trail open on either side.
            log.AddComponent<MeshCollider>().sharedMesh = log.GetComponent<MeshFilter>().sharedMesh;
        }

        private void BuildCrouchBarrier()
        {
            const float halfOpening = 1.15f;
            const float headroom = 1.15f;
            const float lintelHeight = .35f;
            const float wallTop = 5.9f;
            const float wallFoot = -.12f;
            const int sections = 7;
            var barrier = Group("Crouch barrier", new Vector3(0f, Height(CrouchBarrierZ), CrouchBarrierZ));
            var stone = rock;
            if (game.crawlspaceStoneMaterial != null)
            {
                stone = new Material(game.crawlspaceStoneMaterial) { name = "Crawlspace stone" };
                stone.SetTextureScale("_BaseMap", new Vector2(2f, 2f));
                stone.SetFloat("_BumpScale", .85f);
                ownedMaterials.Add(stone);
            }

            // The wings meet the perimeter walls, including a small overlap at their ends.
            float wingWidth = (ForestBoundaryX + 1f - halfOpening) / sections;
            for (int side = -1; side <= 1; side += 2)
            {
                for (int section = 0; section < sections; section++)
                {
                    float top = 5.55f + .28f * ((section + 1) % 3);
                    float depth = 2.1f + .24f * (section % 3);
                    float x = side * (halfOpening + wingWidth * (section + .5f));
                    float z = .12f * (section % 3 - 1);
                    Shape("Rock barrier wing", PrimitiveType.Cube, barrier, new Vector3(x, (top + wallFoot) / 2f, z),
                        new Vector3(wingWidth, top - wallFoot, depth), stone);
                }
            }

            float rockBase = headroom + lintelHeight;
            Shape("Rock above crawlspace", PrimitiveType.Cube, barrier, new Vector3(0f, (rockBase + wallTop) / 2f, 0f),
                new Vector3(2f * halfOpening + .12f, wallTop - rockBase, 2.25f), stone);
            Shape("Timber lintel", PrimitiveType.Cube, barrier, new Vector3(0f, headroom + lintelHeight / 2f, 0f),
                new Vector3(3.2f, lintelHeight, 2.8f), Wood);

            for (int entrance = -1; entrance <= 1; entrance += 2)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    float x = side * (halfOpening + .2f);
                    float z = entrance * 1.32f;
                    Shape("Crawlspace post", PrimitiveType.Cube, barrier, new Vector3(x, .67f, z),
                        new Vector3(.4f, 1.44f, .28f), Wood);
                    Shape("Amber crawlspace marker", PrimitiveType.Cube, barrier,
                        new Vector3(x, .87f, z + entrance * .151f), new Vector3(.26f, .09f, .035f), Amber, false);
                }
            }
        }

        internal static bool ApproachingCrouchBarrier(Vector3 position, Vector3 forward)
        {
            float distance = CrouchBarrierZ - position.z;
            return Mathf.Abs(position.x) <= 2.5f && Mathf.Abs(distance) <= 5f &&
                forward.z * Mathf.Sign(distance) > .28f;
        }

        public Material MakeMaterial(Color color, bool glow = false)
        {
            var template = glow ? game.glowMaterial : game.surfaceMaterial;
            var material = template != null ? new Material(template) : new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", color); material.color = color; material.SetFloat("_Smoothness", 0.07f);
            if (glow) { material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", color * 1.5f); }
            ownedMaterials.Add(material); return material;
        }
        public GameObject Shape(string label, PrimitiveType primitive, Transform parent, Vector3 p, Vector3 scale, Material material, bool solid = true)
        {
            var obj = GameObject.CreatePrimitive(primitive); obj.name = label;
            if (parent != null) obj.transform.SetParent(parent, false);
            obj.transform.localPosition = p; obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().sharedMaterial = material;
            if (!solid) { var c = obj.GetComponent<Collider>(); c.enabled = false; if (Application.isPlaying) Destroy(c); else DestroyImmediate(c); }
            return obj;
        }
        private Transform Group(string label, Vector3 p)
        { var obj = new GameObject(label); obj.transform.SetParent(root, false); obj.transform.position = p; return obj.transform; }
        private void Beam(Transform parent, Vector3 a, Vector3 b, float width, Material material)
        {
            var obj = Shape("Timber beam", PrimitiveType.Cube, parent, (a + b) * 0.5f, new Vector3(width, width, Vector3.Distance(a,b)), material);
            obj.transform.localRotation = Quaternion.LookRotation(b - a);
        }
        private void BuildGround()
        {
            const int nx = 41, nz = 151;
            var vertices = new Vector3[nx*nz]; var uv = new Vector2[vertices.Length]; var indices = new int[(nx-1)*(nz-1)*6];
            for (int z=0;z<nz;z++) for(int x=0;x<nx;x++)
            {
                int i=z*nx+x; float zz=-15f+z*2f; float xx=-40f+x*2f;
                vertices[i]=new Vector3(xx,Height(zz),zz);uv[i]=new Vector2(xx*0.15f,zz*0.15f);
            }
            int at=0;
            for(int z=0;z<nz-1;z++)for(int x=0;x<nx-1;x++)
            { int a=z*nx+x,b=a+1,c=a+nx;indices[at++]=a;indices[at++]=c;indices[at++]=b;indices[at++]=b;indices[at++]=c;indices[at++]=c+1; }
            terrainMesh = new Mesh { name="Forest ground and river banks",vertices=vertices,uv=uv,triangles=indices };
            terrainMesh.RecalculateNormals(); terrainMesh.RecalculateBounds(); ownedMeshes.Add(terrainMesh);
            var obj=Group("Forest terrain",Vector3.zero).gameObject;obj.AddComponent<MeshFilter>().sharedMesh=terrainMesh;obj.AddComponent<MeshRenderer>().sharedMaterial=grass;obj.AddComponent<MeshCollider>().sharedMesh=terrainMesh;
            for(int z=-10;z<280;z+=4)
            {
                if(z>126&&z<146)continue;
                float y=Height(z);
                var patch=Shape("Earth trail",PrimitiveType.Cube,root,new Vector3(0,y+0.012f,z),new Vector3(4.8f,0.024f,4.1f),soil,false);
                if(z>175)patch.transform.rotation=Quaternion.Euler(-Mathf.Atan(0.22f)*Mathf.Rad2Deg,0,0);
            }
            for(int side=-1;side<=1;side+=2)
                for(int z=-10;z<280;z+=12)
                    Shape("Forest boundary rocks",PrimitiveType.Cube,root,new Vector3(side*ForestBoundaryX,Height(z)+3,z),new Vector3(3,7,13),rock);
            Shape("Trailhead boundary",PrimitiveType.Cube,root,new Vector3(0,3,-15),new Vector3(82,7,3),rock);
            Shape("Lookout boundary",PrimitiveType.Cube,root,new Vector3(0,Height(283)+3,283),new Vector3(82,7,3),rock);
        }
        private void BuildForest()
        {
            var random=new System.Random(824);
            for(int z=-5;z<276;z+=6)
                for(int side=-1;side<=1;side+=2)
                    for(int row=0;row<2;row++)
                    {
                        float x=side*(row==0?11f+(float)random.NextDouble()*7f:26f+(float)random.NextDouble()*10f);
                        float zz=z+(float)random.NextDouble()*4;
                        if(zz>125&&zz<146||zz>68&&zz<96&&Mathf.Abs(x)<23)continue;
                        if(game.trees==null||game.trees.Length==0)continue;
                        var prefab=game.trees[random.Next(game.trees.Length)];if(prefab==null)continue;
                        float rotation = (float)random.NextDouble() * 360f;
                        float scale = 1f + (float)random.NextDouble() * .5f;
                        if (Mathf.Abs(zz - CrouchBarrierZ) < 1.95f) continue;
                        var tree=Instantiate(prefab,new Vector3(x,Height(zz),zz),Quaternion.Euler(0,rotation,0),root);
                        tree.transform.localScale *= scale;
                        foreach(var col in tree.GetComponentsInChildren<Collider>()){col.enabled=false;if(Application.isPlaying)Destroy(col);else DestroyImmediate(col);}
                        var trunkOwner = tree;
                        if (!isPreview && row == 0 && zz >= 180f && zz <= 236f)
                        {
                            // Bend the visible tree while keeping the trunk collider at its rest pose.
                            trunkOwner = new GameObject("Ridge tree trunk collision");
                            trunkOwner.layer = tree.layer;
                            trunkOwner.transform.SetParent(root, false);
                            trunkOwner.transform.SetPositionAndRotation(tree.transform.position, tree.transform.rotation);
                            trunkOwner.transform.localScale = tree.transform.localScale;
                            ridgeTrees.Add((tree.transform, tree.transform.localRotation));
                        }
                        var trunk = trunkOwner.AddComponent<CapsuleCollider>();
                        trunk.radius = .45f;
                        trunk.height = 7f;
                        trunk.center = Vector3.up * 3.5f;
                    }
            for(int z=30;z<260;z+=23)
            {
                if(z>124&&z<147)continue;
                float x=z%2==0?-9:9;
                if(game.rocks!=null&&game.rocks.Length>0)
                { var b=Instantiate(game.rocks[(z/23)%game.rocks.Length],new Vector3(x,Height(z),z),Quaternion.identity,root);b.transform.localScale*=2.2f; }
            }
        }
        public void SwayRidgeTrees(float strength, int direction)
        {
            foreach (var (tree, rest) in ridgeTrees)
            {
                float phase = Time.time * 3.2f + tree.position.z * .31f;
                float bend = strength * (2.3f + 1.5f * Mathf.Sin(phase));
                tree.localRotation = Quaternion.AngleAxis(-bend * direction, Vector3.forward) * rest;
            }
        }

        private void BuildLoggingCamp()
        {
            var shelter = PlaceTemplate(game.mineTemplate, new Vector3(-14,0,82), 6f, Quaternion.identity);
            if (shelter != null)
            {
                // The old floor box extends into the forest when the shelter is scaled up.
                if (shelter.TryGetComponent<MeshCollider>(out var shell) && shell.enabled && shell.sharedMesh != null &&
                    shelter.TryGetComponent<BoxCollider>(out var legacyFloor))
                    legacyFloor.enabled = false;

                var renderers = shelter.GetComponentsInChildren<MeshRenderer>();
                if (renderers.Length > 0)
                {
                    Bounds box = renderers[0].bounds; foreach (var r in renderers) box.Encapsulate(r.bounds);
                    shelters.Add(new Bounds(new Vector3(box.center.x,1.5f,box.center.z),new Vector3(Mathf.Max(2,box.size.x-1),3,Mathf.Max(2,box.size.z-1))));
                }
            }
            var lantern = PlaceTemplate(game.lanternTemplate, new Vector3(-6,0,89), 1.0f, Quaternion.identity);
            if (lantern != null) { var lamp = lantern.GetComponentInChildren<Lantern>(); if (lamp != null) { lamp.Fuel = 30; lamp.toggled = true; } }
            var fuel = PlaceTemplate(game.fuelTemplate, new Vector3(6,0,86), 1.6f, Quaternion.identity);
            if (fuel != null) { var supply = fuel.GetComponentInChildren<OilBarrel>(); supply.fuel = 8; supply.MaxFuel = 8; }
            var crate = Shape("Cloth supply box",PrimitiveType.Cube,root,new Vector3(-7,.5f,89),new Vector3(1.2f,1,.8f),Wood);
            var cache = crate.AddComponent<StormInteractable>(); cache.kind = StormInteractable.Kind.Cache; cache.label = "Logging supplies";
            Marker(new Vector3(-7,0,88));
            StormSupplySled.Build(game, this, root, runLayout.SledSite, isPreview);
        }

        private void PlaceSpareFuel()
        {
            var barrel = PlaceTemplate(game.fuelTemplate, runLayout.SpareFuelSite, 1.15f, Quaternion.identity);
            if (barrel == null || !barrel.TryGetComponent<OilBarrel>(out var fuel)) return;

            barrel.name = "Spare fuel barrel";
            fuel.fuel = 2f;
            fuel.MaxFuel = 2f;
            Marker(runLayout.SpareFuelMarkerSite);
        }

        private GameObject PlaceTemplate(GameObject prefab, Vector3 position, float height, Quaternion rotation)
        {
            if (prefab == null) return null;
            var obj = Instantiate(prefab, position, rotation, root); obj.name = "Template / " + prefab.name;
            foreach (var saver in obj.GetComponentsInChildren<LocationSaver>(true)) saver.enabled = false;
            foreach (var saver in obj.GetComponentsInChildren<SpawnObjectSaver>(true)) saver.enabled = false;
            var renderers = obj.GetComponentsInChildren<MeshRenderer>(true);
            if (renderers.Length > 0)
            {
                Bounds box = renderers[0].bounds; foreach (var r in renderers) box.Encapsulate(r.bounds);
                obj.transform.localScale *= height / Mathf.Max(0.01f,box.size.y);
                box = renderers[0].bounds; foreach (var r in renderers) box.Encapsulate(r.bounds);
                obj.transform.position += new Vector3(position.x-box.center.x,position.y-box.min.y,position.z-box.center.z);
            }
            return obj;
        }
        private void PlaceTemplateSupplies(Vector3 p)
        {
            PlaceTemplateSupplies(p, p + Vector3.right * 2f);
        }

        private void PlaceTemplateSupplies(Vector3 foodPosition, Vector3 waterPosition)
        {
            PlaceTemplate(game.foodTemplate, foodPosition, .42f, Quaternion.identity);
            var water = PlaceTemplate(game.waterTemplate, waterPosition, .12f, Quaternion.identity);
            if (water != null)
            {
                var source = water.GetComponentInChildren<WaterDrinkable>();
                if (source != null) { source.KeepPlacedHeight = true; source.refillAble = true; source.WaterAmount = 8; }
            }
        }
        private Mineable PlaceMine(Vector3 p, bool bridgeObstacle, bool addMarker = true)
        {
            var obj = PlaceTemplate(game.miningRockTemplate,p,bridgeObstacle?1.5f:1.1f,Quaternion.identity);
            if (obj == null) return null;
            var mine = obj.GetComponentInChildren<Mineable>(); if (mine == null) mine = obj.AddComponent<Mineable>();
            mine.Health = 150;
            var reward = mine.gameObject.AddComponent<StormMineReward>(); reward.bridgeObstacle = bridgeObstacle; reward.stone = 4; reward.Bind();
            if (addMarker)
                Shape("Amber mining marker",PrimitiveType.Cube,root,p+new Vector3(0,1.7f,0),new Vector3(.45f,.12f,.12f),Amber,false);
            return mine;
        }

        private void PlaceLookoutVein(Vector3 position, bool addMarker)
        {
            var mine = PlaceMine(position, false, addMarker);
            if (mine != null && !isPreview)
                mine.Depleted += _ => StartCoroutine(ReplenishLookoutVein(position));
        }

        private IEnumerator ReplenishLookoutVein(Vector3 position)
        {
            yield return new WaitForSeconds(24f);
            var checkAgain = new WaitForSeconds(1f);
            while (LookoutGate.StonesRemaining > 0)
            {
                if (game.Needs.Stone < LookoutGate.StonesRemaining &&
                    (game.Player.transform.position - position).sqrMagnitude > 2.2f * 2.2f)
                {
                    PlaceLookoutVein(position, false);
                    yield break;
                }
                yield return checkAgain;
            }
        }

        private void BuildRiver()
        {
            var water=MakeMaterial(new Color(.08f,.25f,.29f));water.SetFloat("_Smoothness",.8f);
            Shape("Cold river",PrimitiveType.Cube,root,new Vector3(0,-.65f,136),new Vector3(80,.08f,13.6f),water,false);
            bridge=Group("Released trunk bridge",new Vector3(0,0,136.2f)).gameObject;
            var log=Shape("Bridge trunk",PrimitiveType.Cylinder,bridge.transform,new Vector3(0,-.9f,0),new Vector3(2.2f,9.25f,2.2f),Wood,false);log.transform.localRotation=Quaternion.Euler(90,0,0);
            Shape("Walkable flattened trunk",PrimitiveType.Cube,bridge.transform,new Vector3(0,.09f,0),new Vector3(2.3f,.18f,18.5f),Wood);
            Shape("Bridge branch / jump reuse",PrimitiveType.Cube,bridge.transform,new Vector3(0,.38f,0),new Vector3(2.3f,.40f,.26f),Wood);
            bridge.SetActive(false);
            fallingTrunk=Group("Windfall above the river",new Vector3(-2,0,125));
            var trunk=Shape("Windfall",PrimitiveType.Cylinder,fallingTrunk,new Vector3(0,1.2f,0),new Vector3(.9f,2.5f,.9f),Wood,false);
            trunk.transform.localRotation=Quaternion.Euler(32,0,0);
            Shape("Amber cutting band",PrimitiveType.Cylinder,fallingTrunk,new Vector3(0,1.1f,-.1f),new Vector3(.96f,.13f,.96f),Amber,false);

        }
        public void DropBridge(){StartCoroutine(FallBridge());}
        private IEnumerator FallBridge()
        {
            for(float t=0;t<1.2f;t+=Time.deltaTime){fallingTrunk.rotation=Quaternion.Euler(Mathf.SmoothStep(0,75,t/1.2f),0,0);yield return null;}
            fallingTrunk.gameObject.SetActive(false);bridge.SetActive(true);Physics.SyncTransforms();
            game.Needs.BridgeBuilt=true;
            game.Toast("The bridge is ready. Keep to the middle and jump its raised branch.",7f);
        }
        private void BuildLookout()
        {
            var lookout=Group("Rescue lookout",new Vector3(0,Height(258),258));
            for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)
                Shape("Lookout pillar",PrimitiveType.Cube,lookout,new Vector3(x*3,4,z*3),new Vector3(.38f,8,.38f),Wood);
            Shape("Lookout canopy",PrimitiveType.Cube,lookout,new Vector3(0,8,0),new Vector3(7,.3f,7),roof);
            var beacon = new GameObject("Rescue beacon").transform;
            beacon.SetParent(lookout, false);
            Shape("Beacon pedestal", PrimitiveType.Cylinder, beacon, new Vector3(0, .4f, 0), new Vector3(2.8f, .4f, 2.8f), rock);
            Shape("Beacon bowl", PrimitiveType.Cylinder, beacon, new Vector3(0, 1, 0), new Vector3(2.3f, .2f, 2.3f), Metal);
            var item = beacon.gameObject.AddComponent<StormInteractable>();
            item.kind = StormInteractable.Kind.Beacon;
            item.label = "Rescue beacon";
            item.flame = Fire(beacon, new Vector3(0, 1.3f, 0), 2f);
            item.flame.SetActive(false);
            var generator = PlaceTemplate(game.generatorTemplate,new Vector3(5,Height(255),255),1.55f,Quaternion.identity);
            if (generator != null)
            {
                var rescueGenerator = generator.GetComponentInChildren<Generator>();
                rescueGenerator.MaxFuel = 1000;
                rescueGenerator.CurrentFuel = 0;
                rescueGenerator.Toggled = false;
                if (!isPreview) game.RescueGenerator = rescueGenerator;
            }
            var spotlight = PlaceTemplate(game.spotlightTemplate, new Vector3(-4f, Height(256f), 256f), 2.5f, Quaternion.identity);
            var power = generator != null ? generator.GetComponentInChildren<Generator>() : null;
            Searchlight = StormSearchlight.Build(game, this, lookout, power, spotlight);
            var barrel = PlaceTemplate(game.fuelTemplate,new Vector3(8,Height(250),250),1.5f,Quaternion.identity);
            if (barrel != null) { var source = barrel.GetComponentInChildren<OilBarrel>(); source.fuel = 4; source.MaxFuel = 4; }

        }
        private void WoodPile(Vector3 p)
        {
            p.y=Height(p.z);var pile=Group("Renewable firewood",p);var visual=new GameObject("Branches");visual.transform.SetParent(pile,false);
            for(int i=0;i<3;i++){var log=Shape("Branch",PrimitiveType.Cylinder,visual.transform,new Vector3((i-1)*.4f,.22f,.1f*i),new Vector3(.30f,1.2f,.30f),Wood);log.transform.localRotation=Quaternion.Euler(85,i*18,0);}
            Shape("Amber resource wrap",PrimitiveType.Cube,visual.transform,new Vector3(0,.45f,0),new Vector3(1.4f,.1f,.18f),Amber,false);
            var item=pile.gameObject.AddComponent<StormInteractable>();item.kind=StormInteractable.Kind.Wood;item.label="Firewood";item.model=visual;item.quantity=4;
        }
        private void RingFire(Vector3 p,string label)
        {
            var ring=Group(label,p);
            for(int i=0;i<10;i++){float a=i*Mathf.PI*2/10;Shape("Fire ring stone",PrimitiveType.Sphere,ring,new Vector3(Mathf.Cos(a)*.8f,.15f,Mathf.Sin(a)*.8f),new Vector3(.4f,.3f,.38f),rock);}
            Shape("Fire interaction stone",PrimitiveType.Cylinder,ring,new Vector3(0,.16f,0),new Vector3(1.3f,.16f,1.3f),dark);
            var item=ring.gameObject.AddComponent<StormInteractable>();item.kind=StormInteractable.Kind.Campfire;item.label=label;
            item.flame=Fire(ring,new Vector3(0,.3f,0),1f);item.flame.SetActive(false);fires.Add(item);
        }
        private void Shelter(Vector3 p,float width,float depth)
        {
            var frame=Group("Roofed windbreak",p);
            Shape("Windbreak roof",PrimitiveType.Cube,frame,new Vector3(0,3.2f,0),new Vector3(width,.22f,depth),Wood);
            for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)Shape("Shelter post",PrimitiveType.Cylinder,frame,new Vector3(x*(width/2-.2f),1.6f,z*(depth/2-.2f)),new Vector3(.18f,1.6f,.18f),Wood);
            shelters.Add(new Bounds(p+Vector3.up*1.2f,new Vector3(width-.3f,2.4f,depth-.3f)));
        }
        private void Marker(Vector3 p)
        {Shape("Trail stake",PrimitiveType.Cylinder,root,p+Vector3.up*.5f,new Vector3(.14f,.5f,.14f),Wood);Shape("Amber route marker",PrimitiveType.Cube,root,p+Vector3.up*1.05f,new Vector3(.3f,.13f,.1f),Amber,false);}
        public GameObject Fire(Transform parent,Vector3 p,float scale)
        {
            var obj=new GameObject("Flame and warm light");obj.transform.SetParent(parent,false);obj.transform.localPosition=p;
            var particles=obj.AddComponent<ParticleSystem>();var main=particles.main;main.startLifetime=.65f;main.startSpeed=1.25f*scale;main.startSize=.34f*scale;main.startColor=new Color(1,.5f,.1f,.9f);main.maxParticles=100;main.gravityModifier=-.07f;
            var emission=particles.emission;emission.rateOverTime=35;var shape=particles.shape;shape.shapeType=ParticleSystemShapeType.Cone;shape.angle=12;shape.radius=.18f*scale;
            var renderer=particles.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=game.particleMaterial;
            var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(new Color(1,.8f,.25f),0),new GradientColorKey(new Color(1,.17f,.03f),1)},new[]{new GradientAlphaKey(.9f,0),new GradientAlphaKey(0,1)});
            var colors=particles.colorOverLifetime;colors.enabled=true;colors.color=gradient;
            var light=obj.AddComponent<Light>();light.type=LightType.Point;light.color=new Color(1,.6f,.25f);light.intensity=2.8f*scale;light.range=scale<.5f?8:12;light.shadows=LightShadows.None;
            return obj;
        }
        public StormPlayer CreatePlayer()
        {
            if (game.playerTemplate == null) throw new System.InvalidOperationException("The template player prefab reference is missing.");
            var obj = Instantiate(game.playerTemplate); obj.name = "Template Player / Survivor";
            var player = obj.AddComponent<StormPlayer>(); player.Initialize(game); return player;
        }
        public bool IsSheltered(Vector3 p){foreach(var box in shelters)if(box.Contains(p+Vector3.up))return true;return false;}
        public bool NearBurningFire(Vector3 p){foreach(var fire in fires)if(fire.fuel>0&&Vector3.Distance(p,fire.transform.position)<7f)return true;return false;}
        public StormInteractable NearestFire(Vector3 p)
        {StormInteractable best=fires[0];foreach(var fire in fires)if((p-fire.transform.position).sqrMagnitude<(p-best.transform.position).sqrMagnitude)best=fire;return best;}
        private void LightScene()
        {
            var sunlight = new GameObject("Storm sunlight");
            sunlight.transform.SetParent(root, false);
            sunlight.transform.rotation = Quaternion.Euler(44, -35, 0);
            Sun = sunlight.AddComponent<Light>();
            Sun.type = LightType.Directional;
            Sun.intensity = 1.1f;
            Sun.color = new Color(.93f, .96f, 1f);
            Sun.shadows = LightShadows.Soft;
            if (isPreview) return;

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.50f, .58f, .58f);
            RenderSettings.ambientEquatorColor = new Color(.34f, .40f, .34f);
            RenderSettings.ambientGroundColor = new Color(.2f, .23f, .17f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = .012f;
            RenderSettings.fogColor = new Color(.32f, .39f, .39f);
            RenderSettings.sun = Sun;
            QualitySettings.shadowDistance = 65;
        }

        public void ReleaseGeneratedAssets()
        {
            foreach (var material in ownedMaterials) DestroyGeneratedAsset(material);
            foreach (var mesh in ownedMeshes) DestroyGeneratedAsset(mesh);
            ownedMaterials.Clear();
            ownedMeshes.Clear();
        }

        private void DestroyGeneratedAsset(Object asset)
        {
            if (asset == null) return;
            if (Application.isPlaying) Destroy(asset);
            else DestroyImmediate(asset);
        }

        private void OnDestroy()
        {
            foreach (var (data, instance) in wolfNavigation)
            {
                if (instance.valid) instance.Remove();
                DestroyGeneratedAsset(data);
            }
            ReleaseGeneratedAssets();
        }
    }
}
