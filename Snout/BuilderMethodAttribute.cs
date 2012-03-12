using System;

namespace Snout
{
    internal class BuilderMethodAttribute : Attribute
    {
        public string DslName { get; set; }
        public string FluentName { get; set; }

        public BuilderMethodAttribute(string dslName) : this(dslName, dslName)
        {
        }

        public BuilderMethodAttribute(string dslName, string fluentName)
        {
            FluentName = fluentName;
            DslName = dslName;
        }
    }
}