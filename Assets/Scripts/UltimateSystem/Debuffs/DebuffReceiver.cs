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

    // публичные методы вызываются на СЕРВЕРЕ (из ScreenBlurEffect/SlowEffect)
    public void ApplyBlur() => ApplyBlurClientRpc();
    public void RemoveBlur() => RemoveBlurClientRpc();
    public void ApplySlow() => ApplySlowClientRpc();
    public void RemoveSlow() => RemoveSlowClientRpc();

    [ClientRpc]
    void ApplyBlurClientRpc()
    {
        if (!IsOwner) return;
        if (blurPanel != null) blurPanel.SetActive(true);
    }

    [ClientRpc]
    void RemoveBlurClientRpc()
    {
        if (!IsOwner) return;
        if (blurPanel != null) blurPanel.SetActive(false);
    }

    [ClientRpc]
    void ApplySlowClientRpc()
    {
        if (slowIcon != null) slowIcon.SetActive(true);
    }

    [ClientRpc]
    void RemoveSlowClientRpc()
    {
        if (slowIcon != null) slowIcon.SetActive(false);
    }
}