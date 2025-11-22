using UnityEngine;
using System.Runtime.InteropServices;

public static class IOSShare
{
#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void iosShare_sheet(string imagePath, string message);
#endif

    public static void ShareImage(string imagePath, string message)
    {
#if UNITY_IOS && !UNITY_EDITOR
        iosShare_sheet(imagePath, message);
#else
        Debug.Log($"[IOSShare] ShareImage called with path '{imagePath}' and message '{message}'.");
#endif
    }
}
