using Compiler.Core.CodeAnalysis.LexicalAnalysis.LexerTokens;
using Compiler.Core.CodeAnalysis.LexicalAnalysis.LexerTokens.Library;
using Compiler.Core.CodeAnalysis.SyntaxAnalysis;

namespace Compiler.Core.CodeAnalysis.SemanticAnalyzer;

public class SemanticAnalyser
{
    public Dictionary<string, VariableInformation> variablesDictionary;

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

    private bool afterBreakUsless = false;

    public HashSet<string> SemanticErrors;
    public HashSet<string> SemanticWarnings;
    public Tree AST;
    public List<string> LocalArea;

    public SemanticAnalyser(Tree AST_)
    {
        variablesDictionary = new Dictionary<string, VariableInformation>();

        SemanticErrors = new();
        SemanticWarnings = new();
        AST = AST_;
        LocalArea = new();
        CheckSemantics(AST.Root);

        deleteUnuseddFromAST(AST.Root);
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
        /* Console.WriteLine("----------------------------" + nodeType);
         foreach (var c in context)
         {
             Console.WriteLine(c);
         }*/

        var children = node.GetChildren();

        if (nodeType == typeof(PrintNode))
        {
            //  Console.WriteLine("PR beg");
            context["expression"]++;
            foreach (var item in ((PrintNode)node).Items)
            {
                CheckExpressionSemantic(item);
            }

            //  Console.WriteLine("PR end");

            context["expression"]--;
        }

        if (nodeType == typeof(VariableDeclarationNode))
        {
            context["decl"]++;
            CheckVarDeclarationSemantics((VariableDeclarationNode)node);
            context["decl"]--;
        }
        else if (nodeType == typeof(VariableAssignmentNode))
        {
            context["assignment"]++;
            CheckVarAssignmentSemantics((VariableAssignmentNode)node);
            context["assignment"]--;
        }
        else if (nodeType == typeof(MemberwiseAdditionNode))
        {
            context["assignment"]++;
            CheckVarMemberwiseAdditionSemantics((MemberwiseAdditionNode)node);
            context["assignment"]--;
        }

        else if (nodeType == typeof(ExpressionNode))
        {
            context["expression"]++;
            CheckExpressionSemantic((ExpressionNode)node);
            context["expression"]--;
        }
        else if (nodeType == typeof(BodyNode))
        {
            var indx = 0;
            foreach (var child in children)
            {
                if (afterBreakUsless && (child.GetType() != typeof(BreakNode)))
                {
                    ((BodyNode)node).Items.RemoveAt(indx);
                    indx--;
                }

                CheckSemantics(child);
                indx++;
            }

            afterBreakUsless = false;
        }
        else if (nodeType == typeof(IfNode))
        {
            context["if"]++;
            foreach (var child in children)
            {
                CheckSemantics(child);
            }

            context["if"]--;
        }

        else if (nodeType == typeof(WhileLoopNode))
        {
            context["while"]++;
            foreach (var child in children)
                CheckSemantics(child);
            context["while"]--;
        }

        else if (nodeType == typeof(ForLoopNode))
        {
            context["for"]++;
            foreach (var child in children)
                CheckSemantics(child);
            context["for"]--;
        }

        else if (nodeType == typeof(FunctionNode))
        {
            context["func"]++;
            foreach (var child in children)
                CheckSemantics(child);
            context["func"]--;
        }

        else if (nodeType == typeof(BreakNode) && context["while"] == 0 && context["for"] == 0)
        {
            SemanticErrors.Add("ERROR on " + node + ": Break token not in context of loop");
        }
        else if (nodeType == typeof(BreakNode) && (context["while"] > 0 || context["for"] > 0))
        {
            afterBreakUsless = true;
        }
        else if (nodeType == typeof(ReturnNode) && context["func"] == 0)
        {
            SemanticErrors.Add("ERROR on " + node + ": Return token not in context of Function declaration");
        }
        else if (nodeType == typeof(ReturnNode) && context["func"] > 0)
        {
            afterBreakUsless = true;
            CheckExpressionSemantic(((ReturnNode)node).Expression);
        }
        else
        {
            foreach (var child in children)
                CheckSemantics(child);
        }
    }


    private void CheckVarDeclarationSemantics(VariableDeclarationNode node)
    {
        var varName = node.Identifier.Name;

        var newVar = new VariableInformation(varName);
        LocalArea.Add(varName);
        Console.WriteLine("VD beg ADD - " + varName);
        AddVariable(newVar, node);

        if (node.Expression == null)
        {
            newVar.setType(Types.Empty);
            LocalArea.RemoveAt(LocalArea.Count - 1);
        }
        else
        {
            if ((Operator)node.Expression.Operator.Kind != Operator.Assign)
            {
                SemanticErrors.Add("ERROR on " + node.Expression.Operator +
                                   ":Unexpected operator in variable declaration context");
                LocalArea.RemoveAt(LocalArea.Count - 1);
                return;
            }

            if (node.Expression.Rhs.GetType() == typeof(FunctionNode))
            {
                newVar.setType(Types.Function);
                UpdateVariable(newVar, node);
                newVar.setValue(node.Expression.Rhs);
            }
            else if (node.Expression.Rhs.GetType() == typeof(TupleNode))
            {
                newVar.setType(Types.Tuple);
                UpdateVariable(newVar, node);
            }
            else
            {
                //Console.WriteLine("VD beg DEL - " + LocalArea[LocalArea.Count - 1]);
                LocalArea.RemoveAt(LocalArea.Count - 1);
            }

            newVar.setType(CheckExpressionSemantic(node.Expression));

            if (newVar.getType() != Types.Function && newVar.getType() != Types.Empty &&
                newVar.getType() != Types.Array && newVar.getType() != Types.Tuple)
            {
                //if not empty - there should be LiteralNode in Expression? cause of the test-code or Optimization.

                LocalArea.Add(varName);
                Console.WriteLine("VD get  - " + LocalArea[LocalArea.Count - 1]);
                GetVariable(node, "CheckVarDeclarationSemantics")
                    .setValue(GetTypedValueFromLiteralNode((LiteralNode)node.Expression.Rhs));
                LocalArea.RemoveAt(LocalArea.Count - 1);
            }

            if (node.Expression.Rhs.GetType() == typeof(FunctionNode) ||
                node.Expression.Rhs.GetType() == typeof(TupleNode))
            {
                //Console.WriteLine("VD DEL - " + LocalArea[LocalArea.Count - 1]);
                LocalArea.RemoveAt(LocalArea.Count - 1);
            }
        }
        Console.WriteLine("VD - end " + varName);
    }


    private void CheckVarAssignmentSemantics(VariableAssignmentNode node)
    {
        var varName = node.Identifier.Name;
        
        Console.WriteLine("VA - beg " + varName);
        LocalArea.Add(varName);
        var variable = GetVariable(node, "CheckVarAssignmentSemantics");


        if ((Operator)node.Expression.Operator.Kind != Operator.Assign)
        {
            SemanticErrors.Add("ERROR on " + node.Expression.Operator +
                               ":Unexpected operator in variable assignment context");
            LocalArea.RemoveAt(LocalArea.Count - 1);
            return;
        }

        if (variable != null)
        {
            if (node.Expression.Rhs.GetType() == typeof(FunctionNode))
            {
                variable.setType(Types.Function);
                variable.setValue(node.Expression.Rhs);
            }
            else if (node.Expression.Rhs.GetType() == typeof(TupleNode))
            {
                variable.setType(Types.Tuple);
            }
            else
            {
                //   Console.WriteLine("VA - ADD " + varName);
                LocalArea.RemoveAt(LocalArea.Count - 1);
            }

            variable.usage = true;
            var typExp = CheckExpressionSemantic(node.Expression);

            if ((node.Expression.Rhs.GetType() != typeof(FunctionNode) &&
                 node.Expression.Rhs.GetType() != typeof(TupleNode)) && !(typExp is null) &&  typExp != Types.Function &&
                typExp != Types.Empty &&
                typExp != Types.Array && typExp != Types.Tuple)
            {
                //if not empty - there should be LiteralNode in Expression? cause of the test-code or Optimization.
                Console.WriteLine("_______________"+ (typExp == null) +" - " +node);
                LocalArea.Add(varName);
                variable.setValue(GetTypedValueFromLiteralNode((LiteralNode)node.Expression.Rhs));
                LocalArea.RemoveAt(LocalArea.Count - 1);
            }

            if (node.Expression.Rhs.GetType() == typeof(FunctionNode) ||
                node.Expression.Rhs.GetType() == typeof(TupleNode))
            {
                //  Console.WriteLine("VA - DEL " + varName);
                LocalArea.RemoveAt(LocalArea.Count - 1);
                if (typExp != Types.Tuple && typExp != Types.Function)
                {
                }
            }
        }
        else
        {
            SemanticErrors.Add("ERROR: " + node + ": undeclared variable");
            LocalArea.RemoveAt(LocalArea.Count - 1);
        }
        Console.WriteLine("VA - end " + varName);
    }


    private void CheckVarMemberwiseAdditionSemantics(MemberwiseAdditionNode node)
    {
        var varName = node.Identifier.Name;
        LocalArea.Add(varName);
        var variable = GetVariable(node, "CheckVarMemberwiseAdditionSemantics");
        if (variable == null)
        {
            SemanticErrors.Add("ERROR: " + node + ": undeclared variable");
            return;
        }


        if ((Operator)node.Expression.Operator.Kind != Operator.MemberwiseAdition)
        {
            SemanticErrors.Add("ERROR on " + node.Expression.Operator +
                               ":Unexpected operator in memberwise addition context");
        }

        variable.usage = true;
        if (node.Expression.Rhs.GetType() != typeof(TupleNode))
        {
            //   Console.WriteLine("VA - ADD " + varName);
            LocalArea.RemoveAt(LocalArea.Count - 1);
            SemanticWarnings.Add("WARNING: " + node +
                                 ": since the 2nd term of the Memberwise Addition Operation is not a tuple, then 1st term must be same types during program interpretation, be careful");
        }

        CheckExpressionSemantic(node.Expression);
        if (node.Expression.Rhs.GetType() == typeof(TupleNode))
        {
            //   Console.WriteLine("VA - ADD " + varName);

            SemanticWarnings.Add("WARNING: " + node +
                                 ": since the 2nd term of the Memberwise Addition Operation is a tuple, then 1st term must be a tuple during program interpretation, be careful");
            LocalArea.RemoveAt(LocalArea.Count - 1);
        }
    }


    private Types? CheckExpressionSemantic(ExpressionNode node)
    {
        // Console.WriteLine("bed CES");
        Types? exprType = null;
        if (node.GetType() == typeof(LiteralNode))
        {
            //Console.WriteLine("Lit");
            exprType = VariableInformation.whatTypes(((LiteralNode)node).Kind.ToString());
            return exprType;
        }

        if (node.GetType() == typeof(IdentifierNode))
        {
            LocalArea.Add(((IdentifierNode)node).Name);
            var variable = GetVariable(node, "CheckExpressionSemantic + ID");
            LocalArea.RemoveAt(LocalArea.Count - 1);
            //  Console.WriteLine("Id" + node);
            if (variable != null)
            {
                variable.usage = true;
            }
            else
            {
                SemanticErrors.Add("ERROR: " + node + ": undeclared variable");
            }

            return exprType;
        }

        if (node.GetType() == typeof(ListNode<Node>))
        {
            // Console.WriteLine("LS");
            exprType = Types.Array;
            foreach (var item in node.GetChildren())
            {
                CheckExpressionSemantic((ExpressionNode)item);
            }
            return exprType;
        }

        if (node.GetType() == typeof(ArrayReferenceNode))
        {
            // Console.WriteLine("AR");
            CheckExpressionSemantic(((ArrayReferenceNode)node).Identifier);

            var typ = CheckExpressionSemantic(((ArrayReferenceNode)node).Index);
            if (typ == Types.Integer)
            {
                ((ArrayReferenceNode)node).Index = MakeExpressionSimplee(((ArrayReferenceNode)node).Index);
            }
            else if (typ != Types.Empty)
            {
                SemanticErrors.Add("ERROR: " + node + ": Index of Array must be Integer, but it's " + typ);
            }

            return exprType;
        }

        if (node.GetType() == typeof(TupleNode))
        {
            // Console.WriteLine("T");
            exprType = Types.Tuple;
            foreach (var item in ((TupleNode)node).Items)
            {
                CheckVarDeclarationSemantics(item.Value);
            }

            return exprType;
        }

        if (node.GetType() == typeof(TupleReferenceNode))
        {
            // Console.WriteLine("TR");
            var name = ((TupleReferenceNode)node).Name;

            CheckExpressionSemantic(((TupleReferenceNode)node).Identifier);
            LocalArea.Add(name);
            var ArgName = ((TupleReferenceNode)node).Argument.Name;
            LocalArea.Add(ArgName);
            var tuple = GetVariable(node, "CheckExpressionSemantic + TRN");
            if (tuple == null)
            {
                SemanticErrors.Add("ERROR: " + node + " There is no such property in tuple '" + name + "'");
            }
            else
            {
                tuple.usage = true;
            }

            LocalArea.RemoveAt(LocalArea.Count - 1);
            LocalArea.RemoveAt(LocalArea.Count - 1);
            return exprType;
        }

        if (node.GetType() == typeof(FunctionNode))
        {
            //     Console.WriteLine("F");
            exprType = Types.Function;
            foreach (var param in ((FunctionNode)node).Parametr)
            {
                CheckVarDeclarationSemantics(new VariableDeclarationNode(param));
            }

            context["func"]++;
            if (((FunctionNode)node).Expression != null)
                CheckExpressionSemantic(((FunctionNode)node).Expression);
            else if (((FunctionNode)node).Body != null)
                CheckSemantics(((FunctionNode)node).Body);
            else
                SemanticWarnings.Add("Warning: " + node + " Function have empty body");
            context["func"]--;
            return exprType;
        }

        if (node.GetType() == typeof(FunctionCallNode))
        {
            //Console.WriteLine("FC");
            var name = ((FunctionCallNode)node).Name;
            LocalArea.Add(name);
            var function = GetVariable(node, "CheckExpressionSemantic + FCN");
            LocalArea.RemoveAt(LocalArea.Count - 1);
            if (function == null)
            {
                SemanticErrors.Add("ERROR:" + node + ": Undeclared function ");
            }
            else
            {
                if (function.getType() != Types.Function)
                    SemanticWarnings.Add("WARNING:" + node + ": variable may not be a function");

                function.usage = true;
                //  Console.WriteLine(variablesDictionary[name]);
                try
                {
                    ((FunctionCallNode)node).Function = (FunctionNode)function.value;
                    if (((FunctionCallNode)node).Arguments.Count != ((FunctionCallNode)node).Function.Parametr.Count)
                        SemanticErrors.Add("ERROR " + node + ": Function '" + name + "' takes " +
                                           ((FunctionCallNode)node).Function.Parametr.Count + " arguments, but given " +
                                           ((FunctionCallNode)node).Arguments.Count);
                    else
                    {
                        for (int i = 0; i < ((FunctionCallNode)node).Arguments.Count; i++)
                        {
                            CheckExpressionSemantic(((FunctionCallNode)node).Arguments[i]);
                        }
                    }
                }
                catch (Exception e)
                {
                    // SemanticWarnings.Add("WARNING: " + function.value + " is function???");
                }
            }
        }

        else if (node.Operator == null && node.Lhs != null && node.Rhs == null)
        {
            //   Console.WriteLine("NOT");
            exprType = CheckExpressionSemantic(node.Lhs);
        }

        else if (node.Operator.Kind.ToString() == Operator.Assign.ToString())
        {
            //  Console.WriteLine("O");
            exprType = CheckExpressionSemantic(node.Rhs);

            // Console.WriteLine("end O");
            if (exprType == Types.Integer || exprType == Types.Real)
            {
                node.Rhs = MakeExpressionSimplee(node.Rhs);
            }
        }
        else if (node.Operator.Kind.ToString() == Operator.MemberwiseAdition.ToString())
        {
            //  Console.WriteLine("MA");
            exprType = CheckExpressionSemantic(node.Rhs);

            // Console.WriteLine("end MA");
        }
        else if (node.Operator.Kind.ToString() == Operator.Is.ToString())
        {
            //  Console.WriteLine("is");
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
            //  Console.WriteLine("Ex");
            var LhsType = CheckExpressionSemantic(node.Lhs);
            var RhsType = CheckExpressionSemantic(node.Rhs);


            if ((LhsType == RhsType || (LhsType == Types.Integer && RhsType == Types.Real)
                                    || (LhsType == Types.Real && RhsType == Types.Integer)) && LhsType != null &&
                RhsType != null && LhsType != Types.Array && LhsType != Types.Tuple)
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
                if (RhsType != Types.Array && LhsType != Types.Array)
                {
                    SemanticErrors.Add("ERROR on " + node.Operator + ":Invalid operands type: " + LhsType +
                                       " operator " + RhsType);
                }
            }
        }

        //  Console.WriteLine("end SCh - " + node);
        return exprType;
    }

    private LiteralNode MakeExpressionSimplee(ExpressionNode node)
    {
        if (node.Operator == null && node.Lhs != null && node.Rhs == null)
        {
            return (LiteralNode)node.Lhs;
        }

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

    private void AddVariable(VariableInformation newVar, Node node)
    {
        Console.WriteLine(node + "Add Var " + newVar);
        var whereToAddPair = variablesDictionary;
        for (int i = 0; i < LocalArea.Count; i++)
        {
            if (i == LocalArea.Count - 1)
            {
                if (!whereToAddPair.ContainsKey(LocalArea[i]))
                {
                    whereToAddPair.Add(LocalArea[i], newVar);
                }
                else
                {
                    SemanticErrors.Add("ERROR: " + node + ":Variable already declared in this area(" +
                                       String.Join("_", LocalArea) + ")");
                    return;
                }
            }
            else
            {
                if (whereToAddPair.ContainsKey(LocalArea[i]))
                {
                    whereToAddPair = whereToAddPair[LocalArea[i]].localVars;
                }
                else
                {
                    SemanticErrors.Add("ERROR: " + node + ":There is no subarea called '" +
                                       String.Join("_", LocalArea.GetRange(0, i)) + "'");
                    return;
                }
            }
        }
    }

    private void UpdateVariable(VariableInformation newVar, Node node)
    {
        var whereToUpdatePair = variablesDictionary;
        for (int i = 0; i < LocalArea.Count; i++)
        {
            if (i == LocalArea.Count - 1)
            {
                if (whereToUpdatePair.ContainsKey(LocalArea[i]))
                {
                    whereToUpdatePair[LocalArea[i]] = newVar;
                }
                else
                {
                    SemanticErrors.Add("ERROR: " + node + ":Variable undeclared in this area(" +
                                       String.Join("_", LocalArea) + ")");
                    return;
                }
            }
            else
            {
                if (whereToUpdatePair.ContainsKey(LocalArea[i]))
                {
                    whereToUpdatePair = whereToUpdatePair[LocalArea[i]].localVars;
                }
                else
                {
                    SemanticErrors.Add("ERROR: " + node + ":There is no subarea called '" +
                                       String.Join("_", LocalArea.GetRange(0, i)) + "'");
                    return;
                }
            }
        }
    }

    private VariableInformation? GetVariable(Node node, string met_name)
    {
        Console.WriteLine("------------GET VAR : " + LocalArea.Count + " --------------- from " + met_name);
        // PrintVariableDictionary(variablesDictionary);
        var whereToFindVariable = variablesDictionary;
        for (int i = 0; i < LocalArea.Count; i++)
        {
            Console.WriteLine("GV: " + LocalArea[i]);
            if (i == LocalArea.Count - 1)
            {
                if (whereToFindVariable.ContainsKey(LocalArea[i]))
                {
                    Console.WriteLine("Return var");

                    return whereToFindVariable[LocalArea[i]];
                }

//                SemanticErrors.Add("ERROR: " + node + ":Variable undeclared in this area(" + String.Join("_", LocalArea) + ")");

                Console.WriteLine("Return err1");
                return null;
            }

            if (whereToFindVariable.ContainsKey(LocalArea[i]))
            {
                //      Console.WriteLine("get as local area ");
                whereToFindVariable = whereToFindVariable[LocalArea[i]].localVars;
            }
            else
            {
                /* SemanticErrors.Add("ERROR: " + node + ":There is no subarea called '" +
                                    String.Join("_", LocalArea.GetRange(0, i)) + "'");*/

                Console.WriteLine("Return err3");
                return null;
            }
        }

        return null;
    }

    public void PrintVariableDictionary(Dictionary<string, VariableInformation> varDict, string gap = "")
    {
        if (gap == "")
            Console.WriteLine("---------------------------------------------------------------");
        Console.WriteLine(gap + (gap == "" ? "D" : " Local d") + "ictionary contains: " + varDict.Count + " variables");
        var idx = 1;
        foreach (var c in varDict.ToList())
        {
            Console.WriteLine(gap + (gap == "" ? "" : " ") + idx + ". " + c.Key + " | " + c.Value);
            if (c.Value.localVars.Count != 0)
            {
                var newGap = gap + "----";
                PrintVariableDictionary(c.Value.localVars, newGap);
            }

            idx++;
        }

        if (gap == "")
            Console.WriteLine("---------------------------------------------------------------");
    }

    void deleteUnuseddFromAST(Node node)
    {
        //Console.WriteLine("------------------------Delete Unusaed VARS-----------------------");
        int indx = 0;
        IEnumerable<Node> children = node.GetChildren();
        foreach (var child in children)
        {
            if (node.GetType() == typeof(BodyNode))
            {
                if (child.GetType() == typeof(VariableDeclarationNode))
                {
                    var varName = ((VariableDeclarationNode)child).Identifier.Name;
                    LocalArea.Add(varName);
                    var variable = GetVariable(child, "deleteUnuseddFromAST");
                    if (variable != null)
                    {
                        if (variable.usage == false)
                        {
                            ((BodyNode)node).Items.RemoveAt(indx);
                            SemanticWarnings.Add("WARNING: delete unused variable " +
                                                 ((VariableDeclarationNode)child).Identifier.Name + "(was on " +
                                                 ((VariableDeclarationNode)child).Identifier.Token.Span + ")");
                            indx--;
                        }
                    }
                }
            }

            if (node.GetType() == typeof(ProgramNode))
            {
                if (child.GetType() == typeof(VariableDeclarationNode))
                {
                    var varName = ((VariableDeclarationNode)child).Identifier.Name;
                    LocalArea.Add(varName);
                    var variable = GetVariable(child, "deleteUnuseddFromAST");
                    if (variable != null)
                    {
                        if (variable.usage == false)
                        {
                            ((ProgramNode)node).DeclarationList.RemoveAt(indx);
                            SemanticWarnings.Add("WARNING: delete unused varibale " +
                                                 ((VariableDeclarationNode)child).Identifier.Name + "(was on " +
                                                 ((VariableDeclarationNode)child).Identifier.Token.Span + ")");
                            indx--;
                        }
                    }
                }
            }


            deleteUnuseddFromAST(child);
            if (child.GetType() == typeof(VariableDeclarationNode))
                LocalArea.RemoveAt(LocalArea.Count - 1);

            indx++;
        }

        afterBreakUsless = false;
    }
}