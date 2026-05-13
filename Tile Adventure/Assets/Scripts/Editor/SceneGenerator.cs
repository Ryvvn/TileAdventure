using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TileAdventure.Config;
using TileAdventure.Core;
using TileAdventure.Gameplay;
using TileAdventure.Services;
using TileAdventure.UI;

namespace TileAdventure.Editor
{
    public static class SceneGenerator
    {
        [MenuItem("TileAdventure/Generate All Scenes")]
        public static void GenerateAllScenes()
        {
            GenerateLoadingScene();
            GenerateHomeScene();
            GenerateGameplayScene();
            AssetDatabase.Refresh();
            Debug.Log("All scenes generated successfully.");
        }

        [MenuItem("TileAdventure/Generate Loading Scene")]
        public static void GenerateLoadingScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var mainCameraGo = new GameObject("Main Camera", typeof(Camera));
            var cam = mainCameraGo.GetComponent<Camera>();
            cam.backgroundColor = Color.black;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.orthographic = false;

            var canvasGo = new GameObject("LoadingCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var bootstrapper = canvasGo.AddComponent<SceneBootstrapper>();
            var loadingScreen = canvasGo.AddComponent<LoadingScreen>();

            var bgGo = new GameObject("Background", typeof(Image));
            bgGo.transform.SetParent(canvasGo.transform, false);
            var bgImage = bgGo.GetComponent<Image>();
            bgImage.color = Color.black;
            var bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;

            var progressBarGo = new GameObject("ProgressBar", typeof(Slider));
            progressBarGo.transform.SetParent(canvasGo.transform, false);
            var progressRt = progressBarGo.GetComponent<RectTransform>();
            progressRt.anchorMin = new Vector2(0.2f, 0.45f);
            progressRt.anchorMax = new Vector2(0.8f, 0.5f);
            progressRt.offsetMin = Vector2.zero;
            progressRt.offsetMax = Vector2.zero;

            var progressTextGo = new GameObject("ProgressText", typeof(Text));
            progressTextGo.transform.SetParent(canvasGo.transform, false);
            var progressText = progressTextGo.GetComponent<Text>();
            progressText.text = "0%";
            progressText.alignment = TextAnchor.MiddleCenter;
            progressText.fontSize = 24;
            progressText.color = Color.white;
            var ptRt = progressTextGo.GetComponent<RectTransform>();
            ptRt.anchorMin = new Vector2(0.2f, 0.52f);
            ptRt.anchorMax = new Vector2(0.8f, 0.58f);
            ptRt.offsetMin = Vector2.zero;
            ptRt.offsetMax = Vector2.zero;

            var statusTextGo = new GameObject("StatusText", typeof(Text));
            statusTextGo.transform.SetParent(canvasGo.transform, false);
            var statusText = statusTextGo.GetComponent<Text>();
            statusText.text = "Loading...";
            statusText.alignment = TextAnchor.MiddleCenter;
            statusText.fontSize = 18;
            statusText.color = Color.white;
            var stRt = statusTextGo.GetComponent<RectTransform>();
            stRt.anchorMin = new Vector2(0.2f, 0.6f);
            stRt.anchorMax = new Vector2(0.8f, 0.65f);
            stRt.offsetMin = Vector2.zero;
            stRt.offsetMax = Vector2.zero;

            var audioGo = new GameObject("AudioManager", typeof(AudioManager));

            ApplySerializedRefs(canvasGo, loadingScreen, progressBarGo, progressTextGo, statusTextGo);

            _ = EditorSceneManager.SaveScene(scene, "Assets/Scenes/Loading.unity");
            EditorSceneManager.OpenScene("Assets/Scenes/Loading.unity");
        }

        [MenuItem("TileAdventure/Generate Home Scene")]
        public static void GenerateHomeScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var mainCameraGo = new GameObject("Main Camera", typeof(Camera));
            var cam = mainCameraGo.GetComponent<Camera>();
            cam.backgroundColor = new Color(0.1f, 0.15f, 0.3f);
            cam.clearFlags = CameraClearFlags.SolidColor;

            var canvasGo = new GameObject("HomeCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var bootstrapper = canvasGo.AddComponent<SceneBootstrapper>();
            var homeScreen = canvasGo.AddComponent<HomeScreen>();

            var bgGo = new GameObject("Background", typeof(Image));
            bgGo.transform.SetParent(canvasGo.transform, false);
            var bgImage = bgGo.GetComponent<Image>();
            bgImage.color = new Color(0.1f, 0.15f, 0.3f);
            var bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;

            var logoGo = new GameObject("Logo", typeof(Image));
            logoGo.transform.SetParent(canvasGo.transform, false);
            var logoRt = logoGo.GetComponent<RectTransform>();
            logoRt.anchorMin = new Vector2(0.5f, 0.7f);
            logoRt.anchorMax = new Vector2(0.5f, 0.7f);
            logoRt.sizeDelta = new Vector2(300, 120);
            logoRt.anchoredPosition = Vector2.zero;

            var playButtonGo = new GameObject("PlayButton", typeof(Image), typeof(Button));
            playButtonGo.transform.SetParent(canvasGo.transform, false);
            var playRt = playButtonGo.GetComponent<RectTransform>();
            playRt.anchorMin = new Vector2(0.5f, 0.5f);
            playRt.anchorMax = new Vector2(0.5f, 0.5f);
            playRt.sizeDelta = new Vector2(200, 60);
            playRt.anchoredPosition = Vector2.zero;

            var playLabelGo = new GameObject("Label", typeof(Text));
            playLabelGo.transform.SetParent(playButtonGo.transform, false);
            var playLabel = playLabelGo.GetComponent<Text>();
            playLabel.text = "PLAY";
            playLabel.alignment = TextAnchor.MiddleCenter;
            playLabel.fontSize = 28;
            playLabel.color = Color.white;
            var plRt = playLabelGo.GetComponent<RectTransform>();
            plRt.anchorMin = Vector2.zero;
            plRt.anchorMax = Vector2.one;
            plRt.offsetMin = Vector2.zero;
            plRt.offsetMax = Vector2.zero;

            var gridGo = new GameObject("LevelGrid", typeof(GridLayoutGroup));
            gridGo.transform.SetParent(canvasGo.transform, false);
            var gridRt = gridGo.GetComponent<RectTransform>();
            gridRt.anchorMin = new Vector2(0.1f, 0.05f);
            gridRt.anchorMax = new Vector2(0.9f, 0.38f);
            gridRt.offsetMin = Vector2.zero;
            gridRt.offsetMax = Vector2.zero;
            var gridLayout = gridGo.GetComponent<GridLayoutGroup>();
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 5;
            gridLayout.cellSize = new Vector2(80, 80);
            gridLayout.spacing = new Vector2(10, 10);

            var levelButtonPrefabGo = new GameObject("LevelButtonPrefab", typeof(Image), typeof(Button));
            var prefabLabelGo = new GameObject("Label", typeof(Text));
            prefabLabelGo.transform.SetParent(levelButtonPrefabGo.transform, false);
            var prefabLabel = prefabLabelGo.GetComponent<Text>();
            prefabLabel.text = "0";
            prefabLabel.alignment = TextAnchor.MiddleCenter;
            prefabLabel.fontSize = 24;
            prefabLabel.color = Color.white;
            var pflRt = prefabLabelGo.GetComponent<RectTransform>();
            pflRt.anchorMin = Vector2.zero;
            pflRt.anchorMax = Vector2.one;
            pflRt.offsetMin = Vector2.zero;
            pflRt.offsetMax = Vector2.zero;

            ApplyHomeSerializedRefs(homeScreen, canvasGo, logoGo, playButtonGo, gridGo, levelButtonPrefabGo);

            _ = EditorSceneManager.SaveScene(scene, "Assets/Scenes/Home.unity");
        }

        [MenuItem("TileAdventure/Generate Gameplay Scene")]
        public static void GenerateGameplayScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var mainCameraGo = new GameObject("Main Camera", typeof(Camera));
            var cam = mainCameraGo.GetComponent<Camera>();
            cam.backgroundColor = Color.black;
            cam.clearFlags = CameraClearFlags.SolidColor;

            var canvasGo = new GameObject("GameplayCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var bootstrapper = canvasGo.AddComponent<SceneBootstrapper>();
            var gameplayController = canvasGo.AddComponent<GameplayController>();

            var bgGo = new GameObject("Background", typeof(Image));
            bgGo.transform.SetParent(canvasGo.transform, false);
            var bgImage = bgGo.GetComponent<Image>();
            bgImage.color = new Color(0.08f, 0.12f, 0.18f);
            var bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;

            var boardGo = new GameObject("BoardContainer", typeof(RectTransform));
            boardGo.transform.SetParent(canvasGo.transform, false);
            var boardView = boardGo.AddComponent<BoardView>();
            var boardRt = boardGo.GetComponent<RectTransform>();
            boardRt.anchorMin = new Vector2(0.05f, 0.3f);
            boardRt.anchorMax = new Vector2(0.95f, 0.95f);
            boardRt.offsetMin = Vector2.zero;
            boardRt.offsetMax = Vector2.zero;

            var rackGo = new GameObject("RackContainer", typeof(RectTransform));
            rackGo.transform.SetParent(canvasGo.transform, false);
            var rackView = rackGo.AddComponent<RackView>();
            var rackRt = rackGo.GetComponent<RectTransform>();
            rackRt.anchorMin = new Vector2(0.05f, 0.08f);
            rackRt.anchorMax = new Vector2(0.95f, 0.22f);
            rackRt.offsetMin = Vector2.zero;
            rackRt.offsetMax = Vector2.zero;

            var levelTextGo = new GameObject("LevelText", typeof(Text));
            levelTextGo.transform.SetParent(canvasGo.transform, false);
            var levelText = levelTextGo.GetComponent<Text>();
            levelText.text = "Level 1";
            levelText.alignment = TextAnchor.MiddleCenter;
            levelText.fontSize = 28;
            levelText.color = Color.white;
            var ltRt = levelTextGo.GetComponent<RectTransform>();
            ltRt.anchorMin = new Vector2(0.5f, 0.95f);
            ltRt.anchorMax = new Vector2(0.5f, 0.95f);
            ltRt.sizeDelta = new Vector2(200, 40);

            var progressTextGo = new GameObject("ProgressText", typeof(Text));
            progressTextGo.transform.SetParent(canvasGo.transform, false);
            var progressText = progressTextGo.GetComponent<Text>();
            progressText.text = "0 / 4";
            progressText.alignment = TextAnchor.MiddleCenter;
            progressText.fontSize = 22;
            progressText.color = Color.white;
            var prtRt = progressTextGo.GetComponent<RectTransform>();
            prtRt.anchorMin = new Vector2(0.5f, 0.25f);
            prtRt.anchorMax = new Vector2(0.5f, 0.25f);
            prtRt.sizeDelta = new Vector2(150, 30);

            var tilePrefabGo = new GameObject("TilePrefab", typeof(Image), typeof(TileView));
            var iconGo = new GameObject("Icon", typeof(Image));
            iconGo.transform.SetParent(tilePrefabGo.transform, false);
            var iconRt = iconGo.GetComponent<RectTransform>();
            iconRt.anchorMin = new Vector2(0.15f, 0.15f);
            iconRt.anchorMax = new Vector2(0.85f, 0.85f);

            var winPopupGo = new GameObject("WinPopup", typeof(Image));
            winPopupGo.transform.SetParent(canvasGo.transform, false);
            var winRt = winPopupGo.GetComponent<RectTransform>();
            winRt.anchorMin = new Vector2(0.1f, 0.2f);
            winRt.anchorMax = new Vector2(0.9f, 0.6f);

            var winTextGo = new GameObject("WinText", typeof(Text));
            winTextGo.transform.SetParent(winPopupGo.transform, false);
            var winText = winTextGo.GetComponent<Text>();
            winText.text = "LEVEL COMPLETE!";
            winText.alignment = TextAnchor.MiddleCenter;
            winText.fontSize = 32;
            winText.color = Color.green;
            var wtRt = winTextGo.GetComponent<RectTransform>();
            wtRt.anchorMin = new Vector2(0f, 0.1f);
            wtRt.anchorMax = new Vector2(1f, 0.4f);

            var losePopupGo = new GameObject("LosePopup", typeof(Image));
            losePopupGo.transform.SetParent(canvasGo.transform, false);
            var loseRt = losePopupGo.GetComponent<RectTransform>();
            loseRt.anchorMin = new Vector2(0.1f, 0.2f);
            loseRt.anchorMax = new Vector2(0.9f, 0.6f);

            var loseTextGo = new GameObject("LoseText", typeof(Text));
            loseTextGo.transform.SetParent(losePopupGo.transform, false);
            var loseText = loseTextGo.GetComponent<Text>();
            loseText.text = "RACK FULL!";
            loseText.alignment = TextAnchor.MiddleCenter;
            loseText.fontSize = 32;
            loseText.color = Color.red;
            var lstRt = loseTextGo.GetComponent<RectTransform>();
            lstRt.anchorMin = new Vector2(0f, 0.1f);
            lstRt.anchorMax = new Vector2(1f, 0.4f);

            var restartButtonGo = CreatePopupButton("RestartButton", "RESTART", canvasGo.transform);
            var restartRt = restartButtonGo.GetComponent<RectTransform>();
            restartRt.anchorMin = new Vector2(0.25f, 0.52f);
            restartRt.anchorMax = new Vector2(0.75f, 0.64f);

            var homeButtonGo = CreatePopupButton("HomeButton", "HOME", canvasGo.transform);
            var homeRt = homeButtonGo.GetComponent<RectTransform>();
            homeRt.anchorMin = new Vector2(0.25f, 0.68f);
            homeRt.anchorMax = new Vector2(0.75f, 0.80f);

            ApplyGameplaySerializedRefs(gameplayController, boardView, rackView, levelTextGo, progressTextGo,
                winPopupGo, losePopupGo, tilePrefabGo, restartButtonGo, homeButtonGo);

            _ = EditorSceneManager.SaveScene(scene, "Assets/Scenes/Gameplay.unity");
        }

        [MenuItem("TileAdventure/Generate GameConstants Asset")]
        public static void GenerateGameConstants()
        {
            var constants = ScriptableObject.CreateInstance<GameConstants>();
            AssetDatabase.CreateAsset(constants, "Assets/Resources/Config/GameConstants.asset");
            AssetDatabase.SaveAssets();
            Debug.Log("GameConstants created at Assets/Resources/Config/GameConstants.asset");
        }

        [MenuItem("TileAdventure/Generate Level Assets")]
        public static void GenerateLevelAssets()
        {
            for (int i = 1; i <= 10; i++)
            {
                var levelConfig = LevelGenerator.GenerateLevelConfig(i);
                AssetDatabase.CreateAsset(levelConfig, $"Assets/Resources/Config/Levels/Level_{i:D2}.asset");
            }
            AssetDatabase.SaveAssets();
            Debug.Log("10 level configs created in Assets/Resources/Config/Levels/");
        }

        private static GameObject CreatePopupButton(string name, string label, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var labelGo = new GameObject("Label", typeof(Text));
            labelGo.transform.SetParent(go.transform, false);
            var t = labelGo.GetComponent<Text>();
            t.text = label;
            t.alignment = TextAnchor.MiddleCenter;
            t.fontSize = 28;
            t.color = Color.white;
            var lRt = labelGo.GetComponent<RectTransform>();
            lRt.anchorMin = Vector2.zero;
            lRt.anchorMax = Vector2.one;
            lRt.offsetMin = Vector2.zero;
            lRt.offsetMax = Vector2.zero;

            return go;
        }

        private static void ApplySerializedRefs(GameObject canvasGo, LoadingScreen loadingScreen,
            GameObject progressBar, GameObject progressText, GameObject statusText)
        {
            var so = new SerializedObject(loadingScreen);
            so.FindProperty("_progressBar").objectReferenceValue = progressBar.GetComponent<Slider>();
            so.FindProperty("_progressText").objectReferenceValue = progressText.GetComponent<Text>();
            so.FindProperty("_statusText").objectReferenceValue = statusText.GetComponent<Text>();
            so.FindProperty("_homeSceneName").stringValue = "Home";
            so.ApplyModifiedProperties();
        }

        private static void ApplyHomeSerializedRefs(HomeScreen homeScreen,
            GameObject canvasGo, GameObject logoGo, GameObject playButtonGo, GameObject gridGo, GameObject levelButtonPrefab)
        {
            var so = new SerializedObject(homeScreen);
            so.FindProperty("_logoImage").objectReferenceValue = logoGo.GetComponent<Image>();
            so.FindProperty("_playButton").objectReferenceValue = playButtonGo.GetComponent<Button>();
            so.FindProperty("_levelGridContainer").objectReferenceValue = gridGo.transform;
            so.FindProperty("_levelButtonPrefab").objectReferenceValue = levelButtonPrefab;
            so.FindProperty("_gameplaySceneName").stringValue = "Gameplay";
            so.ApplyModifiedProperties();
        }

        private static void ApplyGameplaySerializedRefs(GameplayController ctrl, BoardView boardView, RackView rackView,
            GameObject levelTextGo, GameObject progressTextGo,
            GameObject winPopup, GameObject losePopup, GameObject tilePrefab,
            GameObject restartButtonGo, GameObject homeButtonGo)
        {
            var so = new SerializedObject(ctrl);
            so.FindProperty("_boardView").objectReferenceValue = boardView;
            so.FindProperty("_rackView").objectReferenceValue = rackView;
            so.FindProperty("_constants").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GameConstants>("Assets/Resources/Config/GameConstants.asset");
            so.FindProperty("_levelText").objectReferenceValue = levelTextGo.GetComponent<Text>();
            so.FindProperty("_progressText").objectReferenceValue = progressTextGo.GetComponent<Text>();
            so.FindProperty("_winPopup").objectReferenceValue = winPopup;
            so.FindProperty("_losePopup").objectReferenceValue = losePopup;
            so.FindProperty("_restartButton").objectReferenceValue = restartButtonGo.GetComponent<Button>();
            so.FindProperty("_homeButton").objectReferenceValue = homeButtonGo.GetComponent<Button>();
            so.ApplyModifiedProperties();

            var bvSo = new SerializedObject(boardView);
            bvSo.FindProperty("_tilePrefab").objectReferenceValue = tilePrefab.GetComponent<TileView>();
            bvSo.FindProperty("_boardContainer").objectReferenceValue = boardView.GetComponent<RectTransform>();
            bvSo.FindProperty("_constants").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GameConstants>("Assets/Resources/Config/GameConstants.asset");
            bvSo.ApplyModifiedProperties();

            var rvSo = new SerializedObject(rackView);
            rvSo.FindProperty("_rackContainer").objectReferenceValue = rackView.GetComponent<RectTransform>();
            rvSo.FindProperty("_rackTilePrefab").objectReferenceValue = tilePrefab.GetComponent<TileView>();
            rvSo.FindProperty("_constants").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GameConstants>("Assets/Resources/Config/GameConstants.asset");
            rvSo.ApplyModifiedProperties();
        }
    }
}
