using UnityEngine;
using UnityEngine.InputSystem;

public class LobbyExitHandler : MonoBehaviour
{
    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            _ = QuitGame();
        }
    }

    async System.Threading.Tasks.Task QuitGame()
    {
        if (SessionManager.Instance != null)
        {
            await SessionManager.Instance.LeaveLobbyAsync();
        }

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}