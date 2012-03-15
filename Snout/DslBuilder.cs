using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Piglet.Parser.Construction;

namespace Snout
{
    public class DslBuilder
    {
        private class SetMember
        {
            public SetMember(int ns, string c)
            {
                NextState = ns;
                Content = c;
            }

            public int NextState;
            public readonly string Content;

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

        public DslBuilder(string input, Type type)
        {
            this.input = input;
            this.type = type;
        }

        public string CreateDslCode()
        {
            var dslParser = new DslParser();
            var parser = dslParser.CreateDslParserFromBnf(input, type);
            
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

                    items.Add(new SetMember(newState, terminals[terminal].DebugName));
                }

                states.Add(items);
            }

            ReduceStates(states);

            // Create the output string
            var output = new StringBuilder();           

            for (int i = 0; i < states.Count; ++i)
            {
                var state = states[i];
                if (state != null)
                {
                    output.AppendLine(string.Format("class SyntaxState{0}", i));
                    output.AppendLine("{");

                    foreach (var setMember in state)
                    {
                        output.AppendFormat(@"
    SyntaxState{0} ", setMember.NextState);
                        output.AppendLine(String.Format(setMember.Content, "SyntaxState", setMember.NextState));
                    }

                    output.AppendLine("}");
                }
            }

            return output.ToString();
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