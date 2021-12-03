using Microsoft.CodeAnalysis;
using System;
using System.Linq;

namespace RoslynTutorial.SourceGenerators
{
    public static class AttributeExtensions
    {
        public static bool HasAttribute<T>(this ISymbol data) where T : Attribute
        {
            return data.GetAttributes().Any(a => a.AttributeClass.Name.Equals(typeof(T).Name));
        }
    }
}