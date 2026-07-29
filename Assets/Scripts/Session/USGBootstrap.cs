using Unity.Services.Core;
using Unity.Services.Authentication;
using UnityEngine;
using ParrelSync; // новое

public class UGSBootstrap : MonoBehaviour
{
    async void Awake()
    {
        DontDestroyOnLoad(gameObject);

        await UnityServices.InitializeAsync();

        // новое — разные профили для оригинала и ParrelSync-клона
#if UNITY_EDITOR
        if (ClonesManager.IsClone())
        {
            string customArgument = ClonesManager.GetArgument();
            string profileName;
            if (!string.IsNullOrEmpty(customArgument))
            {
                profileName = customArgument;
            }
            else
            {
                // рандомный профиль на каждый запуск клона — для локальных тестов с несколькими
                // клиентами: без этого все клоны без ручного custom argument получали один и тот
                // же профиль "Clone" и, соответственно, один PlayerId.
                profileName = "Clone_" + System.Guid.NewGuid().ToString("N").Substring(0, 12);
            }
            AuthenticationService.Instance.SwitchProfile(profileName);
        }
#endif

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        Debug.Log($"Signed in as: {AuthenticationService.Instance.PlayerId}");
    }
}