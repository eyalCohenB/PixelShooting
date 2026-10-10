using System.IO;
using UnityEditor;
using UnityEngine;

namespace PixelShooting.EditorTools
{
    /// <summary>
    /// Builds the submission .unitypackage ("&lt;name&gt;_unity_home_exercise"): every asset under
    /// Assets/, plus the two project settings the game depends on (sorting layers in TagManager,
    /// and the scene list in EditorBuildSettings). The repo docs (README.md, AI_USAGE.md) live
    /// outside Assets/, so they are copied into Assets/_Project/Docs first to travel with it.
    /// </summary>
    public static class SubmissionExporter
    {
        private const string DefaultName = "Eyal_unity_home_exercise";
        private const string DocsFolder = "Assets/_Project/Docs";
        private static readonly string[] Docs = { "README.md", "AI_USAGE.md" };

        [MenuItem("PixelShooting/Export Submission Package...")]
        private static void Export()
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string path = EditorUtility.SaveFilePanel("Export submission package", projectRoot, DefaultName, "unitypackage");
            if (string.IsNullOrEmpty(path)) return;

            CopyDocs(projectRoot);

            string[] paths =
            {
                "Assets",
                "ProjectSettings/TagManager.asset",        // sorting layers: Background, Board, Gameplay, FX
                "ProjectSettings/EditorBuildSettings.asset", // Game.unity in the build list
            };
            AssetDatabase.ExportPackage(paths, path, ExportPackageOptions.Recurse);
            Debug.Log($"Submission package exported: {path}");
            EditorUtility.RevealInFinder(path);
        }

        private static void CopyDocs(string projectRoot)
        {
            Directory.CreateDirectory(Path.Combine(projectRoot, DocsFolder));
            foreach (string doc in Docs)
            {
                string from = Path.Combine(projectRoot, doc);
                if (!File.Exists(from)) continue;
                File.Copy(from, Path.Combine(projectRoot, DocsFolder, doc), true);
            }
            AssetDatabase.Refresh();
        }
    }
}
