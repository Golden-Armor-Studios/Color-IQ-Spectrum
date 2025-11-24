using System.Threading.Tasks;
using UnityEngine;

#if UNITY_IOS || UNITY_EDITOR_OSX
using Apple.GameKit;
#endif

public static class GameCenterService
{
    public static async Task<bool> ReportAchievementAsync(string achievementId, double percentComplete = 100.0)
    {
#if UNITY_IOS || UNITY_EDITOR_OSX
        if (string.IsNullOrEmpty(achievementId))
        {
            return false;
        }

        if (Application.isEditor)
        {
            Debug.Log($"[GameCenter] Skipping achievement '{achievementId}' in editor.");
            return false;
        }

        var localPlayer = GKLocalPlayer.Local;
        if (localPlayer == null || !localPlayer.IsAuthenticated)
        {
            Debug.LogWarning("[GameCenter] Local player not authenticated; unable to report achievement.");
            return false;
        }

        try
        {
            var achievement = GKAchievement.Init(achievementId);
            achievement.PercentComplete = percentComplete;
            achievement.ShowCompletionBanner = true;
            await GKAchievement.Report(achievement);
            Debug.Log($"[GameCenter] Reported achievement '{achievementId}' at {percentComplete}%.");
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[GameCenter] Failed to report achievement '{achievementId}': {ex.Message}");
            return false;
        }
#else
        await Task.CompletedTask;
        return false;
#endif
    }
}
