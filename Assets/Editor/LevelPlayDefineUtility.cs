#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

[InitializeOnLoad]
public static class LevelPlayDefineUtility
{
    const string Define = "LEVELPLAY_SDK";

    static LevelPlayDefineUtility()
    {
        UpdateDefines();
        EditorApplication.projectChanged += UpdateDefines;
        EditorApplication.playModeStateChanged += _ => UpdateDefines();
    }

    static void UpdateDefines()
    {
        bool hasSdk = HasLevelPlaySdk();
        foreach (BuildTargetGroup group in Enum.GetValues(typeof(BuildTargetGroup)))
        {
            if (!IsValidGroup(group))
            {
                continue;
            }
            UpdateDefineForGroup(group, hasSdk);
        }
    }

    static bool HasLevelPlaySdk()
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .Any(assembly => assembly.GetType("IronSourceEvents", false) != null);
    }

    static void UpdateDefineForGroup(BuildTargetGroup group, bool enabled)
    {
        string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
        var defineList = defines.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).ToList();

        bool contains = defineList.Contains(Define);
        if (enabled && !contains)
        {
            defineList.Add(Define);
        }
        else if (!enabled && contains)
        {
            defineList.Remove(Define);
        }
        else
        {
            return;
        }

        string updated = string.Join(";", defineList);
        PlayerSettings.SetScriptingDefineSymbolsForGroup(group, updated);
    }

    static bool IsValidGroup(BuildTargetGroup group)
    {
        if (group == BuildTargetGroup.Unknown)
        {
            return false;
        }
#if UNITY_2022_1_OR_NEWER
        return BuildPipeline.IsBuildTargetGroupSupported(group);
#else
        switch (group)
        {
            case BuildTargetGroup.Standalone:
            case BuildTargetGroup.iOS:
            case BuildTargetGroup.Android:
            case BuildTargetGroup.tvOS:
            case BuildTargetGroup.Switch:
                return true;
            default:
                return false;
        }
#endif
    }
}
#endif
