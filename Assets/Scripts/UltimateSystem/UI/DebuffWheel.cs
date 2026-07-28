using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
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
    [SerializeField] private Button confirmButton;
    [SerializeField] private Text resultText;

    private UltimateSystem currentUser;
    private DebuffData selectedDebuff;
    private bool isSpinning = false;
    private bool isTargetSelectionMode = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (wheelPanel != null)
            wheelPanel.SetActive(false);
    }

    public void ShowWheel(UltimateSystem user)
    {
        if (isSpinning) return;

        currentUser = user;

        if (wheelPanel != null)
            wheelPanel.SetActive(true);

        if (confirmButton != null)
            confirmButton.gameObject.SetActive(false);

        if (resultText != null)
            resultText.text = "Крутим колесо...";

        StartCoroutine(SpinWheel());
    }

    private IEnumerator SpinWheel()
    {
        isSpinning = true;

        selectedDebuff = GetRandomDebuff();

        float elapsed = 0f;
        float startRotation = wheelTransform != null ? wheelTransform.rotation.eulerAngles.z : 0f;
        float targetRotation = startRotation + (360f * 5f) + Random.Range(0f, 360f);

        while (elapsed < spinDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / spinDuration;
            float curveValue = spinCurve != null ? spinCurve.Evaluate(t) : t;

            float currentRotation = Mathf.Lerp(startRotation, targetRotation, curveValue);

            if (wheelTransform != null)
                wheelTransform.rotation = Quaternion.Euler(0, 0, currentRotation);

            yield return null;
        }

        isSpinning = false;

        if (resultText != null)
        {
            resultText.text = $"Выпал: {selectedDebuff.debuffName}";
            if (selectedDebuff.debuffType == DebuffType.RoleSpecific)
                resultText.text += $"\n(Роль: {selectedDebuff.targetRole})";
        }

        if (confirmButton != null)
            confirmButton.gameObject.SetActive(true);

        StartTargetSelection();
    }

    private DebuffData GetRandomDebuff()
    {
        if (allDebuffs == null || allDebuffs.Count == 0) return null;
        return allDebuffs[Random.Range(0, allDebuffs.Count)];
    }

    private void StartTargetSelection()
    {
        isTargetSelectionMode = true;
        if (resultText != null)
            resultText.text += "\n\nВыберите цель кликом!";
    }

    private void Update()
    {
        if (isTargetSelectionMode && Input.GetMouseButtonDown(0))
        {
            TrySelectTarget();
        }
    }

    private void TrySelectTarget()
    {
        Vector2 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        RaycastHit2D hit = Physics2D.Raycast(mousePosition, Vector2.zero);

        if (hit.collider != null)
        {
            PlayerRole targetRole = hit.collider.GetComponent<PlayerRole>();
            NetworkObject targetNetObj = hit.collider.GetComponent<NetworkObject>();

            if (targetRole != null && targetNetObj != null)
            {
                if (selectedDebuff.debuffType == DebuffType.RoleSpecific)
                {
                    if (targetRole.CurrentRole != selectedDebuff.targetRole)
                    {
                        if (resultText != null)
                            resultText.text = $"Этот дебаф только для {selectedDebuff.targetRole}!";
                        return;
                    }
                }

                ApplyDebuff(targetNetObj.OwnerClientId);
            }
        }
    }

    private void ApplyDebuff(ulong targetId)
    {
        isTargetSelectionMode = false;

        if (selectedDebuff != null && currentUser != null)
        {
            ApplyDebuffServerRpc(selectedDebuff.DebuffId, targetId, currentUser.OwnerClientId);
        }

        if (wheelPanel != null)
            wheelPanel.SetActive(false);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ApplyDebuffServerRpc(int debuffId, ulong targetId, ulong casterId)
    {
        DebuffData debuff = allDebuffs.Find(d => d.DebuffId == debuffId);
        if (debuff == null) return;

        if (DebuffManager.Instance != null)
            DebuffManager.Instance.ApplyDebuff(debuff, targetId, casterId);

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(casterId, out var client))
        {
            UltimateSystem casterUlt = client.PlayerObject.GetComponent<UltimateSystem>();
            if (casterUlt != null)
                casterUlt.ResetUltimate();
        }

        NotifyDebuffAppliedClientRpc(debuffId, targetId, debuff.debuffName);
    }

    [ClientRpc]
    private void NotifyDebuffAppliedClientRpc(int debuffId, ulong targetId, string debuffName)
    {
        Debug.Log($"Дебаф {debuffName} применён к игроку {targetId}!");
    }
}