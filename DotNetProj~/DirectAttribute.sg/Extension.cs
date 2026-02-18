using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
            if (data == null) return null;

            var type = typeof(T);

            var constructorArgs = data.ConstructorArguments
                .Select(GetActualValue)
                .ToArray();

            var attribute = (T)Activator.CreateInstance(type, constructorArgs);

            foreach (var namedArg in data.NamedArguments)
            {
                var value = GetActualValue(namedArg.Value);
                var name = namedArg.Key;

                var prop = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
                if (prop != null)
                {
                    prop.SetValue(attribute, value);
                    continue;
                }

                var field = type.GetField(name, BindingFlags.Public | BindingFlags.Instance);
                if (field != null)
                {
                    field.SetValue(attribute, value);
                }
            }

            return attribute;
        }

        private static object GetActualValue(TypedConstant constant)
        {
            if (constant.IsNull) return null;

            if (constant.Kind == TypedConstantKind.Array)
            {
                var values = constant.Values;
                var elementType = GetRuntimeType(constant.Type) ?? typeof(object);
                var array = Array.CreateInstance(elementType, values.Length);

                for (int i = 0; i < values.Length; i++)
                {
                    array.SetValue(GetActualValue(values[i]), i);
                }

                return array;
            }

            if (constant.Kind == TypedConstantKind.Enum)
            {
                var enumType = GetRuntimeType(constant.Type);
                if (enumType != null)
                {
                    return Enum.ToObject(enumType, constant.Value);
                }
            }

            if (constant.Value is ITypeSymbol typeSymbol)
            {
                return GetRuntimeType(typeSymbol);
            }

            return constant.Value;
        }

        private static Type GetRuntimeType(ITypeSymbol symbol)
        {
            var name = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                .Replace("global::", "");
            return Type.GetType(name) ??
                   AppDomain.CurrentDomain.GetAssemblies()
                       .Select(a => a.GetType(name))
                       .FirstOrDefault(t => t != null);
        }

    }
}
