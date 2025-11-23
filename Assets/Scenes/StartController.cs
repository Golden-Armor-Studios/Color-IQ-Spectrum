using System;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using UnityEngine;

#if UNITY_IOS || UNITY_EDITOR_OSX
using Apple.GameKit;
#endif

public class StartController : MonoBehaviour
{
#if UNITY_IOS || UNITY_EDITOR_OSX
    private FirebaseAuth _auth;
    private bool _isAuthenticating;
#endif
    internal static bool AuthCompleted { get; private set; }
    private const string GameCenterConversionKey = "ColorIQ_GameCenterConversion";

    private async void Start()
    {
#if UNITY_IOS || UNITY_EDITOR_OSX
        AuthCompleted = false;
        _auth = await FirebaseInitializer.GetAuthAsync();
        if (_auth != null)
        {
            await AuthenticateWithGameCenterAsync();
        }
        else
        {
            Debug.LogError("[GameCenterAuth] Firebase auth unavailable.");
        }
#else
        Debug.Log("[GameCenterAuth] Skipping Game Center Firebase auth (iOS only).");
#endif
    }

#if UNITY_IOS || UNITY_EDITOR_OSX
    private async Task AuthenticateWithGameCenterAsync()
    {
        if (_isAuthenticating || _auth == null)
        {
            return;
        }

        if (Application.isEditor)
        {
            Debug.Log("[GameCenterAuth] Game Center → Firebase sign-in skipped in editor (device-only).");
            AuthCompleted = true;
            return;
        }

        _isAuthenticating = true;

        try
        {
            var localPlayer = await GKLocalPlayer.Authenticate();
            if (localPlayer == null || !localPlayer.IsAuthenticated)
            {
                Debug.LogWarning("[GameCenterAuth] Game Center authentication was cancelled or failed.");
                AuthCompleted = true;
                return;
            }

            Debug.Log($"[GameCenterAuth] Game Center authenticated: {localPlayer.DisplayName} ({localPlayer.GamePlayerId}).");

            if (!GameCenterAuthProvider.IsPlayerAuthenticated())
            {
                Debug.LogWarning("[GameCenterAuth] Game Center provider does not see an authenticated player.");
                AuthCompleted = true;
                return;
            }

            var credential = await GameCenterAuthProvider.GetCredentialAsync();
            if (credential == null)
            {
                Debug.LogWarning("[GameCenterAuth] Failed to obtain Firebase credential from Game Center.");
                AuthCompleted = true;
                return;
            }

            var user = await _auth.SignInWithCredentialAsync(credential);
            Debug.Log($"[GameCenterAuth] Firebase signed in via Game Center as {user.DisplayName ?? user.UserId}.");

            if (PlayerPrefs.GetInt(GameCenterConversionKey, 0) == 0)
            {
                AnalyticsService.LogGameCenterConversion();
                PlayerPrefs.SetInt(GameCenterConversionKey, 1);
                PlayerPrefs.Save();
            }

            AuthCompleted = true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GameCenterAuth] Failed to sign in with Game Center: {ex}");
            AuthCompleted = true;
        }
    }
#endif
}
