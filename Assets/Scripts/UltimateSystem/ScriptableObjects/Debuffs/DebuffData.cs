using UnityEngine;

public enum DebuffEffectKind { Blur, Slow } // новое

[CreateAssetMenu(fileName = "NewDebuff", menuName = "Debuff/Debuff Data")]
public class DebuffData : ScriptableObject
{
    [Header("Основная информация")]
    public string debuffName = "Новый дебаф";
    public string description = "Описание эффекта";
    public Sprite icon;

    [Header("Тип дебафа")]
    public DebuffType debuffType = DebuffType.Common;

    [Header("Для ролевых дебафов")]
    public GameRole targetRole = GameRole.Typographer; // было Role

    [Header("Эффект")] // новое
    public DebuffEffectKind effectKind = DebuffEffectKind.Blur;

    [Header("Настройки длительности")]
    public float duration = 5f;

    [Header("Визуальные эффекты")]
    public GameObject visualEffectPrefab;

    public int DebuffId => name.GetHashCode();
}