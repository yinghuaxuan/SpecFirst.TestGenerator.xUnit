﻿namespace SpecFirst
{
    using System;
    using Microsoft.CodeAnalysis;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;
    using SpecFirst.Core;
    using SpecFirst.Core.DecisionTable;
    using SpecFirst.Core.Setting;
    using SpecFirst.MarkdownParser;
    using SpecFirst.TestGenerator.xUnit;

    [Generator]
    public sealed class XUnitGenerator : ISourceGenerator
    {
        private static readonly DiagnosticDescriptor NoMarkdownParserFound = new(
            id: "NO_MARKDOWN_PARSER",
            title: "Couldn't find a valid markdown parser",
            messageFormat: "Couldn't find a markdown parser implementing IDecisionTableMarkdownParser interface from referenced assemblies of project {0}",
            category: "MarkdownParser",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);
        private static readonly DiagnosticDescriptor NoTestsGeneratorFound = new(
            id: "NO_TESTS_GENERATOR",
            title: "Couldn't find a valid tests generator",
            messageFormat: "Couldn't find a tests generator implementing ITestsGenerator interface from referenced assemblies of project {0}",
            category: "TestsGenerator",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);
        private static readonly DiagnosticDescriptor UnableParseMarkdownText = new(
            id: "MARKDOWN_PARSER_ERROR",
            title: "Couldn't parse markdown text",
            messageFormat: "Couldn't process the markdown file {0} due to error {1}",
            category: "MarkdownParser",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);
        private static readonly DiagnosticDescriptor UnableGenerateTests = new(
            id: "TESTS_GENERATOR_ERROR",
            title: "Couldn't generate tests",
            messageFormat: "Couldn't generate tests for the markdown file {0} due to error {1}",
            category: "MarkdownParser",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        private IDecisionTableMarkdownParser _markdownParser;
        private ITestsGenerator _testsGenerator;
        private SpecFirstSettingManager _settingManager;

        public void Initialize(GeneratorInitializationContext context)
        {
            //Debugger.Launch();
        }

        public void Execute(GeneratorExecutionContext context)
        {
            AdditionalText settingFile =
                context
                    .AdditionalFiles
                    .FirstOrDefault(f => f.Path.EndsWith("specfirst.config", System.StringComparison.OrdinalIgnoreCase));
            _settingManager = new SpecFirstSettingManager(settingFile.Path, context.Compilation.AssemblyName);
            _markdownParser = new DecisionTableMarkdownParser();
            _testsGenerator = new XUnitTestsGenerator()!;

            IEnumerable<AdditionalText> markdownFiles =
                context.AdditionalFiles.Where(at => at.Path.EndsWith(_settingManager.Settings.SpecFileExtension));
            foreach (AdditionalText file in markdownFiles)
            {
                ProcessMarkdownFile(file, context);
            }
        }

        private void ProcessMarkdownFile(AdditionalText markdownFile, GeneratorExecutionContext context)
        {
            if (!TryParseMarkdownFile(context, markdownFile, out List<DecisionTable> tables))
            {
                return;
            }

            if (!TryGenerateTests(context, markdownFile, tables, out List<string> sources))
            {
                return;
            }

            PersistTestFiles(markdownFile, sources, context);
        }

        private bool TryParseMarkdownFile(GeneratorExecutionContext context, AdditionalText markdownFile, out List<DecisionTable> tables)
        {
            tables = new List<DecisionTable>();
            try
            {
                var markdownText = markdownFile.GetText(context.CancellationToken)?.ToString();
                tables.AddRange(_markdownParser.Parse(markdownText!));
                return true;
            }
            catch (Exception e)
            {
                context.ReportDiagnostic(Diagnostic.Create(UnableParseMarkdownText, Location.None, markdownFile.Path, e.ToString()));
            }

            return false;
        }

        private bool TryGenerateTests(GeneratorExecutionContext context, AdditionalText markdownFile, List<DecisionTable> tables, out List<string> sources)
        {
            sources = new List<string>();
            try
            {
                sources.AddRange(_testsGenerator.Generate(_settingManager.Settings, tables));
                return true;
            }
            catch (Exception e)
            {
                context.ReportDiagnostic(Diagnostic.Create(UnableGenerateTests, Location.None, markdownFile.Path, e.ToString()));
            }

            return false;
        }

        private void PersistTestFiles(AdditionalText markdownFile, IEnumerable<string> sources, GeneratorExecutionContext context)
        {
            var filePath = _settingManager.GetTestFilePath(markdownFile.Path);

            Directory.CreateDirectory(filePath); // create the directory in case it doesn't exist

            PersistTestFile(markdownFile, filePath, sources.ElementAt(0), context);

            PersistTestImplFile(markdownFile, filePath, sources.ElementAt(1), context);
        }

        private void PersistTestFile(AdditionalText markdownFile, string filePath, string tests, GeneratorExecutionContext context)
        {
            string testFileName = _settingManager.GetTestFileName(markdownFile.Path);
            //context.AddSource($"{testFileName}", SourceText.From(tests, Encoding.UTF8));
            var testFile = Path.Combine(filePath, testFileName);
            File.WriteAllText(testFile, tests, Encoding.UTF8);
        }

        private void PersistTestImplFile(AdditionalText markdownFile, string filePath, string implementations, GeneratorExecutionContext context)
        {
            string implementationFileName = _settingManager.GetTestImplFileName(markdownFile.Path);
            var implementationFile = Path.Combine(filePath, implementationFileName);
            if (!File.Exists(implementationFile))
            {
                //context.AddSource($"{implementationFileName}", SourceText.From(implementations, Encoding.UTF8));
                File.WriteAllText(implementationFile, implementations, Encoding.UTF8);
            }
        }
    }
}