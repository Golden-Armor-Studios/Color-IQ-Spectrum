using System;
using System.Threading.Tasks;
using UnityEngine;
using FB;

#if UNITY_IOS || UNITY_EDITOR_OSX
using Apple.GameKit;
#endif

public class StartController : MonoBehaviour
{
    private const string GameCenterConversionKey = "ColorIQ_GameCenterConversion";

    private async void Start()
    {
#if UNITY_IOS || UNITY_EDITOR_OSX
        await FB.Auth.SignInWithGameCenterAsync(GameCenterConversionKey);
#else
        Debug.Log("[GameCenterAuth] Skipping Game Center Firebase auth (iOS only).");
#endif
    }
}
