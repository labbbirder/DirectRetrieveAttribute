using com.bbbirder;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using DiagAccessible = DirectAttribute.sg.Diagnostics.NotAccessible;
using DiagGenerate = DirectAttribute.sg.Diagnostics.NotGenerated;

namespace DirectAttribute.sg
{
    [Generator]
    public class Generator : ISourceGenerator
    {
        private const string ValidAssemblyName = "com.bbbirder.directattribute";
        private readonly DiagnosticDescriptor DiagnosticNotAccessible = new(
            DiagAccessible.AnalyzerID, DiagAccessible.AnalyzerTitle, DiagAccessible.AnalyzerMessageFormat,
            "bbbirder", DiagnosticSeverity.Error, true);
        private readonly DiagnosticDescriptor DiagnosticNotGenerated = new(
            DiagGenerate.AnalyzerID, DiagGenerate.AnalyzerTitle, DiagGenerate.AnalyzerMessageFormat,
            "bbbirder", DiagnosticSeverity.Error, true);

        private static Dictionary<ITypeSymbol, bool> retrievableTypesCache = new();
        private static bool IsTypeRetrievable(ITypeSymbol symbol)
        {
            if (!retrievableTypesCache.TryGetValue(symbol, out var result))
            {
                result = false;
                if (!result)
                {
                    foreach (var baseType in symbol.GetBaseTypes(false))
                    {
                        if (IsTypeRetrievable(baseType))
                        {
                            result = true;
                            break;
                        }
                    }
                }

                if (!result)
                {
                    foreach (var interf in symbol.AllInterfaces)
                    {
                        if (IsTypeRetrievable(interf))
                        {
                            result = true;
                            break;
                        }
                    }
                }

                result |= symbol.GetAttribute<RetrieveSubtypeAttribute>() != null;
                retrievableTypesCache[symbol] = result;
            }

            return result;
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var containsValidReference = context.Compilation.ReferencedAssemblyNames.Any(n => n.Name.Equals(ValidAssemblyName));
            //if (!containsValidReference) return;

            try
            {
                var receiver = context.SyntaxReceiver as AttributeReceiver;
                if (receiver is null)
                {
                    return;
                }

                var builder = new StringBuilder();
                var typeSymbols = new HashSet<INamedTypeSymbol>();
                var memberSymbols = new HashSet<ISymbol>();
                foreach (var member in receiver.memberDeclarationsWithAttribute)
                {
                    var model = context.Compilation.GetSemanticModel(member.SyntaxTree);
                    if (member is BaseFieldDeclarationSyntax fd)
                    {
                        foreach (var v in fd.Declaration.Variables)
                        {
                            var symbol = model.GetDeclaredSymbol(v);
                            VisitSymbol(symbol);
                        }
                    }
                    else
                    {
                        var symbol = model.GetDeclaredSymbol(member);
                        VisitSymbol(symbol);
                    }
                }

                foreach (var type in receiver.typeDeclarations)
                {
                    var model = context.Compilation.GetSemanticModel(type.SyntaxTree);
                    var symbol = model.GetDeclaredSymbol(type);
                    if (IsTypeRetrievable(symbol))
                    {
                        typeSymbols.Add(symbol);
                    }
                }

                if (typeSymbols.Count == 0 && memberSymbols.Count == 0)
                    return;

                builder.AppendLine("using System;");
                builder.AppendLine("using System.Reflection;");
                builder.AppendLine("[assembly: AssemblyMetadata(\"direct-attribute\",\"1\")]");
                builder.AppendLine("namespace com.bbbirder {");
                builder.AppendLine("#if UNITY_5_3_OR_NEWER");
                builder.AppendLine("[UnityEngine.Scripting.Preserve]");
                builder.AppendLine("#endif");
                builder.AppendLine("internal static class RetrievableMetadata {");
                builder.AppendLine("    public static (Type,string)[] records = new  (Type, string)[] {");

                foreach (var type in typeSymbols)
                {
                    var typePattern = type.IsInternalAccessible()
                        ? $"typeof({type.GetFullNameWithoutGenericParameters()})"
                        : $"Type.GetType(\"{type.GetAssemblyQualifiedName()}\")"
                        ;
                    builder.AppendLine($"       ({typePattern}, null),");
                }

                foreach (var member in memberSymbols)
                {
                    var type = member.ContainingType;
                    var typePattern = type.IsInternalAccessible()
                        ? $"typeof({type.GetFullNameWithoutGenericParameters()})"
                        : $"Type.GetType(\"{type.GetAssemblyQualifiedName()}\")"
                        ;
                    builder.AppendLine($"       ({typePattern}, \"{member.Name}\"),");
                }

                builder.AppendLine("    };"); // end of records
                builder.AppendLine("}"); // end of RetrieveMetadata
                builder.AppendLine("}"); // end of namespace com.bbbirder

                context.AddSource(context.Compilation.AssemblyName + ".assembly-attributes.g.cs", builder.ToString());

                void VisitSymbol(ISymbol symbol)
                {
                    var attr = symbol.GetAttribute<DirectRetrieveAttribute>();
                    if (attr is null) return;

                    if (symbol is INamedTypeSymbol typeSymbol)
                    {
                        typeSymbols.Add(typeSymbol);
                    }
                    else
                    {
                        memberSymbols.Add(symbol);
                    }
                }

            }
            catch (Exception e)
            {
                //Debugger.Launch();
                context.ReportDiagnostic(Diagnostic.Create(DiagnosticNotGenerated,
                    null, e.Message + "\t" + e.StackTrace.Replace("\n", "\t")));
            }
            //void ReportNotAccessible(Location loc, string typeName)
            //{
            //    context.ReportDiagnostic(Diagnostic.Create(
            //        DiagnosticNotAccessible,
            //        loc,
            //        typeName
            //    ));
            //}
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new AttributeReceiver());
        }

    }
}
