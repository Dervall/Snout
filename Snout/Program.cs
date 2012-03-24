using System;
using System.IO;
using Jolt;
using System.Reflection;
using System.Linq;
using NDesk.Options;

namespace Snout
{
    class Program
    {
        static void Main(string[] args)
        {
            string assemblyPath = null;
            string docPath = null;
            string outputPath = null;

            var optionSet = new OptionSet
            {
                { "a=|assembly=", "Path to assembly to use", f => assemblyPath = f },
                { "d=|doc=", "Path to documentation to use", f => docPath = f },
                { "o=|output=", "Output path", f => outputPath = f }
            };

            try
            {
                optionSet.Parse(args);
            }
            catch (OptionException)
            {
                Usage(optionSet);
                return;
            }

            if (assemblyPath == null)
            {
                Usage(optionSet);
                return;
            }

            assemblyPath = Path.GetFullPath(assemblyPath);

            if (docPath == null)
            {
                docPath = Path.Combine(Path.GetDirectoryName(assemblyPath),Path.GetFileNameWithoutExtension(assemblyPath) + ".xml");
            }

            if (outputPath == null)
            {
                outputPath = Directory.GetCurrentDirectory();
            }

            Console.WriteLine("Generating DSL from assembly {0}", assemblyPath);
            var targetAssembly = Assembly.LoadFile(assemblyPath);

            Console.WriteLine("Loading documentation from {0}", docPath);
            var commentReader = new XmlDocCommentReader(docPath);

            foreach (var type in targetAssembly.GetTypes())
            {
                var comments = commentReader.GetComments(type);
                if (comments != null)
                {
                    var builderClassNode = comments.Descendants().FirstOrDefault(f => f.Name == "builderclass");
                    if (builderClassNode != null)
                    {
                        var outputFile = string.Format("{0}.cs", builderClassNode.Attributes().Single(f => f.Name == "name").Value);

                        var dslBuilder = new DslBuilder(builderClassNode, commentReader, type);
                        string dslCode = dslBuilder.CreateDslCode();

                        using (var fileWriter = new StreamWriter(new FileStream(Path.Combine(outputPath, outputFile), FileMode.Create, FileAccess.Write)))
                        {
                            fileWriter.Write(dslCode);
                        }
                    }
                }
            }
        }

        private static void Usage(OptionSet optionSet)
        {
            Console.WriteLine("Usage");
            optionSet.WriteOptionDescriptions(Console.Out);
                
        }
    }
}
