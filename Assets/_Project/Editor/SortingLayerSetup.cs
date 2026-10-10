using UnityEditor;

namespace PixelShooting.EditorTools
{
    /// <summary>
    /// Safety net for importing the submission package into a fresh project: the game draws its
    /// sprites on the sorting layers Background, Board, Gameplay and FX (in that order). They live
    /// in ProjectSettings, not Assets, so if they're missing this adds them (after Default, in order,
    /// with the original IDs) when the editor loads.
    /// </summary>
    [InitializeOnLoad]
    public static class SortingLayerSetup
    {
        // Names and the exact unique IDs from this project's TagManager: scenes and prefabs store a
        // renderer's sorting layer by ID, so recreated layers must keep the same IDs to match.
        private static readonly string[] Required = { "Background", "Board", "Gameplay", "FX" };
        private static readonly uint[] Ids = { 2489588563u, 194370915u, 2010411871u, 3714432973u };

        static SortingLayerSetup()
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = tagManager.FindProperty("m_SortingLayers");
            bool changed = false;

            for (int r = 0; r < Required.Length; r++)
            {
                string name = Required[r];
                if (Exists(layers, name)) continue;
                int index = layers.arraySize;
                layers.InsertArrayElementAtIndex(index);
                SerializedProperty layer = layers.GetArrayElementAtIndex(index);
                layer.FindPropertyRelative("name").stringValue = name;
                layer.FindPropertyRelative("uniqueID").intValue = unchecked((int)Ids[r]);
                changed = true;
            }

            if (changed)
            {
                tagManager.ApplyModifiedPropertiesWithoutUndo();
                UnityEngine.Debug.Log("PixelShooting: added missing sorting layers (Background, Board, Gameplay, FX).");
            }
        }

        private static bool Exists(SerializedProperty layers, string name)
        {
            for (int i = 0; i < layers.arraySize; i++)
            {
                if (layers.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == name) return true;
            }
            return false;
        }
    }
}
