using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using Jolt;
using Piglet.Parser.Construction;

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

            public int NextState;
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
            var parser = dslParser.CreateDslParserFromBnf(input, type, commentReader);
            
            var terminals = dslParser.Terminals;
            var table = parser.ParseTable;

            var states = new List<List<SetMember>>();

            // For each state in the table, generate a class with the given commands
            // and the resulting 
            for (var state = 0; state < table.StateCount; ++state)
            {
                var items = new List<SetMember>();

                for (var terminal = 0; terminal < terminals.Count; ++terminal)
                {
                    var action = table.Action[state, terminal];
                    
                    // If action is error, continue
                    if (action == Int16.MinValue) continue;
                    
                    // Ignore the accept
                    if (action == Int16.MaxValue)
                        continue;

                    int newState = FindEndOfReduceChain(action, terminal, state, table);

                    if (newState < 0)
                        continue;

                    var debugName = terminals[terminal].DebugName;
                    var dbgNameParts = debugName.Split('\0');
                    items.Add(new SetMember(newState, dbgNameParts[0], dbgNameParts[1]));
                }

                states.Add(items);
            }

            ReduceStates(states);

            // Create the output string
            var sb = new StringBuilder();
            var output = new IndentedTextWriter(new StringWriter(sb));

            if (type.Namespace != outputNamespace){
                output.WriteLine(string.Format("using {0};", type.Namespace));
                output.WriteLine();
            }
            output.WriteLine(string.Format("namespace {0}", outputNamespace));
            output.WriteLine("{");
            output.Indent++;

            for (int i = 0; i < states.Count; ++i)
            {
                var state = states[i];
                if (state != null && state.Count > 0)
                {
                    var className = string.Format("{0}{1}", syntaxStateClassname, i == 0 ? "" : i.ToString());
                    output.WriteLine(string.Format("public class {0}", className));
                    output.WriteLine("{");
                    output.Indent++;

                    // Create a member called builder that accepts the underlying builder class
                    // and a matching constructor
                    output.WriteLine("private {0} builder;", type.Name);
                    output.WriteLine();
                    output.WriteLine("public {0}({1} builder) {{ this.builder = builder; }}", className, type.Name);

                    foreach (var setMember in state)
                    {
                        output.WriteLine();
                        foreach (var docLine in setMember.Documentation.Split('\n'))
                        {
                            output.WriteLine(docLine.Trim());
                        }
                        output.Write(@"public {1}{0} ", setMember.NextState, syntaxStateClassname);
                        output.WriteLine(String.Format(setMember.Content, syntaxStateClassname, setMember.NextState));
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

        private void ReduceStates(List<List<SetMember>> states)
        {
            // Reduce the number of states
            bool restart = true;

            while (restart)
            {
                restart = false;

                for (int i = 0; i < states.Count - 1 && !restart; ++i)
                {
                    var a = states[i];

                    if (a == null)
                        continue;

                    for (int j = i + 1; j < states.Count && !restart; ++j)
                    {
                        // Compare the
                        var b = states[j];
                        if (b == null)
                            continue;

                        // If the number of states aren't equal these states are not candidates for
                        // replacing
                        if (a.Count != b.Count) continue;

                        if (!a.Where((t, x) => !t.Equals(b[x])).Any())
                        {
                            // Remove one of the states, replace the NextState of all
                            // the states that referred to state j with state i.
                            states[j] = null;

                            foreach (var state in states)
                            {
                                if (state != null)
                                {
                                    foreach (var tuple in state)
                                    {
                                        if (tuple.NextState == j)
                                            tuple.NextState = i;
                                    }
                                }
                            }
                            restart = true;
                        }
                    }
                }
            }
        }

        private static int FindEndOfReduceChain(int action, int terminal, int state, IParseTable<object> table)
        {
            if (action > 0)
                return action;

            if (action == Int16.MinValue)
                return -1;
            if (action == Int16.MaxValue)
                return -2;

            // It is a reduce
            state = table.Goto[state, table.ReductionRules[-(action + 1)].TokenToPush];
            if (state == Int16.MinValue)
                return -1;
            action = table.Action[state, terminal];

            return FindEndOfReduceChain(action, terminal, state, table);
        }
    }
}