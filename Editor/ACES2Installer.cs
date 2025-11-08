#if HAS_URP
using UnityEngine.Rendering.Universal;
#endif
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using System.Reflection;

namespace ACES2.EditorTools
{
    public static class ACES2Installer
    {
        public static bool TryFindSampleAssets(out string materialPath, out string lutPath)
        {
            materialPath = null;
            lutPath = null;
            string[] roots =
            {
                "Assets/Custom/ACES2/Samples~/ACES2/SceneTemplateAssets/ACES2Stuff",
                "Assets/Custom/ACES2/Samples/ACES2/SceneTemplateAssets/ACES2Stuff",
                "Assets/Samples/ACES2/SceneTemplateAssets/ACES2Stuff",
                "Assets"
            };
            foreach (var root in roots)
            {
                if (!AssetDatabase.IsValidFolder(root)) continue;
                var matGuids = AssetDatabase.FindAssets("t:Material MAT_ACES2_Fullscreen", new[] { root });
                if (matGuids.Length == 0) matGuids = AssetDatabase.FindAssets("t:Material ACES2", new[] { root });
                var lutGuids = AssetDatabase.FindAssets("t:Texture3D ACES2_33", new[] { root });
                if (matGuids.Length > 0) materialPath = AssetDatabase.GUIDToAssetPath(matGuids[0]);
                if (lutGuids.Length > 0) lutPath = AssetDatabase.GUIDToAssetPath(lutGuids[0]);
                if (materialPath != null && lutPath != null) return true;
            }
            return false;
        }

        public static void EnsureLutLinkedOnMaterial(string materialPath, string lutPath)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            var lut = AssetDatabase.LoadAssetAtPath<Texture3D>(lutPath);
            if (!mat || !lut) return;
            if (!mat.GetTexture("_Aces2Lut"))
            {
                mat.SetTexture("_Aces2Lut", lut);
                EditorUtility.SetDirty(mat);
                AssetDatabase.SaveAssets();
            }
        }

        public static void OpenPackageManagerToPackage(string packageName)
        {
            // 1) Try both known menu paths (varies by Unity version/layout)
            bool opened =
                EditorApplication.ExecuteMenuItem("Window/Package Manager") ||
                EditorApplication.ExecuteMenuItem("Window/Asset Management/Package Manager");

            // 2) Find the Package Manager window type (namespace differs by version)
            var editorAsm = typeof(EditorApplication).Assembly;
            var pmType =
                editorAsm.GetType("UnityEditor.PackageManager.UI.PackageManagerWindow") ??
                editorAsm.GetType("UnityEditor.PackageManager.UI.Internal.PackageManagerWindow");

            if (pmType == null)
            {
                if (!opened)
                {
                    EditorUtility.DisplayDialog(
                        "Open Package Manager",
                        "Could not open Package Manager automatically. Please open it via Window ▸ Package Manager.",
                        "OK");
                }
                return;
            }

            // 3) Get (or create) the window
            var window = EditorWindow.GetWindow(pmType, true, "Package Manager", true);
            if (window == null)
            {
                EditorUtility.DisplayDialog(
                    "Open Package Manager",
                    "Package Manager window could not be created.",
                    "OK");
                return;
            }

            // 4) Try API variants to focus a package
            //    Newer editors use Internal.* and private methods; use reflection safely.
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            // Prefer SelectPackageAndFilter(packageName, "InProject")
            var selectWithFilter = pmType.GetMethod("SelectPackageAndFilter", flags);
            if (selectWithFilter != null)
            {
                try { selectWithFilter.Invoke(window, new object[] { packageName, "InProject" }); return; }
                catch { /* fall through */ }
            }

            // Older versions: SelectPackage(packageName)
            var selectPackage = pmType.GetMethod("SelectPackage", flags);
            if (selectPackage != null)
            {
                try { selectPackage.Invoke(window, new object[] { packageName }); return; }
                catch { /* fall through */ }
            }

            // If none succeeded, just leave the window open and notify.
            EditorUtility.DisplayDialog(
                "Open Package Manager",
                "Opened Package Manager, but couldn’t focus the package automatically.\n" +
                "Please search for it in the Package Manager.",
                "OK");
        }


        public static void AddUrpRenderFeaturesFromMaterial(string materialPath)
        {
#if HAS_URP
            AddUrpRenderFeaturesFromMaterial_Internal_URP(materialPath);
#else
            EditorUtility.DisplayDialog("ACES2 Setup",
                "Universal Render Pipeline (URP) is not installed or the URP define is missing.\n\nInstall URP 17+ via Package Manager, then try again.",
                "OK");
#endif
        }

#if HAS_URP
        private static ScriptableRendererFeature FindFeature(SerializedProperty featuresProp, string fullTypeName)
        {
            for (int i = 0; i < featuresProp.arraySize; i++)
            {
                var f = featuresProp.GetArrayElementAtIndex(i).objectReferenceValue as ScriptableRendererFeature;
                if (f && f.GetType().FullName == fullTypeName) return f;
            }
            return null;
        }

        private static ScriptableRendererFeature AddFeature(SerializedProperty featuresProp, ScriptableRendererData owner, string fullTypeName)
        {
            var type = Type.GetType(fullTypeName) ??
                       AppDomain.CurrentDomain.GetAssemblies()
                           .Select(a => a.GetType(fullTypeName, false))
                           .FirstOrDefault(x => x != null);
            if (type == null) return null;
            var feature = ScriptableObject.CreateInstance(type) as ScriptableRendererFeature;
            if (!feature) return null;
            AssetDatabase.AddObjectToAsset(feature, owner);
            featuresProp.InsertArrayElementAtIndex(featuresProp.arraySize);
            featuresProp.GetArrayElementAtIndex(featuresProp.arraySize - 1).objectReferenceValue = feature;
            return feature;
        }

        private static void AddUrpRenderFeaturesFromMaterial_Internal_URP(string materialPath)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (!mat)
            {
                EditorUtility.DisplayDialog("ACES2 Setup", $"Material not found:\n{materialPath}", "OK");
                return;
            }

            var urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (!urpAsset)
            {
                EditorUtility.DisplayDialog("ACES2 Setup", "No active URP Asset set in Project Settings → Graphics.", "OK");
                return;
            }

            var urpSO = new SerializedObject(urpAsset);
            var rendererListProp = urpSO.FindProperty("m_RendererDataList");
            if (rendererListProp == null || rendererListProp.arraySize == 0)
            {
                EditorUtility.DisplayDialog("ACES2 Setup", "URP Asset has no Renderer Data.", "OK");
                return;
            }

            var defIdxProp = urpSO.FindProperty("m_DefaultRendererIndex");
            int defaultIndex = defIdxProp != null ? Mathf.Clamp(defIdxProp.intValue, 0, rendererListProp.arraySize - 1) : 0;

            var rendererData = rendererListProp.GetArrayElementAtIndex(defaultIndex).objectReferenceValue as ScriptableRendererData;
            if (!rendererData)
            {
                EditorUtility.DisplayDialog("ACES2 Setup", "Could not access ScriptableRendererData.", "OK");
                return;
            }

            var rdSO = new SerializedObject(rendererData);
            var featuresProp = rdSO.FindProperty("m_RendererFeatures");

            const string FullscreenType = "UnityEngine.Rendering.Universal.FullScreenPassRendererFeature";
            const string TonemapperType = "Custom.ACES2.CustomTonemapperRendererFeature";

            var fullscreen = FindFeature(featuresProp, FullscreenType) ?? AddFeature(featuresProp, rendererData, FullscreenType);
            var custom = FindFeature(featuresProp, TonemapperType) ?? AddFeature(featuresProp, rendererData, TonemapperType);

            rdSO.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(rendererData);
            AssetDatabase.SaveAssets();

            if (fullscreen)
            {
                var so = new SerializedObject(fullscreen);
                SerializedProperty FindPassMatProp(SerializedObject s)
                {
                    // common current name
                    var p = s.FindProperty("m_PassMaterial");
                    if (p != null) return p;

                    // older/alt backing names seen in some 17.x drops
                    p = s.FindProperty("m_Material");
                    if (p != null) return p;

                    // nested settings container (some variants serialize this way)
                    var settings = s.FindProperty("m_Settings");
                    if (settings != null)
                    {
                        var inner = settings.FindPropertyRelative("passMaterial");
                        if (inner != null) return inner;
                    }

                    // very old fallback (just in case)
                    return s.FindProperty("passMaterial");
                }

                var nameProp = so.FindProperty("m_Name");
                var injProp = so.FindProperty("m_InjectionPoint");
                var fetchProp = so.FindProperty("m_FetchColorBuffer");
                var passMatProp = FindPassMatProp(so);

                if (nameProp != null) nameProp.stringValue = "ACES2";
                if (injProp != null) injProp.intValue = (int)RenderPassEvent.AfterRenderingPostProcessing;
                if (fetchProp != null) fetchProp.boolValue = true;
                if (passMatProp != null) passMatProp.objectReferenceValue = mat;

                // force write + refresh
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(fullscreen);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            if (custom)
            {
                var so = new SerializedObject(custom);
                var nameProp = so.FindProperty("m_Name");
                if (nameProp != null) nameProp.stringValue = "Custom Tonemapper Renderer Feature";
                var settings = so.FindProperty("settings");
                if (settings != null)
                {
                    var matProp = settings.FindPropertyRelative("tonemapperMaterial");
                    var passProp = settings.FindPropertyRelative("materialPassIndex");
                    var injProp = settings.FindPropertyRelative("injectionPoint");
                    if (matProp != null) matProp.objectReferenceValue = mat;
                    if (passProp != null) passProp.intValue = 0;
                    if (injProp != null) injProp.intValue = (int)RenderPassEvent.BeforeRendering;
                }
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("ACES2 Setup",
                "Renderer Features configured:\n\n" +
                "• Full Screen Pass: Name=ACES2, After Rendering Post Processing, FetchColorBuffer=On, PassMaterial=MAT_ACES2_Fullscreen\n" +
                "• Custom Tonemapper: Name set, Material linked, Before Rendering Post Processing",
                "OK");
        }
#endif
    }
}
