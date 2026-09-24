using System;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Offline host smoke test: real scene, role transitions, puzzle callbacks and rendered UI.</summary>
[InitializeOnLoad]
public static class WorkOutPlayValidation
{
    private const string Key = "WorkOut.PlayValidation";
    private static int step;
    private static double next, deadline;
    private static int errors;
    private static float budgetBefore;
    private static DocumentApprovalGame document;
    private static ColoringMiniGame coloring;
    private static System.Threading.Tasks.Task services;
    private static string scorePath;
    private static byte[] savedScores;
    private static bool finalSuccess;
    private static string finalMessage;
    private static double shutdownAt;

    static WorkOutPlayValidation()
    {
        if (SessionState.GetBool(Key, false))
        {
            Arm();
        }
    }

    private static void Arm()
    {
        deadline = EditorApplication.timeSinceStartup + 90;
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        Application.logMessageReceived -= OnLog;
        Application.logMessageReceived += OnLog;
    }

    public static void Run()
    {
        step = errors = 0;
        services = null;
        next = 0;
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        var authored = Resources.FindObjectsOfTypeAll<PuzzleSlotCanvas>().First(x => x.gameObject.scene.IsValid());
        var sidebar = (RectTransform)authored.transform.Find("WorkOutDesktop/TeamSidebar");
        sidebar.anchoredPosition += new Vector2(13, 7);
        SessionState.SetVector3("WorkOut.TestSidebarPosition", sidebar.anchoredPosition);
        // Use a local transport: this test never creates a cloud lobby or signs in.
        var bootstrap = UnityEngine.Object.FindFirstObjectByType<UGSBootstrap>();
        if (bootstrap != null) bootstrap.gameObject.SetActive(false);
        SessionState.SetBool(Key, true);
        Arm(); // Also supports projects with domain reload disabled.
        EditorApplication.EnterPlaymode();
    }

    private static void OnLog(string message, string trace, LogType type)
    {
        if (type == LogType.Exception || type == LogType.Error) errors++;
    }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying) return;
        if (EditorApplication.timeSinceStartup > deadline) { Finish(false, "Timed out at step " + step); return; }
        if (EditorApplication.timeSinceStartup < next) return;
        next = EditorApplication.timeSinceStartup + 0.8;
        try
        {
            switch (step++)
            {
                case 0:
                    if (services == null) services = UnityServices.InitializeAsync();
                    if (!services.IsCompleted) { step--; return; }
                    if (services.IsFaulted) throw services.Exception;
                    scorePath = Path.Combine(Application.persistentDataPath, "player_scores.json");
                    savedScores = File.Exists(scorePath) ? File.ReadAllBytes(scorePath) : null;
                    var net = NetworkManager.Singleton;
                    net.GetComponent<UnityTransport>().SetConnectionData("127.0.0.1", 17983);
                    Require(net.StartHost(), "Local host starts");
                    var state = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/GameState.prefab"));
                    state.GetComponent<NetworkObject>().Spawn();
                    RoleAssignmentManager.Instance.AssignBossToHost(net.LocalClientId);
                    GameObject.Find("Canvas/CreateJoinPanel").SetActive(false);
                    var lobby = Resources.FindObjectsOfTypeAll<LobbyScreenController>().First(x => x.gameObject.scene.IsValid());
                    lobby.gameObject.SetActive(true);
                    break;
                case 1:
                    var lobbyCanvas = GameObject.Find("Canvas");
                    Require(lobbyCanvas.transform.Find("LobbyPanel/DesktopWindow") != null, "Lobby style is applied through its parent canvas");
                    WorkOutStyleValidation.Render(lobbyCanvas, "runtime_lobby");
                    GameSessionState.Instance.SetPhase(SessionPhase.InGame);
                    break;
                case 2:
                    var sidebar = (RectTransform)PuzzleSlotCanvas.Instance.transform.Find("WorkOutDesktop/TeamSidebar");
                    Require(Vector2.Distance(sidebar.anchoredPosition, SessionState.GetVector3("WorkOut.TestSidebarPosition", Vector3.zero)) < .01f, "Hand-edited sidebar position survives startup");
                    Require(PuzzleSlotCanvas.Instance.GetComponentsInChildren<Transform>(true).Count(t => t.name == "WorkOutDesktop") == 1, "Session UI is not duplicated at startup");
                    var boss = UnityEngine.Object.FindFirstObjectByType<BossDashboardLayout>();
                    Require(boss != null, "Boss desktop is active");
                    WorkOutStyleValidation.Render(boss.gameObject, "runtime_boss");
                    GameSessionState.Instance.SetPhase(SessionPhase.Lobby);
                    break;
                case 3: StartRole(GameRole.Programmer); break;
                case 4:
                    var budget = UnityEngine.Object.FindFirstObjectByType<BudgetMiniGame>();
                    Require(budget != null && budget.totalText != null, "Budget desktop and labels exist");
                    budgetBefore = budget.GetTotalBudget();
                    budget.sliders[0].slider.value = budget.sliders[0].slider.maxValue * 0.45f;
                    budget.sliders[1].slider.value = budget.sliders[1].slider.maxValue * 0.6f;
                    Require(Mathf.Abs(budget.GetTotalBudget() - budgetBefore) > 1, "Slider changes the game budget");
                    Require(budget.sliders.All(s => !string.IsNullOrWhiteSpace(s.valueText.text)), "All four values update");
                    WorkOutStyleValidation.Render(PuzzleSlotCanvas.Instance.gameObject, "runtime_budget");
                    GameSessionState.Instance.SetPhase(SessionPhase.Lobby);
                    break;
                case 5: StartRole(GameRole.Typographer); break;
                case 6:
                    document = UnityEngine.Object.FindFirstObjectByType<DocumentApprovalGame>();
                    Require(document != null, "Document game activates");
                    var documentView = ((GameObject)Field(document.GetComponent<PuzzleActivator>(), "puzzleRoot")).GetComponent<RectTransform>();
                    var slotRect = (RectTransform)PuzzleSlotCanvas.Instance.SlotRoot;
                    Canvas.ForceUpdateCanvases();
                    WorkOutStyleValidation.Render(PuzzleSlotCanvas.Instance.gameObject, "runtime_documents");
                    Require(Mathf.Abs(documentView.rect.width - slotRect.rect.width) < 1 && Mathf.Abs(documentView.rect.height - slotRect.rect.height) < 1, $"Document view fits the authored puzzle slot: view={documentView.rect.size}, slot={slotRect.rect.size}, anchors={documentView.anchorMin}/{documentView.anchorMax}, parent={documentView.parent.name}");
                    bool approve = (bool)Field(document, "shouldApproveCurrent");
                    WorkOutStyleValidation.Render(PuzzleSlotCanvas.Instance.gameObject, "runtime_documents");
                    ((Button)Field(document, approve ? "approveButton" : "rejectButton")).onClick.Invoke();
                    next = EditorApplication.timeSinceStartup + 2.6;
                    break;
                case 7:
                    Require((int)Field(document, "correctCount") == 1, "Decision button evaluates the actual document");
                    GameSessionState.Instance.SetPhase(SessionPhase.Lobby);
                    break;
                case 8: StartRole(GameRole.Artist); break;
                case 9:
                    coloring = UnityEngine.Object.FindFirstObjectByType<ColoringMiniGame>();
                    Require(coloring != null, "Coloring game activates");
                    var sync = ColoringBossSync.Instance;
                    Require(sync != null, "Coloring sync is spawned");
                    Require(sync.TryGetPicture(out var picture) && picture.RegionCount > 3, "Server picked a picture with fill regions");
                    Canvas.ForceUpdateCanvases();
                    var canvasImage = coloring.GetComponent<RawImage>();
                    Require(canvasImage.texture is Texture2D texture && texture.width > 100 && texture.height > 100, "Coloring texture has usable dimensions");
                    var swatches = (Image[])Field(coloring, "swatches");
                    for (int region = 0; region < picture.RegionCount; region++)
                    {
                        swatches[sync.TargetColors[region]].GetComponent<Button>().onClick.Invoke();
                        coloring.OnPointerClick(new PointerEventData(EventSystem.current) { position = ScreenPointOf(canvasImage, picture, region) });
                    }
                    Require(Enumerable.Range(0, picture.RegionCount).All(r => sync.Fills[r] == sync.TargetColors[r]), "Every click fills its region on the server");
                    WorkOutStyleValidation.Render(PuzzleSlotCanvas.Instance.gameObject, "runtime_coloring");
                    ((Button)Field(coloring, "submitButton")).onClick.Invoke();
                    Require(coloring.IsCompleted && coloring.GetLocalScore() > 0.99f, "Submit completes the coloring with a full match");
                    break;
                case 10:
                    Require(ColoringBossSync.Instance.Submitted.Value && ColoringBossSync.Instance.Accuracy.Value > 0.99f, "Server confirms the color match");
                    WorkOutStyleValidation.Render(PuzzleSlotCanvas.Instance.gameObject, "runtime_coloring_result");
                    GameSessionState.Instance.SetPhase(SessionPhase.Results);
                    break;
                case 11:
                    var results = UnityEngine.Object.FindFirstObjectByType<ResultsScreenController>();
                    Require(results != null, "Results screen activates");
                    WorkOutStyleValidation.Render(results.gameObject, "runtime_results");
                    Require(errors == 0, "No runtime errors (count=" + errors + ")");
                    Finish(true, "Local host, boss, budget sliders, document decision, coloring fills, color match and results passed.");
                    break;
            }
        }
        catch (Exception e) { Finish(false, e.ToString()); }
    }

    private static void StartRole(GameRole role)
    {
        RoleAssignmentManager.Instance.Assignments.Clear();
        RoleAssignmentManager.Instance.Assignments.Add(new PlayerRoleData { ClientId = NetworkManager.Singleton.LocalClientId, Role = role });
        GameSessionState.Instance.SetPhase(SessionPhase.InGame);
    }
    // Picks a texel well inside the region so the screen round trip cannot land on a neighbouring area.
    private static Vector2 ScreenPointOf(RawImage image, ColoringPicture picture, int region)
    {
        Vector2 texel = new Vector2(1f / picture.Width, 1f / picture.Height);
        Vector2? inner = null, any = null;
        for (int y = 0; y < picture.Height && inner == null; y += 2)
        for (int x = 0; x < picture.Width && inner == null; x += 2)
        {
            var uv = new Vector2((x + 0.5f) * texel.x, (y + 0.5f) * texel.y);
            if (picture.RegionAt(uv) != region) continue;
            any ??= uv;
            if (new[] { Vector2.left, Vector2.right, Vector2.up, Vector2.down }.All(d => picture.RegionAt(uv + Vector2.Scale(d * 3f, texel)) == region))
                inner = uv;
        }
        Vector2 target = inner ?? any ?? throw new Exception("Region has no pixels: " + region);
        Rect uvRect = image.uvRect, rect = image.rectTransform.rect;
        var normalized = new Vector2((target.x - uvRect.x) / uvRect.width, (target.y - uvRect.y) / uvRect.height);
        Vector3 world = image.rectTransform.TransformPoint(rect.min + Vector2.Scale(normalized, rect.size));
        return RectTransformUtility.WorldToScreenPoint(null, world);
    }
    private static object Field(object obj, string name) => obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(obj);
    private static void Require(bool condition, string description)
    { if (!condition) throw new Exception(description); Debug.Log("STYLE_TEST_PASS: " + description); }
    private static void Finish(bool success, string message)
    {
        SessionState.SetBool(Key, false);
        EditorApplication.update -= Tick;
        finalSuccess = success;
        finalMessage = message;
        shutdownAt = EditorApplication.timeSinceStartup + 1;
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening) NetworkManager.Singleton.Shutdown();
        EditorApplication.update += CompleteShutdown;
    }

    private static void CompleteShutdown()
    {
        if (EditorApplication.timeSinceStartup < shutdownAt) return;
        EditorApplication.update -= CompleteShutdown;
        Application.logMessageReceived -= OnLog;
        if (scorePath != null)
        {
            if (savedScores != null) File.WriteAllBytes(scorePath, savedScores);
            else if (File.Exists(scorePath)) File.Delete(scorePath);
        }
        Directory.CreateDirectory("artifacts/ui");
        if (errors > 0) { finalSuccess = false; finalMessage += " Runtime error count: " + errors; }
        File.WriteAllText("artifacts/ui/play-validation.txt", (finalSuccess ? "PASS\n" : "FAIL\n") + finalMessage);
        Debug.Log("WORK_OUT_PLAY_VALIDATION_" + (finalSuccess ? "OK: " : "FAILED: ") + finalMessage);
        if (Application.isBatchMode) EditorApplication.Exit(finalSuccess ? 0 : 1);
        else
        {
            EditorApplication.playModeStateChanged += RestoreEditor;
            EditorApplication.ExitPlaymode();
        }
    }

    private static void RestoreEditor(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredEditMode) return;
        EditorApplication.playModeStateChanged -= RestoreEditor;
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
    }
}
