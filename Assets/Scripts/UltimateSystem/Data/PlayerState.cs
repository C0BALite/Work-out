using System;
using UnityEngine;

public class PlayerState : MonoBehaviour
{
    public int PlayerId;
    public PlayerRole Role;

    [Header("Active Debuff Flags (читаются мини-играми)")]
    public bool IsMouseInverted;
    public bool IsScreenShaking;
    public float ScreenShakeIntensity;

    public event Action<float> OnHammerHit;
    public void ReceiveHammerHit(float intensity) => OnHammerHit?.Invoke(intensity);
}