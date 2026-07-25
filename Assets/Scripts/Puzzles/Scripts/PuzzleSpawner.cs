using Unity.Netcode;
using UnityEngine;

public class PuzzleSpawner : NetworkBehaviour
{
    [SerializeField] private GameObject documentApprovalPuzzlePrefab; // пока одна головоломка

    private bool spawned;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        GameSessionState.Instance.Phase.OnValueChanged += OnPhaseChanged;
    }

    void OnPhaseChanged(SessionPhase oldPhase, SessionPhase newPhase)
    {
        Debug.Log($"[PuzzleSpawner] Phase changed to {newPhase}, IsServer={IsServer}, spawned={spawned}");

        if (newPhase == SessionPhase.InGame && !spawned)
        {
            Debug.Log("[PuzzleSpawner] Spawning puzzle prefab: " + documentApprovalPuzzlePrefab);
            var obj = Instantiate(documentApprovalPuzzlePrefab);
            obj.GetComponent<NetworkObject>().Spawn();
            spawned = true;
        }
        else if (newPhase == SessionPhase.Lobby)
        {
            spawned = false;
        }
    }

    void SpawnPuzzles()
    {
        var obj = Instantiate(documentApprovalPuzzlePrefab);
        obj.GetComponent<NetworkObject>().Spawn();
    }
}