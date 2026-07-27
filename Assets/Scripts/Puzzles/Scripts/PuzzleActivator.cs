using Unity.Netcode;
using UnityEngine;

public class PuzzleActivator : NetworkBehaviour
{
    [SerializeField] private GameRole requiredRole;
    [SerializeField] private GameObject puzzleRoot;
    [SerializeField] private MonoBehaviour puzzleBehaviour;

    private IPuzzle puzzle;
    private bool isMine;
    private bool reported; // новое

    void Awake()
    {
        puzzle = puzzleBehaviour as IPuzzle;
        puzzleRoot.SetActive(false);
    }

    public override void OnNetworkSpawn()
    {
        isMine = RoleAssignmentManager.Instance.GetMyRole() == requiredRole;

        if (isMine)
        {
            puzzleRoot.transform.SetParent(PuzzleSlotCanvas.Instance.SlotRoot, false);

            var rt = puzzleRoot.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            puzzleRoot.SetActive(true);
            puzzle.Begin();
        }
        else
        {
            puzzleRoot.SetActive(false);
        }
    }

    void Update() // новое
    {
        if (!isMine || reported || GameSessionState.Instance == null) return;

        bool roundEnded = GameSessionState.Instance.Phase.Value == SessionPhase.Results;

        if (puzzle.IsCompleted || roundEnded)
        {
            if (roundEnded && !puzzle.IsCompleted) puzzle.ForceEnd();

            float timeLeftRatio = Mathf.Clamp01(
                GameSessionState.Instance.TimeRemaining.Value / GameSessionState.Instance.RoundDuration);

            ReportResultServerRpc(requiredRole, puzzle.GetLocalScore(), timeLeftRatio);
            reported = true;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void ReportResultServerRpc(GameRole role, float score, float timeLeftRatio, ServerRpcParams rpcParams = default)
    {
        GameSessionState.Instance.RegisterResult(role, score, timeLeftRatio, rpcParams.Receive.SenderClientId);
    }
}