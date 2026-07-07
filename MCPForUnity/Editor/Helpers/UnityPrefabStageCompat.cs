using System;
using System.Reflection;
using UnityEditor;
#if UNITY_2020_1_OR_NEWER
using UnityEditor.SceneManagement;
#else
using UnityEditor.Experimental.SceneManagement;
#endif

namespace MCPForUnity.Editor.Helpers
{
    /// <summary>
    /// Version-compatible wrapper for the Prefab Stage API.
    ///
    /// API timeline:
    ///   Pre-2020.1 : PrefabStageUtility lives in UnityEditor.Experimental.SceneManagement,
    ///                and the public PrefabStage.assetPath property / PrefabStageUtility.OpenPrefab
    ///                did not exist yet.
    ///   2020.1+     : PrefabStageUtility moved to UnityEditor.SceneManagement, PrefabStage.assetPath
    ///                became public, and PrefabStageUtility.OpenPrefab(string) was introduced.
    ///
    /// The calling convention (PrefabStageUtility.GetCurrentPrefabStage / GetPrefabStage) is
    /// identical across versions, so call sites only need the namespace alias shipped in
    /// ManagePrefabs.cs (and added to sibling files). The members that genuinely differ are
    /// handled here via reflection / fallback so the package compiles and runs on 2019.4.
    /// </summary>
    public static class UnityPrefabStageCompat
    {
        /// <summary>
        /// Opens a prefab asset in the Prefab Stage. On Unity 2020.1+ this dispatches to
        /// <c>PrefabStageUtility.OpenPrefab(string)</c>; on 2019.x that API did not exist,
        /// so we fall back to <c>AssetDatabase.OpenAsset</c> which opens the prefab in
        /// isolation mode, then return the now-current PrefabStage.
        /// </summary>
        public static PrefabStage OpenPrefab(string assetPath)
        {
#if UNITY_2020_1_OR_NEWER
            return PrefabStageUtility.OpenPrefab(assetPath);
#else
            var obj = AssetDatabase.LoadMainAssetAtPath(assetPath);
            if (obj != null)
            {
                AssetDatabase.OpenAsset(obj);
            }
            return PrefabStageUtility.GetCurrentPrefabStage();
#endif
        }

        /// <summary>
        /// Returns the asset path of a PrefabStage. The public <c>assetPath</c> property was
        /// added in Unity 2020.1; in 2019.x the value lives on <c>prefabAssetPath</c> (which
        /// may be non-public). Fail-soft via reflection across versions.
        /// </summary>
        public static string GetAssetPath(PrefabStage stage)
        {
            if (stage == null) return null;
            var type = stage.GetType();
            var prop = type.GetProperty("assetPath", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                      ?? type.GetProperty("prefabAssetPath", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            try { return prop?.GetValue(stage) as string; }
            catch { return null; }
        }

        /// <summary>
        /// Returns the editing mode of a PrefabStage. The <c>mode</c> property (and the
        /// in-context editing concept) was introduced in Unity 2020.1. In 2019.x only
        /// isolation-mode editing exists, so we reflectively read <c>mode</c> if present
        /// and otherwise fall back to "InIsolation".
        /// </summary>
        public static string GetMode(PrefabStage stage)
        {
            if (stage == null) return null;
#if UNITY_2020_1_OR_NEWER
            return stage.mode.ToString();
#else
            var prop = stage.GetType().GetProperty("mode", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            try { return prop?.GetValue(stage)?.ToString() ?? "InIsolation"; }
            catch { return "InIsolation"; }
#endif
        }
    }
}
