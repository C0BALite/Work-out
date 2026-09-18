using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>One-time migration of the desktop design to ordinary, editable Unity assets.</summary>
[InitializeOnLoad]
public static class WorkOutUIAuthoring
{
    private const string Request = "artifacts/ui/bake-request.txt";
    private const string Report = "artifacts/ui/bake-result.txt";
    private const string UIPrefabs = "Assets/Prefabs/UI";

    static WorkOutUIAuthoring() { EditorApplication.update += Poll; }

    private static void Poll()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (File.Exists("artifacts/ui/play-request.txt"))
        {
            File.Delete("artifacts/ui/play-request.txt");
            WorkOutPlayValidation.Run();
            return;
        }
        if (File.Exists("artifacts/ui/repair-request.txt"))
        {
            File.Delete("artifacts/ui/repair-request.txt");
            try { RepairPuzzleCanvases(); }
            catch (Exception e) { File.WriteAllText("artifacts/ui/repair-result.txt", e.ToString()); }
            return;
        }
        if (!File.Exists(Request)) return;
        File.Delete(Request);
        try { Bake(); }
        catch (Exception e) { File.WriteAllText(Report, "FAIL\n" + e); Debug.LogException(e); }
    }

    [MenuItem("Tools/Work Out/Bake editable UI")]
    public static void Bake()
    {
        Directory.CreateDirectory("artifacts/ui");
        Application.logMessageReceived -= CaptureLog;
        Application.logMessageReceived += CaptureLog;
        try { BakeAssets(); }
        finally { Application.logMessageReceived -= CaptureLog; }
    }

    private static void BakeAssets()
    {
        Directory.CreateDirectory(UIPrefabs);
        BakeSprites();
        AssetDatabase.Refresh();

        EditPrefab("Assets/Prefabs/PlayerTIle.prefab", WorkOutScreenStyle.ListRow);
        EditPrefab("Assets/Prefabs/ResultsRow.prefab", WorkOutScreenStyle.ListRow);
        EditPrefab("Assets/Prefabs/DocumentReferenceRow.prefab", root =>
        {
            WorkOutTheme.Skin(root.transform, WorkOutTheme.Surface.Raised).raycastTarget = false;
            var texts = root.GetComponentsInChildren<TMP_Text>(true);
            WorkOutTheme.Text(texts[0], 18); WorkOutTheme.Text(texts[1], 16);
            WorkOutTheme.Place(texts[0].transform, 0, .35f, 1, 1, 12, 0, 12, 3);
            WorkOutTheme.Place(texts[1].transform, 0, 0, 1, .38f, 12, 5, 12, 0);
        });
        foreach (var path in new[] {
            "Assets/Scripts/Puzzles/Prefabs/BudgetMiniGame/Puzzle_BudgetMiniGame.prefab",
            "Assets/Scripts/Puzzles/Prefabs/DrawMiniGame/PuzzleDesignerMiniGame.prefab",
            "Assets/Scripts/Puzzles/Prefabs/MarketingMiniGame/Puzzle_Marketing.prefab" })
            EditPrefab(path, BakePuzzle);

        EditPrefab("Assets/Scripts/UltimateSystem/Prefabs/PlayerAbility.prefab", BakeAbility);
        EditPrefab("Assets/Prefabs/BossCanvas.prefab", root =>
        {
            WorkOutScreenStyle.Boss(root.transform);
            foreach (var panel in root.GetComponentsInChildren<DocumentReferencePanelUI>(true)) panel.BakeLayout();
            foreach (var panel in root.GetComponentsInChildren<BudgetBossPanelUI>(true)) panel.EnsureVisuals();
        });

        // Include the secondary test scene: it also uses the shared runtime controllers.
        foreach (var path in new[] { "Assets/Scenes/SampleScene.unity", "Assets/UltimateTests.unity" })
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            // Preserve the current editor state, including any unsaved work, before migration.
            var backupPath = "artifacts/ui/" + Path.GetFileNameWithoutExtension(path) + "-before-bake.unity";
            if (!File.Exists(backupPath)) EditorSceneManager.SaveScene(scene, backupPath, true);
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var flow in root.GetComponentsInChildren<GameFlowController>(true)) BakeFlow(flow, path.Contains("SampleScene"));
                foreach (var wheel in root.GetComponentsInChildren<DebuffWheelUI>(true)) wheel.EnsureCursorIcon();
                foreach (var ability in root.GetComponentsInChildren<UltimateSystem>(true)) BakeAbility(ability.gameObject);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            if (opened) EditorSceneManager.CloseScene(scene, true);
        }
        AssetDatabase.SaveAssets();
        Validate();
        File.WriteAllText(Report, "PASS\nUI saved to scene and prefabs. Runtime construction removed.\n" + DateTime.Now);
        Debug.Log("WORK_OUT_UI_BAKE_OK");
    }

    private static void BakeSprites()
    {
        const string folder = "Assets/Resources/WorkOutUI";
        Directory.CreateDirectory(folder);
        foreach (WorkOutTheme.Surface surface in Enum.GetValues(typeof(WorkOutTheme.Surface)))
        {
            string path = folder + "/" + surface + ".png";
            if (!File.Exists(path)) File.WriteAllBytes(path, WorkOutTheme.GetSprite(surface).texture.EncodeToPNG());
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = new Vector4(28, 28, 28, 28);
            importer.spritePixelsPerUnit = 100;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.SaveAndReimport();
        }
        WorkOutTheme.ClearSpriteCache();
    }

    private static void EditPrefab(string path, Action<GameObject> action)
    {
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            // The hierarchy marker prevents accidental reapplication over hand-edited assets.
            if (root.GetComponent<WorkOutAuthoredUI>() != null) return;
            action(root);
            root.AddComponent<WorkOutAuthoredUI>();
            PrefabUtility.SaveAsPrefabAsset(root, path, out bool success);
            if (!success) throw new Exception("Failed to save prefab: " + path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void CaptureLog(string message, string stack, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Warning)
            File.AppendAllText("artifacts/ui/authoring-errors.txt", message + "\n" + stack + "\n");
    }

    private static T Get<T>(Object target, string field) where T : Object => (T)new SerializedObject(target).FindProperty(field).objectReferenceValue;
    private static void Set(Object target, string field, Object value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(field).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void BakePuzzle(GameObject prefab)
    {
        foreach (var t in prefab.GetComponentsInChildren<Transform>(true))
            if (t.name == "DrawingCamera") GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
        var activator = prefab.GetComponent<PuzzleActivator>();
        var root = Get<GameObject>(activator, "puzzleRoot");
        var behaviour = Get<MonoBehaviour>(activator, "puzzleBehaviour");
        WorkOutScreenStyle.Puzzle(root, behaviour);
        WorkOutTheme.Place(root.transform, 0, 0, 1, 1);
        NormalizePuzzleCanvas(prefab, root);
        if (behaviour is BudgetMiniGame budget) budget.EnsureMarkers();
        if (behaviour is PaintDrawer paint)
        {
            var buttons = root.GetComponentsInChildren<Button>(true);
            Set(paint, "brushButtonImage", buttons.First(b => b.name.Contains("Brush")).GetComponent<Image>());
            Set(paint, "eraserButtonImage", buttons.First(b => b.name.Contains("Eraser")).GetComponent<Image>());
        }
        // The view is visible in Prefab Mode; PuzzleActivator controls visibility in play mode.
        root.SetActive(true);
    }

    // A screen-space Canvas controls its own RectTransform. Put it on the host,
    // leaving the movable view to inherit the session Canvas when attached in play.
    private static void NormalizePuzzleCanvas(GameObject prefab, GameObject view)
    {
        var canvas = view.GetComponent<Canvas>();
        if (canvas == null || prefab == view) return;
        var raycaster = view.GetComponent<GraphicRaycaster>();
        var scaler = view.GetComponent<CanvasScaler>();
        if (raycaster != null) Object.DestroyImmediate(raycaster);
        if (scaler != null) Object.DestroyImmediate(scaler);
        Object.DestroyImmediate(canvas);
        if (prefab.GetComponent<RectTransform>() == null) prefab.AddComponent<RectTransform>();
        var hostCanvas = prefab.GetComponent<Canvas>();
        if (hostCanvas == null) hostCanvas = prefab.AddComponent<Canvas>();
        hostCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        if (prefab.GetComponent<CanvasScaler>() == null) prefab.AddComponent<CanvasScaler>();
        if (prefab.GetComponent<GraphicRaycaster>() == null) prefab.AddComponent<GraphicRaycaster>();
        WorkOutTheme.ScaleCanvas(prefab);
        WorkOutTheme.Place(view.transform, 0, 0, 1, 1);
    }

    private static void RepairPuzzleCanvases()
    {
        foreach (var path in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Scripts/Puzzles/Prefabs" }).Select(AssetDatabase.GUIDToAssetPath))
        {
            var prefab = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var activator = prefab.GetComponent<PuzzleActivator>();
                if (activator == null) continue;
                NormalizePuzzleCanvas(prefab, Get<GameObject>(activator, "puzzleRoot"));
                PrefabUtility.SaveAsPrefabAsset(prefab, path, out bool success);
                if (!success) throw new Exception("Failed saving " + path);
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
        }
        File.WriteAllText("artifacts/ui/repair-result.txt", "PASS");
    }

    private static void BakeAbility(GameObject root)
    {
        var ability = root.GetComponent<UltimateSystem>();
        if (ability != null) WorkOutScreenStyle.Ability(Get<GameObject>(ability, "abilityCanvas"), Get<Slider>(ability, "ultimateBar"), Get<Button>(ability, "ultimateButton"));
        var score = root.GetComponent<PlayerScore>();
        if (score != null) WorkOutScreenStyle.Score(Get<GameObject>(score, "scoreCanvas"), Get<TMP_Text>(score, "scoreText"));
        foreach (var receiver in root.GetComponentsInChildren<DebuffReceiver>(true)) receiver.EnsureDeliveryConfirmText();
    }

    private static void BakeFlow(GameFlowController flow, bool createPrefabs)
    {
        var lobby = Get<GameObject>(flow, "lobbyCanvas");
        var parent = lobby.GetComponentInParent<Canvas>(true);
        if (parent != null) lobby = parent.gameObject;
        var session = Get<GameObject>(flow, "inGameCanvas");
        var results = Get<GameObject>(flow, "resultsCanvas");
        BakeSceneCanvas(lobby, "LobbyCanvas", () =>
        {
            WorkOutScreenStyle.Lobby(lobby);
            foreach (var controller in lobby.GetComponentsInChildren<LobbyUIController>(true))
            {
                var label = Get<TMP_Text>(controller, "lobbyCodeDisplay");
                var copy = label.GetComponent<LobbyCodeCopyButton>() ?? label.gameObject.AddComponent<LobbyCodeCopyButton>();
                label.raycastTarget = true;
                copy.CreatePopup();
            }
        }, createPrefabs);
        BakeSceneCanvas(session, "SessionCanvas", () =>
        {
            WorkOutScreenStyle.Session(session, Get<TMP_Text>(flow, "timerText"));
            foreach (var wheel in session.GetComponentsInChildren<DebuffWheelUI>(true)) wheel.EnsureCursorIcon();
        }, createPrefabs);
        BakeSceneCanvas(results, "ResultsCanvas", () => WorkOutScreenStyle.Results(results), createPrefabs);
        var boss = Get<GameObject>(flow, "bossCanvas");
        if (boss == null)
        {
            boss = (GameObject)PrefabUtility.InstantiatePrefab(Get<GameObject>(flow, "bossCanvasPrefab"), flow.gameObject.scene);
            boss.name = "BossCanvas";
            boss.SetActive(false);
            Set(flow, "bossCanvas", boss);
        }
    }

    private static void BakeSceneCanvas(GameObject root, string name, Action action, bool createPrefab)
    {
        if (root.GetComponent<WorkOutAuthoredUI>() != null) return;
        action();
        root.AddComponent<WorkOutAuthoredUI>();
        if (createPrefab) PrefabUtility.SaveAsPrefabAssetAndConnect(root, UIPrefabs + "/" + name + ".prefab", InteractionMode.AutomatedAction);
    }

    [MenuItem("Tools/Work Out/Validate editable UI")]
    public static void Validate()
    {
        foreach (var path in AssetDatabase.FindAssets("t:Prefab", new[] { UIPrefabs, "Assets/Prefabs", "Assets/Scripts/Puzzles/Prefabs", "Assets/Scripts/UltimateSystem/Prefabs" }).Select(AssetDatabase.GUIDToAssetPath).Distinct())
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (root.GetComponent<WorkOutAuthoredUI>() == null) continue;
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) != 0) throw new Exception("Missing script: " + path + " / " + t.name);
            foreach (var image in root.GetComponentsInChildren<Image>(true))
                if (image.sprite != null && !AssetDatabase.Contains(image.sprite)) throw new Exception("Unsaved sprite: " + path + " / " + image.name);
            var desktop = root.GetComponent<WorkOutDesktop>();
            if (desktop != null && Get<TMP_Text>(desktop, "clockLabel") == null) throw new Exception("Missing desktop binding: " + path);
        }
    }
}
