using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class UltimateSystem : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private float passiveFillRate = 5f;
    [SerializeField] private float correctActionBonus = 20f;

    [Header("UI")]
    [SerializeField] private Slider ultimateBar;
    [SerializeField] private Button ultimateButton;
    [SerializeField] private GameObject debuffWheelPanel;
    [SerializeField] private GameObject abilityCanvas; // корневой Canvas Prefab-а

    private NetworkVariable<float> ultimateValue = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private bool isUltimateReady = false;
    public System.Action OnUltimateReady;

    private void Start()
    {
        if (ultimateButton != null)
        {
            ultimateButton.interactable = false;
            ultimateButton.onClick.AddListener(OnUltimateButtonClicked);
        }

        ultimateValue.OnValueChanged += OnUltimateValueChanged;
    }

    public override void OnNetworkSpawn()
    {
        if (abilityCanvas != null)
            abilityCanvas.SetActive(false); // новое — скрыт до начала InGame, актуальную видимость считаем в Update
    }

    void Update()
    {
        if (abilityCanvas != null)
        {
            // новое — весь canvas (бар, кнопка, блюр-панель, иконка замедления) виден только
            // владельцу и только пока идёт раунд; вне InGame (лобби/выбор ролей/результаты) скрыт
            bool inGame = GameSessionState.Instance != null
                && GameSessionState.Instance.Phase.Value == SessionPhase.InGame;
            abilityCanvas.SetActive(IsOwner && inGame);
        }

        if (IsOwner && ultimateBar != null && ultimateBar.transform.parent != null)
        {
            bool isBoss = RoleAssignmentManager.Instance != null
                && RoleAssignmentManager.Instance.GetMyRole() == GameRole.Boss;
            ultimateBar.transform.parent.gameObject.SetActive(!isBoss); // босс не участвует в ультimate
        }

        if (IsServer && !isUltimateReady)
        {
            float newValue = ultimateValue.Value + (passiveFillRate * Time.deltaTime);
            ultimateValue.Value = Mathf.Clamp(newValue, 0f, 100f);
        }

        UpdateUI();
    }
    private void OnUltimateValueChanged(float previousValue, float newValue)
    {
        if (newValue >= 100f && !isUltimateReady)
        {
            isUltimateReady = true;
            OnUltimateReady?.Invoke();
        }
    }

    private void UpdateUI()
    {
        if (ultimateBar != null)
            ultimateBar.value = ultimateValue.Value / 100f;
        if (ultimateButton != null && IsOwner)
            ultimateButton.interactable = isUltimateReady;
    }

    public void AddProgressForCorrectAction()
    {
        if (!IsServer) return;
        if (isUltimateReady) return;

        float newValue = ultimateValue.Value + correctActionBonus;
        ultimateValue.Value = Mathf.Clamp(newValue, 0f, 100f);
    }

    private void OnUltimateButtonClicked()
    {
        if (!IsOwner) return;
        if (!isUltimateReady) return;

        RequestUltimateServerRpc();
    }

    [ServerRpc]
    private void RequestUltimateServerRpc()
    {
        if (!isUltimateReady) return;
        OpenDebuffWheelClientRpc();
    }

    [ClientRpc]
    private void OpenDebuffWheelClientRpc()
    {
        if (IsOwner)
        {
            if (DebuffWheelUI.Instance != null) // было DebuffWheel.Instance
                DebuffWheelUI.Instance.ShowWheel(this);
        }
    }

    public void ResetUltimate()
    {
        if (!IsServer) return;
        ultimateValue.Value = 0f;
        isUltimateReady = false;
    }

    private void OnDestroy()
    {
        if (ultimateButton != null)
            ultimateButton.onClick.RemoveListener(OnUltimateButtonClicked);
        ultimateValue.OnValueChanged -= OnUltimateValueChanged;
    }
}