using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace DirectAttributes.SourceGenerator
{
    internal class AttributeReceiver : ISyntaxReceiver
    {
        public List<TypeDeclarationSyntax> TypeDeclarations { get; } = new();
        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if(syntaxNode is AttributeListSyntax)
            {
                var t = syntaxNode.FirstAncestorOrSelf<TypeDeclarationSyntax>();
                if(t != null)
                {
                    Append(t);
                }
            }
            if (syntaxNode is TypeDeclarationSyntax td)
            {
                Append(td);
            }
        }
        void Append(TypeDeclarationSyntax td)
        {
            if (TypeDeclarations.Any(d => d.IsEquivalentTo(td))) return;
            TypeDeclarations.Add(td);
        }
        public void Clear() {
            TypeDeclarations.Clear();
        }
    }
}
