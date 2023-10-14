using Compiler.Core.CodeAnalysis.LexicalAnalysis;
using Compiler.Core.CodeAnalysis.LexicalAnalysis.LexerTokens;
using Compiler.Core.CodeAnalysis.LexicalAnalysis.LexerTokens.Library;

namespace Compiler.Core.CodeAnalysis.SyntaxAnalysis;

public partial class SyntaxAnalisis
{
    public Tree Tree { get; private set; }
    public string syntaxError = "";
    public Token? errorToken;

    private readonly List<Token> tokens;
    private int position = 0;

    public SyntaxAnalisis(List<Token> tokens)
    {
        this.tokens = tokens;
        this.CreateAST();
    }

    public void SyntaxError(string nodeParser, Token token)
    {
        syntaxError = token.ToString() + " : " + "Unexpexted token " + nodeParser;
        errorToken = token;
    }

    private void CreateAST()
    {
        ProgramNode root = new ProgramNode();
        Tree = new Tree(root);
        while (position < tokens.Count)
        {
            var currentToken = tokens[position];
            if (currentToken.TokenId == Tokens.TkVar)
            {
                position++;
                var endConditionsSemicolon = new List<Tokens>();
                endConditionsSemicolon.Add(Tokens.TkSemicolon);
                var newChild = ParseVariableDeclaration(endConditionsSemicolon);
                root.DeclarationList.Add(newChild);
            }
            else if (currentToken.TokenId == Tokens.TkIdentifier)
            {
                var endConditionsSemicolon = new List<Tokens>();
                endConditionsSemicolon.Add(Tokens.TkSemicolon);
                var newChild = new VariableAssignmentNode(ParseVariableDeclaration(endConditionsSemicolon));
                root.DeclarationList.Add(newChild);
            }

            if (currentToken.TokenId == Tokens.TkPrint)
            {
                var newChild = ParsePrint();
                root.DeclarationList.Add(newChild);
            }

            if (currentToken.TokenId == Tokens.TkIf)
            {
                var newChild = ParseIf();
                root.DeclarationList.Add(newChild);
            }
            if (currentToken.TokenId == Tokens.TkFor)
            {
                var newChild = ParseFor();
                root.DeclarationList.Add(newChild);
            }
            if (currentToken.TokenId == Tokens.TkWhile)
            {
                var newChild = ParseWhile();
                root.DeclarationList.Add(newChild);
            }

            if (errorToken != null)
            {
                Console.WriteLine("Error on " + position + " from " + tokens.Count + " tokens");
                return;
            }

            position++;
        }
    }

    private FunctionNode ParseFunction()
    {
        throw new NotImplementedException();
    }

    private WhileLoopNode ParseWhile()
    {
        var newIfNode = new WhileLoopNode();
        var endConditions = new List<Tokens>();
        endConditions.Add(Tokens.TkLoop);
        newIfNode.Condition = ParseExpression(endConditions);
        endConditions.Clear();
        endConditions.Add(Tokens.TkEnd);
        newIfNode.Body = ParseBody(endConditions);
        return newIfNode;
    }

    private IfNode ParseIf()
    {
        var newIfNode = new IfNode();
        var endConditions = new List<Tokens>();
        endConditions.Add(Tokens.TkThen);
        newIfNode.Condition = ParseExpression(endConditions);
        endConditions.Clear();
        endConditions.Add(Tokens.TkEnd);
        endConditions.Add(Tokens.TkElse);
        newIfNode.ThenBody = ParseBody(endConditions);
        if (tokens[position].TokenId == Tokens.TkElse)
        {
            endConditions.Clear();
            endConditions.Add(Tokens.TkEnd);
            newIfNode.ElseBody = ParseBody(endConditions);
        }
        return newIfNode;
    }
    
    private ForLoopNode ParseFor()
    {
        var newForNode = new ForLoopNode();
        var endConditions = new List<Tokens>();
        newForNode.VariableIdentifier = new IdentifierNode(tokens[position]);
        position++;
        endConditions.Add(Tokens.TkEnd);
        newForNode.Body = ParseBody(endConditions);
        return newForNode;
    }
    

    private BodyNode ParseBody(List<Tokens> endConditions)
    {
        BodyNode body = new BodyNode();
        while (position < tokens.Count)
        {
            var currentToken = tokens[position];
            if (currentToken.TokenId == Tokens.TkVar)
            {
                position++;
                var endConditionsSemicolon = new List<Tokens>();
                endConditionsSemicolon.Add(Tokens.TkSemicolon);
                var newChild = ParseVariableDeclaration(endConditionsSemicolon);
                body.Items.Add(newChild);
            }
            else if (currentToken.TokenId == Tokens.TkIdentifier)
            {
                var endConditionsSemicolon = new List<Tokens>();
                endConditionsSemicolon.Add(Tokens.TkSemicolon);
                var newChild = new VariableAssignmentNode(ParseVariableDeclaration(endConditionsSemicolon));
                body.Items.Add(newChild);
            }
            else if (currentToken.TokenId == Tokens.TkPrint)
            {
                var newChild = ParsePrint();
                body.Items.Add(newChild);
            }

            else if (currentToken.TokenId == Tokens.TkIf)
            {
                var newChild = ParseIf();
                body.Items.Add(newChild);
            }
            else if (endConditions.Contains(currentToken.TokenId))
            {
                //условие выхода: если выполнено условие выхода заданное в начале - возвращается.
                return body;
            }

            if (errorToken != null)
            {
                return body;
            }

            position++;
        }

        return body;
    }

    private VariableDeclarationNode ParseVariableDeclaration(List<Tokens> endConditions)
    {
        VariableDeclarationNode newNode = null;
        while (position < tokens.Count)
        {
            var currentToken = tokens[position];
            if (currentToken.TokenId == Tokens.TkIdentifier && newNode == null)
            {
                newNode = new VariableDeclarationNode(new IdentifierNode(currentToken));
            }
            else if (endConditions.Contains(currentToken.TokenId) && newNode != null)
            {
                if (newNode.Expression == null)
                    newNode.Type = new TypeNode(TypeKind.Empty, newNode.Identifier);
                return newNode;
            }
            else if (currentToken.TokenId == Tokens.TkAssign && newNode != null)
            {
                position++;
                newNode.Expression = ParseRightPartExpression(endConditions);
                newNode.Expression.Operator = new OperatorNode(Operator.Assign, currentToken);
            }
            else
            {
                SyntaxError("Variable declaration", currentToken);
            }

            if (errorToken != null)
                return newNode;
            position++;
        }

        return newNode;
    }

    private ExpressionNode ParseRightPartExpression(List<Tokens> endConditions)
    {
        ExpressionNode newNode = new ExpressionNode();
        while (position < tokens.Count)
        {
            var currentToken = tokens[position];
            if (newNode.Rhs == null)
            {
                //первичное назначение правой части
                if (currentToken.TokenId == Tokens.TkIdentifier)
                    newNode.Rhs = new IdentifierNode(currentToken);
                if (currentToken.TokenId == Tokens.TkIntLiteral)
                    newNode.Rhs = new LiteralNode(LiteralKind.Integer, currentToken);
                if (currentToken.TokenId == Tokens.TkRealLiteral)
                    newNode.Rhs = new LiteralNode(LiteralKind.Real, currentToken);
                if (currentToken.TokenId == Tokens.TkStringLiteral)
                    newNode.Rhs = new LiteralNode(LiteralKind.String, currentToken);
                if (currentToken.TokenId == Tokens.TkBoolLiteral)
                    newNode.Rhs = new LiteralNode(LiteralKind.Boolean, currentToken);
                if (currentToken.TokenId == Tokens.TkSquareOpen)
                {
                    position++;
                    newNode.Rhs = ParseArray();
                }

                if (currentToken.TokenId == Tokens.TkCurlyOpen)
                {
                    position++;
                    newNode.Rhs = ParseTuple();
                }

                if (currentToken.TokenId == Tokens.TkRoundOpen)
                {
                    position++;
                    var endConditionsRClose = new List<Tokens>();
                    endConditionsRClose.Add(Tokens.TkRoundClose);
                    newNode.Rhs = ParseExpression(endConditionsRClose);
                    newNode.Rhs.Operator.makeWeightPersistent();
                    position++;
                }
            }
            else if (endConditions.Contains(currentToken.TokenId))
            {
                //условие выхода: если выполнено условие выхода заданное в начале - возвращается.
                position--;
                return newNode;
            }
            else if ((currentToken.TokenId == Tokens.TkPlus) || (currentToken.TokenId == Tokens.TkMinus) ||
                     (currentToken.TokenId == Tokens.TkMultiply) || (currentToken.TokenId == Tokens.TkDivide) ||
                     (currentToken.TokenId == Tokens.TkPercent) || (currentToken.TokenId == Tokens.TkXor) ||
                     (currentToken.TokenId == Tokens.TkAnd) || (currentToken.TokenId == Tokens.TkOr) ||
                     (currentToken.TokenId == Tokens.TkEqual) || (currentToken.TokenId == Tokens.TkNotEqual) ||
                     (currentToken.TokenId == Tokens.TkLess) || (currentToken.TokenId == Tokens.TkLeq) ||
                     (currentToken.TokenId == Tokens.TkGreater) || (currentToken.TokenId == Tokens.TkGeq))
            {
                position++;
                var newRhs = ParseRightPartExpression(endConditions);
                newRhs.Lhs = newNode.Rhs;
                newRhs.Operator = new OperatorNode(currentToken);
                newNode.Rhs = newRhs;
                if (newRhs.GetType().Name == "ExpressionNode")
                {
                    if (newRhs.Operator != null && newRhs.Lhs != null && newRhs.Rhs != null &&
                        newRhs.Rhs.Operator != null && newRhs.Rhs.Lhs != null)
                    {
                        if (newRhs.Operator.Compare(newRhs.Rhs.Operator) == 1)
                        {
                            var newLhs = new ExpressionNode(newRhs.Lhs, newRhs.Operator, newRhs.Rhs.Lhs);
                            newRhs.Lhs = newLhs;
                            newRhs.Operator = newRhs.Rhs.Operator;
                            newRhs.Rhs = newRhs.Rhs.Rhs;
                        }
                    }
                }

                newNode.Rhs = newRhs;
            }
            else if (currentToken.TokenId == Tokens.TkIs)
            {
                newNode.Rhs = new ExpressionNode(newNode.Rhs, new OperatorNode(currentToken), null);
                position++;
                if (tokens[position].TokenId == Tokens.TkIntLiteralIdentifier ||
                    tokens[position].TokenId == Tokens.TkRealLiteralIdentifier ||
                    tokens[position].TokenId == Tokens.TkStringLiteralIdentifier ||
                    tokens[position].TokenId == Tokens.TkBoolLiteralIdentifier ||
                    tokens[position].TokenId == Tokens.TkArrayIdentifier ||
                    tokens[position].TokenId == Tokens.TkTupleIdentifier ||
                    tokens[position].TokenId == Tokens.TkEmptyIdentifier)
                {
                    newNode.Rhs.Rhs = new IdentifierNode(tokens[position]);
                }
                else
                    SyntaxError("Is operation", currentToken);
            }
            else
            {
                SyntaxError("Right part expression parser", currentToken);
            }

            if (errorToken != null)
                return newNode;
            position++;
        }

        return newNode;
    }

    private ExpressionNode ParseExpression(List<Tokens> endConditions)
    {
        ExpressionNode newNode = new ExpressionNode();
        while (position < tokens.Count)
        {
            var currentToken = tokens[position];
            if (newNode.Lhs == null)
            {
                //первичное назначение правой части
                if (currentToken.TokenId == Tokens.TkIdentifier)
                    newNode.Lhs = new IdentifierNode(currentToken);
                if (currentToken.TokenId == Tokens.TkIntLiteral)
                    newNode.Lhs = new LiteralNode(LiteralKind.Integer, currentToken);
                if (currentToken.TokenId == Tokens.TkRealLiteral)
                    newNode.Lhs = new LiteralNode(LiteralKind.Real, currentToken);
                if (currentToken.TokenId == Tokens.TkStringLiteral)
                    newNode.Lhs = new LiteralNode(LiteralKind.String, currentToken);
                if (currentToken.TokenId == Tokens.TkBoolLiteral)
                    newNode.Lhs = new LiteralNode(LiteralKind.Boolean, currentToken);
                if (currentToken.TokenId == Tokens.TkSquareOpen)
                {
                    position++;
                    newNode.Lhs = ParseArray();
                }

                if (currentToken.TokenId == Tokens.TkCurlyOpen)
                {
                    position++;
                    newNode.Lhs = ParseTuple();
                }

                if (currentToken.TokenId == Tokens.TkRoundOpen)
                {
                    position++;
                    var endConditionsRClose = new List<Tokens>();
                    endConditionsRClose.Add(Tokens.TkRoundClose);
                    newNode.Lhs = ParseExpression(endConditionsRClose);
                    position++;
                }
            }
            else if (endConditions.Contains(currentToken.TokenId))
            {
                //условие выхода: если выполнено условие выхода заданное в начале - возвращается.
                position--;
                return newNode;
            }
            else if ((currentToken.TokenId == Tokens.TkPlus) || (currentToken.TokenId == Tokens.TkMinus) ||
                     (currentToken.TokenId == Tokens.TkMultiply) || (currentToken.TokenId == Tokens.TkDivide) ||
                     (currentToken.TokenId == Tokens.TkPercent) || (currentToken.TokenId == Tokens.TkXor) ||
                     (currentToken.TokenId == Tokens.TkAnd) || (currentToken.TokenId == Tokens.TkOr) ||
                     (currentToken.TokenId == Tokens.TkEqual) || (currentToken.TokenId == Tokens.TkNotEqual) ||
                     (currentToken.TokenId == Tokens.TkLess) || (currentToken.TokenId == Tokens.TkLeq) ||
                     (currentToken.TokenId == Tokens.TkGreater) || (currentToken.TokenId == Tokens.TkGeq))
            {
                position++;
                newNode.Rhs = ParseRightPartExpression(endConditions).Rhs;
                newNode.Operator = new OperatorNode(currentToken);
                if (newNode.GetType().Name == "ExpressionNode")
                {
                    if (newNode.Operator != null && newNode.Lhs != null && newNode.Rhs != null &&
                        newNode.Rhs.Operator != null && newNode.Rhs.Lhs != null)
                    {
                        if (newNode.Operator.Compare(newNode.Rhs.Operator) == 1)
                        {
                            var newLhs = new ExpressionNode(newNode.Lhs, newNode.Operator, newNode.Rhs.Lhs);
                            newNode.Lhs = newLhs;
                            newNode.Operator = newNode.Rhs.Operator;
                            newNode.Rhs = newNode.Rhs.Rhs;
                        }
                    }
                }
            }
            else if (currentToken.TokenId == Tokens.TkIs)
            {
                newNode.Operator = new OperatorNode(currentToken);
                position++;
                if (tokens[position].TokenId == Tokens.TkIntLiteralIdentifier ||
                    tokens[position].TokenId == Tokens.TkRealLiteralIdentifier ||
                    tokens[position].TokenId == Tokens.TkStringLiteralIdentifier ||
                    tokens[position].TokenId == Tokens.TkBoolLiteralIdentifier ||
                    tokens[position].TokenId == Tokens.TkArrayIdentifier ||
                    tokens[position].TokenId == Tokens.TkTupleIdentifier ||
                    tokens[position].TokenId == Tokens.TkEmptyIdentifier)
                {
                    newNode.Rhs = new IdentifierNode(tokens[position]);
                }
                else
                    SyntaxError("Is operation", currentToken);
            }
            else
            {
                SyntaxError("expression parser", currentToken);
            }

            if (errorToken != null)
                return newNode;
            position++;
        }

        return newNode;
    }

    private TupleNode ParseTuple()
    {
        TupleNode newTuple = new TupleNode();
        TupleElementNode currentElement = null;
        while (position < tokens.Count)
        {
            var currentToken = tokens[position];
            if (currentToken.TokenId == Tokens.TkCurlyClose)
            {
                if (currentElement != null)
                {
                    newTuple.Items.Add(currentElement.Identifier, currentElement);
                }

                return newTuple;
            }

            if (currentElement == null)
            {
                var endConditionsTupleVarDec = new List<Tokens>();
                endConditionsTupleVarDec.Add(Tokens.TkCurlyClose);
                endConditionsTupleVarDec.Add(Tokens.TkComma);
                var dec = ParseVariableDeclaration(endConditionsTupleVarDec);
                currentElement = new TupleElementNode(dec);
                position--;
            }
            else if (currentToken.TokenId == Tokens.TkComma)
            {
                newTuple.Items.Add(currentElement.Identifier, currentElement);
                currentElement = null;
            }


            if (errorToken != null)
                return newTuple;
            position++;
        }

        return newTuple;
    }

    private ListNode<Node> ParseArray()
    {
        ListNode<Node> newNode = new ListNode<Node>();
        ExpressionNode currentElement = null;
        while (position < tokens.Count)
        {
            var currentToken = tokens[position];
            if (currentToken.TokenId == Tokens.TkSquareClose)
            {
                if (currentElement != null)
                {
                    if (currentElement.Rhs == null)
                        newNode.Items.Add(currentElement.Lhs);
                    else
                        newNode.Items.Add(currentElement);
                }

                return newNode;
            }

            if (currentElement == null)
            {
                var endConditionsRClose = new List<Tokens>();
                endConditionsRClose.Add(Tokens.TkComma);
                endConditionsRClose.Add(Tokens.TkSquareClose);
                currentElement = ParseExpression(endConditionsRClose);
            }
            else if (currentToken.TokenId == Tokens.TkComma)
            {
                if (currentElement.Rhs == null)
                    newNode.Items.Add(currentElement.Lhs);
                else
                    newNode.Items.Add(currentElement);
                currentElement = null;
            }


            if (errorToken != null)
                return newNode;
            position++;
        }

        return newNode;
    }

    private PrintNode ParsePrint()
    {
        var newNode = new PrintNode(new IdentifierNode(tokens[position]));
        position++;
        ExpressionNode currentElement = null;
        while (position < tokens.Count)
        {
            var currentToken = tokens[position];
            if (currentToken.TokenId == Tokens.TkSemicolon)
            {
                if (currentElement != null)
                {
                    if (currentElement.Rhs == null)
                        newNode.Items.Add(currentElement.Lhs);
                    else
                        newNode.Items.Add(currentElement);
                }

                return newNode;
            }

            if (currentElement == null)
            {
                var endConditionsRClose = new List<Tokens>();
                endConditionsRClose.Add(Tokens.TkComma);
                endConditionsRClose.Add(Tokens.TkSemicolon);
                currentElement = ParseExpression(endConditionsRClose);
            }
            else if (currentToken.TokenId == Tokens.TkComma)
            {
                if (currentElement.Rhs == null)
                    newNode.Items.Add(currentElement.Lhs);
                else
                    newNode.Items.Add(currentElement);
                currentElement = null;
            }

            if (errorToken != null)
                return newNode;
            position++;
        }

        return newNode;
    }
}