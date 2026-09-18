using System;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// <summary>Batch-mode visual verification of the actual scene and mini-game prefabs.</summary>
public static class WorkOutStyleValidation
{
    private static readonly string Output = Path.GetFullPath("artifacts/ui");

    public static void Capture()
    {
        try
        {
            Directory.CreateDirectory(Output);
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            var lobby = GameObject.Find("Canvas");
            Render(lobby, "lobby");
            lobby.SetActive(false);
            var results = Resources.FindObjectsOfTypeAll<ResultsScreenController>().First(x => x.gameObject.scene.IsValid()).gameObject;
            results.SetActive(false);
            var session = Resources.FindObjectsOfTypeAll<PuzzleSlotCanvas>().First(x => x.gameObject.scene.IsValid()).gameObject;
            session.SetActive(true);
            // The original timer is replaced only for this editor-only capture canvas.
            var oldTimer = session.transform.Find("Text (TMP)");
            if (oldTimer != null) oldTimer.gameObject.SetActive(false);
            foreach (string name in new[] { "WheelPanel", "TargetSelectionPanel" })
            { var overlay = session.transform.Find(name); if (overlay != null) overlay.gameObject.SetActive(false); }
            var slot = session.transform.Find("SlotRoot");

            CapturePuzzle("Assets/Scripts/Puzzles/Prefabs/BudgetMiniGame/Puzzle_BudgetMiniGame.prefab", session, slot, "budget");
            CapturePuzzle("Assets/Scripts/Puzzles/Prefabs/MarketingMiniGame/Puzzle_Marketing.prefab", session, slot, "documents");
            CapturePuzzle("Assets/Scripts/Puzzles/Prefabs/DrawMiniGame/PuzzleDesignerMiniGame.prefab", session, slot, "drawing");
            session.SetActive(false);

            var boss = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/BossCanvas.prefab"));
            Invoke(boss.GetComponentInChildren<DocumentReferencePanelUI>(true), "Start");
            Invoke(boss.GetComponentInChildren<BudgetBossPanelUI>(true), "Awake");
            Render(boss, "boss");
            UnityEngine.Object.DestroyImmediate(boss);
            Debug.Log("WORK_OUT_STYLE_VALIDATION_OK: five actual Unity screens captured to " + Output);
            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    private static void CapturePuzzle(string path, GameObject session, Transform slot, string name)
    {
        var instance = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
        var activator = instance.GetComponent<PuzzleActivator>();
        var serialized = new SerializedObject(activator);
        var root = (GameObject)serialized.FindProperty("puzzleRoot").objectReferenceValue;
        var behaviour = (MonoBehaviour)serialized.FindProperty("puzzleBehaviour").objectReferenceValue;
        root.transform.SetParent(slot, false);
        WorkOutTheme.Place(root.transform, 0, 0, 1, 1);
        root.SetActive(true);
        if (behaviour is BudgetMiniGame budget)
        {
            Invoke(budget, "Start");
            budget.sliders[0].slider.value = budget.sliders[0].slider.maxValue * 0.47f;
            budget.sliders[1].slider.value = budget.sliders[1].slider.maxValue * 0.65f;
            if (budget.totalText == null || budget.sliders.Any(s => s.valueText == null)) throw new Exception("Budget labels are not bound.");
        }
        else if (behaviour is DocumentApprovalGame document)
        {
            Invoke(document, "Awake");
            document.Begin();
            if (!root.GetComponentsInChildren<Button>().All(b => b.interactable)) throw new Exception("Document decision controls disabled.");
        }
        // PaintDrawer.Start uses the final canvas dimensions during play; an empty RawImage is its blank canvas here.
        Render(session, name);
        Render(session, name + "_compact", 1280, 720);
        UnityEngine.Object.DestroyImmediate(root);
        if (instance != null) UnityEngine.Object.DestroyImmediate(instance);
    }

    private static void Invoke(MonoBehaviour behaviour, string method)
    {
        if (behaviour == null) throw new Exception("Missing component for " + method);
        behaviour.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(behaviour, null);
    }

    public static void Render(GameObject root, string name, int width = 1600, int height = 900)
    {
        Directory.CreateDirectory(Output);
        var go = new GameObject("StyleCaptureCamera", typeof(Camera));
        var camera = go.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = WorkOutTheme.Hex(0x111b30);
        camera.orthographic = true;
        camera.orthographicSize = 450;
        camera.transform.position = new Vector3(0, 0, -10);
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 100;
        var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
        camera.targetTexture = target;
        var otherCanvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude)
            .Where(c => !Application.isPlaying && c.transform != root.transform && !c.transform.IsChildOf(root.transform)).ToArray();
        foreach (var canvas in otherCanvases)
        {
            canvas.gameObject.SetActive(false);
        }
        var visibleCanvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude).Where(c => c.isRootCanvas).ToArray();
        var saved = visibleCanvases.Select(c => (canvas: c, mode: c.renderMode, camera: c.worldCamera, scale: c.scaleFactor,
            distance: c.planeDistance, scaler: c.GetComponent<CanvasScaler>(), enabled: c.GetComponent<CanvasScaler>() != null && c.GetComponent<CanvasScaler>().enabled)).ToArray();
        foreach (var entry in saved)
        {
            if (entry.scaler != null) entry.scaler.enabled = false;
            entry.canvas.renderMode = RenderMode.ScreenSpaceCamera;
            entry.canvas.worldCamera = camera;
            entry.canvas.planeDistance = 1;
            entry.canvas.scaleFactor = width / 1600f;
        }
        Canvas.ForceUpdateCanvases();
        foreach (var text in root.GetComponentsInChildren<TMP_Text>()) text.ForceMeshUpdate();
        Canvas.ForceUpdateCanvases();
        camera.Render();
        var previous = RenderTexture.active;
        RenderTexture.active = target;
        var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
        texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        texture.Apply();
        File.WriteAllBytes(Path.Combine(Output, name + ".png"), texture.EncodeToPNG());
        RenderTexture.active = previous;
        camera.targetTexture = null;
        UnityEngine.Object.DestroyImmediate(texture);
        UnityEngine.Object.DestroyImmediate(target);
        UnityEngine.Object.DestroyImmediate(go);
        foreach (var entry in saved)
        {
            entry.canvas.renderMode = entry.mode;
            entry.canvas.worldCamera = entry.camera;
            entry.canvas.scaleFactor = entry.scale;
            entry.canvas.planeDistance = entry.distance;
            if (entry.scaler != null) entry.scaler.enabled = entry.enabled;
        }
        foreach (var canvas in otherCanvases) if (canvas != null) canvas.gameObject.SetActive(true);
    }
}
