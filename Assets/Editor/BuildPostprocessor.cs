using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using System.IO;

public static class BuildPostprocessor
{
    [PostProcessBuild(10)]
    public static void OnPostProcessBuild(BuildTarget target, string path)
    {
        if (target != BuildTarget.iOS)
        {
            return;
        }

        string projectPath = PBXProject.GetPBXProjectPath(path);
        PBXProject project = new PBXProject();
        project.ReadFromFile(projectPath);

#if UNITY_2019_3_OR_NEWER
        string mainTarget = project.GetUnityMainTargetGuid();
        string frameworkTarget = project.GetUnityFrameworkTargetGuid();
#else
        string mainTarget = project.TargetGuidByName(PBXProject.GetUnityTargetName());
        string frameworkTarget = project.TargetGuidByName("UnityFramework");
#endif

        const string key = "CODE_SIGN_ALLOW_ENTITLEMENTS_MODIFICATION";
        const string value = "YES";
        const string disableEntitlementsValidationKey = "CODE_SIGN_ALLOW_ENTITLEMENT_WRITES";
        const string disableEntitlementsValidationValue = "YES";

        var targetGuids = new System.Collections.Generic.HashSet<string>();
        void AddTarget(string guid)
        {
            if (!string.IsNullOrEmpty(guid))
            {
                targetGuids.Add(guid);
            }
        }

        AddTarget(project.ProjectGuid());

#if UNITY_2019_3_OR_NEWER
        AddTarget(project.GetUnityMainTargetGuid());
        AddTarget(project.GetUnityFrameworkTargetGuid());
#else
        AddTarget(project.TargetGuidByName(PBXProject.GetUnityTargetName()));
        AddTarget(project.TargetGuidByName("UnityFramework"));
#endif

        foreach (var targetGuid in targetGuids)
        {
            project.SetBuildProperty(targetGuid, key, value);
            project.SetBuildProperty(targetGuid, disableEntitlementsValidationKey, disableEntitlementsValidationValue);
        }

        string projectGuid = project.ProjectGuid();
        if (!string.IsNullOrEmpty(projectGuid))
        {
            project.SetBuildProperty(projectGuid, key, value);
            project.SetBuildProperty(projectGuid, disableEntitlementsValidationKey, disableEntitlementsValidationValue);
        }

        project.WriteToFile(projectPath);
    }
}
