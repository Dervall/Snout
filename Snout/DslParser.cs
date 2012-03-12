using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Piglet.Parser;
using Piglet.Parser.Configuration;

namespace Snout
{
    class DslParser
    {
        public List<ITerminal<object>> Terminals { get; set; }

        public IParser<object> CreateDslParserFromBnf(string input, Type builderType)
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

            foreach (var memberInfo in builderType.GetMethods().OfType<MemberInfo>().Union(builderType.GetProperties()))
            {
                var attribute = memberInfo.GetCustomAttributes(true).OfType<BuilderMethodAttribute>().FirstOrDefault();
                if (attribute != null)
                {
                    var terminal = dslConfigurator.CreateTerminal(Regex.Escape(attribute.DslName));
                    terminal.DebugName = attribute.DslName;
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
                ((List<List<object>>)f[2]).ForEach(a => ((INonTerminal<object>)f[0]).AddProduction(a.ToArray()));
                return null;
            });

            ruleElementList.AddProduction(ruleElementList, "|", optionalRuleElement).SetReduceFunction(f => ((List<List<object>>)f[0]).Union((List<List<object>>)f[2]).ToList());
            ruleElementList.AddProduction(optionalRuleElement).SetReduceFunction(f => f[0]);

            optionalRuleElement.AddProduction(ruleElement).SetReduceFunction(f => new List<List<object>> {(List<object>) f[0]});
            optionalRuleElement.AddProduction().SetReduceFunction(f => new List<List<object>>());

            ruleElement.AddProduction(ruleElement, name).SetReduceFunction(f => ((List<object>)f[0]).Union(new List<object> {f[1]}).ToList());
            ruleElement.AddProduction(name).SetReduceFunction(f => new List<object> {f[0]});

            var bnfParser = configurator.CreateParser();
            bnfParser.Parse(input);
            
            return dslConfigurator.CreateParser();
        }
    }

}
