using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BlueLike.EditorTools
{
    /// <summary>
    /// Editor helpers for getting an imported character onto the toon shader quickly.
    ///
    /// The manual version of this job is: select every material, change the shader, re-assign
    /// the base map, decide which one is hair and which is the face, then author a ramp and a
    /// jitter map by hand. These menu items do all of that in one click so you can judge the
    /// look instead of fighting the inspector.
    /// </summary>
    public static class ToonMaterialTools
    {
        private const string ToonShaderName = "BlueLike/AnimeToon";
        private const string HairShaderName = "BlueLike/AnimeToonHair";
        private const string GeneratedFolder = "Assets/BlueLike/Generated";

        // Substrings used to guess what a material is for. Extend these for your own naming.
        private static readonly string[] HairHints = { "hair", "kami", "머리", "헤어" };
        private static readonly string[] FaceHints = { "face", "facial", "kao", "얼굴", "페이스" };
        private static readonly string[] EyeHints = { "eye", "iris", "pupil", "highlight", "눈" };

        [MenuItem("Tools/BlueLike/Convert Selection To Toon Shader")]
        private static void ConvertSelection()
        {
            var materials = new HashSet<Material>();

            foreach (Object obj in Selection.objects)
            {
                if (obj is Material m)
                {
                    materials.Add(m);
                    continue;
                }
                if (obj is GameObject go)
                {
                    foreach (Renderer r in go.GetComponentsInChildren<Renderer>(true))
                    {
                        foreach (Material mat in r.sharedMaterials)
                        {
                            if (mat != null) materials.Add(mat);
                        }
                    }
                }
            }

            if (materials.Count == 0)
            {
                EditorUtility.DisplayDialog("BlueLike",
                    "Select a character GameObject (or some materials) first.", "OK");
                return;
            }

            Shader toon = Shader.Find(ToonShaderName);
            Shader hair = Shader.Find(HairShaderName);
            if (toon == null)
            {
                EditorUtility.DisplayDialog("BlueLike",
                    "Could not find the shader '" + ToonShaderName + "'. Is the Shaders folder imported?", "OK");
                return;
            }

            int converted = 0;
            foreach (Material mat in materials)
            {
                string lower = mat.name.ToLowerInvariant();

                // Grab the base map and colour before the shader swap drops them.
                Texture baseMap = GetFirstTexture(mat);
                Color baseColor = GetBaseColor(mat);

                bool isHair = ContainsAny(lower, HairHints);
                bool isFace = !isHair && ContainsAny(lower, FaceHints);
                bool isEye = ContainsAny(lower, EyeHints);

                Undo.RecordObject(mat, "Convert To Toon");
                mat.shader = isHair && hair != null ? hair : toon;

                if (baseMap != null && mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", baseMap);
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);

                if (mat.HasProperty("_IsFace")) mat.SetFloat("_IsFace", isFace ? 1f : 0f);

                // Eyes want no outline and a flatter read, otherwise the iris gets a black ring.
                if (isEye)
                {
                    if (mat.HasProperty("_OutlineWidth")) mat.SetFloat("_OutlineWidth", 0f);
                    if (mat.HasProperty("_ShadowThreshold")) mat.SetFloat("_ShadowThreshold", 0.05f);
                    if (mat.HasProperty("_SpecIntensity")) mat.SetFloat("_SpecIntensity", 0f);
                }

                // The face should never receive a hard terminator from mesh normals.
                if (isFace)
                {
                    if (mat.HasProperty("_OutlineWidth")) mat.SetFloat("_OutlineWidth", 0.8f);
                    if (mat.HasProperty("_SpecIntensity")) mat.SetFloat("_SpecIntensity", 0.08f);
                    if (mat.HasProperty("_ReceivedShadowStrength")) mat.SetFloat("_ReceivedShadowStrength", 0.25f);
                }

                EditorUtility.SetDirty(mat);
                converted++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[BlueLike] Converted " + converted + " material(s) to the toon shader.");
        }

        [MenuItem("Tools/BlueLike/Generate Shadow Ramp Texture")]
        private static void GenerateRamp()
        {
            const int width = 256;
            var tex = new Texture2D(width, 1, TextureFormat.RGBA32, false, true);

            for (int x = 0; x < width; x++)
            {
                float t = x / (float)(width - 1);
                // Two hard bands with a narrow blend, which is the classic anime read:
                // core shadow, mid tone, light.
                float v;
                if (t < 0.46f) v = 0f;
                else if (t < 0.50f) v = Mathf.SmoothStep(0f, 0.55f, (t - 0.46f) / 0.04f);
                else if (t < 0.72f) v = 0.55f;
                else if (t < 0.76f) v = Mathf.Lerp(0.55f, 1f, (t - 0.72f) / 0.04f);
                else v = 1f;

                tex.SetPixel(x, 0, new Color(v, v, v, 1f));
            }
            tex.Apply();

            string path = SaveTexture(tex, "ShadowRamp.png");
            ConfigureDataTexture(path, TextureWrapMode.Clamp);
            Debug.Log("[BlueLike] Ramp written to " + path +
                      ". Assign it to the material's Shadow Ramp slot and tick 'Use Ramp Texture'.");
        }

        [MenuItem("Tools/BlueLike/Generate Hair Jitter Map")]
        private static void GenerateJitter()
        {
            const int size = 256;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false, true);

            // Vertical streaks: the highlight band breaks along the strand direction, which
            // is what stops the hair looking like moulded plastic.
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float n = Mathf.PerlinNoise(x * 0.09f, y * 0.012f);
                    float fine = Mathf.PerlinNoise(x * 0.42f, y * 0.004f) * 0.35f;
                    float v = Mathf.Clamp01(n * 0.7f + fine);
                    tex.SetPixel(x, y, new Color(v, v, v, 1f));
                }
            }
            tex.Apply();

            string path = SaveTexture(tex, "HairJitter.png");
            ConfigureDataTexture(path, TextureWrapMode.Repeat);
            Debug.Log("[BlueLike] Jitter map written to " + path +
                      ". Assign it to the hair material's Jitter / Shift Map slot.");
        }

        [MenuItem("Tools/BlueLike/Generate Face SDF Starter Map")]
        private static void GenerateFaceSdf()
        {
            const int size = 512;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false, true);

            // A left-to-right gradient is the correct *starting point*, not the finished map.
            // The real work is painting this by hand so the value at each pixel says "the
            // light must swing this far before I fall into shadow".
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float t = x / (float)(size - 1);
                    tex.SetPixel(x, y, new Color(t, t, t, 1f));
                }
            }
            tex.Apply();

            string path = SaveTexture(tex, "FaceSDF_Starter.png");
            ConfigureDataTexture(path, TextureWrapMode.Clamp);
            Debug.Log("[BlueLike] Face SDF starter written to " + path +
                      ". This is a placeholder gradient - see docs/02-character-pipeline.md " +
                      "for how to paint the real one.");
        }

        // ---------------------------------------------------------------------------------
        private static string SaveTexture(Texture2D tex, string fileName)
        {
            if (!Directory.Exists(GeneratedFolder)) Directory.CreateDirectory(GeneratedFolder);
            string path = Path.Combine(GeneratedFolder, fileName).Replace("\\", "/");
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            return path;
        }

        private static void ConfigureDataTexture(string path, TextureWrapMode wrap)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;

            // These are lookup tables, not pictures: no colour conversion, no mips, no
            // compression artefacts, or the shadow terminator will crawl with banding.
            importer.sRGBTexture = false;
            importer.mipmapEnabled = false;
            importer.wrapMode = wrap;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        private static bool ContainsAny(string value, string[] hints)
        {
            for (int i = 0; i < hints.Length; i++)
            {
                if (value.Contains(hints[i])) return true;
            }
            return false;
        }

        private static Texture GetFirstTexture(Material mat)
        {
            string[] candidates = { "_BaseMap", "_MainTex", "_Albedo", "_Diffuse" };
            for (int i = 0; i < candidates.Length; i++)
            {
                if (mat.HasProperty(candidates[i]))
                {
                    Texture t = mat.GetTexture(candidates[i]);
                    if (t != null) return t;
                }
            }
            return null;
        }

        private static Color GetBaseColor(Material mat)
        {
            string[] candidates = { "_BaseColor", "_Color", "_MainColor" };
            for (int i = 0; i < candidates.Length; i++)
            {
                if (mat.HasProperty(candidates[i])) return mat.GetColor(candidates[i]);
            }
            return Color.white;
        }
    }
}
