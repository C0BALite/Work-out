using UnityEngine;
using UnityEngine.UI;

public class UltimateBarUI : MonoBehaviour
{
    [SerializeField] private int trackingPlayerId;
    [SerializeField] private Image fillImage;
    [SerializeField] private Button ultimateButton;
    [SerializeField] private GameObject readyEffect;

    void Start()
    {
        var player = PlayerManager.Instance.GetPlayer(trackingPlayerId);
        if (player == null) return;

        player.Ultimate.SetPlayerId(trackingPlayerId);
        player.Ultimate.OnReady += OnReady;
        player.Ultimate.OnConsumed += OnConsumed;
        ultimateButton.onClick.AddListener(OnUltimateButtonClicked);
        ultimateButton.gameObject.SetActive(false);
        readyEffect.SetActive(false);
    }

    void Update()
    {
        var player = PlayerManager.Instance.GetPlayer(trackingPlayerId);
        if (player != null && !player.Ultimate.IsReady)
            fillImage.fillAmount = player.Ultimate.NormalizedCharge;
    }

    void OnReady()
    {
        ultimateButton.gameObject.SetActive(true);
        readyEffect.SetActive(true);
    }

    void OnConsumed()
    {
        ultimateButton.gameObject.SetActive(false);
        readyEffect.SetActive(false);
        fillImage.fillAmount = 0;
    }

    void OnUltimateButtonClicked()
    {
        var player = PlayerManager.Instance.GetPlayer(trackingPlayerId);
        player.Ultimate.Consume();
        UltimateFlowController.Instance.StartFlow(trackingPlayerId);
    }
}