
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Globalization;

namespace Custom.ACES2.Editor
{
    public class Aces2LutImporter : EditorWindow
    {
        [MenuItem("Tools/ACES2/Import 3D LUT/ (.cube || .spi3d)")]
        public static void ImportLUT()
        {
            string path = EditorUtility.OpenFilePanel("Select 3D LUT", "", "cube,spi3d");
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                Texture3D tex = null;
                int size = 0;

                if (path.EndsWith(".cube"))
                {
                    tex = ImportCube(path, out size);
                }
                else if (path.EndsWith(".spi3d"))
                {
                    tex = ImportSpi3d(path, out size);
                }
                else
                {
                    EditorUtility.DisplayDialog("ACES 2 LUT Importer", "Unsupported file type. Use .cube or .spi3d", "OK");
                    return;
                }

                if (tex == null)
                {
                    EditorUtility.DisplayDialog("ACES 2 LUT Importer", "Failed to parse LUT.", "OK");
                    return;
                }

                string savePath = EditorUtility.SaveFilePanelInProject("Save Texture3D", $"ACES2_{size}", "asset", "Choose a save location");
                if (string.IsNullOrEmpty(savePath)) return;

                AssetDatabase.CreateAsset(tex, savePath);
                AssetDatabase.SaveAssets();
                EditorUtility.DisplayDialog("ACES 2 LUT Importer", $"Imported {size}^3 3D LUT", "OK");
            }
            catch (System.Exception ex)
            {
                Debug.LogError("ACES2 LUT Import failed: " + ex);
                EditorUtility.DisplayDialog("ACES 2 LUT Importer", "Error: " + ex.Message, "OK");
            }
        }

        static Texture3D ImportCube(string path, out int size)
        {
            size = 0;
            var lines = File.ReadAllLines(path);
            int lutSize = 0;
            var data = new List<Color>();

            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (string.IsNullOrEmpty(line)) continue;
                if (line.StartsWith("#")) continue;

                if (line.StartsWith("LUT_3D_SIZE"))
                {
                    var parts = line.Split(new char[]{' ', '\t'}, System.StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2) int.TryParse(parts[1], out lutSize);
                    continue;
                }

                if (char.IsLetter(line[0])) continue; // skip TITLE, DOMAIN_MIN/MAX, etc.

                var comps = line.Split(new char[]{' ', '\t'}, System.StringSplitOptions.RemoveEmptyEntries);
                if (comps.Length >= 3)
                {
                    float r = float.Parse(comps[0], CultureInfo.InvariantCulture);
                    float g = float.Parse(comps[1], CultureInfo.InvariantCulture);
                    float b = float.Parse(comps[2], CultureInfo.InvariantCulture);
                    data.Add(new Color(r, g, b, 1f));
                }
            }

            if (lutSize <= 0) throw new System.Exception("Invalid LUT_3D_SIZE in .cube");
            int expected = lutSize * lutSize * lutSize;
            if (data.Count != expected) throw new System.Exception($".cube entries {data.Count} != expected {expected}");

            var tex = new Texture3D(lutSize, lutSize, lutSize, TextureFormat.RGBAHalf, false);
            tex.name = $"LUT_{lutSize}";
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            tex.SetPixels(data.ToArray());
            tex.Apply(updateMipmaps: false, makeNoLongerReadable: false);

            size = lutSize;
            return tex;
        }

        static Texture3D ImportSpi3d(string path, out int size)
        {
            size = 0;
            var lines = File.ReadAllLines(path);
            int lutSize = 0;
            bool started = false;
            var data = new List<Color>();

            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (string.IsNullOrEmpty(line)) continue;
                if (line.StartsWith("#")) continue;
                if (line.StartsWith("SPILUT")) continue;

                if (line.StartsWith("3D_SIZE"))
                {
                    var parts = line.Split(new char[]{' ', '\t'}, System.StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2) int.TryParse(parts[1], out lutSize);
                    continue;
                }

                if (line.StartsWith("BEGIN_DATA"))
                {
                    started = true;
                    continue;
                }
                if (line.StartsWith("END_DATA"))
                {
                    started = false;
                    break;
                }

                if (!started) continue;

                var comps = line.Split(new char[]{' ', '\t'}, System.StringSplitOptions.RemoveEmptyEntries);
                if (comps.Length >= 3)
                {
                    float r = float.Parse(comps[0], CultureInfo.InvariantCulture);
                    float g = float.Parse(comps[1], CultureInfo.InvariantCulture);
                    float b = float.Parse(comps[2], CultureInfo.InvariantCulture);
                    data.Add(new Color(r, g, b, 1f));
                }
            }

            if (lutSize <= 0) throw new System.Exception("Invalid 3D_SIZE in .spi3d");
            int expected = lutSize * lutSize * lutSize;
            if (data.Count != expected) throw new System.Exception($".spi3d entries {data.Count} != expected {expected}");

            var tex = new Texture3D(lutSize, lutSize, lutSize, TextureFormat.RGBAHalf, false);
            tex.name = $"LUT_{lutSize}";
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            tex.SetPixels(data.ToArray());
            tex.Apply(updateMipmaps: false, makeNoLongerReadable: false);

            size = lutSize;
            return tex;
        }
    }
}
