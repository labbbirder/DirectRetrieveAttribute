using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis;

namespace DirectAttribute.sg
{
    internal static class Extensions
    {
        public static string GetSimpleName(this INamespaceSymbol ns)
        {
            if (ns.IsGlobalNamespace)
            {
                return "";
            }

            return ns.ToString();
        }

        public static bool IsFullNameEquals<T>(this INamedTypeSymbol type)
        {

            return type.ContainingNamespace.GetSimpleName() == typeof(T).Namespace
                && type.Name == typeof(T).Name
                ;
        }

        public static string GetFullName(this ITypeSymbol type)
        {
            return type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat
                .WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted));
        }

        //public static string GetAssemblyQualifiedName(this INamedTypeSymbol type)
        //{
        //    var nestTypes = type.GetContainingTypes(true).Reverse().ToArray();
        //    var finalString = string.Join("+", nestTypes.Select(t => t.Name));
        //    var rootDeclType = nestTypes.First();
        //    var ns = rootDeclType.ContainingNamespace;
        //    while (ns != null && !ns.IsGlobalNamespace)
        //    {
        //        finalString = ns.Name + "." + finalString;
        //        ns = ns.ContainingNamespace;
        //    }
        //    return finalString + ", " + type.ContainingAssembly.Name;
        //}

        public static IEnumerable<INamedTypeSymbol> GetContainingTypes(this INamedTypeSymbol type, bool includeSelf = true)
        {
            if (!includeSelf)
            {
                type = type?.BaseType;
            }

            while (type != null)
            {
                yield return type;
                type = type.ContainingType;
            }
        }

        public static IEnumerable<ITypeSymbol> GetBaseTypes(this ITypeSymbol type, bool includeSelf = true)
        {
            if (!includeSelf)
            {
                type = type?.BaseType;
            }

            while (type != null)
            {
                yield return type;
                type = type.BaseType;
            }
        }

        public static bool IsInternalAccessible(this INamedTypeSymbol type)
        {
            foreach (var declType in type.GetContainingTypes())
            {
                if (declType.DeclaredAccessibility < Accessibility.Internal) return false;
            }

            return true;
        }

        public static bool IsTypeOrSubTypeOf<T>(this ITypeSymbol symbol)
        {
            foreach (var baseType in symbol.GetBaseTypes())
            {
                if (baseType is INamedTypeSymbol namedType && namedType.IsFullNameEquals<T>()) return true;
            }

            return false;
        }

        public static IEnumerable<T> GetAttributes<T>(this ISymbol type) where T : Attribute
        {
            return type.GetAttributes().Where(a => a.AttributeClass.IsTypeOrSubTypeOf<T>()).Select(ToAttribute<T>);
        }

        public static T GetAttribute<T>(this ISymbol type) where T : Attribute
        {
            return type.GetAttributes<T>().FirstOrDefault();
        }


        public static AttributeUsageAttribute GetAttributeUsage(this AttributeData a)
        {
            return a.AttributeClass.GetAttribute<AttributeUsageAttribute>();
        }

        public static AttributeTargets GetAttributeTargets(this AttributeData a)
        {
            var usage = a.GetAttributeUsage();
            if (usage != null) return usage.ValidOn;
            return AttributeTargets.All;
        }

        private static T ToAttribute<T>(this AttributeData data) where T : Attribute
        {
            if (data is null) return null;
            var constructorArguments = data.ConstructorArguments.Select(a => a.Kind == TypedConstantKind.Array ? a.Values : a.Value).ToArray();
            var attribute = default(T);
            try
            {
                attribute = Activator.CreateInstance(typeof(T), constructorArguments) as T;
            }
            catch
            {
                attribute = FormatterServices.GetUninitializedObject(typeof(T)) as T;
            }

            foreach (var pair in data.NamedArguments)
            {
                var name = pair.Key;
                var value = pair.Value.Value;
                attribute.GetType().GetProperty(name)?.SetValue(attribute, value);
                attribute.GetType().GetField(name)?.SetValue(attribute, value);
            }

            return attribute as T;
        }
    }
}
