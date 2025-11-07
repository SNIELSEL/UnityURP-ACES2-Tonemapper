using UnityEditor;
using UnityEngine;

namespace ACES2.EditorTools
{
    [InitializeOnLoad]
    static class ACES2Onboarding
    {
        const string kSeenKey = "ACES2_Onboarding_Shown_v1";

        static ACES2Onboarding()
        {
            if (Application.isBatchMode) return;
            if (EditorPrefs.GetBool(kSeenKey, false)) return;
            EditorApplication.delayCall += Run;
        }

        static void Run()
        {
            EditorPrefs.SetBool(kSeenKey, true);

            if (!ACES2Installer.TryFindSampleAssets(out var matPath, out var lutPath))
            {
                var choice = EditorUtility.DisplayDialogComplex(
                    "ACES2 Tonemapper",
                    "Sample assets not found. Do you want to open Package Manager to import the ACES2 sample?",
                    "Open Package Manager", "Cancel", "Skip and Configure Manually");

                if (choice == 0)
                {
                    ACES2Installer.OpenPackageManagerToPackage("com.nielshaverkotte.aces2");
                }
                return;
            }

            ACES2Installer.EnsureLutLinkedOnMaterial(matPath, lutPath);

            var add = EditorUtility.DisplayDialog("ACES2 Tonemapper",
                "ACES2 assets were found. Do you want to add the two URP Renderer Features now?",
                "Add Now", "Later");

            if (add)
            {
                ACES2Installer.AddUrpRenderFeaturesFromMaterial(matPath);
            }
        }
    }
}
