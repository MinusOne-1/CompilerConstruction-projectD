using System.ComponentModel;
using System.Text;
using Compiler.Core.CodeAnalysis.LexicalAnalysis.LexerTokens;
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
            builder.AppendLine(JsonConvert.SerializeObject(leaf.Token));
            return builder;
        }

        foreach (var child in GetChildren())
            child.ToString(builder, child == lastChild, indent);

        return builder;
    }
}

public class ListNode<T> : Node
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
    public IdentifierNode Identifier { get; }

    protected DeclarationNode(IdentifierNode identifier)
    {
        Identifier = identifier;
    }
}

public abstract class SimpleDeclarationNode : DeclarationNode
{
    public TypeNode? Type { get; }

    protected SimpleDeclarationNode(IdentifierNode identifier, TypeNode? type)
        : base(identifier)
    {
        Type = type;
    }
}

public class VariableDeclarationNode : SimpleDeclarationNode
{
    public ExpressionNode? Expression { get; }

    public override IEnumerable<Node> GetChildren() =>
        GetChildren(null, Identifier, Type, Expression);

    public VariableDeclarationNode(IdentifierNode identifier, TypeNode? type, ExpressionNode? expression)
        : base(identifier, type)
    {
        Expression = expression;
    }
}

public class TypeDeclarationNode : SimpleDeclarationNode
{
    public override IEnumerable<Node> GetChildren() =>
        GetChildren(null, Identifier, Type);

    public TypeDeclarationNode(IdentifierNode identifier, TypeNode type)
        : base(identifier, type)
    {
    }
}

public class RoutineDeclarationNode : DeclarationNode
{
    public TypeNode? ReturnType { get; }
    public List<ParameterDeclarationNode> Parameters { get; }
    public BodyNode Body { get; }

    public override IEnumerable<Node> GetChildren() =>
        GetChildren(Parameters, Identifier, ReturnType, Body);

    public RoutineDeclarationNode(IdentifierNode identifier, TypeNode? returnType,
        ListNode<ParameterDeclarationNode> parameters, BodyNode body)
        : base(identifier)
    {
        ReturnType = returnType;
        Parameters = parameters.Items;
        Body = body;
    }
}

public class ParameterDeclarationNode : Node
{
    public IdentifierNode Identifier { get; }
    public TypeNode Type { get; }

    public override IEnumerable<Node> GetChildren() =>
        GetChildren(null, Identifier, Type);

    public ParameterDeclarationNode(IdentifierNode identifier, TypeNode type)
    {
        Identifier = identifier;
        Type = type;
    }
}

public enum TypeKind
{
    [Description("integer")] Integer,
    [Description("real")] Real,
    [Description("boolean")] Boolean,
    [Description("array")] Array,
    [Description("turple")] List
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

    public BodyNode(SimpleDeclarationNode declaration, BodyNode? remaining)
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

public class RoutineCallNode : PrimaryNode, IStatementNode
{
    public IdentifierNode RoutineIdentifier { get; }
    public List<ExpressionNode> Arguments { get; }

    public override IEnumerable<Node> GetChildren() =>
        GetChildren(Arguments, RoutineIdentifier);

    public RoutineCallNode(IdentifierNode routineIdentifier, ListNode<ExpressionNode> arguments)
    {
        RoutineIdentifier = routineIdentifier;
        Arguments = arguments.Items;
    }
}

public class WhileLoopNode : Node, IStatementNode
{
    public ExpressionNode Condition { get; }
    public BodyNode Body { get; }

    public override IEnumerable<Node> GetChildren() =>
        GetChildren(null, Condition, Body);

    public WhileLoopNode(ExpressionNode condition, BodyNode body)
    {
        Condition = condition;
        Body = body;
    }
}

public class ForLoopNode : Node, IStatementNode
{
    public IdentifierNode VariableIdentifier { get; }
    public RangeNode Range { get; }
    public BodyNode Body { get; }

    public override IEnumerable<Node> GetChildren() =>
        GetChildren(null, VariableIdentifier, Range, Body);

    public ForLoopNode(IdentifierNode variableIdentifier, RangeNode range, BodyNode body)
    {
        VariableIdentifier = variableIdentifier;
        Range = range;
        Body = body;
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

public class IfNode : Node, IStatementNode
{
    public ExpressionNode Condition { get; }
    public BodyNode ThenBody { get; }
    public BodyNode? ElseBody { get; }

    public override IEnumerable<Node> GetChildren() =>
        GetChildren(null, Condition, ThenBody, ElseBody);

    public IfNode(ExpressionNode condition, BodyNode thenBody, BodyNode? elseBody = null)
    {
        Condition = condition;
        ThenBody = thenBody;
        ElseBody = elseBody;
    }
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
    public ExpressionNode? Lhs { get; }
    public OperatorNode? Operator { get; }
    public ExpressionNode? Rhs { get; }

    public override IEnumerable<Node> GetChildren() =>
        GetChildren(null, Lhs, Operator, Rhs);

    protected ExpressionNode()
    {
    }

    protected ExpressionNode(OperatorNode op, ExpressionNode rhs)
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

public class RelationNode : ExpressionNode
{
    public RelationNode(ExpressionNode lhs, OperatorNode op, ExpressionNode rhs)
        : base(lhs, op, rhs)
    {
    }
}

public class SimpleNode : ExpressionNode
{
    public SimpleNode(ExpressionNode lhs, OperatorNode op, ExpressionNode rhs)
        : base(lhs, op, rhs)
    {
    }
}

public class FactorNode : ExpressionNode
{
    public FactorNode(ExpressionNode lhs, OperatorNode op, ExpressionNode rhs)
        : base(lhs, op, rhs)
    {
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

public class UnaryNode : PrimaryNode
{
    public UnaryNode(OperatorNode op, LiteralNode rhs)
        : base(op, rhs)
    {
    }
}

public enum LiteralKind
{
    [Description("integer")] Integer,
    [Description("real")] Real,
    [Description("boolean")] Boolean
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
    [Description("/= operator")] NotEqual,
    [Description("< operator")] Less,
    [Description("<= operator")] LessOrEqual,
    [Description("> operator")] Greater,
    [Description(">= operator")] GreaterOrEqual
}

public class OperatorNode : Node, ILeafNode
{
    public Token Token { get; }
    public Enum? Kind { get; }

    public override IEnumerable<Node> GetChildren() =>
        GetChildren(null);

    public OperatorNode(Operator kind, Token token)
    {
        Kind = kind;
        Token = token;
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