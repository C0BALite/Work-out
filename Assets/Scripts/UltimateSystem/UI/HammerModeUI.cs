using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class HammerModeUI : MonoBehaviour
{
    [SerializeField] private GameObject hammerPanel;
    [SerializeField] private Transform portraitsContainer;
    [SerializeField] private HammerPortrait portraitPrefab;
    [SerializeField] private TextMeshProUGUI timerText;

    private HammerData hammer;
    private float remainingTime;
    private int hitsRemaining;
    private bool isActive;
    private System.Action onFinished;
    private List<HammerPortrait> portraits = new();

    public void Show(List<Player> targets, HammerData data, System.Action finishedCallback)
    {
        hammer = data;
        remainingTime = data.targetingTimeLimit;
        hitsRemaining = data.maxHits;
        onFinished = finishedCallback;
        isActive = true;

        foreach (var p in targets)
        {
            var port = Instantiate(portraitPrefab, portraitsContainer);
            port.Setup(p, OnPortraitClicked);
            portraits.Add(port);
        }

        hammerPanel.SetActive(true);
    }

    void Update()
    {
        if (!isActive) return;
        remainingTime -= Time.deltaTime;
        if (timerText != null) timerText.text = remainingTime.ToString("F1");
        if (remainingTime <= 0 || hitsRemaining <= 0)
            EndHammerMode();
    }

    void OnPortraitClicked(Player target)
    {
        if (!isActive || hitsRemaining <= 0) return;
        target.State.ReceiveHammerHit(hammer.hitShakeIntensity);
        hitsRemaining--;
        if (hitsRemaining <= 0) EndHammerMode();
    }

    void EndHammerMode()
    {
        isActive = false;
        hammerPanel.SetActive(false);
        foreach (var p in portraits) Destroy(p.gameObject);
        portraits.Clear();
        onFinished?.Invoke();
    }
}