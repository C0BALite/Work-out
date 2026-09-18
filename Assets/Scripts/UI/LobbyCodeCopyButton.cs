using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(TMP_Text))]
public class LobbyCodeCopyButton : MonoBehaviour, IPointerClickHandler
{
    private const float PopupVisibleDuration = 1.2f;
    private const float PopupFadeDuration = 0.25f;

    private TMP_Text codeLabel;
    private string lobbyCode;
    [SerializeField] private CanvasGroup popup;
    private Coroutine hidePopupCoroutine;

    private void Awake()
    {
        EnsureInitialized();
    }

    public void SetCode(string code)
    {
        EnsureInitialized();
        lobbyCode = code;
        codeLabel.text = $"Lobby code: {code}";
    }

    private void EnsureInitialized()
    {
        if (codeLabel != null)
            return;

        codeLabel = GetComponent<TMP_Text>();

    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (string.IsNullOrEmpty(lobbyCode))
            return;

        GUIUtility.systemCopyBuffer = lobbyCode;
        ShowPopup();
    }

    #if UNITY_EDITOR
    public void CreatePopup()
    {
        if (popup != null) return;
        codeLabel = GetComponent<TMP_Text>();
        var popupObject = new GameObject("CodeCopiedPopup", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
        popupObject.transform.SetParent(transform, false);

        var popupRect = (RectTransform)popupObject.transform;
        popupRect.anchorMin = new Vector2(0.5f, 0f);
        popupRect.anchorMax = new Vector2(0.5f, 0f);
        popupRect.pivot = new Vector2(0.5f, 1f);
        popupRect.anchoredPosition = new Vector2(0f, -12f);
        popupRect.sizeDelta = new Vector2(280f, 52f);

        var background = popupObject.GetComponent<Image>();
        background.color = new Color(0.08f, 0.08f, 0.08f, 0.92f);
        background.raycastTarget = false;

        popup = popupObject.GetComponent<CanvasGroup>();
        popup.interactable = false;
        popup.blocksRaycasts = false;

        var textObject = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(popupObject.transform, false);

        var textRect = (RectTransform)textObject.transform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(12f, 6f);
        textRect.offsetMax = new Vector2(-12f, -6f);

        var popupText = textObject.GetComponent<TextMeshProUGUI>();
        popupText.text = "Code copied";
        popupText.font = codeLabel.font;
        popupText.fontSize = 26f;
        popupText.alignment = TextAlignmentOptions.Center;
        popupText.color = Color.white;
        popupText.raycastTarget = false;

        popupObject.SetActive(false);
    }

    #endif

    private void ShowPopup()
    {
        if (hidePopupCoroutine != null)
            StopCoroutine(hidePopupCoroutine);

        popup.gameObject.SetActive(true);
        popup.alpha = 1f;
        hidePopupCoroutine = StartCoroutine(HidePopup());
    }

    private IEnumerator HidePopup()
    {
        yield return new WaitForSecondsRealtime(PopupVisibleDuration);

        float elapsed = 0f;
        while (elapsed < PopupFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            popup.alpha = 1f - Mathf.Clamp01(elapsed / PopupFadeDuration);
            yield return null;
        }

        popup.gameObject.SetActive(false);
        hidePopupCoroutine = null;
    }
}
