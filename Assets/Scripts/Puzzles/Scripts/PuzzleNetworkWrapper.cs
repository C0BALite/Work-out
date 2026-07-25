using Unity.Netcode;
using UnityEngine;

public class PuzzleNetworkWrapper : NetworkBehaviour
{
    [SerializeField] private GameRole assignedRole;
    [SerializeField] private MonoBehaviour puzzleBehaviour; // ссылка на DrawingGame/BudgetMinigame и т.д., должен реализовывать IPuzzle

    private IPuzzle puzzle;

    // Прогресс, который видно ТОЛЬКО серверу и боссу — не всем игрокам
    private NetworkVariable<float> Progress = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        puzzle = puzzleBehaviour as IPuzzle;

        bool isMine = RoleAssignmentManager.Instance.GetMyRole() == assignedRole;
        gameObject.SetActive(isMine); // включаем UI только у нужного игрока

        if (isMine)
            puzzle.Begin();
    }

    void Update()
    {
        if (puzzle == null || !gameObject.activeSelf) return;
        // клиент периодически репортит прогресс серверу (не сырые данные, а число 0..1)
        // упрощённо для демки — раз в кадр, для реального проекта лучше раз в 0.2-0.5 сек
        ReportProgressServerRpc(puzzle.GetLocalScore());
    }

    [ServerRpc]
    void ReportProgressServerRpc(float score)
    {
        Progress.Value = score;
    }
}