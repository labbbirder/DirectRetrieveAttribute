using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Text.RegularExpressions;
using com.bbbirder;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using DiagAccessible = SourceGenerator.Diagnostics.NotAccessible;
using DiagGenerate = SourceGenerator.Diagnostics.NotGenerated;

namespace DirectAttributes.SourceGenerator {
    [Generator]
    public class Generator : ISourceGenerator {
        readonly DiagnosticDescriptor DiagnosticNotAccessible = new(
            DiagAccessible.AnalyzerID, DiagAccessible.AnalyzerTitle, DiagAccessible.AnalyzerMessageFormat,
            "bbbirder", DiagnosticSeverity.Error, true);
        readonly DiagnosticDescriptor DiagnosticNotGenerated = new(
            DiagGenerate.AnalyzerID, DiagGenerate.AnalyzerTitle, DiagGenerate.AnalyzerMessageFormat,
            "bbbirder", DiagnosticSeverity.Error, true);
        readonly Type AttributeType = typeof(DirectRetrieveAttribute);
        readonly AttributeReceiver receiver = new();

        public void Execute(GeneratorExecutionContext context) {
            //Debugger.Launch();
            try
            {
                if (context.SyntaxReceiver is not AttributeReceiver)
                {
                    return;
                }
                if (receiver.TypeDeclarations.Count == 0) return;
                var builder = new StringBuilder();
                builder.AppendLine("using com.bbbirder;");
                foreach(var td in receiver.TypeDeclarations)
                {
                    var model = td.GetModel(context);
                    var targetType = model?.GetDeclaredSymbol(td);
                    var hasDirect = targetType.CheckDirectAttributeDeeply(out var attr);
                    if (!hasDirect) continue;
                    if (!targetType.IsGlobalAccessible(model))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            DiagnosticNotAccessible,
                            attr.ApplicationSyntaxReference.GetSyntax().GetLocation(),
                            targetType.Name
                        ));
                        return;
                    }
                    var typeDisplay = targetType.GetDisplayStringWithoutTypeName();
                    builder.AppendAttribute(typeDisplay);
                    foreach(var member in targetType.GetMembers())
                    {
                        if (!member.TryGetDirectAttribute(out var memAttr)) continue;
                        builder.AppendAttribute(typeDisplay, member.Name);

                    }
                }
                receiver?.Clear();
                context.AddSource("assembly-attributes.g.cs", builder.ToString());
            }
            catch (Exception e) {
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticNotGenerated,
                    null, e.Message + "\n" + e.StackTrace));
            }
        }

        public void Initialize(GeneratorInitializationContext context) {
            receiver?.Clear();
            context.RegisterForSyntaxNotifications(() => receiver);
        }

    }

    static class TextGenerator {
        const string attributeLine1 = "[assembly: GeneratedDirectRetrieve(typeof({0}))]";
        const string attributeLine2 = "[assembly: GeneratedDirectRetrieve(typeof({0}),\"{1}\")]";
        internal static void AppendAttribute(this StringBuilder builder, string typeName, string memberName = null) {
            if (memberName is null) {
                builder.AppendFormat(attributeLine1, typeName);
            }
            else {
                builder.AppendFormat(attributeLine2, typeName, memberName);
            }
            builder.AppendLine();
        }
    }
}
