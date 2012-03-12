using System;
using System.Collections.Generic;
using Piglet.Parser.Configuration;
using Piglet.Parser.Configuration.Fluent;
using Piglet.Parser.Construction;

namespace Snout
{
    class Program
    {
        private static IList<ITerminal<object>> terminals;
        
        private class SetMember
        {
            public SetMember(int ns, string c)
            {
                NextState = ns;
                Content = c;
            }

            public int NextState;
            public string Content;

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

        static void Main(string[] args)
        {
            string input = @"

            rule : IsMadeUp rulePartList;

            rulePartList : rulePartList Or rulePart 
                         | rulePart ;
            
            rulePart : listElements optionalWhenFound ;

            optionalWhenFound : WhenFound 
                              | ;

            listElements : listElements Followed by 
                         | by ;

            by : ByLiteral | bySimple | byList ;

            byList : listBys optionalNaming optionalListSettings ;

            listBys : ByTypedListOf 
                    | ByListOf ;

            optionalListSettings : ThatIs listOfListSettings
                                 | ;

            listOfListSettings : listOfListSettings And listSetting 
                               | listSetting;

            listSetting : Optional | SeparatedBy ;

            bySimple : simpleBys optionalNaming optionalSimpleSettings;

            simpleBys : ByTExpressionType | ByExpression;

            optionalSimpleSettings : ThatIs | ;

            optionalNaming : As | ;
            ";

            var dslParser = new DslParser();
            var parser = dslParser.CreateDslParserFromBnf(input, typeof(PigletDslBuilder));
            terminals = dslParser.Terminals;
            var table = parser.ParseTable;

            var states = new List<List<SetMember>>();

            // For each state in the table, generate a class with the given commands
            // and the resulting 
            for (int state = 0; state < table.StateCount; ++state)
            {
                var items = new List<SetMember>();

                for (int terminal = 0; terminal < terminals.Count; ++terminal)
                {
                    var action = table.Action[state, terminal];
                    if (action != short.MinValue)
                    {
                        // Ignore the accept
                        if (action == short.MaxValue)
                            continue;

                        int newState = FindEndOfReduceChain(action, terminal, state, table);

                        if (newState < 0)
                            continue;



                        items.Add(new SetMember(newState, terminals[terminal].DebugName));
                    }
                }

                //                if (items.Any())
                {
                    Console.WriteLine("interface SyntaxState{0}", states.Count);
                    Console.WriteLine("{");

                    foreach (var setMember in items)
                    {
                        Console.Write("\t");
                        Console.Write("SyntaxState{0} ", setMember.NextState);
                        Console.WriteLine(setMember.Content);
                    }

                    Console.WriteLine("}");
                    states.Add(items);
                }
            }

            // Reduce the number of states
            // Using quite retarded logic
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

                        if (a.Count == b.Count)
                        {
                            bool match = true;
                            for (int x = 0; x < a.Count; ++x)
                            {
                                if (!a[x].Equals(b[x]))
                                {
                                    match = false;
                                    break;
                                }
                            }

                            if (match)
                            {
                                Console.WriteLine("Replacing state {0} with state {1}", j, i);

                                // Kill mr b, replace all references to B with A
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

            // Screw around with inheritance


            // Print shit
            for (int i = 0; i < states.Count; ++i)
            {
                var state = states[i];
                if (state != null)
                {
                    Console.WriteLine("interface SyntaxState{0}", i);
                    Console.WriteLine("{");

                    foreach (var setMember in state)
                    {
                        Console.WriteLine("\tSyntaxState{0} {1}", setMember.NextState, setMember.Content);
                    }

                    Console.WriteLine("}");
                }

            }

            int apa = 3;
        }

        

        private static int FindEndOfReduceChain(int action, int terminal, int state, IParseTable<object> table)
        {
            if (action > 0)
                return action;

            if (action == short.MinValue)
                return -1;
            if (action == short.MaxValue)
                return -2;

            // It is a reduce
            state = table.Goto[state, table.ReductionRules[-(action + 1)].TokenToPush];
            if (state == short.MinValue)
                return -1;
            action = table.Action[state, terminal];

            return FindEndOfReduceChain(action, terminal, state, table);

        }
    }

    internal class PigletDslBuilder
    {
        [BuilderMethod("Followed")]
        public void NextElement()
        {
        }

        [BuilderMethod("IsMadeUp")]
        public void StartRule()
        {
        }

        [BuilderMethod("ByLiteral", "By")]
        public void AddLiteral(string literal)
        {
        }

        [BuilderMethod("ByTExpressionType", "By")]
        public void AddType<TExpressionType>()
        {
        }

        [BuilderMethod("ByExpression", "By")]
        public void AddExpression(IExpressionConfigurator expression)
        {
        }

        [BuilderMethod("ByTypedListOf", "ByListOf")]
        public void AddTypedList<TListType>(IRule listElement)
        {
        }

        [BuilderMethod("ByListOf", "ByListOf")]
        public void AddListOf(IRule listElement)
        {
        }

        [BuilderMethod("As")]
        public void SetProductionElementName(string name)
        {
        }

        [BuilderMethod("ThatIs")]
        public void StartElementSpecification()
        {
        }

        [BuilderMethod(("Optional"))]
        public void SetOptionalFlag()
        {
        }

        [BuilderMethod(("And"))]
        public void NextElementAttribute()
        {
        }

        [BuilderMethod("SeparatedBy")]
        public void SetListSeparator(string separator)
        {
        }

        [BuilderMethod("Or")]
        public void BeginNextProduction()
        {
        }

        [BuilderMethod("WhenFound")]
        public void SetReductionRule(Func<dynamic, object> func)
        {
        }
    }
}
