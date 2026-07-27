using UnityEngine;
using UnityEngine.EventSystems;

public class HammerPortrait : MonoBehaviour, IPointerClickHandler
{
    private Player targetPlayer;
    private System.Action<Player> onClick;

    public void Setup(Player player, System.Action<Player> callback)
    {
        targetPlayer = player;
        onClick = callback;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        onClick?.Invoke(targetPlayer);
    }
}