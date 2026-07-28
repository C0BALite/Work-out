using Unity.Netcode;
using UnityEngine;

public class DebuffReceiver : NetworkBehaviour
{
    [Header("Visual Effects")]
    [SerializeField] private GameObject blurPanel;
    [SerializeField] private GameObject slowIcon;

    public override void OnNetworkSpawn()
    {
        if (blurPanel != null) blurPanel.SetActive(false);
        if (slowIcon != null) slowIcon.SetActive(false);
    }

    public void ApplyBlur()
    {
        if (!IsOwner) return;
        if (blurPanel != null) blurPanel.SetActive(true);
    }

    public void RemoveBlur()
    {
        if (!IsOwner) return;
        if (blurPanel != null) blurPanel.SetActive(false);
    }

    public void ApplySlow()
    {
        if (slowIcon != null) slowIcon.SetActive(true);
    }

    public void RemoveSlow()
    {
        if (slowIcon != null) slowIcon.SetActive(false);
    }
}