using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace DirectAttribute.sg
{
    internal class AttributeReceiver : ISyntaxReceiver
    {
        //public class Record
        //{
        //    public BaseTypeDeclarationSyntax TypeDeclaration;
        //    public bool confirmed = false;
        //    public void Deconstruct(out BaseTypeDeclarationSyntax td,out bool cf)
        //    {
        //        td = TypeDeclaration;
        //        cf = confirmed;
        //    }
        //}
        //public List<Record> TypeDeclarations { get; } = new();
        public List<TypeDeclarationSyntax> typeDeclarations = new();
        public List<MemberDeclarationSyntax> memberDeclarationsWithAttribute = new();
        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is AttributeListSyntax)
            {
                var member = syntaxNode.FirstAncestorOrSelf<MemberDeclarationSyntax>();
                // Debugger.Launch();
                if (member != null)
                {
                    memberDeclarationsWithAttribute.Add(member);
                }
            }
            if (syntaxNode is TypeDeclarationSyntax td)
            {
                typeDeclarations.Add(td);
            }
        }
    }
}
