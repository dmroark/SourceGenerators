using System;

namespace RoslynTutorial.SourceGenerators.Attributes
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public class PropertyWithNotificationAttribute : Attribute { }
}
