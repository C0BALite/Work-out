using System;
using UnityEngine;

public class PlayerUltimate : MonoBehaviour
{
    [SerializeField] private float passiveChargePerSecond = 3f;
    [SerializeField] private float correctActionCharge = 12f;
    [SerializeField] private float maxCharge = 100f;

    private int playerId;
    private float currentCharge;
    private bool isReady;

    public float NormalizedCharge => currentCharge / maxCharge;
    public bool IsReady => isReady;

    public event Action OnReady;
    public event Action OnConsumed;

    void OnEnable() => GameEvents.OnCorrectAction += OnCorrectAction;
    void OnDisable() => GameEvents.OnCorrectAction -= OnCorrectAction;

    void Update()
    {
        if (!isReady)
            AddCharge(passiveChargePerSecond * Time.deltaTime);
    }

    void OnCorrectAction(int id)
    {
        if (id == playerId) AddCharge(correctActionCharge);
    }

    void AddCharge(float amount)
    {
        if (isReady) return;
        currentCharge = Mathf.Min(currentCharge + amount, maxCharge);
        if (currentCharge >= maxCharge && !isReady)
        {
            isReady = true;
            OnReady?.Invoke();
            GameEvents.ReportUltimateReady(playerId);
        }
    }

    public void Consume()
    {
        if (!isReady) return;
        currentCharge = 0;
        isReady = false;
        OnConsumed?.Invoke();
        GameEvents.ReportUltimateUsed(playerId);
    }

    // Вызывается при спавне (онлайн) или в Start (локально)
    public void SetPlayerId(int id) => playerId = id;
}