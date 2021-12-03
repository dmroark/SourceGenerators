using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using RoslynTutorial.SourceGenerators.Attributes;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace RoslynTutorial.SourceGenerators
{
#pragma warning disable RS1024 // Compare symbols correctly
    [Generator]
    public class NotifyPropertyChangedGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            //Debugger.Launch();

            var compilation = context.Compilation;

            #region Optimized
            if (context.SyntaxReceiver is SyntaxReceiver receiver)
            {
                var declarationGroups = receiver.Declarations
                    .GroupBy(d => d.SyntaxTree)
                    .ToDictionary(g => g.Key, g => g.ToArray());

                foreach (var group in declarationGroups)
                {
                    var syntaxTree = group.Key;
                    var semanticModel = compilation.GetSemanticModel(syntaxTree);
                                        
                    var targetTypes = group.Value.Select(c => semanticModel.GetDeclaredSymbol(c))
                        .OfType<ITypeSymbol>()
                        .Where(t => t.HasAttribute<NotifyPropertyChangedAttribute>())
                        .ToImmutableHashSet();

                    foreach (var targetType in targetTypes)
                    {
                        var source = GeneratePropertyChanged(targetType);
                        context.AddSource($"{targetType.Name}.NotifyPropertyChanged.cs", source);
                    }
                }
            }
            #endregion

            //foreach (var syntaxTree in compilation.SyntaxTrees)
            //{
            //    var semanticModel = compilation.GetSemanticModel(syntaxTree);

            //    var targetTypes = syntaxTree.GetRoot().DescendantNodesAndSelf().OfType<ClassDeclarationSyntax>()
            //        .Select(c => semanticModel.GetDeclaredSymbol(c))
            //        .OfType<ITypeSymbol>()
            //        .Where(t => t.HasAttribute<NotifyPropertyChangedAttribute>())
            //        .ToImmutableHashSet();

            //    foreach (var targetType in targetTypes)
            //    {
            //        var source = GeneratePropertyChanged(targetType);
            //        context.AddSource($"{targetType.Name}.NotifyPropertyChanged.cs", source);
            //    }
            //}
        }

        /// <summary>
        /// Generates new file with partial class.
        /// </summary>
        private string GeneratePropertyChanged(ITypeSymbol targetType)
        {
            return $@"
using System;
using System.ComponentModel;

namespace {targetType.ContainingNamespace}
{{
    {GenerateClass(targetType)}
}}";
        }

        /// <summary>
        /// Generates partial class with INotifyPropertyChanged implementation.
        /// </summary>
        private string GenerateClass(ITypeSymbol targetType)
        {
            return $@"
    public partial class {targetType.Name} : INotifyPropertyChanged
    {{
        {GenerateProperties(targetType)}
        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(string propertyName)
        {{
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }}
    }}";
        }

        /// <summary>
        /// Generates properties.
        /// </summary>
        private string GenerateProperties(ITypeSymbol targetType)
        {
            var result = new StringBuilder();

            var fields = targetType.GetMembers().OfType<IFieldSymbol>()
                .Where(f => f.HasAttribute<PropertyWithNotificationAttribute>());

            foreach (var field in fields)
            {
                var propertyName = GetPropertyName(field.Name);

                var newProperty = $@"
        ///<summary>
        /// {propertyName}.
        ///</summary>
        public {field.Type.Name} {propertyName}
        {{
            get => {field.Name};
            set
            {{
                {field.Name} = value;
                RaisePropertyChanged(nameof({propertyName}));
            }}
        }}";
                result.AppendLine(newProperty);
            }

            return result.ToString();
        }

        private string GetPropertyName(string fieldName)
        {
            if (fieldName.StartsWith("_"))
            {
                fieldName = fieldName.Substring("_".Length);
            }

            return char.ToUpperInvariant(fieldName[0]) + fieldName.Substring(1);
        }

        public class SyntaxReceiver : ISyntaxReceiver
        {
            public HashSet<ClassDeclarationSyntax> Declarations { get; } = new HashSet<ClassDeclarationSyntax>();

            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                if (syntaxNode is ClassDeclarationSyntax declaration && declaration.AttributeLists.Any())
                {
                    Declarations.Add(declaration);
                }
            }
        }
    }
}