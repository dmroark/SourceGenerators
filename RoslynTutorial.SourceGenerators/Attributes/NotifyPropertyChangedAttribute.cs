using System;
using System.Collections.Generic;
using System.Text;

namespace RoslynTutorial.SourceGenerators.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class NotifyPropertyChangedAttribute : Attribute { }
}
