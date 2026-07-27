using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Ultimate/Wheel Config", fileName = "NewWheelConfig")]
public class WheelConfig : ScriptableObject
{
    public List<WheelSectorData> sectors;
    public int fullRotations = 5;
    public AnimationCurve spinCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public float spinDuration = 3f;
}