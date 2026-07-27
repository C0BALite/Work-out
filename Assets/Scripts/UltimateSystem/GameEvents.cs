using System;

public static class GameEvents
{
    public static event Action<int> OnCorrectAction;
    public static void ReportCorrectAction(int playerId) => OnCorrectAction?.Invoke(playerId);

    public static event Action<int> OnUltimateReady;
    public static void ReportUltimateReady(int playerId) => OnUltimateReady?.Invoke(playerId);

    public static event Action<int> OnUltimateUsed;
    public static void ReportUltimateUsed(int playerId) => OnUltimateUsed?.Invoke(playerId);
}