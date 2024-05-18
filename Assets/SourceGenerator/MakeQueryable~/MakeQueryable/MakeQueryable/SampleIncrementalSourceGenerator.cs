using System.CodeDom.Compiler;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

/// <summary>
/// A sample source generator that creates a custom report based on class properties. The target class should be annotated with the 'Generators.ReportAttribute' attribute.
/// When using the source code as a baseline, an incremental source generator is preferable because it reduces the performance overhead.
/// </summary>
[Generator]
public class SampleIncrementalSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Filter classes annotated with the [Report] attribute. Only filtered Syntax Nodes can trigger code generation.
        var provider = context.SyntaxProvider
            .CreateSyntaxProvider(
                (s, _) => s is ClassDeclarationSyntax,
                (ctx, _) => GetClassDeclarationForSourceGen(ctx))
            .Where(t => t.reportAttributeFound)
            .Select((t, _) => t.Item1);

        // Generate the source code.
        context.RegisterSourceOutput(provider, GenerateCode);
    }

    /// <summary>
    /// Checks whether the Node is annotated with the [Report] attribute and maps syntax context to the specific node type (ClassDeclarationSyntax).
    /// </summary>
    /// <param name="context">Syntax context, based on CreateSyntaxProvider predicate</param>
    /// <returns>The specific cast and whether the attribute was found.</returns>
    private static (ClassDeclarationSyntax, bool reportAttributeFound) GetClassDeclarationForSourceGen(
        GeneratorSyntaxContext context)
    {
        var classDeclarationSyntax = (ClassDeclarationSyntax)context.Node;

        // Go through all attributes of the class.
        foreach (AttributeListSyntax attributeListSyntax in classDeclarationSyntax.AttributeLists)
        foreach (AttributeSyntax attributeSyntax in attributeListSyntax.Attributes)
        {
            if (ModelExtensions.GetSymbolInfo(context.SemanticModel, attributeSyntax).Symbol is not IMethodSymbol attributeSymbol)
                continue; // if we can't get the symbol, ignore it

            string attributeName = attributeSymbol.ContainingType.ToDisplayString();

            // Check the full name of the [Report] attribute.
            if (attributeName == "Generators.ReportAttribute")
                return (classDeclarationSyntax, true);
        }

        return (classDeclarationSyntax, false);
    }

    /// <summary>
    /// Generate code action.
    /// It will be executed on specific nodes (ClassDeclarationSyntax annotated with the [Report] attribute) changed by the user.
    /// </summary>
    /// <param name="context">Source generation context used to add source files.</param>
    /// <param name="compilation">Compilation used to provide access to the Semantic Model.</param>
    /// <param name="classDeclarations">Nodes annotated with the [Report] attribute that trigger the generate action.</param>
    void GenerateCode(SourceProductionContext context, ClassDeclarationSyntax classDeclarationSyntax)
    {
        // Go through all filtered class declarations.
        MemoryStream sourceStream = new();
        StreamWriter sourceStreamWriter = new(sourceStream, Encoding.UTF8);
        IndentedTextWriter codeWriter = new (sourceStreamWriter);
        codeWriter.WriteLine("// <auto-generated/>");
        codeWriter.WriteLine("namespace Generators");
        codeWriter.WriteLine('{');
        codeWriter.Indent++;
        codeWriter.WriteLine("static class ReportExtensions");
        codeWriter.WriteLine('{');
        codeWriter.Indent++;
        
        codeWriter.Write("public static global::System.Collections.Generic.IEnumerable<string> Report(this ");
        codeWriter.AppendFullTypeName(classDeclarationSyntax);
        codeWriter.WriteLine(" self)");
        codeWriter.WriteLine('{');
        codeWriter.Indent++;
        var yieldReturnAdded = false;
        // Go through all class members with a particular type (property) to generate method lines.
        foreach (var member in classDeclarationSyntax.Members)
        {
            if (member is not PropertyDeclarationSyntax propertySyntax)
                continue;
            
            if (!(propertySyntax.Modifiers.Any(SyntaxKind.PublicKeyword) || propertySyntax.Modifiers.Any(SyntaxKind.ProtectedKeyword)))
                continue;
            
            yieldReturnAdded = true;
            codeWriter.Write("yield return $\"");
            codeWriter.Write(propertySyntax.Identifier.Text);
            codeWriter.Write(": {self.");
            codeWriter.Write(propertySyntax.Identifier.Text);
            codeWriter.WriteLine("}\";");
        }
        if (!yieldReturnAdded)
        {
            codeWriter.WriteLine("yield break;");
        }
        
        codeWriter.Indent--;
        codeWriter.WriteLine('}');
        
        // Close the class.
        codeWriter.Indent--;
        codeWriter.WriteLine('}');
        
        // Close the namespace.
        codeWriter.Indent--;
        codeWriter.WriteLine('}');
        sourceStreamWriter.Flush();
        
        // Add the source code to the compilation.
        context.AddSource($"Report.{classDeclarationSyntax.Identifier.Text}.g.cs", SourceText.From(sourceStream, Encoding.UTF8, canBeEmbedded: true));
    }
}

static class CodeWriterExtensions
{
    public static void AppendFullTypeName(this TextWriter codeWriter, ClassDeclarationSyntax classDeclarationSyntax)
    {
        var ancestorCount = 0;
        var parent = classDeclarationSyntax.Parent;
        while (parent is BaseNamespaceDeclarationSyntax or BaseTypeDeclarationSyntax)
        {
            ancestorCount++;
            parent = parent.Parent;
        }
        parent = classDeclarationSyntax.Parent;
        
        var names = new string[ancestorCount];
        var currentAncestor = ancestorCount - 1;
        while (parent is BaseNamespaceDeclarationSyntax or BaseTypeDeclarationSyntax)
        {
            switch (parent)
            {
                case BaseTypeDeclarationSyntax parentClass:
                    names[currentAncestor] = parentClass.Identifier.Text;
                    break;
                case BaseNamespaceDeclarationSyntax parentNamespace:
                    names[currentAncestor] = parentNamespace.Name.ToString();
                    break;
            }

            currentAncestor--;
            parent = parent.Parent;
        }

        codeWriter.Write("global::");
        foreach (var name in names)
        {
            codeWriter.Write(name);
            codeWriter.Write('.');
        }
        codeWriter.Write(classDeclarationSyntax.Identifier.Text);
    }
}
