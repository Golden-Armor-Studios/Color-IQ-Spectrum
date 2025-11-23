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

        if (!string.IsNullOrEmpty(mainTarget))
        {
            project.SetBuildProperty(mainTarget, key, value);
            project.SetBuildProperty(mainTarget, disableEntitlementsValidationKey, disableEntitlementsValidationValue);
        }
        if (!string.IsNullOrEmpty(frameworkTarget))
        {
            project.SetBuildProperty(frameworkTarget, key, value);
            project.SetBuildProperty(frameworkTarget, disableEntitlementsValidationKey, disableEntitlementsValidationValue);
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
