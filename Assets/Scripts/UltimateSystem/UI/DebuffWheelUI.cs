using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

public class DebuffWheelUI : MonoBehaviour
{
    public static DebuffWheelUI Instance { get; private set; }

    [SerializeField] private Transform wheelTransform;
    [SerializeField] private float spinDuration = 3f;
    [SerializeField] private AnimationCurve spinCurve;

    [SerializeField] private GameObject wheelPanel;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private TargetSelectionUI targetSelectionUI;

    private UltimateSystem currentUser;
    private DebuffData selectedDebuff;
    private bool isSpinning;

    [SerializeField] private Image cursorDebuffIcon;
    [SerializeField] private TMP_Text cursorDebuffLabel;
    [SerializeField] private RectTransform cursorDebuffIconRect;
    private bool cursorIconAttached;

    void Awake()
    {
        Instance = this;
        if (wheelPanel != null) wheelPanel.SetActive(false);

    }

    #if UNITY_EDITOR
    public void EnsureCursorIcon()
    {
        if (cursorDebuffIconRect != null) return;

        var go = new GameObject("DebuffCursorIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(transform, false);

        cursorDebuffIconRect = go.GetComponent<RectTransform>();
        cursorDebuffIconRect.anchorMin = new Vector2(0.5f, 0.5f);
        cursorDebuffIconRect.anchorMax = new Vector2(0.5f, 0.5f);
        cursorDebuffIconRect.pivot = new Vector2(0.5f, 0.5f);
        cursorDebuffIconRect.sizeDelta = new Vector2(180f, 48f);
        cursorDebuffIconRect.anchoredPosition = Vector2.zero;

        cursorDebuffIcon = go.GetComponent<Image>();
        cursorDebuffIcon.raycastTarget = false;
        cursorDebuffIcon.preserveAspect = true;

        var labelGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelGo.transform.SetParent(go.transform, false);
        var labelRect = labelGo.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        cursorDebuffLabel = labelGo.GetComponent<TextMeshProUGUI>();
        cursorDebuffLabel.alignment = TextAlignmentOptions.Center;
        cursorDebuffLabel.fontSize = 28f;
        cursorDebuffLabel.color = Color.white;
        cursorDebuffLabel.raycastTarget = false;

        go.SetActive(false);
    }

    #endif

    void Update()
    {
        if (!cursorIconAttached || cursorDebuffIconRect == null) return;
        if (Mouse.current == null) return;

        Vector2 screenPos = Mouse.current.position.ReadValue();
        var canvasRect = transform as RectTransform;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, screenPos, null, out Vector2 localPoint))
        {
            cursorDebuffIconRect.anchoredPosition = localPoint;
        }
    }

    public void ShowWheel(UltimateSystem user)
    {
        if (isSpinning) return;
        currentUser = user;
        DetachCursorIcon();

        if (wheelPanel != null) wheelPanel.SetActive(true);
        if (resultText != null) resultText.text = "Spinning the wheel...";

        StartCoroutine(SpinWheel());
    }

    IEnumerator SpinWheel()
    {
        isSpinning = true;
        selectedDebuff = DebuffWheelNetwork.Instance.GetRandomDebuff();

        float elapsed = 0f;
        float startRotation = wheelTransform != null ? wheelTransform.rotation.eulerAngles.z : 0f;
        float targetRotation = startRotation + 360f * 5f + Random.Range(0f, 360f);

        while (elapsed < spinDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / spinDuration;
            float curveValue = spinCurve != null ? spinCurve.Evaluate(t) : t;
            if (wheelTransform != null)
                wheelTransform.rotation = Quaternion.Euler(0, 0, Mathf.Lerp(startRotation, targetRotation, curveValue));
            yield return null;
        }

        isSpinning = false;

        if (resultText != null)
        {
            resultText.text = $"Selected: {selectedDebuff.debuffName}";
            if (selectedDebuff.debuffType == DebuffType.RoleSpecific)
                resultText.text += $"\n(Role: {WorkOutDesktop.RoleName(selectedDebuff.targetRole)})";
        }

        AttachCursorIcon(selectedDebuff);

        if (wheelPanel != null) wheelPanel.SetActive(false);

        targetSelectionUI.Show(OnTargetChosen);
        cursorDebuffIconRect.SetAsLastSibling();
    }

    void AttachCursorIcon(DebuffData debuff)
    {


        bool hasIcon = debuff != null && debuff.icon != null;
        cursorDebuffIcon.enabled = hasIcon;
        cursorDebuffIcon.sprite = hasIcon ? debuff.icon : null;

        bool showName = !hasIcon && debuff != null;
        cursorDebuffLabel.gameObject.SetActive(showName);
        if (showName)
            cursorDebuffLabel.text = debuff.debuffName;

        cursorDebuffIcon.gameObject.SetActive(true);
        cursorIconAttached = true;
    }

    void DetachCursorIcon()
    {
        cursorIconAttached = false;
        if (cursorDebuffIcon != null)
            cursorDebuffIcon.gameObject.SetActive(false);
    }

    void OnTargetChosen(ulong targetId)
    {
        if (selectedDebuff.debuffType == DebuffType.RoleSpecific)
        {
            var targetRole = RoleAssignmentManager.Instance.GetRoleFor(targetId);
            if (targetRole != selectedDebuff.targetRole)
            {
                Debug.Log($"Этот дебаф только для {selectedDebuff.targetRole}!");
                return;
            }
        }

        DebuffWheelNetwork.Instance.ApplyDebuffServerRpc(selectedDebuff.DebuffId, targetId, currentUser.OwnerClientId);
        DetachCursorIcon();
    }
}
