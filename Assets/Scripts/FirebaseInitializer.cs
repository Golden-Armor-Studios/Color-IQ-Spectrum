using System;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using UnityEngine;

public static class FirebaseInitializer
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
            var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();
            if (dependencyStatus == DependencyStatus.Available)
            {
                _ = FirebaseApp.DefaultInstance;
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
