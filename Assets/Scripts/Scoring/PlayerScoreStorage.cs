using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Локальное хранилище очков на диске конкретного игрока — по его PlayerId (UGS Authentication),
// переживает перезапуск игры. Один общий JSON-файл на профиль на устройстве.
public static class PlayerScoreStorage
{
    [System.Serializable]
    private class Entry
    {
        public string playerId;
        public int score;
    }

    [System.Serializable]
    private class Wrapper
    {
        public List<Entry> entries = new List<Entry>();
    }

    private static string FilePath => Path.Combine(Application.persistentDataPath, "player_scores.json");

    public static int LoadScore(string playerId)
    {
        var wrapper = ReadWrapper();
        foreach (var e in wrapper.entries)
            if (e.playerId == playerId) return e.score;
        return 0;
    }

    public static void SaveScore(string playerId, int score)
    {
        var wrapper = ReadWrapper();
        foreach (var e in wrapper.entries)
        {
            if (e.playerId == playerId)
            {
                e.score = score;
                Write(wrapper);
                return;
            }
        }
        wrapper.entries.Add(new Entry { playerId = playerId, score = score });
        Write(wrapper);
    }

    private static Wrapper ReadWrapper()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonUtility.FromJson<Wrapper>(File.ReadAllText(FilePath)) ?? new Wrapper();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[PlayerScoreStorage] Не удалось прочитать {FilePath}: {e.Message}");
        }
        return new Wrapper();
    }

    private static void Write(Wrapper wrapper)
    {
        try
        {
            File.WriteAllText(FilePath, JsonUtility.ToJson(wrapper));
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[PlayerScoreStorage] Не удалось записать {FilePath}: {e.Message}");
        }
    }
}
