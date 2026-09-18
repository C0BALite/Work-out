using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;

public class DebuffWheel : NetworkBehaviour
{
    public static DebuffWheel Instance { get; private set; }

    [Header("Wheel Settings")]
    [SerializeField] private List<DebuffData> allDebuffs = new List<DebuffData>();
    [SerializeField] private Transform wheelTransform;
    [SerializeField] private float spinDuration = 3f;
    [SerializeField] private AnimationCurve spinCurve;

    [Header("UI")]
    [SerializeField] private GameObject wheelPanel;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private TargetSelectionUI targetSelectionUI; // новое

    private UltimateSystem currentUser;
    private DebuffData selectedDebuff;
    private bool isSpinning;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        if (wheelPanel != null) wheelPanel.SetActive(false);
    }

    public void ShowWheel(UltimateSystem user)
    {
        if (isSpinning) return;
        currentUser = user;

        if (wheelPanel != null) wheelPanel.SetActive(true);
        if (resultText != null) resultText.text = "Spinning the wheel...";

        StartCoroutine(SpinWheel());
    }

    IEnumerator SpinWheel()
    {
        isSpinning = true;
        selectedDebuff = GetRandomDebuff();

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

        if (wheelPanel != null) wheelPanel.SetActive(false);

        targetSelectionUI.Show(OnTargetChosen); // новое — вместо клика по игровому миру
    }

    DebuffData GetRandomDebuff()
    {
        if (allDebuffs == null || allDebuffs.Count == 0) return null;
        return allDebuffs[Random.Range(0, allDebuffs.Count)];
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

        ApplyDebuffServerRpc(selectedDebuff.DebuffId, targetId, currentUser.OwnerClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    void ApplyDebuffServerRpc(int debuffId, ulong targetId, ulong casterId)
    {
        DebuffData debuff = allDebuffs.Find(d => d.DebuffId == debuffId);
        if (debuff == null) return;

        DebuffManager.Instance?.ApplyDebuff(debuff, targetId, casterId);

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(casterId, out var client))
        {
            var casterUlt = client.PlayerObject.GetComponent<UltimateSystem>();
            casterUlt?.ResetUltimate();
        }

        NotifyDebuffAppliedClientRpc(debuffId, targetId, debuff.debuffName);
    }

    [ClientRpc]
    void NotifyDebuffAppliedClientRpc(int debuffId, ulong targetId, string debuffName)
    {
        Debug.Log($"Дебаф {debuffName} применён к игроку {targetId}!");
    }
}
