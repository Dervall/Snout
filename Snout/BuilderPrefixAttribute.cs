using System;

namespace Snout
{
    public class BuilderPrefixAttribute : Attribute
    {
        public string BuilderClassPrefix { get; set; }

        public BuilderPrefixAttribute(string builderClassPrefix)
        {
            BuilderClassPrefix = builderClassPrefix;
        }
    }
}