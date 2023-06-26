using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using com.bbbirder;
using System.Diagnostics;

namespace DirectAttributes.SourceGenerator
{

    public static class SymbolExtensions
    {
        public class RetrieveAttributeData
        {
            public INamedTypeSymbol targetType;
            public string memberName;
        }
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
        //public static RetrieveAttributeData ParseDirectAttribute(this AttributeData attr)
        //{
        //    var args = attr.ConstructorArguments;
        //    if (args.Count() == 1)
        //    {
        //        var type = args[0].Value as INamedTypeSymbol;
        //        return new() { targetType = type };
        //    }
        //    else if (args.Count() == 2)
        //    {
        //        var type = args[0].Value as INamedTypeSymbol;
        //        return new() { targetType = type, memberName = (string)args[1].Value };
        //    }
        //    else
        //    {
        //        return null;
        //    }
        //}
        public static bool TryGetDirectAttribute(this ISymbol symbol, out AttributeData attr, bool checkIsInherit = false)
        {
            attr = default;
            var attributes = symbol?.GetAttributes();
            if (attributes == null) return false;

            attr = attributes.Value.FirstOrDefault(a =>
            {
                if (checkIsInherit)
                {
                    var isInherit = a.GetInherit();
                    return isInherit && a.AttributeClass.IsInheritAttribute(typeof(DirectRetrieveAttribute));
                }
                else
                {
                    return a.AttributeClass.IsInheritAttribute(typeof(DirectRetrieveAttribute));
                }
            });
            return attr != null;
        }
        public static bool IsInheritAttribute(this INamedTypeSymbol symbol, Type attributeType)
        {
            if (symbol.IsAttribute(attributeType))
            {
                return true;
            }
            if (symbol.BaseType is null)
            {
                return false;
            }
            return symbol.BaseType.IsInheritAttribute(attributeType);
        }
        public static SemanticModel GetModel(this SyntaxNode node, GeneratorExecutionContext context)
        {
            var tree = node.SyntaxTree;
            if (!context.Compilation.ContainsSyntaxTree(tree)) return null;
            return context.Compilation.GetSemanticModel(tree);
        }
        public static string GetDisplayStringWithoutTypeName(this INamedTypeSymbol symbol) {
            var builder = new StringBuilder();
            if(symbol.ContainingNamespace.IsGlobalNamespace) {
                builder.Append("global::");
            }
            else {
                builder.Append(symbol.ContainingNamespace.ToString());
                builder.Append(".");
            }
            AppendNestType(symbol, true);
            return builder.ToString();
            void AppendNestType(INamedTypeSymbol nestType, bool isTail = false) {
                if (nestType == null) return;
                AppendNestType(nestType.ContainingType);
                builder.Append(nestType.Name);
                if(nestType.IsGenericType && nestType.TypeParameters.Length > 0) {
                    var commaCount = nestType.TypeParameters.Length - 1;
                    builder.Append('<');
                    builder.Append(',', commaCount);
                    builder.Append('>');
                }
                if (!isTail) builder.Append(".");
            }
        }
        public static bool HasAttribute(this ISymbol symbol, string atrributeName)
        {
            return symbol.GetAttributes()
                .Any(_ => _.AttributeClass?.ToDisplayString() == atrributeName);
        }
        public static AttributeData FindAttribute(this ISymbol symbol, string atrributeName)
        {
            return symbol.GetAttributes()
                .FirstOrDefault(_ => _.AttributeClass?.ToDisplayString() == atrributeName);
        }
        public static bool GetInherit(this AttributeData attr)
        {
            return attr?.AttributeClass!=null && attr.AttributeClass.GetAttributeNamedArgument("Inherited");
        }
        public static bool GetAllowMultiple(this AttributeData attr)
        {
            return attr.AttributeClass.GetAttributeNamedArgument("AllowMultiple");
        }
        static bool GetAttributeNamedArgument(this INamedTypeSymbol attrClass, string argumentName, bool omitValue = false)
        {
            var usage = attrClass.FindAttribute("System.AttributeUsageAttribute");
            if (usage != null)
            {
                var arg = usage.NamedArguments.FirstOrDefault(arg=>arg.Key==argumentName);
                if (arg.Key!=argumentName) return omitValue;
                return (bool)arg.Value.Value;
            }
            if (attrClass.BaseType!=null)
            {
                return attrClass.BaseType.GetAttributeNamedArgument(argumentName);
            }
            return omitValue;
        }
        public static bool CheckDirectAttributeDeeply(this ISymbol symbol,out AttributeData attr)
        {
            attr = default;
            if (!(symbol is INamedTypeSymbol typeSymbol)) return false ;
            if(symbol.TryGetDirectAttribute(out attr))
            {
                return true;
            }
            AttributeData temp = null ;
            if(typeSymbol.AllInterfaces.Any(i=>i.TryGetDirectAttribute(out temp,true)))
            {
                attr = temp;
                return true;
            }
            return typeSymbol.CheckDirectAttributeOnBaseType(out attr);
        }
        static bool CheckDirectAttributeOnBaseType(this INamedTypeSymbol symbol,out AttributeData attr)
        {
            if (symbol.BaseType != null)
            {
                if (symbol.BaseType.TryGetDirectAttribute(out attr, true))
                {
                    return true;
                }
                return symbol.BaseType.CheckDirectAttributeOnBaseType(out attr);
            }
            attr = default;
            return false;
        }
        public static bool IsAttribute(this ISymbol symbol,Type attributeType)
        {
            bool sameNamespace;
            if (string.IsNullOrEmpty(attributeType.Namespace) && string.IsNullOrEmpty(symbol.ContainingNamespace.ToDisplayString()))
            {
                sameNamespace = true;
            }
            else
            {
                sameNamespace = symbol.ContainingNamespace.ToDisplayString() == attributeType.Namespace;
            }

            return symbol.Name == attributeType.Name
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

        public static bool IsImplements(this INamedTypeSymbol symbol, string typeName)
        {
            return symbol.AllInterfaces.Any(_ => _.ToDisplayString() == typeName);
        }
    }
}
