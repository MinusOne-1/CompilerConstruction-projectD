using Compiler.Core.CodeAnalysis.LexicalAnalysis;

namespace Compiler.Core.CodeAnalysis.SyntaxAnalysis;

public partial class SyntaxAnalisis
{
    public Tree Tree { get; private set; }
    public Lexer lexer { get; private set; }

    private void SaveTree(ProgramNode root)
    {
        Tree = new Tree(root);
    }

    private ProgramNode CreateAbstractSyntaxTree()
    {
        /*TODO: Написать логику построение AST*/
        var rootNode = new ProgramNode();
        return rootNode;
    }

    public SyntaxAnalisis(Lexer lex)
    {
        lexer = lex;
        var rootNode = CreateAbstractSyntaxTree();
        SaveTree(rootNode);
    }
}