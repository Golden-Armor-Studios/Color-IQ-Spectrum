using System;
using Firebase.Crashlytics;
using UnityEngine;

namespace FB
{
    public static class CrashlyticsLogForwarder
    {
        static bool _registered;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void RegisterHandler()
        {
            if (_registered)
            {
                return;
            }

            _registered = true;
            Application.logMessageReceived += HandleLogMessage;
        }

        static void HandleLogMessage(string condition, string stackTrace, LogType type)
        {
            if (!FirebaseApp.IsInitialized)
            {
                return;
            }

            try
            {
                Crashlytics.Log($"{type}: {condition}");
                if (!string.IsNullOrEmpty(stackTrace))
                {
                    Crashlytics.Log(stackTrace);
                }

                if (type == LogType.Exception)
                {
                    Crashlytics.LogException(new Exception(condition));
                }
            }
            catch (Exception ex)
            {
                Debug.unityLogger.LogWarning("[Crashlytics]", $"Failed to forward log: {ex.Message}");
            }
        }
    }
}
