using UnityEditor;
using UnityEngine;

namespace ACES2.EditorTools
{
    static class ACES2Menu
    {
        [MenuItem("Tools/ACES2/Setup", priority = 10)]
        static void Setup()
        {
            if (!ACES2Installer.TryFindSampleAssets(out var matPath, out var lutPath))
            {
                var choice = EditorUtility.DisplayDialogComplex(
                    "ACES2 Tonemapper",
                    "Default ACES2 assets not found. Import the ACES2 sample from Package Manager?",
                    "Open Package Manager", "Cancel", "Skip");
                if (choice == 0) ACES2Installer.OpenPackageManagerToPackage("com.nielshaverkotte.aces2");
                return;
            }

            ACES2Installer.EnsureLutLinkedOnMaterial(matPath, lutPath);
            ACES2Installer.AddUrpRenderFeaturesFromMaterial(matPath);
            EditorUtility.DisplayDialog("ACES2 Tonemapper", "Setup complete.", "OK");
        }

        [MenuItem("Tools/ACES2/Find Sample Assets", priority = 20)]
        static void FindAssets()
        {
            if (ACES2Installer.TryFindSampleAssets(out var matPath, out var lutPath))
            {
                Selection.objects = new Object[]
                {
                    AssetDatabase.LoadAssetAtPath<Object>(matPath),
                    AssetDatabase.LoadAssetAtPath<Object>(lutPath)
                };
                EditorGUIUtility.PingObject(Selection.activeObject);
                EditorUtility.DisplayDialog("ACES2 Tonemapper", "Found ACES2 material and LUT.", "OK");
            }
            else
            {
                var ok = EditorUtility.DisplayDialog("ACES2 Tonemapper",
                    "Sample assets not found. Open Package Manager to import the ACES2 sample?",
                    "Open Package Manager", "Close");
                if (ok) ACES2Installer.OpenPackageManagerToPackage("com.nielshaverkotte.aces2");
            }
        }

        [MenuItem("Tools/ACES2/Open Package Page", priority = 40)]
        static void OpenPM() => ACES2Installer.OpenPackageManagerToPackage("com.nielshaverkotte.aces2");
    }
}
