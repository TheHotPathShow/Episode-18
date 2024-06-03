// #define USING_V1

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Codimation.TreeSitter
{
    public static class TreeSitterUtility
    {
        [DllImport("tree_sitter_unity_richtext")]
        extern static CodeSnippets highlight_c_sharp(IntPtr sourceCodeCString);
        [DllImport("tree_sitter_unity_richtext")]
        extern static IntPtr highlight_c_sharp_v2(IntPtr sourceCodeCString);
    
        [DllImport("tree_sitter_unity_richtext")]
        extern static IntPtr highlight_c_sharp_v2_manual(IntPtr sourceCodeCString, IntPtr highlightQueryRaw, IntPtr localsQueryRaw);
    
        public static string Highlight(string sourceCode)
        {
#if USING_V1
        var sourceCodeCString = Marshal.StringToHGlobalAnsi(sourceCode);
        var codeSnippets = highlight_c_sharp(sourceCodeCString);
        Marshal.FreeHGlobal(sourceCodeCString);
        var strBuilder = new StringBuilder();
        for (int i = 0; i < codeSnippets.snippet_count; i++)
        {
            unsafe
            {
                var str = Marshal.PtrToStringAnsi((IntPtr)codeSnippets.snippets[i]);
                // strBuilder.Append($"{i + 1}: ");
                strBuilder.Append(str
                    .Replace("<span class=\"attribute\">", "<color=#9a7fed>")
                    .Replace("<span class=\"constant\">", "<color=#aa44ff>")
                    .Replace("<span class=\"comment\">", "<color=#84c26a>")
                    .Replace("<span class=\"constructor\">", "<color=#38A256>")
                    .Replace("<span class=\"constant_builtin\">", "<color=#518fc7>")
                    .Replace("<span class=\"function\">", "<color=#38A256>")
                    .Replace("<span class=\"function_builtin\">", "<color=#38A256>")
                    .Replace("<span class=\"keyword\">", "<color=#518fc7>")
                    .Replace("<span class=\"operator\">", "<color=white>")
                    .Replace("<span class=\"property\">", "<color=#42a1c5>")
                    .Replace("<span class=\"punctuation\">", "<color=white>")
                    .Replace("<span class=\"string\">", "<color=#c9a26d>")
                    .Replace("<span class=\"type\">", "<color=#9a7fed>")
                    .Replace("<span class=\"type.builtin\">", "<color=#518fc7>")
                    .Replace("<span class=\"variable\">", "<color=#bdbdbd>")
                    .Replace("<span class=\"variable_builtin\">", "<color=#bdbdbd>")
                    .Replace("<span class=\"variable_parameter\">", "<color=#bdbdbd>")
                    .Replace("<span class=\"number\">", "<color=#db7c77>")
                    .Replace("</span>", "</color>")
                    .Replace("&quot;", "\"")
                    .Replace("&amp;", "&")
                    .Replace("&#39;", "'")
                    .Replace("&lt;", "<b></b><<b></b>")
                    .Replace("&gt;", "<b></b>><b></b>")
                );
            }
        }
        
        return strBuilder.ToString();
#else
            var sourceCodeCString = Marshal.StringToHGlobalAnsi(sourceCode);
            var highlightQueryReader = new StreamReader(@"Packages\com.daxode.codimation\Codimation.TreeSitter\NativeSource~\tree-sitter-unity-richtext\queries\highlights.scm");
            var highlightQuery = highlightQueryReader.ReadToEnd();
            var highlightQueryCString = Marshal.StringToHGlobalAnsi(highlightQuery);
            var localsQueryReader = new StreamReader(@"Packages\com.daxode.codimation\Codimation.TreeSitter\NativeSource~\tree-sitter-unity-richtext\queries\locals.scm");
            var localsQuery = localsQueryReader.ReadToEnd();
            var localsQueryCString = Marshal.StringToHGlobalAnsi(localsQuery);

            // return $"{highlightQuery} {localsQuery}";
            var highlightedCodeCString = highlight_c_sharp_v2_manual(sourceCodeCString, highlightQueryCString, localsQueryCString);
            Marshal.FreeHGlobal(sourceCodeCString);
            Marshal.FreeHGlobal(highlightQueryCString);
            Marshal.FreeHGlobal(localsQueryCString);
            highlightQueryReader.Close();
            localsQueryReader.Close();
            var highlightedCode = Marshal.PtrToStringAnsi(highlightedCodeCString);
            return highlightedCode;
#endif
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

[StructLayout(LayoutKind.Sequential)]
unsafe struct CodeSnippets
{
    public char** snippets;
    public uint snippet_count;
}