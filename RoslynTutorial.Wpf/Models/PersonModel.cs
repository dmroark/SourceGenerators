using RoslynTutorial.SourceGenerators.Attributes;
using System;

namespace RoslynTutorial.Wpf.Models
{
    [Log]
    [NotifyPropertyChanged]
    public partial class PersonModel
    {
        [PropertyWithNotification]
        private string _firstName;

        [PropertyWithNotification]
        private string _lastName;

        private string _email;

        [Log]
        public virtual void DoSomething()
        {
        }

        [Log]
        public virtual DateTime Ping()
        {
            return DateTime.UtcNow;
        }
    }
}
