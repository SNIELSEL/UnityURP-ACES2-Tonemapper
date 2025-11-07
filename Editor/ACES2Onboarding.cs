using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
namespace ACES2.EditorTools
{
    static class ACES2Onboarding
    {
        const string PackageVersion = "1.0.1";

        static string ProjectKey
        {
            get
            {
                using var md5 = MD5.Create();
                var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(Application.dataPath));
                var sb = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++) sb.Append(bytes[i].ToString("x2"));
                return $"ACES2_Onboarding_{PackageVersion}_{sb}";
            }
        }

        [InitializeOnLoadMethod]
        static void Init()
        {
            if (Application.isBatchMode) return;
            EditorApplication.update -= FirstTick;
            EditorApplication.update += FirstTick;
        }

        [DidReloadScripts]
        static void OnReload()
        {
            if (Application.isBatchMode) return;
            TryRun();
        }

        static void FirstTick()
        {
            EditorApplication.update -= FirstTick;
            TryRun();
        }

        [MenuItem("Tools/ACES2/Run Setup")]
        static void RunSetup()
        {
            EditorPrefs.DeleteKey(ProjectKey);
            TryRun(true);
        }

        static void TryRun(bool forced = false)
        {
            if (!forced && EditorPrefs.GetBool(ProjectKey, false)) return;
            if (ACES2Installer.TryFindSampleAssets(out var matPath, out var lutPath))
            {
                ACES2Installer.EnsureLutLinkedOnMaterial(matPath, lutPath);
                var add = EditorUtility.DisplayDialog("ACES2 Tonemapper", "ACES2 assets found. Add the two URP Renderer Features now?", "Add Now", "Later");
                if (add) ACES2Installer.AddUrpRenderFeaturesFromMaterial(matPath);
                EditorPrefs.SetBool(ProjectKey, true);
                return;
            }

            var choice = EditorUtility.DisplayDialogComplex(
                "ACES2 Tonemapper",
                "Sample assets not found. Import the ACES2 sample from Package Manager?",
                "Open Package Manager", "Cancel", "Skip");

            if (choice == 0) ACES2Installer.OpenPackageManagerToPackage("com.nielshaverkotte.aces2");
            EditorPrefs.SetBool(ProjectKey, true);
        }
    }
}
