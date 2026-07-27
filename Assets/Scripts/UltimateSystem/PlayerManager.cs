using System.Collections.Generic;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance;
    
    // Теперь список заполняется кодом, а не руками в инспекторе
    public List<Player> AllPlayers = new();

    void Awake() => Instance = this;

    public void RegisterPlayer(Player player)
    {
        if (!AllPlayers.Contains(player))
            AllPlayers.Add(player);
    }

    public void UnregisterPlayer(Player player)
    {
        AllPlayers.Remove(player);
    }

    public Player GetPlayer(int id) => AllPlayers.Find(p => p.PlayerId == id);
    public Player GetPlayerByRole(PlayerRole role) => AllPlayers.Find(p => p.Role == role);
    public PlayerState GetState(int id) => GetPlayer(id)?.State;
    public List<Player> GetOtherPlayers(int excludeId) => AllPlayers.FindAll(p => p.PlayerId != excludeId);
}