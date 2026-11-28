using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace StormSurvival.Editor
{
    [InitializeOnLoad]
    public static class StormProjectTools
    {
        private const string PathToScene = "Assets/StormSurvival/Scenes/TheBigStorm.unity";
        private const string PreviewName = "Forest Preview (Editor Only)";
        private const string FramedPreviewKey = "StormSurvival.FramedPreview";
        private static StormWorld preview;
        private static Scene previewScene;

        static StormProjectTools()
        {
            EditorApplication.delayCall += RefreshPreview;
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorSceneManager.sceneClosing += OnSceneClosing;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            AssemblyReloadEvents.beforeAssemblyReload += RemovePreview;
        }

        [MenuItem("The Big Storm/Open Survival Scene")]
        public static void Open()
        {
            if(EditorApplication.isPlaying){Debug.LogWarning("Stop Play mode first.");return;}
            if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())EditorSceneManager.OpenScene(PathToScene);
        }
        [MenuItem("The Big Storm/Validate Project")]
        public static void ValidateProject()
        {
            var previous=EditorSceneManager.GetSceneManagerSetup();bool opened=SceneManager.GetActiveScene().path!=PathToScene;
            if(opened&&!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            try
            {
                if(opened)EditorSceneManager.OpenScene(PathToScene);
                var game=Object.FindFirstObjectByType<StormGame>();
                if(game==null)throw new Exception("StormGame scene controller missing.");
                if(game.surfaceMaterial==null||game.glowMaterial==null||game.particleMaterial==null)throw new Exception("A survival material is missing.");
                if(game.groundTexture==null||game.woodTexture==null)throw new Exception("A forest texture is missing.");
                foreach(var tree in game.trees)if(tree==null)throw new Exception("A tree prefab is missing.");
                foreach(var rock in game.rocks)if(rock==null)throw new Exception("A rock prefab is missing.");
                foreach(var prefab in new[]{game.playerTemplate,game.foodTemplate,game.waterTemplate,game.fuelTemplate,game.generatorTemplate,game.spotlightTemplate,game.lanternTemplate,game.mineTemplate,game.miningRockTemplate})
                    if(prefab==null)throw new Exception("A Mineshaft template prefab reference is missing.");
                if(game.playerTemplate.GetComponent<UnityStandardAssets.Characters.FirstPerson.FirstPersonController>()==null||game.playerTemplate.GetComponent<ObjectUsage>()==null)
                    throw new Exception("The original template player components are missing.");
                if(EditorBuildSettings.scenes.Length==0||EditorBuildSettings.scenes[0].path!=PathToScene||!EditorBuildSettings.scenes[0].enabled)throw new Exception("Survival scene must be the first enabled build scene.");
                Debug.Log("The Big Storm: scene and asset references passed. Enter Play mode and run Validate Running Level.");
            }
            finally{if(opened)EditorSceneManager.RestoreSceneManagerSetup(previous);}
        }
        [MenuItem("The Big Storm/Validate Running Level")]
        public static void ValidateRunning()
        {
            if(!EditorApplication.isPlaying)throw new Exception("Enter Play mode in TheBigStorm first.");
            var game=StormGame.Instance;
            if(game==null||game.Player==null||game.UI==null||game.Weather==null)throw new Exception("A runtime system failed to initialize.");
            if(Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length!=1)throw new Exception("Expected one AudioListener.");
            if(game.Player.Controller==null||game.Player.Inventory==null||!game.Player.Stats.ExternallyManaged||game.RescueGenerator==null)throw new Exception("Template integration failed.");
            if(game.Player.gameObject.layer!=2)throw new Exception("The player must ignore interaction raycasts.");
            int wood=0,fires=0,beacons=0;
            foreach(var item in StormInteractable.All)
            {if(item.kind==StormInteractable.Kind.Wood)wood++;if(item.kind==StormInteractable.Kind.Campfire)fires++;if(item.kind==StormInteractable.Kind.Beacon)beacons++;}
            if(wood<10||fires!=7||beacons!=1||game.World.Backcountry==null||game.World.Wolves.Count!=5)
                throw new Exception("World interactions are incomplete.");
            if(!game.Needs.BridgeBuilt&&Object.FindObjectsByType<StormMineReward>(FindObjectsSortMode.None).Length<1)throw new Exception("Mine rewards are missing.");
            if(!game.World.IsSheltered(new Vector3(-5,0,24)))throw new Exception("Trailhead shelter bounds do not cover the camp.");
            if(Mathf.Abs(StormWorld.Height(136)+3f)>.001f)throw new Exception("River terrain is incorrect.");
            Debug.Log("The Big Storm: runtime systems, interaction counts, shelter, and river checks passed. Complete the manual playtest next.");
        }
        [MenuItem("The Big Storm/Preview World in Scene View")]
        public static void Preview()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Stop Play mode first.");
                return;
            }

            var scene = SceneManager.GetSceneByPath(PathToScene);
            if (!scene.isLoaded)
            {
                Open();
                scene = SceneManager.GetSceneByPath(PathToScene);
            }

            if (!scene.isLoaded) return;
            RemovePreview();
            ShowPreview(scene);
            FramePreview();
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            if (scene.path != PathToScene || EditorApplication.isPlayingOrWillChangePlaymode) return;
            ShowPreview(scene);
            FrameFirstPreview();
        }

        private static void OnSceneClosing(Scene scene, bool removingScene)
        {
            if (previewScene == scene) RemovePreview();
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode) RemovePreview();
            if (state == PlayModeStateChange.EnteredEditMode) RefreshPreview();
        }

        private static void RefreshPreview()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            var scene = SceneManager.GetSceneByPath(PathToScene);
            if (!scene.isLoaded) return;

            ShowPreview(scene);
            FrameFirstPreview();
        }

        private static void FrameFirstPreview()
        {
            if (preview != null && !SessionState.GetBool(FramedPreviewKey, false)) FramePreview();
        }

        private static void ShowPreview(Scene scene)
        {
            if (preview != null && previewScene == scene) return;
            RemovePreview();

            StormGame game = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                game = root.GetComponentInChildren<StormGame>();
                if (game != null) break;
            }
            if (game == null) return;

            var previewRoot = new GameObject(PreviewName);
            SceneManager.MoveGameObjectToScene(previewRoot, scene);
            previewRoot.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
            previewRoot.tag = "EditorOnly";
            previewScene = scene;
            preview = previewRoot.AddComponent<StormWorld>();
            try
            {
                preview.Build(game, previewRoot.transform);
            }
            catch
            {
                RemovePreview();
                throw;
            }
            SceneView.RepaintAll();
        }

        private static void RemovePreview()
        {
            if (preview == null) return;
            var previewRoot = preview.gameObject;
            preview.ReleaseGeneratedAssets();
            preview = null;
            previewScene = default;
            Object.DestroyImmediate(previewRoot);
        }

        private static void FramePreview()
        {
            if (preview == null) return;
            var view = SceneView.lastActiveSceneView;
            if (view == null && SceneView.sceneViews.Count > 0) view = SceneView.sceneViews[0] as SceneView;
            if (view == null) return;
            view.LookAt(new Vector3(0f, 2f, 130f), Quaternion.Euler(52f, -24f, 0f), 155f);
            SessionState.SetBool(FramedPreviewKey, true);
        }
    }
}
