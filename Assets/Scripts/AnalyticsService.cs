using System.Threading.Tasks;
using Firebase;
using Firebase.Analytics;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AnalyticsService
{
    private static bool firebaseInitialized;
    private static bool initializing;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Setup()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        InitializeFirebaseIfNeeded();
    }

    private static async void InitializeFirebaseIfNeeded()
    {
        if (firebaseInitialized || initializing)
        {
            return;
        }

        initializing = true;
        bool ready = await FirebaseInitializer.InitializeAsync();
        firebaseInitialized = ready;
        if (ready)
        {
            LogScreenView(SceneManager.GetActiveScene().name);
        }
        else
        {
            Debug.LogError("[Analytics] Firebase initialization failed.");
        }

        initializing = false;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        LogScreenView(scene.name);
    }

    public static void LogScreenView(string screenName)
    {
        if (!firebaseInitialized)
        {
            return;
        }

        FirebaseAnalytics.LogEvent(FirebaseAnalytics.EventScreenView,
            new Firebase.Analytics.Parameter(FirebaseAnalytics.ParameterScreenName, screenName),
            new Firebase.Analytics.Parameter(FirebaseAnalytics.ParameterScreenClass, screenName));
    }

    public static void LogIapConversion(string productId, double value = 0, string currency = "USD")
    {
        if (!firebaseInitialized)
        {
            return;
        }

        FirebaseAnalytics.LogEvent("iap_conversion",
            new Firebase.Analytics.Parameter("product_id", productId),
            new Firebase.Analytics.Parameter(FirebaseAnalytics.ParameterValue, value),
            new Firebase.Analytics.Parameter(FirebaseAnalytics.ParameterCurrency, currency));
    }

    public static void LogGameCenterConversion()
    {
        if (!firebaseInitialized)
        {
            return;
        }

        FirebaseAnalytics.LogEvent("gamecenter_login");
    }
}
