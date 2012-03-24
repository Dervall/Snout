using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using Jolt;
using Piglet.Parser;
using Piglet.Parser.Configuration;
using Convert = System.Convert;

namespace Snout
{
    public class DslParser
    {
        public List<ITerminal<object>> Terminals { get; set; }

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

        public IParser<object> CreateDslParserFromBnf(string input, Type builderType, XmlDocCommentReader commentReader)
        {
            // Create a list that will hold the terminals for this given input
            Terminals = new List<ITerminal<object>>();

            // Configurator for the parser we're configuring that we are going to use to
            // generate the DSL code.
            var dslConfigurator = ParserFactory.Configure<object>();
            
            // Create the parser that we're going to use to read the BNF for the DSL
            var configurator = ParserFactory.Configure<object>();

            var nonTerminals = new Dictionary<string, INonTerminal<object>>();
            var terminals = new Dictionary<string, ITerminal<object>>();

            foreach (var methodInfo in builderType.GetMethods())
            {
                // Read the xml documentation for this method. Look for the buildermethod tag
                // If this is found, we create a suitable terminal for this method
                var attribute = GetBuilderMethod(methodInfo, commentReader);
                if (attribute != null)
                {
                    var terminal = dslConfigurator.CreateTerminal(Regex.Escape(attribute.DslName));
                    var method = new StringBuilder(attribute.FluentName);
                    
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
                        applyBracersIfNotEmpty(string.Join(",",methodInfo.GetGenericArguments().Select(f => f.Name))),
                        string.Join(", ", parameters.Select(f => f.Name).ToArray()));
                    if (!parameters.Any() && !methodInfo.GetGenericArguments().Any() && attribute.UseProperty)
                    {
                        // No parameters and user wants this rendered as a property
                        method.Append(@"
        {{
            get
            {{
                builder."+builderCall+@";
                return new {0}{1}(builder);
            }}
        }}");
                    }
                    else
                    {
                        // There is supposed to be a method call                        
                        method.AppendFormat("({0})",
                            parameters.Any() ?
                            parameters.Select(f =>
                            {
                                var dynamicAttribute = f.GetCustomAttributes(typeof (DynamicAttribute), true).OfType<DynamicAttribute>().FirstOrDefault();

                                return string.Format("{0} {1}", GetFullName(f.ParameterType,
                                    dynamicAttribute != null ? dynamicAttribute.TransformFlags : null),
                                            f.Name);
                            }).ToArray<object>() : new object[] { "" });
                        method.Append(@"
        {{
            builder." + builderCall + @";
            return new {0}{1}(builder);
        }}");
                    }

                    terminal.DebugName = method + "\0" + GetDocumentation(methodInfo, commentReader);
                    terminals.Add(attribute.DslName, terminal);
                    Terminals.Add(terminal);
                }
            }

            var name = configurator.CreateTerminal("[a-zA-z_0-9]+",
                s => {
                    // If this name is found on the builder type, then it's a terminal
                    // otherwise it's a nonterminal
                    if (terminals.ContainsKey(s))
                    {
                        return terminals[s];
                    }

                    if (nonTerminals.ContainsKey(s))
                    {
                        return nonTerminals[s];
                    }
                    var t = dslConfigurator.CreateNonTerminal();
                    t.DebugName = s;
                    nonTerminals.Add(s, t);
                    return t;
                }
            );
            
            var grammar = configurator.CreateNonTerminal();
            var ruleList = configurator.CreateNonTerminal();
            var rule = configurator.CreateNonTerminal();
            var ruleElementList = configurator.CreateNonTerminal();
            var ruleElement = configurator.CreateNonTerminal();
            var optionalRuleElement = configurator.CreateNonTerminal();

            grammar.AddProduction(ruleList);

            ruleList.AddProduction(ruleList, rule);
            ruleList.AddProduction(rule);

            rule.AddProduction(name, ":", ruleElementList, ";").SetReduceFunction(f =>
            {
                ((List<List<object>>)f[2]).ForEach(a => ((INonTerminal<object>) f[0]).AddProduction(a.ToArray()));
                return null;
            });

            ruleElementList.AddProduction(ruleElementList, "|", optionalRuleElement).SetReduceFunction(f => ((List<List<object>>)f[0]).Union((List<List<object>>)f[2]).ToList());
            ruleElementList.AddProduction(optionalRuleElement).SetReduceFunction(f => f[0]);

            optionalRuleElement.AddProduction(ruleElement).SetReduceFunction(f => new List<List<object>> {(List<object>) f[0]});
            optionalRuleElement.AddProduction().SetReduceFunction(f =>  new List<List<object>> {new List<object>()} );

            ruleElement.AddProduction(ruleElement, name).SetReduceFunction(f => ((List<object>)f[0]).Union(new List<object> {f[1]}).ToList());
            ruleElement.AddProduction(name).SetReduceFunction(f => new List<object> {f[0]});

            var bnfParser = configurator.CreateParser();
            bnfParser.Parse(input);
            
            return dslConfigurator.CreateParser();
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
