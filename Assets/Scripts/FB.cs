using System;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Analytics;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_IOS || UNITY_EDITOR_OSX
using Apple.GameKit;
#endif

namespace FB
{
    public static class FirebaseApp
    {
        static TaskCompletionSource<bool> _initializationTcs;
        static FirebaseAuth _authInstance;

        public static bool IsInitialized => _initializationTcs?.Task.IsCompletedSuccessfully ?? false;

        public static Task<bool> InitializeAsync()
        {
            if (_initializationTcs != null)
            {
                return _initializationTcs.Task;
            }

            _initializationTcs = new TaskCompletionSource<bool>();
            InitializeInternal();
            return _initializationTcs.Task;
        }

        static async void InitializeInternal()
        {
            try
            {
                var dependencyStatus = await global::Firebase.FirebaseApp.CheckAndFixDependenciesAsync();
                if (dependencyStatus == DependencyStatus.Available)
                {
                    _ = global::Firebase.FirebaseApp.DefaultInstance;
                    _initializationTcs.TrySetResult(true);
                    Debug.Log("[Firebase] Initialized default app.");
                }
                else
                {
                    Debug.LogError($"[Firebase] Dependencies unavailable: {dependencyStatus}");
                    _initializationTcs.TrySetResult(false);
                }
            }
            catch (Exception ex)
            {
                _initializationTcs.TrySetException(ex);
            }
        }

        public static async Task<FirebaseAuth> GetAuthAsync()
        {
            bool initialized = await InitializeAsync();
            if (!initialized)
            {
                return null;
            }

            return _authInstance ??= FirebaseAuth.DefaultInstance;
        }
    }

    public static class AnalyticsService
    {
        static bool _firebaseInitialized;
        static bool _initializing;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Setup()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            InitializeFirebaseIfNeeded();
        }

        static async void InitializeFirebaseIfNeeded()
        {
            if (_firebaseInitialized || _initializing)
            {
                return;
            }

            _initializing = true;
            bool ready = await FirebaseApp.InitializeAsync();
            _firebaseInitialized = ready;
            if (ready)
            {
                LogScreenView(SceneManager.GetActiveScene().name);
            }
            else
            {
                Debug.LogError("[Analytics] Firebase initialization failed.");
            }
            _initializing = false;
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            LogScreenView(scene.name);
        }

        public static void LogScreenView(string screenName)
        {
            if (!_firebaseInitialized)
            {
                return;
            }

            FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventScreenView,
                new Parameter(FirebaseAnalytics.ParameterScreenName, screenName),
                new Parameter(FirebaseAnalytics.ParameterScreenClass, screenName));
        }

        public static void LogIapConversion(string productId, double value = 0, string currency = "USD")
        {
            if (!_firebaseInitialized)
            {
                return;
            }

            FirebaseAnalytics.LogEvent("iap_conversion",
                new Parameter("product_id", productId),
                new Parameter(FirebaseAnalytics.ParameterValue, value),
                new Parameter(FirebaseAnalytics.ParameterCurrency, currency));
        }

        public static void LogGameCenterConversion()
        {
            if (!_firebaseInitialized)
            {
                return;
            }

            FirebaseAnalytics.LogEvent("gamecenter_login");
        }
    }

#if UNITY_IOS || UNITY_EDITOR_OSX
    public static class Auth
    {
        static FirebaseAuth _firebaseAuth;
        public static FirebaseUser CurrentUser { get; private set; }
        public static bool AuthCompleted { get; private set; }

        public static async Task SignInWithGameCenterAsync(string conversionPrefKey)
        {
            AuthCompleted = false;
            _firebaseAuth = await FirebaseApp.GetAuthAsync();
            if (_firebaseAuth == null)
            {
                Debug.LogError("[GameCenterAuth] Firebase auth unavailable.");
                AuthCompleted = true;
                return;
            }

            if (Application.isEditor)
            {
                Debug.Log("[GameCenterAuth] Skipping Game Center → Firebase sign-in in editor.");
                AuthCompleted = true;
                return;
            }

            try
            {
                var localPlayer = await GKLocalPlayer.Authenticate();
                if (localPlayer == null || !localPlayer.IsAuthenticated)
                {
                    Debug.LogWarning("[GameCenterAuth] Game Center authentication was cancelled or failed.");
                    AuthCompleted = true;
                    return;
                }

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

                var user = await _firebaseAuth.SignInWithCredentialAsync(credential);
                Debug.Log($"[GameCenterAuth] Firebase signed in via Game Center as {user.DisplayName ?? user.UserId}.");

                CurrentUser = user;
                if (PlayerPrefs.GetInt(conversionPrefKey, 0) == 0)
                {
                    AnalyticsService.LogGameCenterConversion();
                    PlayerPrefs.SetInt(conversionPrefKey, 1);
                    PlayerPrefs.Save();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameCenterAuth] Failed to sign in with Game Center: {ex}");
            }
            finally
            {
                AuthCompleted = true;
            }
        }
    }
#endif
}
