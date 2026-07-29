public interface IPuzzle
{
//    GameRole RequiredRole { get; }         // какая роль должна видеть эту головоломку
    void Begin();                           // вызывается когда фаза становится InGame и роль совпала
    void ForceEnd();                        // на случай истечения общего таймера
    bool IsCompleted { get; }               // локально определяет, когда игрок "закончил"
    float GetLocalScore();                  // 0..1 — как хорошо выполнено (для скоринга)
}