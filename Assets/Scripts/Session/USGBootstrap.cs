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
            string profileName = string.IsNullOrEmpty(customArgument) ? "Clone" : customArgument;
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