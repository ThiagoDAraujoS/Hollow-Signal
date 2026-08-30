// Assets/OpenVAT/Editor/NewtonsoftInstaller.cs
// Author: Luke Stilson

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

[InitializeOnLoad]
static class NewtonsoftChecker
{
    const string PackageName = "com.unity.nuget.newtonsoft-json";
    const string SuppressKey = "openvat-unity.SuppressNewtonsoftPrompt";

    static NewtonsoftChecker()
    {
        // Delay a frame so domain reload completes cleanly.
        EditorApplication.update += DelayedCheck;
    }

    static void DelayedCheck()
    {
        EditorApplication.update -= DelayedCheck;
        if (EditorPrefs.GetBool(SuppressKey, false)) return;

        NewtonsoftInstaller.CheckInstalled(isInstalled =>
        {
            if (isInstalled) return;

            var choice = EditorUtility.DisplayDialogComplex(
                "OpenVAT: Newtonsoft JSON Required",
                "This package needs 'Newtonsoft JSON'. Install it now via Package Manager?",
                "Install", "Later", "Don't ask again"
            );

            if (choice == 0) // Install
                NewtonsoftInstaller.Install();
            else if (choice == 2) // Don't ask again
                EditorPrefs.SetBool(SuppressKey, true);
        });
    }
}

public static class NewtonsoftInstaller
{
    const string PackageName = "com.unity.nuget.newtonsoft-json";
    static ListRequest listRequest;
    static AddRequest addRequest;

    [MenuItem("Tools/OpenVAT/Install Newtonsoft JSON")]
    public static void InstallMenu() => Install();

    public static void CheckInstalled(System.Action<bool> callback)
    {
        listRequest = Client.List(true);
        EditorApplication.update += PollList;

        void PollList()
        {
            if (!listRequest.IsCompleted) return;
            EditorApplication.update -= PollList;

            bool found = false;
            if (listRequest.Status == StatusCode.Success)
            {
                foreach (var p in listRequest.Result)
                    if (p.name == PackageName) { found = true; break; }
            }
            else if (listRequest.Status >= StatusCode.Failure)
            {
                Debug.LogError("Package list failed: " + listRequest.Error?.message);
            }

            callback?.Invoke(found);
        }
    }

    public static void Install(string versionOrId = PackageName)
    {
        // versionOrId can be "com.unity.nuget.newtonsoft-json@3.2.1" if you want to pin.
        addRequest = Client.Add(versionOrId);
        EditorApplication.update += PollAdd;

        void PollAdd()
        {
            if (!addRequest.IsCompleted) return;
            EditorApplication.update -= PollAdd;

            if (addRequest.Status == StatusCode.Success)
            {
                Debug.Log($"Installed {addRequest.Result.name} {addRequest.Result.version}");
                EditorUtility.DisplayDialog(
                    "Newtonsoft JSON",
                    "Installation complete. Unity will recompile scripts now.",
                    "OK"
                );
            }
            else if (addRequest.Status >= StatusCode.Failure)
            {
                Debug.LogError("Install failed: " + addRequest.Error?.message);
                EditorUtility.DisplayDialog(
                    "Newtonsoft JSON",
                    "Install failed. See Console for details.",
                    "OK"
                );
            }
        }
    }
}
#endif
