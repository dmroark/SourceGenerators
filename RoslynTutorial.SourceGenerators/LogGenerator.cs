using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RoslynTutorial.SourceGenerators.Attributes;
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace RoslynTutorial.SourceGenerators
{
#pragma warning disable RS1024 // Compare symbols correctly
    [Generator]
    public class LogGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {

        }

        public void Execute(GeneratorExecutionContext context)
        {
            var compilation = context.Compilation;

            foreach (var syntaxTree in compilation.SyntaxTrees)
            {
                var semanticModel = compilation.GetSemanticModel(syntaxTree);

                var targetTypes = syntaxTree.GetRoot().DescendantNodesAndSelf().OfType<ClassDeclarationSyntax>()
                    .Select(c => semanticModel.GetDeclaredSymbol(c))
                    .OfType<ITypeSymbol>()
                    .Where(t => !t.IsSealed
                                && t.HasAttribute<LogAttribute>()
                                && t.GetMembers().OfType<IMethodSymbol>().Any(m => m.HasAttribute<LogAttribute>()))
                    .ToImmutableHashSet();

                foreach (var targetType in targetTypes)
                {
                    var source = GenerateLogProxy(targetType);
                    context.AddSource($"{targetType.Name}.LogProxy.cs", source);
                }
            }
        }

        private string GenerateLogProxy(ITypeSymbol targetType)
        {
            return $@"
using System;
using System.Diagnostics;

namespace {targetType.ContainingNamespace}
{{
    public class {targetType.Name}LogProxy : {targetType.Name}
    {{
{GenerateDecoratedMethods(targetType)}
    }}

    public class {targetType.Name}Factory
    {{
        public static {targetType.Name} Create() => new {targetType.Name}LogProxy();
    }}
}}";
        }

        private string GenerateDecoratedMethods(ITypeSymbol targetType)
        {
            var methods = targetType.GetMembers().OfType<IMethodSymbol>().Where(m => m.HasAttribute<LogAttribute>());

            var result = new StringBuilder();

            foreach (var method in methods)
            {
                var modifier = method.DeclaredAccessibility.ToString().ToLower();
                var parameters = GetMethodParameters(method);

                var methodResult = method.ReturnsVoid ? string.Empty : "var result = ";
                var returnString = method.ReturnsVoid ? string.Empty : "return result;";
                var failReturnString = method.ReturnsVoid ? string.Empty : $"return default({method.ReturnType});";

                var loggedResult = method.ReturnsVoid ? string.Empty : " with result {result}";

                var newMethod = $@"{modifier} override {method.ReturnType} {method.Name}({parameters})
        {{
            try
            {{
                Debug.WriteLine(""{method.Name}() starting..."");
                {methodResult}base.{method.Name}({parameters});
                Debug.WriteLine($""{method.Name}() completed{loggedResult}"");
                {returnString}
            }}
            catch
            {{
                Debug.WriteLine(""Error occured in {method.Name}()"");
                {failReturnString}
            }}
        }}
";
                result.AppendLine($"\t\t{newMethod}");
            }

            return result.ToString();
        }

        private static string GetMethodParameters(IMethodSymbol method)
        {
            return string.Join(", ", method.Parameters.Select(x => $"{GetFullQualifiedName(x.Type)} {x.Name}"));
        }

        private static string GetFullQualifiedName(ISymbol symbol)
        {
            var ns = symbol.ContainingNamespace;

            return ns.IsGlobalNamespace ? symbol.Name : ns.ToDisplayString() + "." + symbol.Name;
        }
    }
}