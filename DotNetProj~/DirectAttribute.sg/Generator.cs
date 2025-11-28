using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using BBBirder.DirectAttribute;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Scriban;

namespace DirectAttribute.sg
{
    [Generator(LanguageNames.CSharp)]
    public class Generator : IIncrementalGenerator
    {
        private const string EditTimeKeyword = "ROSLYN_EDIT_TIME";

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var isEditTimePipeline = context.CompilationProvider.Select((opt, ct) =>
            {
                var tree = opt.SyntaxTrees.FirstOrDefault();
                if (tree is null) return false;
                return tree.Options.PreprocessorSymbolNames
                    .Contains(EditTimeKeyword, StringComparer.InvariantCulture);
            });

            // We just need to generate on actual build, hence we don't need to build the whole pipelines.

            var outPipeline = isEditTimePipeline.Combine(context.CompilationProvider);
            context.RegisterImplementationSourceOutput(outPipeline, (ctx, source) =>
            {
                try
                {
                    var (isEditTime, comp) = source;
                    ProcessOutput(ctx, isEditTime, comp);
                }
                catch (Exception e)
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(
                        new DiagnosticDescriptor("BB001", "SGERR", e.ToString(), "SGERR", DiagnosticSeverity.Error,
                            true),
                        null));
                }
            });
        }

        private static void ProcessOutput(SourceProductionContext ctx, bool isEditTime, Compilation comp)
        {
            if (isEditTime)
            {
                ctx.AddSource($"{comp.AssemblyName}-direct-retrieve.g.cs", "// ignore empty output");
                return;
            }

            var sms = new Dictionary<SyntaxTree, SemanticModel>();
            var typeTexts = new Dictionary<INamedTypeSymbol, string>(comparer: SymbolEqualityComparer.Default);
            var privateTypes = new List<string>();

            // structure marked members

            var attrs = comp.SyntaxTrees.SelectMany(t =>
                t.GetRoot().DescendantNodesAndSelf().OfType<AttributeSyntax>());

            var markedMembers =
                new Dictionary<INamedTypeSymbol, List<object>>(comparer: SymbolEqualityComparer.Default);
            foreach (var attr in attrs)
            {
                var model = GetModel(attr.SyntaxTree);
                var attrType = model.GetTypeInfo(attr).Type as INamedTypeSymbol;

                if (!attrType.IsTypeOrSubTypeOf<DirectRetrieveAttribute>()) continue;

                var targetMember =
                    attr.Parent?.FirstAncestorOrSelf<AttributeListSyntax>()?.Parent as MemberDeclarationSyntax;
                if (targetMember is null) continue;


                if (!markedMembers.TryGetValue(attrType, out var list))
                {
                    markedMembers[attrType] = list = new();
                }

                IEnumerable<SyntaxNode> memberNodes = targetMember is FieldDeclarationSyntax field
                    ? field.Declaration.Variables
                    : new[] { targetMember };

                foreach (var memberNode in memberNodes)
                {
                    var member = model.GetDeclaredSymbol(memberNode);

                    list.Add(new
                    {
                        type_text = GetTypeText(member is INamedTypeSymbol nts ? nts : member.ContainingType),
                        member_name = member is INamedTypeSymbol ? null : member.Name,
                    });
                }

                //GC.KeepAlive(attr);
                //GC.KeepAlive(targetMember);
            }

            // structure subtypes

            var decls = comp.SyntaxTrees.SelectMany(t =>
                t.GetRoot().DescendantNodesAndSelf().OfType<BaseTypeDeclarationSyntax>());

            var collectedType =
                new Dictionary<INamedTypeSymbol, bool>(comparer: SymbolEqualityComparer.Default);
            var typeDerives =
                new Dictionary<INamedTypeSymbol, List<string>>(comparer: SymbolEqualityComparer.Default);
            foreach (var decl in decls)
            {
                var typeInfo = GetModel(decl.SyntaxTree).GetDeclaredSymbol(decl);

                var registered = false;
                foreach (var interfType in typeInfo.Interfaces.Where(IsTypeCollected))
                {
                    var interfDefType = GetDefination(interfType);
                    if (!typeDerives.TryGetValue(interfDefType, out var list))
                    {
                        typeDerives[interfDefType] = list = new();
                    }

                    list.Add(GetTypeText(typeInfo));
                    registered = true;
                }

                if (registered || typeInfo.BaseType is not { } baseType) continue;

                var baseDefType = GetDefination(baseType);
                var shouldCollect = typeDerives.ContainsKey(baseDefType)
                                    || typeInfo.GetBaseTypes(includeSelf: false).OfType<INamedTypeSymbol>()
                                        .Any(IsTypeCollected);
                if (shouldCollect)
                {
                    if (!typeDerives.TryGetValue(baseDefType, out var list))
                    {
                        typeDerives[baseDefType] = list = new();
                    }

                    list.Add(GetTypeText(typeInfo));
                }
            }

            if (!markedMembers.Any() && !typeDerives.Any())
            {
                ctx.AddSource($"{comp.AssemblyName}-direct-retrieve.g.cs", "// ignore empty output");
                return;
            }

            var content = Template.Parse(Templates.MarkedMembers).Render(new
            {
                privateTypes,
                target_members = markedMembers
                    .Select(kvp => new { attr_type_text = GetTypeText(kvp.Key), records = kvp.Value, }),
                subtype_infos = typeDerives
                    .Select(kvp => new { base_type_text = GetTypeText(kvp.Key), subtypes = kvp.Value }),
            });

            ctx.AddSource($"{comp.AssemblyName}-direct-retrieve.g.cs", content);

            bool IsTypeCollected(INamedTypeSymbol type)
            {
                if (!collectedType.TryGetValue(type, out var isCollected))
                {
                    isCollected =
                        type.GetAttribute<RetrieveSubtypeAttribute>() != null
                        || type.AllInterfaces.Any(IsTypeCollected)
                        || type.GetBaseTypes(false).OfType<INamedTypeSymbol>()
                            .Any(IsTypeCollected)
                        ;
                    collectedType[type] = isCollected;
                }

                return isCollected;
            }

            SemanticModel GetModel(SyntaxTree tree)
            {
                if (!sms.TryGetValue(tree, out var model))
                {
                    sms[tree] = model = comp.GetSemanticModel(tree);
                }

                return model;
            }

            string GetTypeText(INamedTypeSymbol type)
            {
                if (!typeTexts.TryGetValue(type, out var text))
                {
                    if (type.IsInternalAccessible())
                    {
                        text = $"typeof({type.GetFullNameWithoutGenericParameters()})";
                    }
                    else
                    {
                        text = $"@type{privateTypes.Count}";
                        privateTypes.Add(type.GetAssemblyQualifiedName());
                    }

                    typeTexts[type] = text;
                }

                return text;
            }
        }

        private static INamedTypeSymbol GetDefination(INamedTypeSymbol type)
        {
            if (type.IsGenericType)
            {
                return type.ConstructUnboundGenericType();
            }
            else
            {
                return type;
            }
        }
    }
}
