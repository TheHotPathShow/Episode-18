using Codimation.TreeSitter;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityInternalsWrapper;
using AssetDatabase = UnityEditor.AssetDatabase;

#if TREE_SITTER_INSPECTOR
using System;
using System.IO;
#endif

static class TreeSitterPrefencesGUI
{
    [SettingsProvider]
    public static SettingsProvider CreateSettingsProvider()
    {
        var provider = new SettingsProvider("Preferences/Codimation/Script Inspector", SettingsScope.User)
        {
            label = "Script Inspector",
            guiHandler = _ =>
            {
                var currentNamedBuildTarget = NamedBuildTarget
                    .FromBuildTargetGroup(BuildPipeline
                        .GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget));

                // Add title to the window and a description under the title
                EditorGUILayout.LabelField("Script Inspector", EditorStyles.boldLabel);
                
                // Get the current scripting define symbols
                var symbols = PlayerSettings.GetScriptingDefineSymbols(currentNamedBuildTarget);
                var hasTreeSitterInspectorDefine = symbols.Contains("TREE_SITTER_INSPECTOR");
                var hasTreeSitterInspector = EditorGUILayout.Toggle("Enable .cs inspector", hasTreeSitterInspectorDefine);
                EditorGUILayout.HelpBox("Enable the .cs inspector to highlight the syntax of your scripts. It is based on TreeSitter.", MessageType.Info);
                
                // Set the TreeSitterInspector define symbol
                if (hasTreeSitterInspector && !hasTreeSitterInspectorDefine)
                {
                    symbols += ";TREE_SITTER_INSPECTOR";
                    PlayerSettings.SetScriptingDefineSymbols(currentNamedBuildTarget, symbols);
                    AssetDatabase.Refresh();
                }
                else if (!hasTreeSitterInspector && hasTreeSitterInspectorDefine)
                {
                    symbols = symbols.Replace("TREE_SITTER_INSPECTOR", "").Replace(";;", ";");
                    PlayerSettings.SetScriptingDefineSymbols(currentNamedBuildTarget, symbols);
                    AssetDatabase.Refresh();
                }
            },
            keywords = new[] { "Tree Sitter", "Script Inspector" }
        };
        return provider;
    }
}

#if TREE_SITTER_INSPECTOR
[CustomEditor(typeof(MonoScript))]
[CanEditMultipleObjects]
public class ScriptInspector : Editor
{
    const int kMaxChars = 7000;
    [NonSerialized]
    GUIStyle m_TextStyle;
    TextAsset m_TextAsset;
    GUIContent m_CachedPreview;
    GUID m_AssetGUID;
    Hash128 m_LastDependencyHash;

    public void OnEnable()
    {
        this.SetInternalAlwaysAllowExpansion(true);
        m_TextAsset = target as TextAsset;
        m_AssetGUID = AssetDatabase.GUIDFromAssetPath(AssetDatabase.GetAssetPath(m_TextAsset));
        CachePreview();
    }

    public override void OnInspectorGUI()
    {
        if (targets.Length == 1)
        {
            var assetPath = AssetDatabase.GetAssetPath(target);
            var assemblyName = UnityEditor.Compilation.CompilationPipeline.GetAssemblyNameFromScriptPath(assetPath);
            // assemblyName is null for MonoScript's inside assemblies.
            if (assemblyName != null)
            {
                GUILayout.Label("Assembly Information", EditorStyles.boldLabel);

                EditorGUILayout.LabelField("Filename", assemblyName);

                var assemblyDefinitionFile = UnityEditor.Compilation.CompilationPipeline.GetAssemblyDefinitionFilePathFromScriptPath(assetPath);

                if (assemblyDefinitionFile != null)
                {
                    var assemblyDefinitionFileAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(assemblyDefinitionFile);

                    using (new EditorGUI.DisabledScope(true))
                    {
                        EditorGUILayout.ObjectField("Definition File", assemblyDefinitionFileAsset, typeof(TextAsset), false);
                    }
                }

                EditorGUILayout.Space();
            }
        }

        if (m_TextStyle == null)
        {
            m_TextStyle = "ScriptText";
        }
        m_TextStyle.richText = true;

        var dependencyHash = UnityInternalsWrapper.AssetDatabase.GetSourceAssetFileHash(m_AssetGUID.ToString());
        if (m_LastDependencyHash != dependencyHash)
        {
            CachePreview();
            m_LastDependencyHash = dependencyHash;
        }

        bool enabledTemp = GUI.enabled;
        GUI.enabled = true;
        if (m_TextAsset != null)
        {
            Rect rect = GUILayoutUtility.GetRect(m_CachedPreview, m_TextStyle);
            rect.x = 0;
            rect.y -= 3;
            rect.width = GUIClip.visibleRect.width;
            GUI.Box(rect, "");
            EditorGUI.SelectableLabel(rect, m_CachedPreview.text, m_TextStyle);
        }
        GUI.enabled = enabledTemp;
    }

    void CachePreview()
    {
        string text = string.Empty;

        if (m_TextAsset != null)
        {
            if (targets.Length > 1)
            {
                text = this.GetInternalTargetTitle();
            }
            else if (Path.GetExtension(AssetDatabase.GetAssetPath(m_TextAsset)) != ".bytes")
            {
                text = m_TextAsset.GetInternalPreview(kMaxChars);
                if (text!.Length >= kMaxChars)
                    text = text.Substring(0, kMaxChars) + "...\n\n<...etc...>";
                text = TreeSitterUtility.Highlight(text);
            }
            else
            {
                text = $"{EditorUtility.FormatBytes(m_TextAsset.dataSize)} size .bytes file";
            }
        }

        m_CachedPreview = new GUIContent(text);
    }
}
#endif