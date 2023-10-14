using System.ComponentModel;
using System.Text;
using Compiler.Core.CodeAnalysis.LexicalAnalysis.LexerTokens;
using Compiler.Core.CodeAnalysis.LexicalAnalysis.LexerTokens.Library;
using Newtonsoft.Json;

namespace Compiler.Core.CodeAnalysis.SyntaxAnalysis;

public class Tree
{
    public ProgramNode Root { get; }

    public Tree(ProgramNode root)
    {
        Root = root;
    }

    public override string ToString() => Root.ToString();
}

public abstract class Node
{
    public abstract IEnumerable<Node> GetChildren();

    protected static IEnumerable<Node> GetChildren(IEnumerable<Node?>? childrenList, params Node?[] children) =>
        (childrenList ?? Enumerable.Empty<Node?>())
        .Concat(children)
        .Where(c => c != null)
        .Select(c => c!)
        .ToList();

    public IEnumerable<T> GetChildren<T>()
        where T : Node =>
        GetChildren().Where(x => x is T).Cast<T>();

    public override string ToString() => ToString(new StringBuilder(), true, string.Empty).ToString();

    private StringBuilder ToString(StringBuilder builder, bool isLast, string indent)
    {
        var marker = isLast ? "└──" : "├──";
        var name = GetType().Name.Replace("Node", string.Empty);
        builder.AppendLine($"{indent}{marker}{name}");
        indent += isLast ? "    " : "│   ";

        var children = GetChildren();
        var lastChild = children.LastOrDefault();

        if (this is ILeafNode leaf)
        {
            builder.Length -= 2;
            if (leaf.Kind != null) builder.Append($": {leaf.Kind}");
            builder.Append(' ');
            builder.AppendLine(leaf.Token.ToString());
            return builder;
        }

        foreach (var child in GetChildren())
            child.ToString(builder, child == lastChild, indent);

        return builder;
    }
}

public class ListNode<T> : ExpressionNode
    where T : Node
{
    public List<T> Items { get; } = new();

    public override IEnumerable<Node> GetChildren() =>
        GetChildren(Items);

    public ListNode()
    {
    }

    public ListNode(T item, ListNode<T>? items)
    {
        Items.Add(item);

        if (items is not null)
            Items.AddRange(items.GetChildren().Cast<T>());
    }
}

public class PrintNode : DeclarationNode
{
    public List<Node> Items { get; } = new();

    public override IEnumerable<Node> GetChildren() =>
        GetChildren(Items);

    public PrintNode(IdentifierNode Identifier_) : base(Identifier_)
    {
    }
}

public class TupleElementNode : VariableDeclarationNode
{
    public override IEnumerable<Node> GetChildren() =>
        GetChildren(null, Identifier, Type, Expression);

    public TupleElementNode(IdentifierNode identifier)
        : base(identifier)
    {
    }

    public TupleElementNode(VariableDeclarationNode node)
        : base(node.Identifier)
    {
        Expression = node.Expression;
        Type = node.Type;
    }
}

public class TupleNode : ExpressionNode
{
    public Dictionary<IdentifierNode, TupleElementNode> Items { get; } = new();

    public override IEnumerable<Node> GetChildren() =>
        GetChildren(Items.Values);

    public TupleNode()
    {
    }
}

public interface ILeafNode
{
    public Token Token { get; }
    public Enum? Kind { get; }
}

public class ProgramNode : Node
{
    public List<DeclarationNode> DeclarationList { get; } = new();

    public override IEnumerable<Node> GetChildren() =>
        GetChildren(DeclarationList);

    public ProgramNode()
    {
    }

    public ProgramNode(ListNode<DeclarationNode> declarationList)
    {
        DeclarationList = declarationList.Items;
    }
}

public abstract class DeclarationNode : Node
{
    public IdentifierNode? Identifier { get; }

    protected DeclarationNode(IdentifierNode identifier)
    {
        Identifier = identifier;
    }

    protected DeclarationNode()
    {
    }
}

public class VariableDeclarationNode : DeclarationNode
{
    public ExpressionNode? Expression { get; set; }
    public TypeNode? Type { get; set; }

    public override IEnumerable<Node> GetChildren() =>
        GetChildren(null, Identifier, Type, Expression);

    public VariableDeclarationNode(IdentifierNode identifier)
        : base(identifier)
    {
    }
}
public class VariableAssignmentNode : VariableDeclarationNode
{
    public ExpressionNode? Expression { get; set; }
    public TypeNode? Type { get; set; }

    public override IEnumerable<Node> GetChildren() =>
        GetChildren(null, Identifier, Type, Expression);

    public VariableAssignmentNode(IdentifierNode identifier)
        : base(identifier)
    {
    }

    public VariableAssignmentNode(VariableDeclarationNode node) : base(node.Identifier)
    {
        Type = node.Type;
        Expression = node.Expression;
    }
}

public enum TypeKind
{
    [Description("integer")] Integer,
    [Description("real")] Real,
    [Description("boolean")] Boolean,
    [Description("array")] Array,
    [Description("tuple")] Tuple,
    [Description("empty")] Empty
}

public class TypeNode : Node
{
    public TypeKind Kind { get; }
    public IdentifierNode Identifier { get; }

    public override IEnumerable<Node> GetChildren() =>
        GetChildren(null, Identifier);

    public TypeNode(TypeKind kind, IdentifierNode identifier)
    {
        Kind = kind;
        Identifier = identifier;
    }
}

public class ArrayTypeNode : TypeNode
{
    public TypeNode ElementsType { get; }
    public ExpressionNode SizeExpression { get; }

    public override IEnumerable<Node> GetChildren() =>
        GetChildren(null, Identifier, ElementsType, SizeExpression);

    public ArrayTypeNode(IdentifierNode identifier, TypeNode elementsType, ExpressionNode sizeExpression)
        : base(TypeKind.Array, identifier)
    {
        ElementsType = elementsType;
        SizeExpression = sizeExpression;
    }
}

public class BodyNode : ListNode<Node>
{
    public BodyNode()
    {
    }

    public BodyNode(DeclarationNode declaration, BodyNode? remaining)
        : base(declaration, remaining)
    {
    }

    public BodyNode(Node statement, BodyNode? remaining) // statement is IStatementNode
        : base(statement, remaining)
    {
    }
}

public interface IStatementNode
{
}

public class AssignmentNode : Node, IStatementNode
{
    public ModifiablePrimaryNode Identifier { get; }
    public ExpressionNode Expression { get; }

    public override IEnumerable<Node> GetChildren() =>
        GetChildren(null, Identifier, Expression);

    public AssignmentNode(ModifiablePrimaryNode identifier, ExpressionNode expression)
    {
        Identifier = identifier;
        Expression = expression;
    }
}
public class FunctionNode : DeclarationNode, IStatementNode
{
    public ExpressionNode? Parametr { get; set; }
    public ExpressionNode Expression { get; set; }

    public override IEnumerable<Node> GetChildren() =>
        GetChildren(null, Parametr, Expression);

    public FunctionNode()
    {
    }

    public FunctionNode(ExpressionNode condition, ExpressionNode body)
    {
        Parametr = condition;
        Expression = body;
    }
}

public class WhileLoopNode : DeclarationNode, IStatementNode
{
    public ExpressionNode Condition { get; set; }
    public BodyNode Body { get; set; }

    public override IEnumerable<Node> GetChildren() =>
        GetChildren(null, Condition, Body);

    public WhileLoopNode()
    {
    }

    public WhileLoopNode(ExpressionNode condition, BodyNode body)
    {
        Condition = condition;
        Body = body;
    }
}

public class ForLoopNode : DeclarationNode, IStatementNode
{
    public IdentifierNode VariableIdentifier { get; set; }
    public RangeNode Range { get; set;}
    public BodyNode Body { get; set;}

    public override IEnumerable<Node> GetChildren() =>
        GetChildren(null, VariableIdentifier, Range, Body);

    public ForLoopNode(IdentifierNode variableIdentifier, RangeNode range, BodyNode body) 
    {
        VariableIdentifier = variableIdentifier;
        Range = range;
        Body = body;
    }

    public ForLoopNode()
    {
    }
}

public class RangeNode : Node
{
    public ExpressionNode From { get; }
    public ExpressionNode To { get; }
    public bool IsReversed { get; }

    public override IEnumerable<Node> GetChildren() =>
        GetChildren(null, From, To);

    public RangeNode(ExpressionNode from, ExpressionNode to, bool isReversed = false)
    {
        From = from;
        To = to;
        IsReversed = isReversed;
    }
}

public class IfNode : DeclarationNode, IStatementNode
{
    public ExpressionNode Condition { get; set; }
    public BodyNode ThenBody { get; set;}
    public BodyNode? ElseBody { get; set;}

    public override IEnumerable<Node> GetChildren() =>
        GetChildren(null, Condition, ThenBody, ElseBody);

    public IfNode(ExpressionNode condition, BodyNode thenBody, BodyNode? elseBody = null)
    {
        Condition = condition;
        ThenBody = thenBody;
        ElseBody = elseBody;
    }
    public IfNode()
    { }
}

public class ReturnNode : Node, IStatementNode
{
    public ExpressionNode? Expression { get; }

    public override IEnumerable<Node> GetChildren() =>
        GetChildren(null, Expression);

    public ReturnNode(ExpressionNode? expression = null)
    {
        Expression = expression;
    }
}

public class ExpressionNode : Node
{
    public ExpressionNode? Lhs { get; set; }
    public OperatorNode? Operator { get; set; }
    public ExpressionNode? Rhs { get; set; }

    public override IEnumerable<Node> GetChildren() =>
        GetChildren(null, Lhs, Operator, Rhs);

    public ExpressionNode()
    {
    }

    public ExpressionNode(OperatorNode op, ExpressionNode rhs)
    {
        Operator = op;
        Rhs = rhs;
    }

    public ExpressionNode(ExpressionNode lhs, OperatorNode op, ExpressionNode rhs)
    {
        Lhs = lhs;
        Operator = op;
        Rhs = rhs;
    }
}

public abstract class PrimaryNode : ExpressionNode
{
    protected PrimaryNode()
    {
    }

    protected PrimaryNode(OperatorNode op, ExpressionNode rhs)
        : base(op, rhs)
    {
    }
}

public enum LiteralKind
{
    [Description("integer")] Integer,
    [Description("real")] Real,
    [Description("boolean")] Boolean,
    [Description("string")] String
}

public class LiteralNode : PrimaryNode, ILeafNode
{
    public Token Token { get; }
    public Enum? Kind { get; }

    public override IEnumerable<Node> GetChildren() =>
        GetChildren(null);

    public LiteralNode(LiteralKind kind, Token token)
    {
        Kind = kind;
        Token = token;
    }
}

public class IdentifierNode : PrimaryNode, ILeafNode
{
    public Token Token { get; }
    public Enum? Kind => null!;

    public override IEnumerable<Node> GetChildren() =>
        GetChildren(null);

    public IdentifierNode(Token identifier)
    {
        Token = identifier;
    }

    public string Name => (Token as IdentifierTk)?.Value!;
}

public enum Operator
{
    [Description("+ operator")] Plus,
    [Description("- operator")] Minus,
    [Description("* operator")] Multiply,
    [Description("/ operator")] Divide,
    [Description("% operator")] Modulo,
    [Description("xor operator")] Xor,
    [Description("and operator")] And,
    [Description("or operator")] Or,
    [Description("= operator")] Equal,
    [Description("!= operator")] NotEqual,
    [Description("< operator")] Less,
    [Description("<= operator")] LessOrEqual,
    [Description("> operator")] Greater,
    [Description(">= operator")] GreaterOrEqual,
    [Description(":= operator")] Assign,
    [Description("is operator")] Is
}

public class OperatorNode : Node, ILeafNode
{
    public Token Token { get; }
    public int Weight { get; set; }
    public Enum? Kind { get; }

    public override IEnumerable<Node> GetChildren() =>
        GetChildren(null);

    public OperatorNode(Operator kind, Token token)
    {
        Kind = kind;
        Token = token;
        SetWeight();
    }

    public OperatorNode(Token token)
    {
        Token = token;
        if (token.TokenId == Tokens.TkPlus)
            Kind = Operator.Plus;
        if (token.TokenId == Tokens.TkMinus)
            Kind = Operator.Minus;
        if (token.TokenId == Tokens.TkMultiply)
            Kind = Operator.Multiply;
        if (token.TokenId == Tokens.TkDivide)
            Kind = Operator.Divide;
        if (token.TokenId == Tokens.TkPercent)
            Kind = Operator.Modulo;
        if (token.TokenId == Tokens.TkXor)
            Kind = Operator.Xor;
        if (token.TokenId == Tokens.TkAnd)
            Kind = Operator.And;
        if (token.TokenId == Tokens.TkOr)
            Kind = Operator.Or;
        if (token.TokenId == Tokens.TkEqual)
            Kind = Operator.Equal;
        if (token.TokenId == Tokens.TkNotEqual)
            Kind = Operator.NotEqual;
        if (token.TokenId == Tokens.TkLess)
            Kind = Operator.Less;
        if (token.TokenId == Tokens.TkLeq)
            Kind = Operator.LessOrEqual;
        if (token.TokenId == Tokens.TkGreater)
            Kind = Operator.Greater;
        if (token.TokenId == Tokens.TkGeq)
            Kind = Operator.GreaterOrEqual;
        if (token.TokenId == Tokens.TkAssign)
            Kind = Operator.Assign;
        if (token.TokenId == Tokens.TkIs)
            Kind = Operator.Is;
        SetWeight();
    }

    protected void SetWeight()
    {
        if (Token.TokenId == Tokens.TkPlus)
            Weight = 1;
        if (Token.TokenId == Tokens.TkMinus)
            Weight = 1;
        if (Token.TokenId == Tokens.TkMultiply)
            Weight = 2;
        if (Token.TokenId == Tokens.TkDivide)
            Weight = 2;
        if (Token.TokenId == Tokens.TkPercent)
            Weight = 2;
        if (Token.TokenId == Tokens.TkXor)
            Weight = -1;
        if (Token.TokenId == Tokens.TkAnd)
            Weight = -1;
        if (Token.TokenId == Tokens.TkOr)
            Weight = -1;
        if (Token.TokenId == Tokens.TkEqual)
            Weight = -1;
        if (Token.TokenId == Tokens.TkNotEqual)
            Weight = -1;
        if (Token.TokenId == Tokens.TkLess)
            Weight = -1;
        if (Token.TokenId == Tokens.TkLeq)
            Weight = -1;
        if (Token.TokenId == Tokens.TkGreater)
            Weight = -1;
        if (Token.TokenId == Tokens.TkGeq)
            Weight = -1;
        if (Token.TokenId == Tokens.TkAssign)
            Weight = 10;
        if (Token.TokenId == Tokens.TkIs)
            Weight = -1;
    }

    public void makeWeightPersistent()
    {
        Weight += 2;
    }


    public int Compare(OperatorNode otherNode)
    {
        /*
         * Return -1 if weight of current obj is less than another obj.
         * 0 - if weights are equal.
         * 1 - if current weight bigger than another obj's weight.
         */
        if (Weight == otherNode.Weight)
            return 0;
        if (Weight < otherNode.Weight)
            return -1;
        return 1;
    }
}

public class ModifiablePrimaryNode : PrimaryNode
{
    public IdentifierNode? Identifier { get; }
    public ModifiablePrimaryNode? Prev { get; }
    public ExpressionNode? Index { get; }

    public override IEnumerable<Node> GetChildren() =>
        GetChildren(null, Identifier, Prev, Index);

    public ModifiablePrimaryNode(IdentifierNode? identifier, ModifiablePrimaryNode? prev = null,
        ExpressionNode? index = null)
    {
        Identifier = identifier;
        Prev = prev;
        Index = index;
    }
}