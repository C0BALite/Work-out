using UnityEngine;

public class Player : MonoBehaviour
{
    public int PlayerId;
    public PlayerRole Role;
    public PlayerState State { get; private set; }
    public PlayerUltimate Ultimate { get; private set; }

    void Awake()
    {
        State = GetComponent<PlayerState>();
        if (State == null) State = gameObject.AddComponent<PlayerState>();
        State.PlayerId = PlayerId;
        State.Role = Role;

        Ultimate = GetComponent<PlayerUltimate>();
        if (Ultimate == null) Ultimate = gameObject.AddComponent<PlayerUltimate>();
        Ultimate.SetPlayerId(PlayerId);
    }

    void Start()
    {
        // Автоматически регистрируемся в менеджере при появлении в сцене
        PlayerManager.Instance.RegisterPlayer(this);
    }

    void OnDestroy()
    {
        // Убираем себя при деспавне/выходе
        if (PlayerManager.Instance != null)
            PlayerManager.Instance.UnregisterPlayer(this);
    }
}