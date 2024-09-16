using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;

namespace Jetpack.DebugCode
{
    public static class MaterialShaderFinder
    {
        public const bool SHOULDREPORT = true;

        public static void Report()
        {
            if (!SHOULDREPORT)
                return;

            ReportResource_Materials();
            ReportResource_Shaders();       // Shader.Find("Sprites/Default") returns a valid shader, but Resources.LoadAll<Shader>("") doesn't return that (unless it's a different name)

            InvestigateUnlitShader();
        }

        private static void ReportResource_Materials()
        {
            Material[] materials = Resources.LoadAll<Material>(""); // The empty string "" means it will load all materials from the "Resources" folder and its subfolders.

            Debug.Log("Resources.LoadAll<Material>(\"\");");

            foreach (Material material in materials)
                Debug.Log($"material: '{material.name}'");
        }
        private static void ReportResource_Shaders()
        {
            Shader[] shaders = Resources.LoadAll<Shader>("");

            Debug.Log("Resources.LoadAll<Shader>(\"\");");

            foreach (Shader shader in shaders)
                Debug.Log($"shader: {shader.name}");
        }

        // These functions are used to strings that work with Resources.Load<Material>("...");
        private static void ReportResourceMaterials()
        {
            Material[] materials = Resources.LoadAll<Material>(""); // The empty string "" means it will load all materials from the "Resources" folder and its subfolders.

            string[] paths = materials.
                //Select(o => AssetDatabase.GetAssetPath(o)).
                //Select(o => "AssetDatabase is only available in UnityEditor").
                Select(o => o.name).
                SelectMany(o => SplitExtension(o)).
                SelectMany(o => GetAltPaths(o)).
                ToArray();

            var found = paths.
                Select(o => new
                {
                    path = o,
                    mat = Resources.Load<Material>(o),
                }).
                Where(o => o.mat != null).
                ToArray();

            //string report = string.Join("\n", found.Select(o => o.path));

            foreach (var item in found)
                Debug.Log(item.path);
        }
        private static string[] SplitExtension(string path)
        {
            Match match = Regex.Match(path, @"\.\w+$");

            if (!match.Success)
                return new[] { path };

            return new[]
            {
                path,
                path.Substring(0, match.Index),
            };
        }
        private static string[] GetAltPaths(string path)
        {
            string[] name_split = path.Split('/');

            string[] retVal = new string[name_split.Length];

            for (int i = 0; i < name_split.Length; i++)
                retVal[i] = string.Join("/", Enumerable.Range(i, name_split.Length - i).Select(o => name_split[o]));
            return retVal;
        }

        private static void InvestigateUnlitShader()
        {
            Shader shader = Shader.Find("Sprites/Default");

            if (shader == null)
                Debug.Log("Shader.Find(\"Sprites/Default\") returned null");

            else
                Debug.Log($"Shader.Find(\"Sprites/Default\") returned {shader.name}");
        }
    }
}
