using Unity.Netcode;
using UnityEngine;
using TMPro;
using System.Collections;

public class DebuffReceiver : NetworkBehaviour
{
    [Header("Visual Effects")]
    [SerializeField] private GameObject blurPanel;
    [SerializeField] private GameObject slowIcon;

    private TMP_Text deliveryConfirmText;
    private Coroutine deliveryConfirmHideRoutine;

    public override void OnNetworkSpawn()
    {
        if (blurPanel != null) blurPanel.SetActive(false);
        if (slowIcon != null) slowIcon.SetActive(false);
    }

    public void ShowDeliveryConfirm(string debuffName)
    {
        if (!IsOwner) return;

        EnsureDeliveryConfirmText();
        deliveryConfirmText.text = $"Получен дебаф: {debuffName}";
        deliveryConfirmText.gameObject.SetActive(true);

        if (deliveryConfirmHideRoutine != null)
            StopCoroutine(deliveryConfirmHideRoutine);
        deliveryConfirmHideRoutine = StartCoroutine(HideDeliveryConfirmAfterDelay(3f));
    }

    void EnsureDeliveryConfirmText()
    {
        if (deliveryConfirmText != null) return;

        Transform parent = blurPanel != null ? blurPanel.transform.parent : transform;
        var go = new GameObject("DebuffDeliveryConfirm", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -40f);
        rect.sizeDelta = new Vector2(600f, 80f);

        deliveryConfirmText = go.GetComponent<TextMeshProUGUI>();
        deliveryConfirmText.alignment = TextAlignmentOptions.Center;
        deliveryConfirmText.fontSize = 36f;
        deliveryConfirmText.color = Color.white;
        deliveryConfirmText.raycastTarget = false;
        go.SetActive(false);
    }

    IEnumerator HideDeliveryConfirmAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (deliveryConfirmText != null)
            deliveryConfirmText.gameObject.SetActive(false);
        deliveryConfirmHideRoutine = null;
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