using Unity.Netcode;
using UnityEngine;

public class PuzzleActivator : NetworkBehaviour
{
    [SerializeField] private GameRole requiredRole;
    [SerializeField] private GameObject puzzleRoot;   // Canvas с самой головоломкой
    [SerializeField] private MonoBehaviour puzzleBehaviour; // DocumentApprovalGame и т.п.

    private IPuzzle puzzle;
    private bool activated;

    void Awake()
    {
        puzzle = puzzleBehaviour as IPuzzle;
        puzzleRoot.SetActive(false);
    }

    public override void OnNetworkSpawn()
    {
        bool isMyRole = RoleAssignmentManager.Instance.GetMyRole() == requiredRole;

        if (isMyRole)
        {
            // локально переносим в слот-канвас — это чисто визуальная операция,
            // не синхронизируется через NGO, каждый клиент делает это независимо
            puzzleRoot.transform.SetParent(PuzzleSlotCanvas.Instance.SlotRoot, false);

            var rt = puzzleRoot.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            puzzleRoot.SetActive(true);
            puzzle.Begin();
            activated = true;
        }
        else
        {
            // не наша роль — этот экземпляр на этом клиенте просто ничего не показывает
            puzzleRoot.SetActive(false);
        }
    }
}