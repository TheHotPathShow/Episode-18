using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

[assembly:InternalsVisibleTo("Codimation.TreeSitter")]

namespace UnityInternalsWrapper
{
    internal static class AssetDatabase
    {
        public static Hash128 GetSourceAssetFileHash(string assetPath)
        {
            return UnityEditor.AssetDatabase.GetSourceAssetFileHash(assetPath);
        }
    }
    
    // UnityEngine.GUIClip.visibleRect is internal
    internal static class GUIClip
    {
        public static Rect visibleRect => UnityEngine.GUIClip.visibleRect;
    }
    
    internal static class EditorExtension
    {
        public static void SetInternalAlwaysAllowExpansion(this Editor editor, bool value) => editor.alwaysAllowExpansion = value;
        public static string GetInternalTargetTitle(this Editor editor) => editor.targetTitle;
    }
    
    internal static class TextAssetExtension
    {
        public static string GetInternalPreview(this TextAsset textAsset, int maxChars) => textAsset.GetPreview(maxChars);
    }
}