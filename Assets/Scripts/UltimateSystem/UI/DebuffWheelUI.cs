using UnityEngine;
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

    void Awake()
    {
        Instance = this;
        if (wheelPanel != null) wheelPanel.SetActive(false);
    }

    public void ShowWheel(UltimateSystem user)
    {
        if (isSpinning) return;
        currentUser = user;

        if (wheelPanel != null) wheelPanel.SetActive(true);
        if (resultText != null) resultText.text = "Крутим колесо...";

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
            resultText.text = $"Выпал: {selectedDebuff.debuffName}";
            if (selectedDebuff.debuffType == DebuffType.RoleSpecific)
                resultText.text += $"\n(Роль: {selectedDebuff.targetRole})";
        }

        if (wheelPanel != null) wheelPanel.SetActive(false);

        targetSelectionUI.Show(OnTargetChosen);
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
    }
}