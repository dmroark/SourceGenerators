using System;

namespace RoslynTutorial.SourceGenerators.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class LogAttribute : Attribute { }
}
