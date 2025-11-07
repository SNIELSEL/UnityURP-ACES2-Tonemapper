using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ACES2.EditorTools
{
    public static class ACES2Installer
    {
        public const string TargetMatAssetName = "MAT_ACES2_Fullscreen.mat";
        public const string TargetLutAssetName = "ACES2_33.asset";

        static readonly string[] CandidateRoots =
        {
            "Assets/Custom/ACES2/Samples/ACES2/SceneTemplateAssets/ACES2Stuff",
            "Assets/Samples"
        };

        public static bool TryFindSampleAssets(out string matPath, out string lutPath)
        {
            matPath = null;
            lutPath = null;

            foreach (var root in CandidateRoots)
            {
                if (!AssetDatabase.IsValidFolder(root)) continue;

                if (root.EndsWith("/Samples"))
                {
                    var matches = AssetDatabase.FindAssets("t:Material MAT_ACES2_Fullscreen", new[] { root });
                    if (matches != null && matches.Length > 0)
                    {
                        var p = AssetDatabase.GUIDToAssetPath(matches[0]);
                        var folder = Path.GetDirectoryName(p).Replace("\\", "/");
                        var lp = Path.Combine(folder, TargetLutAssetName).Replace("\\", "/");
                        if (File.Exists(lp))
                        {
                            matPath = p;
                            lutPath = lp;
                            return true;
                        }
                    }
                }
                else
                {
                    var mp = Path.Combine(root, TargetMatAssetName).Replace("\\", "/");
                    var lp = Path.Combine(root, TargetLutAssetName).Replace("\\", "/");
                    if (File.Exists(mp) && File.Exists(lp))
                    {
                        matPath = mp;
                        lutPath = lp;
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool EnsureLutLinkedOnMaterial(string matPath, string lutPath)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            var lut = AssetDatabase.LoadAssetAtPath<Texture3D>(lutPath);
            if (mat == null || lut == null) return false;
            mat.SetTexture("_Aces2Lut", lut);
            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            return true;
        }

        public static void OpenPackageManagerToPackage(string packageNameOrDisplayName)
        {
            var t = Type.GetType("UnityEditor.PackageManager.UI.Window, UnityEditor.PackageManagerUIModule");
            if (t != null)
            {
                var mOpen = t.GetMethod("Open", new[] { typeof(string) });
                if (mOpen != null)
                {
                    mOpen.Invoke(null, new object[] { packageNameOrDisplayName });
                    return;
                }
            }
            EditorApplication.ExecuteMenuItem("Window/Package Manager");
        }

        public static void AddUrpRenderFeaturesFromMaterial(string projectMaterialPath)
        {
            var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urp == null) { EditorUtility.DisplayDialog("ACES2", "No Universal Render Pipeline asset is active.", "OK"); return; }

            var rd = GetDefaultRendererData(urp);
            if (rd == null) { EditorUtility.DisplayDialog("ACES2", "Could not access the default URP Renderer Data.", "OK"); return; }

            var mat = AssetDatabase.LoadAssetAtPath<Material>(projectMaterialPath);
            if (mat == null) { EditorUtility.DisplayDialog("ACES2", $"Material not found:\n{projectMaterialPath}", "OK"); return; }

            var custom = EnsureFeature<Custom.ACES2.CustomTonemapperRendererFeature>(rd, "Custom Tonemapper Renderer Feature");
            if (custom != null)
            {
                var so = new SerializedObject(custom);
                so.FindProperty("settings").FindPropertyRelative("tonemapperMaterial").objectReferenceValue = mat;
                so.FindProperty("settings").FindPropertyRelative("materialPassIndex").intValue = 0;
                so.FindProperty("settings").FindPropertyRelative("injectionPoint").enumValueIndex = (int)RenderPassEvent.BeforeRendering;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            var fsType = Type.GetType("UnityEngine.Rendering.Universal.FullScreenPassRendererFeature, Unity.RenderPipelines.Universal.Runtime");
            if (fsType != null)
            {
                var fullScreen = EnsureFeature(rd, fsType, "ACES2");
                if (fullScreen != null)
                {
                    var so = new SerializedObject(fullScreen);
                    SetIfExists(so, "m_Name", "ACES2");
                    SetIfExists(so, "m_InjectionPoint", (int)RenderPassEvent.AfterRenderingPostProcessing);
                    SetIfExists(so, "m_FetchColorBuffer", true);
                    SetIfExists(so, "m_PassMaterial", mat);
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            EditorUtility.SetDirty(rd);
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("ACES2", "URP Renderer Features added/updated.", "OK");
        }

        static ScriptableRendererData GetDefaultRendererData(UniversalRenderPipelineAsset urp)
        {
            var so = new SerializedObject(urp);
            var list = so.FindProperty("m_RendererDataList");
            var indexProp = so.FindProperty("m_DefaultRendererIndex");
            if (list == null || !list.isArray) return null;
            var idx = Mathf.Clamp(indexProp.intValue, 0, list.arraySize - 1);
            return list.GetArrayElementAtIndex(idx).objectReferenceValue as ScriptableRendererData;
        }

        static T EnsureFeature<T>(ScriptableRendererData rd, string name) where T : ScriptableRendererFeature
        {
            var f = rd.rendererFeatures.FirstOrDefault(x => x is T) as T;
            if (f != null) return f;
            f = ScriptableObject.CreateInstance<T>();
            f.name = name;
            rd.rendererFeatures.Add(f);
#if UNITY_6000_0_OR_NEWER
            rd.SetDirty();
#endif
            AssetDatabase.AddObjectToAsset(f, rd);
            return f;
        }

        static ScriptableRendererFeature EnsureFeature(ScriptableRendererData rd, Type type, string name)
        {
            var f = rd.rendererFeatures.FirstOrDefault(x => x != null && x.GetType() == type);
            if (f != null) return f;
            f = ScriptableObject.CreateInstance(type) as ScriptableRendererFeature;
            f.name = name;
            rd.rendererFeatures.Add(f);
#if UNITY_6000_0_OR_NEWER
            rd.SetDirty();
#endif
            AssetDatabase.AddObjectToAsset(f, rd);
            return f;
        }

        static void SetIfExists(SerializedObject so, string propName, object value)
        {
            var p = so.FindProperty(propName);
            if (p == null) return;
            if (value is int i) p.intValue = i;
            else if (value is bool b) p.boolValue = b;
            else if (value is UnityEngine.Object o) p.objectReferenceValue = o;
            else if (value is string s) p.stringValue = s;
        }
    }
}
