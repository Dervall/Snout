using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Jolt;
using Piglet.Lexer;
using Convert = System.Convert;

namespace Snout
{
    public class DslParser
    {
        static string GetFullName(Type t, IEnumerable<bool> transformFlags)
        {
            if (!t.IsGenericType)
            {
                if (transformFlags != null && transformFlags.Take(1).First())
                    return "dynamic";

                // Translate String into string and Object into object
                if (t.Name == "Object") return "object";
                if (t.Name == "String") return "string";

                return t.Name;
            }

            var sb = new StringBuilder();
            sb.Append(t.Name.Substring(0, t.Name.LastIndexOf("`")));

            sb.Append(t.GetGenericArguments().Aggregate("<", (aggregate, type) =>
            {
                transformFlags = transformFlags.Skip(1);

                return aggregate +
                (aggregate == "<" ? "" : ",") +
                GetFullName(type,
                            transformFlags );
            }));
            sb.Append(">");

            return sb.ToString();
        }

        public class Identifier
        {
            public string DslName { get; set; }
            public string MethodContents { get; set; }

            public string Documentation { get; set; }
        }

        public Dictionary<char, Identifier> Identifiers { get; set; } 

        public ILexer<int> CreateDslParserFromRegularExpression(string input, Type builderType, XmlDocCommentReader commentReader)
        {
            List<Identifier> identifiers = CreateIdentifiers(builderType, commentReader).ToList();
            IDictionary<string, char> usedIdentifiers = new Dictionary<string, char>();

            // We are going to create a lexer to read the input
            var inputLexer = LexerFactory<char>.Configure(configurator =>
            {
                Action<char> singleChar = c => configurator.Token(Regex.Escape(c.ToString()), f => c);
                char identifierChar = 'a';
                
                configurator.Token("[A-Za-z_][A-Za-z_0-9]+", f =>
                {
                    if (usedIdentifiers.ContainsKey(f))
                    {
                        return usedIdentifiers[f];
                    }

                    // Make sure the identifer exists
                    if (identifiers.SingleOrDefault(i => i.DslName == f) == null)
                    {
                        throw new Exception("No buildermethod called " + f + " was found in the class!");
                    }

                    if (identifierChar == 'Z' + 1)
                        identifierChar = 'a';
                    if (identifierChar > 'z')
                        throw new Exception("Snout cannot handle the amount of symbols you're using. Use less");

                    usedIdentifiers.Add(f, identifierChar);

                    return identifierChar++;
                }); // Identifiers

                singleChar('+');
                singleChar('*');
                singleChar('|');
                singleChar('?');
                singleChar('(');
                singleChar(')');

                configurator.Ignore("\\s+");
                configurator.Ignore(@"/\*([^*]+|\*[^/])*\*/");
            });

            inputLexer.SetSource(input);

            var dslRegEx = new StringBuilder();

            // Use the lexer to get a regular expression!
            for (var token = inputLexer.Next(); token.Item1 != -1; token = inputLexer.Next() )
            {
                dslRegEx.Append(token.Item2);
            }

            // Condense it into the Identifers dictionary
            Identifiers =
                identifiers.Where(f => usedIdentifiers.ContainsKey(f.DslName)).ToDictionary(
                    f => usedIdentifiers[f.DslName]);

            // Create another lexer, this one we will return
            return LexerFactory<int>.Configure(c => c.Token(dslRegEx.ToString(), f => 0));
        }

        private IEnumerable<Identifier> CreateIdentifiers(Type builderType, XmlDocCommentReader commentReader)
        {
            foreach (var methodInfo in builderType.GetMethods())
            {
                // Read the xml documentation for this method. Look for the buildermethod tag
                // If this is found, we create a suitable terminal for this method
                var builderMethod = GetBuilderMethod(methodInfo, commentReader);
                if (builderMethod != null)
                {
                    var identifier = new Identifier {DslName = Regex.Escape(builderMethod.DslName)};
                    var method = new StringBuilder(builderMethod.FluentName);

                    // If there are generic type arguments to the builder method these will need to be transferred to the
                    // new interface method
                    var genericArguments = methodInfo.GetGenericArguments();
                    if (genericArguments.Any())
                    {
                        method.Append("<");
                        var dynamicAttribute = methodInfo.GetCustomAttributes(typeof(DynamicAttribute), true).OfType<DynamicAttribute>().FirstOrDefault();
                        IEnumerable<bool> transformFlags = dynamicAttribute == null ? null : dynamicAttribute.TransformFlags;
                        method.Append(string.Join(", ", genericArguments.Select(f =>
                        {
                            var ret = GetFullName(f, transformFlags);
                            if (transformFlags != null)
                                transformFlags = transformFlags.Skip(1);
                            return ret;
                        })));
                        method.Append(">");
                    }

                    Func<string, string> applyBracersIfNotEmpty = f => f.Length > 0 ? "<" + f + ">" : "";

                    // Append stuff to the debug name to make it call the builder method
                    // and to carry along the properties
                    var parameters = methodInfo.GetParameters();
                    var builderCall = string.Format("{0}{1}({2})",
                        methodInfo.Name,
                        applyBracersIfNotEmpty(string.Join(",", methodInfo.GetGenericArguments().Select(f => f.Name))),
                        string.Join(", ", parameters.Select(f => f.Name).ToArray()));
                    if (!parameters.Any() && !methodInfo.GetGenericArguments().Any() && builderMethod.UseProperty)
                    {
                        // No parameters and user wants this rendered as a property
                        method.Append(@"
        {{
            get
            {{
                builder." + builderCall + @";
                return new {0}{1}(builder);
            }}
        }}");
                    }
                    else
                    {
                        // There is supposed to be a method call                        
                        method.AppendFormat("({0})",
                            parameters.Any() ?
                            string.Join(", ", parameters.Select(f =>
                            {
                                var dynamicAttribute = f.GetCustomAttributes(typeof(DynamicAttribute), true).OfType<DynamicAttribute>().FirstOrDefault();

                                return string.Format("{0} {1}", GetFullName(f.ParameterType,
                                    dynamicAttribute != null ? dynamicAttribute.TransformFlags : null),
                                            f.Name);
                            }).ToArray<object>()) : "" );
                        method.Append(@"
        {{
            builder." + builderCall + @";
            return new {0}{1}(builder);
        }}");
                    }

                    identifier.MethodContents = method.ToString();
                    identifier.Documentation = GetDocumentation(methodInfo, commentReader);
                    yield return identifier;
                }
            }
        }

        private string GetDocumentation(MethodInfo method, XmlDocCommentReader commentReader)
        {
            var methodComments = commentReader.GetComments(method);

            // We are going to take the comments verbatim, but remove the <buildermethod/> tag
            var builderNode = methodComments.Descendants().Single(f => f.Name == "buildermethod");
            builderNode.Remove();
            return "///" + string.Join("\n///", methodComments.Nodes().SelectMany( n => n.ToString().Split('\n').Where(f => !string.IsNullOrWhiteSpace(f)).Select(f => f.TrimStart())));
        }

        private BuilderMethod GetBuilderMethod(MethodInfo methodInfo, XmlDocCommentReader commentReader)
        {
            var comments = commentReader.GetComments(methodInfo);

            if (comments != null)
            {
                // Look for a buildermethod subnode. If this is found create a BuilderMethod instance
                var builderMethodNode = comments.Descendants().SingleOrDefault(f => f.Name == "buildermethod");
                if (builderMethodNode != null)
                {
                    Func<string, string, string> attributeValueOrDefault = (attributeName, defaultValue) =>
                    {
                        var attribute = builderMethodNode.Attributes().SingleOrDefault(n => n.Name == attributeName);
                        return attribute == null ? defaultValue : attribute.Value;
                    };

                    string fluentName = attributeValueOrDefault("name", methodInfo.Name);
                    string dslName = attributeValueOrDefault("dslname", fluentName);
                    bool useProperty = Convert.ToBoolean(attributeValueOrDefault("useproperty", "true"));

                    return new BuilderMethod
                               {
                                   FluentName = fluentName,
                                   DslName = dslName,
                                   UseProperty = useProperty
                               };
                }
            }

            return null;
        }
    }
}
