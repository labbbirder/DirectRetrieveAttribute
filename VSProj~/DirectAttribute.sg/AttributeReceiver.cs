using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace DirectAttribute.sg
{
    internal class AttributeReceiver : ISyntaxReceiver
    {
        public class Record
        {
            public BaseTypeDeclarationSyntax TypeDeclaration;
            public bool confirmed = false;
            public void Deconstruct(out BaseTypeDeclarationSyntax td,out bool cf)
            {
                td = TypeDeclaration;
                cf = confirmed;
            }
        }
        public List<Record> TypeDeclarations { get; } = new();
        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if(syntaxNode is AttributeListSyntax)
            {
                var t = syntaxNode.FirstAncestorOrSelf<BaseTypeDeclarationSyntax>();
                // Debugger.Launch();
                if (t != null)
                {
                    Append(t,true);
                }
            }
            if (syntaxNode is TypeDeclarationSyntax td)
            {
                Append(td,false);
            }
        }
        void Append(BaseTypeDeclarationSyntax td, bool confirmed)
        {
            var pre = TypeDeclarations.FirstOrDefault(d => d.TypeDeclaration.IsEquivalentTo(td));
            if(pre != null)
            {
                pre.confirmed |= confirmed;
                return;
            }
            TypeDeclarations.Add(new()
            {
                confirmed= confirmed,
                TypeDeclaration = td,
            });
        }
        public void Clear() {
            TypeDeclarations.Clear();
        }
    }
}
