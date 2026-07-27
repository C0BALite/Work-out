using UnityEngine;

public class ScreenShaker : MonoBehaviour
{
    [SerializeField] private int playerId;
    private Vector3 originalPos;
    private float intensity;
    private float timer;

    void Start()
    {
        originalPos = transform.localPosition;
        var state = PlayerManager.Instance.GetState(playerId);
        if (state != null) state.OnHammerHit += TriggerShake;
    }

    void Update()
    {
        var state = PlayerManager.Instance.GetState(playerId);
        if (state != null && state.IsScreenShaking)
        {
            float shake = state.ScreenShakeIntensity;
            transform.localPosition = originalPos + Random.insideUnitSphere * shake;
        }
        else if (timer > 0)
        {
            timer -= Time.deltaTime;
            transform.localPosition = originalPos + Random.insideUnitSphere * intensity;
            if (timer <= 0) transform.localPosition = originalPos;
        }
        else
        {
            transform.localPosition = originalPos;
        }
    }

    void TriggerShake(float hitIntensity)
    {
        intensity = hitIntensity;
        timer = 0.3f;
    }
}