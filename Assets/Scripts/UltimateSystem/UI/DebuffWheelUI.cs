using System;
using System.Collections;
using UnityEngine;

public class DebuffWheelUI : MonoBehaviour
{
    [SerializeField] private RectTransform wheelTransform;
    [SerializeField] private Transform pointer;

    public void Show() => gameObject.SetActive(true);
    public void Hide() => gameObject.SetActive(false);

    public void Spin(WheelConfig config, Action<WheelSectorData> onComplete)
    {
        StartCoroutine(SpinCoroutine(config, onComplete));
    }

    IEnumerator SpinCoroutine(WheelConfig config, Action<WheelSectorData> onComplete)
    {
        int winnerIndex = UnityEngine.Random.Range(0, config.sectors.Count);
        var winner = config.sectors[winnerIndex];
        float sectorAngle = 360f / config.sectors.Count;
        float targetAngle = 360f - (winnerIndex * sectorAngle) - (sectorAngle / 2f);
        float totalAngle = config.fullRotations * 360f + targetAngle;

        float elapsed = 0;
        float startAngle = wheelTransform.eulerAngles.z;

        while (elapsed < config.spinDuration)
        {
            float t = elapsed / config.spinDuration;
            float curveT = config.spinCurve.Evaluate(t);
            float angle = Mathf.Lerp(startAngle, startAngle + totalAngle, curveT);
            wheelTransform.rotation = Quaternion.Euler(0, 0, angle);
            elapsed += Time.deltaTime;
            yield return null;
        }

        wheelTransform.rotation = Quaternion.Euler(0, 0, startAngle + totalAngle);
        yield return new WaitForSeconds(0.5f);
        onComplete?.Invoke(winner);
    }
}