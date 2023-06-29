using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Resources;
using System.Text;
using System.Linq;
using com.bbbirder;
using DiagAccessible = DirectAttribute.sg.Diagnostics.NotAccessible;

namespace DirectAttribute.sg.NotAccessible
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class Analyzer : DiagnosticAnalyzer
    {
        readonly DiagnosticDescriptor DiagnosticNotAccessible = new(
            DiagAccessible.AnalyzerID, DiagAccessible.AnalyzerTitle, DiagAccessible.AnalyzerMessageFormat,
            "bbbirder", DiagnosticSeverity.Error, true);
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(DiagnosticNotAccessible);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeTypeDeclaration, SyntaxKind.Attribute);

        }
        public void AnalyzeTypeDeclaration(SyntaxNodeAnalysisContext context)
        {
            var node = context.Node as AttributeSyntax;
            var model = context.SemanticModel;
            if (node is null) return;
            var attributeSymbol = model.GetSymbolInfo(node).Symbol.ContainingType;
            if (attributeSymbol is null) return;
            if (!attributeSymbol.IsAttribute(typeof(DirectRetrieveAttribute))) return;
            var targetTypeDeclaration = node.FirstAncestorOrSelf<TypeDeclarationSyntax>();
            var accessible = targetTypeDeclaration.IsGlobalAccessible(model);
            if (!accessible)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticNotAccessible,
                    node.GetLocation(),
                    targetTypeDeclaration.Identifier.ValueText
                    ));
            }

        }
    }

}