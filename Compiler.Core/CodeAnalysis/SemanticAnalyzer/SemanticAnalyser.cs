using Compiler.Core.CodeAnalysis.LexicalAnalysis.LexerTokens;
using Compiler.Core.CodeAnalysis.LexicalAnalysis.LexerTokens.Library;
using Compiler.Core.CodeAnalysis.SyntaxAnalysis;
using Microsoft.VisualBasic.CompilerServices;

namespace Compiler.Core.CodeAnalysis.SemanticAnalyzer;

public class SemanticAnalyser
{
    private Dictionary<string, VariableInformation> variablesDictionary;
    private Dictionary<string, FucntionInformation> functionsDictionary;

    private Dictionary<string, int> context = new()
    {
        { "decl", 0 },
        { "assignment", 0 },
        { "expression", 0 },
        { "for", 0 },
        { "while", 0 },
        { "if", 0 },
        { "then", 0 },
        { "else", 0 },
        { "func", 0 }
    };


    public List<string> SemanticErrors;
    public Tree AST;

    public SemanticAnalyser(Tree AST_)
    {
        variablesDictionary = new Dictionary<string, VariableInformation>();
        functionsDictionary = new Dictionary<string, FucntionInformation>();

        SemanticErrors = new List<string>();
        AST = AST_;
        CheckSemantics(AST.Root);
    }

    public static Object GetTypedValueFromLiteralNode(LiteralNode node)
    {
        var type_ = (LiteralKind?)node.Kind;

        if (type_ == LiteralKind.Integer)
        {
            return Convert.ToInt32(node.Token.TokenValue);
        }

        if (type_ == LiteralKind.Real)
        {
            return Convert.ToDouble(node.Token.TokenValue);
        }

        if (type_ == LiteralKind.Boolean)
        {
            return Convert.ToBoolean(node.Token.TokenValue);
        }

        if (type_ == LiteralKind.String)
        {
            return node.Token.TokenValue;
        }

        return null;
    }

    private void CheckSemantics(Node node)
    {
        var nodeType = node.GetType();

        var children = node.GetChildren();
        if (nodeType == typeof(VariableDeclarationNode))
        {
            context["decl"]++;
            CheckVarDeclarationSemantics((VariableDeclarationNode)node);
            context["decl"]--;
        }
        if (nodeType == typeof(VariableAssignmentNode))
        {
            context["assignment"]++;
            CheckVarAssignmentSemantics((VariableAssignmentNode)node);
            context["assignment"]--;
        }

        if (nodeType == typeof(ExpressionNode))
        {
            context["expression"]++;
            CheckExpressionSemantic((ExpressionNode)node);
            context["expression"]--;
        }

        if (nodeType == typeof(IfNode))
        {
            context["if"]++;
            CheckIfStatementSemantics();
            context["if"]--;
        }

        if (nodeType == typeof(WhileLoopNode))
        {
            context["while"]++;
            CheckWhileLoopSemantic();
            context["while"]--;
        }

        if (nodeType == typeof(ForLoopNode))
        {
            context["for"]++;
            CheckForLoopSemantics();
            context["for"]--;
        }

        if (nodeType == typeof(FunctionNode))
        {
            context["func"]++;
            CheckFunctionDeclarationSemantics();
            context["func"]--;
        }
        if (nodeType == typeof(ReturnNode))
        {
            if (context["func"] == 0)
            {
                SemanticErrors.Add("ERROR on "+node+": Return token not in context of Function declaration");
            }
        }

        foreach (var child in children)
            CheckSemantics(child);
    }


    private void CheckVarDeclarationSemantics(VariableDeclarationNode node)
    {
        var varName = node.Identifier.Token.TokenValue;
        var newVar = new VariableInformation(varName);
        if (node.Expression == null)
        {
            newVar.setType(Types.Empty);
        }

        else {
            if ((Operator)node.Expression.Operator.Kind != Operator.Assign)
            {
                SemanticErrors.Add("ERROR on " + node.Expression.Operator +
                                   ":Unexpected operator in variable declaration context");
                return;
            }

            
            
            newVar.setType(CheckExpressionSemantic(node.Expression));
            
            if (newVar.getType() != Types.Empty)
            {
                //if not empty - there should be LiteralNode in Expression? cause of the test-code or Optimization.
                newVar.setValue(GetTypedValueFromLiteralNode((LiteralNode)node.Expression.Rhs));
               
            }
        }
        
        variablesDictionary.Add(varName, newVar);
    }
    
    private void CheckVarAssignmentSemantics(VariableAssignmentNode node)
    {
        var varName = node.Identifier.Token.TokenValue;
        VariableInformation varInfo;
        if ((Operator)node.Expression.Operator.Kind != Operator.Assign)
        {
            SemanticErrors.Add("ERROR on " + node.Expression.Operator +
                               ":Unexpected operator in variable declaration context");
            return;
        }
        
        if (!variablesDictionary.ContainsKey(varName))
        {
            SemanticErrors.Add("ERROR on " + node +
                               ":Variable Wasn't declared before be used");
            return;
        }
        CheckExpressionSemantic(node.Expression);

    }


    private Types? CheckExpressionSemantic(ExpressionNode node)
    {
        Types? exprType = null;
        if (node.GetType() == typeof(LiteralNode))
        {
            
            exprType = VariableInformation.whatTypes(((LiteralNode)node).Kind.ToString());
            return exprType;
        }

        if (node.GetType() == typeof(IdentifierNode))
        {
            if (variablesDictionary.ContainsKey(VariableInformation.whatName(node)))
            {
                VariableInformation varInfo;
                variablesDictionary.TryGetValue(VariableInformation.whatName(node), out varInfo);
                exprType = varInfo.getType();
                if (exprType == Types.Empty)
                {
                    exprType = null;
                    SemanticErrors.Add("ERROR on " + node + ":Uninitialized variable used in expression");
                }
            }
            else
            {
                SemanticErrors.Add("ERROR on " + node + ":Undeclared variable.");
            }

            return exprType;
        }

        if (node.Operator.Kind.ToString() == Operator.Assign.ToString())
        {
            exprType = CheckExpressionSemantic(node.Rhs);
            if (exprType == Types.Integer || exprType == Types.Real)
            {
                node.Rhs = MakeExpressionSimplee(node.Rhs);
            }
        }
        else if (node.Operator.Kind.ToString() == Operator.Is.ToString())
        {
            if (node.Rhs.GetType() == typeof(IdentifierNode))
            {
                if (((IdentifierNode)node.Rhs).Token.TokenId.ToString() == Tokens.TkIntLiteralIdentifier.ToString() ||
                    ((IdentifierNode)node.Rhs).Token.TokenId.ToString() == Tokens.TkRealLiteralIdentifier.ToString() ||
                    ((IdentifierNode)node.Rhs).Token.TokenId.ToString() == Tokens.TkBoolLiteralIdentifier.ToString() ||
                    ((IdentifierNode)node.Rhs).Token.TokenId.ToString() == Tokens.TkStringLiteralIdentifier.ToString())
                {
                    return null;
                }
            }

            SemanticErrors.Add("ERROR on " + node.Operator + ":Invalid operands: " + node.Rhs);
        }
        else
        {
            
            var LhsType = CheckExpressionSemantic(node.Lhs);
            var RhsType = CheckExpressionSemantic(node.Rhs);
            
            
            if ((LhsType == RhsType || (LhsType == Types.Integer && RhsType == Types.Real)
                                    || (LhsType == Types.Real && RhsType == Types.Integer)) && LhsType != null &&
                RhsType != null)
            {
                // optimize if possible

                node.Lhs = MakeExpressionSimplee(node.Lhs);
                node.Rhs = MakeExpressionSimplee(node.Rhs);
                exprType = Types.Real;
            }
            else if (LhsType == null || RhsType == null)
            {
                // just skip
                exprType = null;
            }
            else
            {
                SemanticErrors.Add("ERROR on " + node.Operator + ":Invalid operands type: " + LhsType +
                                   " operator " + RhsType);
            }
        }

        return exprType;
    }

    private LiteralNode MakeExpressionSimplee(ExpressionNode node)
    {
        var value = CalculateExpressionIfPossible(node);
        Token newToken;
        LiteralNode simplerNode;
        if (value is int)
        {
            newToken = new IntTk(value.ToString());
            simplerNode = new LiteralNode(LiteralKind.Integer, newToken);
        }
        else if (value is double)
        {
            newToken = new RealTk(value.ToString());
            simplerNode = new LiteralNode(LiteralKind.Real, newToken);
        }
        else if (value is string)
        {
            newToken = new StringTk(value.ToString());
            simplerNode = new LiteralNode(LiteralKind.String, newToken);
        }
        else
        {
            newToken = new BoolTk(value.ToString());
            simplerNode = new LiteralNode(LiteralKind.Boolean, newToken);
        }
        return simplerNode;
    }

    private Object? CalculateExpressionIfPossible(ExpressionNode node)
    {
        if (node.GetType() == typeof(LiteralNode))
        {
            return GetTypedValueFromLiteralNode((LiteralNode)node);
        }

        if (node.GetType() == typeof(IdentifierNode))
        {
            if (variablesDictionary.ContainsKey(VariableInformation.whatName(node)))
            {
                VariableInformation varInfo;
                variablesDictionary.TryGetValue(VariableInformation.whatName(node), out varInfo);
                return varInfo.getValue();
            }
        }

        if (node.Operator.Kind.ToString() == Operator.Plus.ToString())
        {
            var Lhs = CalculateExpressionIfPossible(node.Lhs);
            var Rhs = CalculateExpressionIfPossible(node.Rhs);
            if (Lhs is int && Rhs is int)
                return (int)Lhs + (int)Rhs; 
            if (Lhs is double && Rhs is double)
                return (double)Lhs + (double)Rhs; 
            if (Lhs is double && Rhs is int)
                return (double)Lhs + (int)Rhs; 
            if (Lhs is int && Rhs is double)
                return (int)Lhs + (double)Rhs; 
            
            
            if (Lhs is string && Rhs is string)
                return (string)Lhs + (string)Rhs;
        }
        if (node.Operator.Kind.ToString() == Operator.Minus.ToString())
        {
            var Lhs = CalculateExpressionIfPossible(node.Lhs);
            var Rhs = CalculateExpressionIfPossible(node.Rhs);
            if (Lhs is int && Rhs is int)
                return (int)Lhs - (int)Rhs; 
            if (Lhs is double && Rhs is double)
                return (double)Lhs - (double)Rhs; 
            if (Lhs is double && Rhs is int)
                return (double)Lhs - (int)Rhs; 
            if (Lhs is int && Rhs is double)
                return (int)Lhs - (double)Rhs; 
        }
        if (node.Operator.Kind.ToString() == Operator.Divide.ToString())
        {
            var Lhs = CalculateExpressionIfPossible(node.Lhs);
            var Rhs = CalculateExpressionIfPossible(node.Rhs);
            if (Lhs is int && Rhs is int)
                return (int)Lhs / (int)Rhs; 
            if (Lhs is double && Rhs is double)
                return (double)Lhs / (double)Rhs; 
            if (Lhs is double && Rhs is int)
                return (double)Lhs / (int)Rhs; 
            if (Lhs is int && Rhs is double)
                return (int)Lhs / (double)Rhs; 
        }
        if (node.Operator.Kind.ToString() == Operator.Multiply.ToString())
        {
            var Lhs = CalculateExpressionIfPossible(node.Lhs);
            var Rhs = CalculateExpressionIfPossible(node.Rhs);
            if (Lhs is int && Rhs is int)
                return (int)Lhs * (int)Rhs; 
            if (Lhs is double && Rhs is double)
                return (double)Lhs * (double)Rhs; 
            if (Lhs is double && Rhs is int)
                return (double)Lhs * (int)Rhs; 
            if (Lhs is int && Rhs is double)
                return (int)Lhs * (double)Rhs; 
        }
        

        return null;
    }
    
    private void CheckIfStatementSemantics()
    {
        throw new NotImplementedException();
    }

    private void CheckWhileLoopSemantic()
    {
        throw new NotImplementedException();
    }

    private void CheckForLoopSemantics()
    {
        throw new NotImplementedException();
    }

    private void CheckFunctionDeclarationSemantics()
    {
        throw new NotImplementedException();
    }

}