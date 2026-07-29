using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PuzzleSpawner : NetworkBehaviour
{
    [SerializeField] private List<GameObject> puzzlePrefabs; // по одному Prefab-у на роль

    private List<NetworkObject> spawnedPuzzles = new List<NetworkObject>();
    private bool spawned;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        GameSessionState.Instance.Phase.OnValueChanged += OnPhaseChanged;
    }

    void OnPhaseChanged(SessionPhase oldPhase, SessionPhase newPhase)
    {
        if (newPhase == SessionPhase.InGame && !spawned)
        {
            foreach (var prefab in puzzlePrefabs)
            {
                var obj = Instantiate(prefab);
                var netObj = obj.GetComponent<NetworkObject>();
                netObj.Spawn();
                spawnedPuzzles.Add(netObj);
            }
            spawned = true;
        }
        else if (newPhase == SessionPhase.Lobby)
        {
            foreach (var netObj in spawnedPuzzles)
            {
                if (netObj != null && netObj.IsSpawned)
                    netObj.Despawn(true);
            }
            spawnedPuzzles.Clear();
            spawned = false;
        }
    }
}