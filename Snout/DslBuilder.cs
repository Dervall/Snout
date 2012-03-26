using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Jolt;

namespace Snout
{
    public class DslBuilder
    {
        private class SetMember
        {
            public SetMember(int ns, string c, string doc)
            {
                NextState = ns;
                Content = c;
                Documentation = doc;
            }

            public readonly int NextState;
            public readonly string Content;

            public string Documentation { get; private set; }

            public bool Equals(SetMember other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return other.NextState == NextState && Equals(other.Content, Content);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != typeof(SetMember)) return false;
                return Equals((SetMember)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (NextState * 397) ^ (Content != null ? Content.GetHashCode() : 0);
                }
            }
        }

        private readonly string input;
        private readonly Type type;
        private readonly XmlDocCommentReader commentReader;
        private readonly string syntaxStateClassname;
        private readonly string outputNamespace;

        public DslBuilder(XElement builderClassNode, XmlDocCommentReader commentReader, Type builderType)
        {
            input = builderClassNode.Value;
            syntaxStateClassname = builderClassNode.Attributes().Single(f => f.Name == "name").Value;
            outputNamespace = builderClassNode.Attributes().Single(f => f.Name == "namespace").Value;
            type = builderType;
            this.commentReader = commentReader;
        }

        public string CreateDslCode()
        {
            var dslParser = new DslParser();
            var dslLexer = dslParser.CreateDslParserFromRegularExpression(input, type, commentReader);
            
            var identifiers = dslParser.Identifiers;
            var table = dslLexer.Table;

            var states = new List<List<SetMember>>();

            // For each state in the table, generate a class with the given commands
            // and the resulting 
            for (var state = 0; state < dslLexer.StateCount; ++state)
            {
                var items = new List<SetMember>();

                foreach (var inputChar in identifiers.Keys)
                {
                    var nextState = table[state, inputChar];
                    if (nextState >= 0)
                    {
                        var identifier = identifiers[inputChar];
                        items.Add(new SetMember(nextState, identifier.MethodContents, identifier.Documentation));
                    }
                }

                states.Add(items);
            }

            return CreateDslCode(states);
        }

        private string CreateDslCode(List<List<SetMember>> states)
        {
            // Create the output string
            var sb = new StringBuilder();
            var output = new IndentedTextWriter(new StringWriter(sb));

            // This is so common that we might as well print it out to start with
            output.WriteLine("using System;");
            output.WriteLine("using System.ComponentModel;");

            if (type.Namespace != outputNamespace)
            {
                output.WriteLine(string.Format("using {0};", type.Namespace));
                output.WriteLine();
            }
            output.WriteLine(string.Format("namespace {0}", outputNamespace));
            output.WriteLine("{");
            output.Indent++;

            output.WriteLine("[EditorBrowsable(EditorBrowsableState.Never)]");
            output.WriteLine("public interface IHide{0}ObjectMembers", syntaxStateClassname);
            output.WriteLine("{");
            output.Indent++;
            output.WriteLine("[EditorBrowsable(EditorBrowsableState.Never)]");
            output.WriteLine("Type GetType();");
            output.WriteLine("");
            output.WriteLine("[EditorBrowsable(EditorBrowsableState.Never)]");
            output.WriteLine("int GetHashCode();");
            output.WriteLine("");
            output.WriteLine("[EditorBrowsable(EditorBrowsableState.Never)]");
            output.WriteLine("string ToString();");
            output.WriteLine("");
            output.WriteLine("[EditorBrowsable(EditorBrowsableState.Never)]");
            output.WriteLine("bool Equals(object obj);");
            output.Indent--;
            output.WriteLine("}");
            output.WriteLine();

            for (int i = 0; i < states.Count; ++i)
            {
                var state = states[i];
                if (state != null) // && state.Count > 0)
                {
                    var className = string.Format("{0}{1}", syntaxStateClassname, i == 0 ? "" : i.ToString());
                    output.WriteLine(string.Format("public class {0} : IHide{1}ObjectMembers", className,
                                                   syntaxStateClassname));
                    output.WriteLine("{");
                    output.Indent++;

                    // Create a member called builder that accepts the underlying builder class
                    // and a matching constructor
                    output.WriteLine("private readonly {0} builder;", type.Name);
                    output.WriteLine();
                    output.WriteLine("internal {0}({1} builder) {{ this.builder = builder; }}", className, type.Name);

                    foreach (var setMember in state)
                    {
                        output.WriteLine();
                        foreach (var docLine in setMember.Documentation.Split('\n'))
                        {
                            output.WriteLine(docLine.Trim());
                        }
                        output.Write(@"public {1}{0} ", setMember.NextState == 0 ? "" : setMember.NextState.ToString(),
                                     syntaxStateClassname);
                        output.WriteLine(String.Format(setMember.Content, syntaxStateClassname,
                                                       setMember.NextState == 0 ? "" : setMember.NextState.ToString()));
                    }
                    output.Indent--;
                    output.WriteLine("}");
                    output.WriteLine();
                }
            }
            output.Indent--;
            output.WriteLine("}");
            output.Close();
            return sb.ToString();
        }
    }
}