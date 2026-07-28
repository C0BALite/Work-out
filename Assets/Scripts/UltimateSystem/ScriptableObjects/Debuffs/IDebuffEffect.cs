// Интерфейс для всех эффектов дебафов
// Каждый дебаф реализует свою логику
public interface IDebuffEffect
{
    // Применить дебаф к цели
    void Apply(ulong targetClientId);

    // Убрать дебаф с цели
    void Remove(ulong targetClientId);
}