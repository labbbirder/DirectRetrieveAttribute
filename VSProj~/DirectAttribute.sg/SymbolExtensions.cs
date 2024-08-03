using com.bbbirder;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Linq;
using System.Text;

namespace DirectAttribute.sg
{

    public static class SymbolExtensions
    {

        public static bool IsGlobalAccessible(this TypeDeclarationSyntax syntax, SemanticModel model)
        {
            var targetTypeSymbol = model.GetDeclaredSymbol(syntax);
            var usings = syntax.SyntaxTree.GetCompilationUnitRoot().DescendantNodes()
                .OfType<UsingDirectiveSyntax>().LastOrDefault();
            var position = usings is null ? 0 : usings.Span.End;
            var accessible = model.IsAccessible(position, targetTypeSymbol);
            return accessible;
        }
        public static bool IsGlobalAccessible(this INamedTypeSymbol symbol, SemanticModel model)
        {
            var usings = model.SyntaxTree.GetCompilationUnitRoot().DescendantNodes()
                .OfType<UsingDirectiveSyntax>().LastOrDefault();
            var position = usings is null ? 0 : usings.Span.End;
            var accessible = model.IsAccessible(position, symbol);
            return accessible;
        }

        public static bool HasDirectAttribute(this ISymbol symbol)
        {
            var attributes = symbol?.GetAttributes();
            if (attributes == null) return false;

            return attributes.Value.Any(a => {
                return a.AttributeClass.IsTypeOrSubtype(typeof(DirectRetrieveAttribute));
            });
        }

        /// <summary>
        /// Whether the symbol is a target type. Check basetype only.
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="basetype"></param>
        /// <returns></returns>
        public static bool IsTypeOrSubtype(this INamedTypeSymbol symbol, Type basetype)
        {
            if (symbol.IsSameTypeName(basetype))
            {
                return true;
            }
            if (symbol.BaseType is null)
            {
                return false;
            }
            return IsTypeOrSubtype(symbol.BaseType, basetype);
        }

        private static bool CheckDirectAttributeOnBaseType(this INamedTypeSymbol symbol)
        {
            if (symbol.BaseType != null)
            {
                if (symbol.BaseType.HasDirectAttribute())
                {
                    return true;
                }
                return symbol.BaseType.CheckDirectAttributeOnBaseType();
            }
            return false;
        }

        public static string GetFullNameWithoutGenericParameters(this INamedTypeSymbol symbol)
        {
            var builder = new StringBuilder();
            if (symbol.ContainingNamespace.IsGlobalNamespace)
            {
                builder.Append("global::");
            }
            else
            {
                builder.Append(symbol.ContainingNamespace.ToString());
                builder.Append(".");
            }
            AppendNestType(symbol, true);
            return builder.ToString();
            void AppendNestType(INamedTypeSymbol nestType, bool isTail = false)
            {
                if (nestType == null) return;
                AppendNestType(nestType.ContainingType);
                builder.Append(nestType.Name);
                if (nestType.IsGenericType && nestType.TypeParameters.Length > 0)
                {
                    builder.Append('<');
                    for (int i = 0; i < nestType.TypeParameters.Length; i++)
                    {
                        //var gp = nestType.TypeParameters[i];
                        //builder.Append('`');
                        //builder.Append(gp.Ordinal + 1);
                        if (i != nestType.TypeParameters.Length - 1)
                        {
                            builder.Append(',');
                        }
                    }
                    builder.Append('>');
                }
                if (!isTail) builder.Append(".");
            }
        }

        public static string GetAssemblyQualifiedName(this INamedTypeSymbol symbol)
        {
            var builder = new StringBuilder();
            if (symbol.ContainingNamespace.IsGlobalNamespace)
            {
                builder.Append("global::");
            }
            else
            {
                builder.Append(symbol.ContainingNamespace.ToString());
                builder.Append(".");
            }
            AppendNestType(symbol, true);
            builder.Append(", ");
            builder.Append(symbol.ContainingAssembly.Name);
            return builder.ToString();
            void AppendNestType(INamedTypeSymbol nestType, bool isTail = false)
            {
                if (nestType == null) return;
                AppendNestType(nestType.ContainingType);
                builder.Append(nestType.Name);
                if (nestType.IsGenericType && nestType.TypeParameters.Length > 0)
                {
                    builder.Append('`');
                    builder.Append(nestType.TypeParameters.Length);
                }
                if (!isTail) builder.Append("+");
            }
        }

        /// <summary>
        /// Whether the types have the same namespace and name
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="anotherType"></param>
        /// <returns></returns>
        public static bool IsSameTypeName(this ISymbol symbol, Type anotherType)
        {
            bool sameNamespace;
            if (string.IsNullOrEmpty(anotherType.Namespace) && string.IsNullOrEmpty(symbol.ContainingNamespace.ToDisplayString()))
            {
                sameNamespace = true;
            }
            else
            {
                sameNamespace = symbol.ContainingNamespace.ToDisplayString() == anotherType.Namespace;
            }

            return symbol.Name == anotherType.Name
                && sameNamespace;
        }

        public static bool IsDerivedFromType(this INamedTypeSymbol symbol, string typeName)
        {
            if (symbol.Name == typeName)
            {
                return true;
            }

            if (symbol.BaseType == null)
            {
                return false;
            }

            return symbol.BaseType.IsDerivedFromType(typeName);
        }
    }
}
