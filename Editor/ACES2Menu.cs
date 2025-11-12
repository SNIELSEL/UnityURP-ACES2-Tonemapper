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
                    "Default ACES2 assets not found. You can import the ACES2 sample manually from the Package Manager.",
                    "OK", "Cancel", "Skip");
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
                EditorUtility.DisplayDialog("ACES2 Tonemapper",
                    "Sample assets not found. You can import the ACES2 sample manually from the Package Manager.",
                    "OK");
            }
        }
    }
}
