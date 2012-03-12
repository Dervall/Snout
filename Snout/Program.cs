using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Piglet.Parser;
using Piglet.Parser.Configuration;
using Piglet.Parser.Construction;

namespace Snout
{
    class Program
    {
        private static IList<ITerminal<int>> terminals;
        private static IParserConfigurator<int> configurator;

        static ITerminal<int> Terminal(string contents)
        {
            var t = configurator.CreateTerminal(Regex.Escape(contents));
            t.DebugName = contents;
            terminals.Add(t);
            return t;
        }

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
            configurator = ParserFactory.Configure<int>();

            var rule = configurator.CreateNonTerminal();
            rule.DebugName = "rule";

            var listElements = configurator.CreateNonTerminal();
            listElements.DebugName = "listElements";

            var by = configurator.CreateNonTerminal();
            by.DebugName = "by";
            var byList = configurator.CreateNonTerminal();
            var bySimple = configurator.CreateNonTerminal();

            var listBys = configurator.CreateNonTerminal();
            var simpleBys = configurator.CreateNonTerminal();

            var optionalNaming = configurator.CreateNonTerminal();

            var optionalListSettings = configurator.CreateNonTerminal();
            var listOfListSettings = configurator.CreateNonTerminal();
            var listSetting = configurator.CreateNonTerminal();

            var optionalSimpleSettings = configurator.CreateNonTerminal();

            var rulePart = configurator.CreateNonTerminal();
            var rulePartList = configurator.CreateNonTerminal();

            terminals = new List<ITerminal<int>>();

            var IsMadeUp = Terminal("IsMadeUp");
            var Followed = Terminal("Followed");

            var ByLiteral = Terminal("By(string literal)");
            var ByTExpressionType = Terminal("By<TExpressionType>()");
            var ByExpression = Terminal("By(IExpressionConfigurator expression)");
            var ByTypedListOf = Terminal("ByListOf<TListType>(IRule listElement)");
            var ByListOf = Terminal("ByListOf(IRule listElement)");

            var As = Terminal("As(string name)");

            var ThatIs = Terminal("ThatIs");
            var Optional = Terminal("Optional");
            var And = Terminal("And");
            var SeparatedBy = Terminal("SeparatedBy(string separator)");
            var Or = Terminal("Or");
            var WhenFound = Terminal("WhenFound(Func<dynamic, object> func)");

            rule.AddProduction(IsMadeUp, rulePartList);

            rulePartList.AddProduction(rulePartList, Or, rulePart);
            rulePartList.AddProduction(rulePart);

            var optionalWhenFound = configurator.CreateNonTerminal();
            rulePart.AddProduction(listElements, optionalWhenFound);

            optionalWhenFound.AddProduction(WhenFound);
            optionalWhenFound.AddProduction();

            listElements.AddProduction(listElements, Followed, by);
            listElements.AddProduction(by);

            by.AddProduction(ByLiteral);
            by.AddProduction(bySimple);
            by.AddProduction(byList);

            byList.AddProduction(listBys, optionalNaming, optionalListSettings);

            listBys.AddProduction(ByTypedListOf);
            listBys.AddProduction(ByListOf);

            optionalListSettings.AddProduction(ThatIs, listOfListSettings);
            optionalListSettings.AddProduction();

            listOfListSettings.AddProduction(listOfListSettings, And, listSetting);
            listOfListSettings.AddProduction(listSetting);

            listSetting.AddProduction(Optional);
            listSetting.AddProduction(SeparatedBy);

            bySimple.AddProduction(simpleBys, optionalNaming, optionalSimpleSettings);

            simpleBys.AddProduction(ByTExpressionType);
            simpleBys.AddProduction(ByExpression);

            optionalSimpleSettings.AddProduction(ThatIs);
            optionalSimpleSettings.AddProduction();



            optionalNaming.AddProduction(As);
            optionalNaming.AddProduction();



            configurator.LexerSettings.CreateLexer = false;

            var parser = configurator.CreateParser();

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

        private static int FindEndOfReduceChain(int action, int terminal, int state, IParseTable<int> table)
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
}
