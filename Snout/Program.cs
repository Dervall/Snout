using System;
using System.Collections.Generic;
using System.Reflection;
using Piglet.Parser.Configuration;
using Piglet.Parser.Configuration.Fluent;
using Piglet.Parser.Construction;

namespace Snout
{
    class Program
    {
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

            var dslBuilder = new DslBuilder(input, typeof(PigletDslBuilder));
            string dslCode = dslBuilder.CreateDslCode();

            Console.WriteLine(dslCode);
        }
    }

    [BuilderPrefix("PigletSyntaxState")]
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
