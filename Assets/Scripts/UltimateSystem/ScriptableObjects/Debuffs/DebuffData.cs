using UnityEngine;

// Создаём.asset файл для каждого дебафа
// Правый клик в Project → Create → Debuff → New Debuff
[CreateAssetMenu(fileName = "NewDebuff", menuName = "Debuff/Debuff Data")]
public class DebuffData : ScriptableObject
{
    [Header("Основная информация")]
    public string debuffName = "Новый дебаф";
    public string description = "Описание эффекта";
    public Sprite icon;                    // Иконка для рулетки

    [Header("Тип дебафа")]
    public DebuffType debuffType = DebuffType.Common;

    [Header("Для ролевых дебафов")]
    // Если debuffType = RoleSpecific, укажите, к какой роли привязан
    public Role targetRole = Role.Designer;

    [Header("Настройки длительности")]
    public float duration = 5f;            // Сколько секунд длится дебаф

    [Header("Визуальные эффекты")]
    public GameObject visualEffectPrefab;  // Префаб эффекта (частицы и т.д.)

    // Уникальный ID дебафа (для сетевой синхронизации)
    public int DebuffId => name.GetHashCode();
}