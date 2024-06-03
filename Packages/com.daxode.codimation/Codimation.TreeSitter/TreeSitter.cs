using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEditor;

namespace Codimation.TreeSitter
{
    public static class TreeSitterUtility
    {
        [DllImport("tree_sitter_unity_richtext")]
        extern static IntPtr highlight_c_sharp(IntPtr sourceCodeCString);
    
        [DllImport("tree_sitter_unity_richtext")]
        extern static IntPtr highlight_c_sharp_set_queries(IntPtr highlightQueryRaw, IntPtr localsQueryRaw);
        [DllImport("tree_sitter_unity_richtext")]
        extern static IntPtr highlight_c_sharp_set_defaults();
        
        [DllImport("tree_sitter_unity_richtext")]
        extern static void highlight_c_sharp_free(IntPtr highlightedCodeCString);

        [InitializeOnLoadMethod]
        public static void Setup()
        {
            var highlightQueryReader = new StreamReader(@"Packages\com.daxode.codimation\Codimation.TreeSitter\NativeSource~\tree-sitter-unity-richtext\queries\highlights.scm");
            var highlightQuery = highlightQueryReader.ReadToEnd();
            var highlightQueryCString = Marshal.StringToHGlobalAnsi(highlightQuery);
            var localsQueryReader = new StreamReader(@"Packages\com.daxode.codimation\Codimation.TreeSitter\NativeSource~\tree-sitter-unity-richtext\queries\locals.scm");
            var localsQuery = localsQueryReader.ReadToEnd();
            var localsQueryCString = Marshal.StringToHGlobalAnsi(localsQuery);
            
            highlight_c_sharp_set_queries(highlightQueryCString, localsQueryCString);
            
            Marshal.FreeHGlobal(highlightQueryCString);
            Marshal.FreeHGlobal(localsQueryCString);
            highlightQueryReader.Close();
            localsQueryReader.Close();
        }
        
        public static string Highlight(string sourceCode)
        {
            var sourceCodeCString = Marshal.StringToHGlobalAnsi(sourceCode);
            var highlightedCodeCString = highlight_c_sharp(sourceCodeCString);
            Marshal.FreeHGlobal(sourceCodeCString);
            var highlightedCode = Marshal.PtrToStringAnsi(highlightedCodeCString);
            highlight_c_sharp_free(highlightedCodeCString);
            return highlightedCode;
        }

        public static string HighlightWithLineNumbers(string sourceCode)
        {
            var highlightedString = Highlight(sourceCode);
        
            // Seek through every new line adding line numbers
            var stringBuilder = new System.Text.StringBuilder();
            var currentSeek = 0;
            var currentLine = 1;
            while (currentSeek < highlightedString.Length)
            {
                var newLineIndex = highlightedString.IndexOf('\n', currentSeek);
                if (newLineIndex == -1)
                {
                    newLineIndex = highlightedString.Length;
                }
                var line = highlightedString.Substring(currentSeek, newLineIndex - currentSeek);
                stringBuilder.Append($"<color=white>{currentLine++}: </color>{line}\n");
                currentSeek = newLineIndex + 1;
            }
        
            return stringBuilder.ToString();
        }
    }
}