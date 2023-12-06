using System.Linq.Expressions;
using Compiler.Core.CodeAnalysis.LexicalAnalysis.LexerTokens.Library;
using Compiler.Core.CodeAnalysis.SemanticAnalyzer;
using Compiler.Core.CodeAnalysis.SyntaxAnalysis;

namespace Compiler.Core.Interpretator;

public class Interpretator
{
    public Dictionary<string, VariableInformation> variablesDictionary;

    public Tree AST;
    public List<string> LocalArea;
    public bool ErrorOccurs = false;
    public bool BreakOccurs = false;
    public bool ReturnOccurs = false;
    public object ReturnedVar;

    public Interpretator(Dictionary<string, VariableInformation> variableDict, Tree ast)
    {
        variablesDictionary = variableDict;
        AST = ast;
        LocalArea = new();
        Run(AST.Root);
        Console.WriteLine("---------------------------------------------------------------");
        Console.WriteLine("Program ends with" + (ErrorOccurs ? " error." : "out errors."));
    }

    private void Run(Node node)
    {
        
        if (ErrorOccurs || BreakOccurs || ReturnOccurs)
            return;
        var nodeType = node.GetType();

        var children = node.GetChildren();

        if (nodeType == typeof(PrintNode))
        {
            RunPrint((PrintNode)node);
        }

        if (nodeType == typeof(VariableDeclarationNode))
        {
            RunVarDeclaration((VariableDeclarationNode)node);
        }
        else if (nodeType == typeof(VariableAssignmentNode))
        {
            RunVarAssignment((VariableAssignmentNode)node);
        }
        else if (nodeType == typeof(MemberwiseAdditionNode))
        {
            RunVarMemberwiseAddition((MemberwiseAdditionNode)node);
        }

        else if (nodeType == typeof(ExpressionNode))
        {
            RunExpression((ExpressionNode)node);
        }
        else if (nodeType == typeof(IfNode))
        {
            var ifCondition = ((IfNode)node).Condition;
            var ifBody = ((IfNode)node).ThenBody;
            var elseBody = ((IfNode)node).ElseBody;
            var conditionValue = RunExpression(ifCondition);
            if (conditionValue == null)
            {
                ShowError("Error in if condition", node);
                return;
            }

            if (conditionValue is not bool)
            {
                ShowError("if condition should be bool, but it is " + conditionValue.GetType(), node);
                return;
            }

            if ((bool)conditionValue)
            {
                Run(ifBody);
            }

            if (!(bool)conditionValue && elseBody != null)
            {
                Run(elseBody);
            }
        }

        else if (nodeType == typeof(WhileLoopNode))
        {
            var whileBody = ((WhileLoopNode)node).Body;
            var whileCondition = ((WhileLoopNode)node).Condition;
            while (true)
            {
                var value = RunExpression(whileCondition);
                if (value == null)
                {
                    ShowError("Error in while conditions", node);
                    return;
                }

                if (value is bool)
                {
                    if (!(bool)value)
                    {
                        break;
                    }
                }

                Run(whileBody);
                if (BreakOccurs)
                {
                    break;
                }
            }

            BreakOccurs = false;
        }

        else if (nodeType == typeof(ForLoopNode))
        {
            var forbody = ((ForLoopNode)node).Body;
            var variable_name = ((ForLoopNode)node).VariableIdentifier.Name;
            //was guaranteed by Semantics Checks: (range int...int) and (variable is declared)
            var for_from = (int)RunExpression(((ForLoopNode)node).Range.From);
            var for_to = (int)RunExpression(((ForLoopNode)node).Range.To);
            LocalArea.Add(variable_name);
            var variable = GetVariable();
            LocalArea.RemoveAt(LocalArea.Count - 1);

            variable.setValue(for_from);
            variable.setType(Types.Integer);
            for (int i = for_from; i < for_to; i++)
            {
                variable.setValue(i);
                Run(forbody);
                if (BreakOccurs)
                {
                    break;
                }
            }

            BreakOccurs = false;
        }

        else if (nodeType == typeof(ReturnNode))
        {
            var value = RunExpression(((ReturnNode)node).Expression);
            ReturnOccurs = true;
            if (value == null)
                ShowError("Invalid return statement", node);
            ReturnedVar = value;
        }
        else if (nodeType == typeof(BreakNode))
        {
            //break occurs only in loops in interpretator stage, so I will return back from the recursion while some loop node aren't be found
            BreakOccurs = true;
        }
        else if (nodeType == typeof(FunctionNode))
        {
            return; // we haven't to process function until it will be called
        }
        else if (nodeType == typeof(FunctionCallNode))
        {
            RunFunctionCall((FunctionCallNode)node);
        }
        else
        {
            foreach (var child in children)
                Run(child);
        }
    }

    private object? RunExpression(ExpressionNode node)
    {
        if (ErrorOccurs || BreakOccurs || ReturnOccurs)
            return null;
        if (node.GetType() == typeof(LiteralNode))
        {
            return GetTypedValueFromLiteralNode((LiteralNode)node);
        }

        if (node.GetType() == typeof(IdentifierNode))
        {
            if (((IdentifierNode)node).Token.TokenId == Tokens.TkIntLiteralIdentifier)
                return 1;
            if (((IdentifierNode)node).Token.TokenId == Tokens.TkRealLiteralIdentifier)
            {
                return 1.1;
            }

            if (((IdentifierNode)node).Token.TokenId == Tokens.TkStringLiteralIdentifier)
                return "1";
            if (((IdentifierNode)node).Token.TokenId == Tokens.TkBoolLiteralIdentifier)
                return true;
            return GetVariableValue(((IdentifierNode)node).Name);
        }

        if (node.GetType() == typeof(ArrayReferenceNode))
        {
            return GetArrayReferedValue(node);
        }

        if (node.GetType() == typeof(TupleReferenceNode))
        {
            return getTupleProperty(node);
        }

        if (node.GetType() == typeof(FunctionCallNode))
        {
            RunFunctionCall((FunctionCallNode)node);
            return ReturnedVar;
        }

        if (node.GetType() == typeof(FunctionNode))
        {
            return node;
        }

        if (node.GetType() == typeof(TupleNode))
        {
            // TODO: Tuple Node return dictionary<string, VariableInformation>?
        }

        if (node.GetType() == typeof(ListNode<Node>))
        {
            List<object> res = new();
            foreach (var item in ((ListNode<Node>)node).Items)
            {
                var itemValue = RunExpression((ExpressionNode)item);
                if (itemValue == null)
                {
                    ShowError("Invalid list item", node);
                    return null;
                }

                res.Add(itemValue);
            }

            return res;
        }

        if (node.GetType() == typeof(ExpressionNode))
        {
            if (node.Lhs != null && node.Rhs == null)
            {
                return RunExpression(node.Lhs);
            }

            if (node.Lhs == null && node.Rhs != null)
            {
                return RunExpression(node.Rhs);
            }

            if (node.Lhs != null && node.Rhs != null)
            {
                
                
                var Lhs = RunExpression(node.Lhs);
                var Rhs = RunExpression(node.Rhs);

                //comparators
                if (node.Operator.Kind.ToString() == Operator.Greater.ToString())
                {
                    if (Lhs is int && Rhs is int)
                        return (int)Lhs > (int)Rhs;
                    if (Lhs is double && Rhs is double)
                        return (double)Lhs > (double)Rhs;
                    if (Lhs is double && Rhs is int)
                        return (double)Lhs > (int)Rhs;
                    if (Lhs is int && Rhs is double)
                        return (int)Lhs > (double)Rhs;

                    if (Lhs != null && Rhs != null)
                        ShowError("Invalid terms: " + Lhs.GetType() + " > " + Rhs.GetType(), node);
                }

                if (node.Operator.Kind.ToString() == Operator.GreaterOrEqual.ToString())
                {
                    if (Lhs is int && Rhs is int)
                        return (int)Lhs >= (int)Rhs;
                    if (Lhs is double && Rhs is double)
                        return (double)Lhs >= (double)Rhs;
                    if (Lhs is double && Rhs is int)
                        return (double)Lhs >= (int)Rhs;
                    if (Lhs is int && Rhs is double)
                        return (int)Lhs >= (double)Rhs;

                    if (Lhs != null && Rhs != null)
                        ShowError("Invalid terms: " + Lhs.GetType() + " >= " + Rhs.GetType(), node);
                }

                if (node.Operator.Kind.ToString() == Operator.Less.ToString())
                {
                    if (Lhs is int && Rhs is int)
                        return (int)Lhs < (int)Rhs;
                    if (Lhs is double && Rhs is double)
                        return (double)Lhs < (double)Rhs;
                    if (Lhs is double && Rhs is int)
                        return (double)Lhs < (int)Rhs;
                    if (Lhs is int && Rhs is double)
                        return (int)Lhs < (double)Rhs;

                    if (Lhs != null && Rhs != null)
                        ShowError("Invalid terms: " + Lhs.GetType() + " < " + Rhs.GetType(), node);
                }

                if (node.Operator.Kind.ToString() == Operator.LessOrEqual.ToString())
                {
                    if (Lhs is int && Rhs is int)
                        return (int)Lhs <= (int)Rhs;
                    if (Lhs is double && Rhs is double)
                        return (double)Lhs <= (double)Rhs;
                    if (Lhs is double && Rhs is int)
                        return (double)Lhs <= (int)Rhs;
                    if (Lhs is int && Rhs is double)
                        return (int)Lhs <= (double)Rhs;

                    if (Lhs != null && Rhs != null)
                        ShowError("Invalid terms: " + Lhs.GetType() + " <= " + Rhs.GetType(), node);
                }

                if (node.Operator.Kind.ToString() == Operator.NotEqual.ToString())
                {
                    if (Lhs is int && Rhs is int)
                        return (int)Lhs != (int)Rhs;
                    if (Lhs is double && Rhs is double)
                        return (double)Lhs != (double)Rhs;
                    if (Lhs is double && Rhs is int)
                        return (double)Lhs != (int)Rhs;
                    if (Lhs is int && Rhs is double)
                        return (int)Lhs != (double)Rhs;
                    if (Lhs is string && Rhs is string)
                        return (string)Lhs != (string)Rhs;

                    if (Lhs != null && Rhs != null)
                        ShowError("Invalid terms: " + Lhs.GetType() + " != " + Rhs.GetType(), node);
                }

                if (node.Operator.Kind.ToString() == Operator.Equal.ToString())
                {
                    if (Lhs is int && Rhs is int)
                        return (int)Lhs == (int)Rhs;
                    if (Lhs is double && Rhs is double)
                        return (double)Lhs == (double)Rhs;
                    if (Lhs is double && Rhs is int)
                        return (double)Lhs == (int)Rhs;
                    if (Lhs is int && Rhs is double)
                        return (int)Lhs == (double)Rhs;
                    if (Lhs is string && Rhs is string)
                        return (string)Lhs == (string)Rhs;
                    if (Lhs is List<object> && Rhs is List<object>)
                        return (List<object>)Lhs == (List<object>)Rhs;

                    if (Lhs != null && Rhs != null)
                        ShowError("Invalid terms: " + Lhs.GetType() + " == " + Rhs.GetType(), node);
                }

                //logical
                if (node.Operator.Kind.ToString() == Operator.And.ToString())
                {
                    if (Lhs is bool && Rhs is bool)
                        return (bool)Lhs && (bool)Rhs;

                    if (Lhs != null && Rhs != null)
                        ShowError("Invalid terms: " + Lhs.GetType() + " and " + Rhs.GetType(), node);
                }

                if (node.Operator.Kind.ToString() == Operator.Or.ToString())
                {
                    if (Lhs is bool && Rhs is bool)
                        return (bool)Lhs || (bool)Rhs;

                    if (Lhs != null && Rhs != null)
                        ShowError("Invalid terms: " + Lhs.GetType() + " or " + Rhs.GetType(), node);
                }

                if (node.Operator.Kind.ToString() == Operator.Xor.ToString())
                {
                    //TODO: xor
                }

                if (node.Operator.Kind.ToString() == Operator.Is.ToString())
                {
                    if (Lhs != null && Rhs != null)
                        return Lhs.GetType() == Rhs.GetType();
                }

                if (node.Operator.Kind.ToString() == Operator.Plus.ToString())
                {
                    //Console.WriteLine(Lhs + " " + Rhs + " " + node);
                
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

                    if (Lhs is List<object> && Rhs is List<object>)
                    {
                        var res = new List<object>();
                        res.AddRange((List<object>)Lhs);
                        res.AddRange((List<object>)Rhs);
                        return res;
                    }

                    if (Lhs is List<object> && Rhs != null)
                    {
                        var res = new List<object>();
                        res.AddRange((List<object>)Lhs);
                        res.Add(Rhs);
                        return res;
                    }

                    if (Rhs is List<object> && Lhs != null)
                    {
                        var res = new List<object>();
                        res.Add(Lhs);
                        res.AddRange((List<object>)Rhs);
                        return res;
                    }

                    if (Lhs != null && Rhs != null)
                        ShowError("Invalid terms: " + Lhs.GetType() + " + " + Rhs.GetType(), node);
                }

                if (node.Operator.Kind.ToString() == Operator.Minus.ToString())
                {
                    if (Lhs is int && Rhs is int)
                        return (int)Lhs - (int)Rhs;
                    if (Lhs is double && Rhs is double)
                        return (double)Lhs - (double)Rhs;
                    if (Lhs is double && Rhs is int)
                        return (double)Lhs - (int)Rhs;
                    if (Lhs is int && Rhs is double)
                        return (int)Lhs - (double)Rhs;

                    if (Lhs != null && Rhs != null)
                        ShowError("Invalid terms: " + Lhs.GetType() + " - " + Rhs.GetType(), node);
                }

                if (node.Operator.Kind.ToString() == Operator.Divide.ToString())
                {
                    if (Lhs is int && Rhs is int)
                        return (int)Lhs / (int)Rhs;
                    if (Lhs is double && Rhs is double)
                        return (double)Lhs / (double)Rhs;
                    if (Lhs is double && Rhs is int)
                        return (double)Lhs / (int)Rhs;
                    if (Lhs is int && Rhs is double)
                        return (int)Lhs / (double)Rhs;

                    if (Lhs != null && Rhs != null)
                        ShowError("Invalid terms: " + Lhs.GetType() + " / " + Rhs.GetType(), node);
                }

                if (node.Operator.Kind.ToString() == Operator.Multiply.ToString())
                {
                    if (Lhs is int && Rhs is int)
                        return (int)Lhs * (int)Rhs;
                    if (Lhs is double && Rhs is double)
                        return (double)Lhs * (double)Rhs;
                    if (Lhs is double && Rhs is int)
                        return (double)Lhs * (int)Rhs;
                    if (Lhs is int && Rhs is double)
                        return (int)Lhs * (double)Rhs;

                    if (Lhs != null && Rhs != null)
                        ShowError("Invalid terms: " + Lhs.GetType() + " * " + Rhs.GetType(), node);
                }
            }
        }

        return null;
    }

    private void RunFunctionCall(FunctionCallNode node)
    {
        var funcName = (node).Identifier.Name;
        LocalArea.Add(funcName);
        var mayBeFunction = GetVariable();
        LocalArea.RemoveAt(LocalArea.Count - 1);
        if (mayBeFunction == null)
        {
            ShowError("Some Error in function call", node);

            return;
        }

        if (mayBeFunction.getType() != Types.Function)
        {
            ShowError("Variable '" + funcName + "' can't be called, cause it has type " + mayBeFunction.getType(),
                node);

            return;
        }

        var functionNode = (FunctionNode)mayBeFunction.value;
        if (functionNode.Parametr.Count != (node).Arguments.Count)
        {
            ShowError(
                "Function '" + funcName + "' has " + functionNode.Parametr.Count + " parameters, but " +
                (node).Arguments.Count + " was given",
                node);

            return;
        }

        var argumentsList = new List<object>();
        for (var i = 0; i < functionNode.Parametr.Count; i++)
        {
            var value = RunExpression((node).Arguments[i]);
            if (value == null)
            {
                ShowError("Error in function call's argument ", (node).Arguments[i]);
                return;
            }

            argumentsList.Add(value);
        }

        LocalArea.Add(funcName);
        for (var i = 0; i < functionNode.Parametr.Count; i++)
        {
            var name = functionNode.Parametr[i].Name;
            LocalArea.Add(name);
            var variable = GetVariable();
            variable.setValue(argumentsList[i]);
            LocalArea.RemoveAt(LocalArea.Count - 1);
        }

        if (functionNode.Body != null)
            Run(functionNode.Body);
        else if (functionNode.Expression != null)
            ReturnedVar = RunExpression(functionNode.Expression);
        ReturnOccurs = false;
        LocalArea.RemoveAt(LocalArea.Count - 1);
    }

    private void RunVarDeclaration(VariableDeclarationNode node)
    {
        if (ErrorOccurs || BreakOccurs || ReturnOccurs)
            return;
        LocalArea.Add(node.Identifier.Name);
        AddVariableIfThereIsNot();
        LocalArea.RemoveAt(LocalArea.Count - 1);
        if (node.Expression != null)
        {
            RunVarAssignment(new VariableAssignmentNode(node));
        }
        else
        {
            LocalArea.Add(node.Identifier.Name);
            GetVariable().setValue(null);
            LocalArea.RemoveAt(LocalArea.Count - 1);
        }
    }


    private void RunVarAssignment(VariableAssignmentNode node)
    {
        if (ErrorOccurs || BreakOccurs || ReturnOccurs)
            return;
        if (node.Expression != null)
        {
            // in Assignment Node Rhs of expression is always here
            var value = RunExpression(node.Expression.Rhs);
            if (value == null)
            {
                ShowError("Error during expression calculation", node);
                return;
            }

            LocalArea.Add(node.Identifier.Name);
            GetVariable().setValue(value);
            if (value.GetType() == typeof(FunctionNode))
            {
                foreach (var param in ((FunctionNode)value).Parametr)
                {
                    LocalArea.Add(param.Name);
                    AddVariableIfThereIsNot();
                    LocalArea.RemoveAt(LocalArea.Count - 1);
                }
            }

            LocalArea.RemoveAt(LocalArea.Count - 1);
        }
    }

    private void RunVarMemberwiseAddition(MemberwiseAdditionNode node)
    {
        if (ErrorOccurs || BreakOccurs || ReturnOccurs)
            return;
        throw new NotImplementedException();
    }


    private void RunPrint(PrintNode node)
    {
       
        if (ErrorOccurs || BreakOccurs || ReturnOccurs)
            return;
        var res = new List<string>();
        foreach (var item in node.Items)
        {
            var itemValue = RunExpression(item);
            if (itemValue == null)
            {
                ShowError("Invalid item for printing", item);
                return;
            }

            if (itemValue.GetType() == typeof(List<object>))
            {
                var str_list = "[";
                for (int j = 0; j < ((List<object>)itemValue).Count; j++)
                {
                    if (j != 0)
                        str_list += ", ";
                    str_list += ((List<object>)itemValue)[j].ToString();

                    if (j == ((List<object>)itemValue).Count - 1)
                        str_list += "]";
                }

                res.Add(str_list);
            }
            else
            {
                res.Add(itemValue.ToString());
            }
        }

        Console.WriteLine(String.Join(", ", res));
    }


    private object GetVariableValue(string name)
    {
        LocalArea.Add(name);
        var variable = GetVariable();
        object value = null;
        if (variable != null)
            value = variable.value;
        LocalArea.RemoveAt(LocalArea.Count - 1);
        return value;
    }

    private object? getTupleProperty(Node node)
    {
        var tupleName = ((TupleReferenceNode)node).Identifier.Name;
        var propertyName = ((TupleReferenceNode)node).Argument.Name;
        LocalArea.Add(tupleName);
        var tuple = GetVariable();
        if (tuple.getType() != Types.Tuple)
        {
            ShowError("Can not get property from " + tuple.getType(), node);
            return null;
        }

        LocalArea.Add(propertyName);

        var property = GetVariable();
        if (property == null)
        {
            ShowError("There is no property called '" + propertyName + "' in tuple '" + tupleName + "'", node);
            return null;
        }

        LocalArea.RemoveAt(LocalArea.Count - 1);
        LocalArea.RemoveAt(LocalArea.Count - 1);
        return property.value;
    }

    private object? GetArrayReferedValue(Node node)
    {
        var index = RunExpression(((ArrayReferenceNode)node).Index);
        if (index == null)
        {
            ShowError("Invalid index for array reference", node);
            return null;
        }

        if (index.GetType() != typeof(int))
        {
            ShowError("Invalid index for array reference: index sould be integer, not " + index.GetType(), node);
            return null;
        }

        var intIndex = (int)index;
        var value = (List<object>)GetVariableValue(((ArrayReferenceNode)node).Identifier.Name);
        if (intIndex >= value.Count)
        {
            ShowError("Index out of range", node);
            return null;
        }

        return value[intIndex];
    }


    private void ShowError(string error, Node node)
    {
        Console.WriteLine("INTERPRETATION ERROR on " + node + ": " + error);
        ErrorOccurs = true;
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

    private void AddVariableIfThereIsNot()
    {
        var whereToFindVariable = variablesDictionary;
        for (int i = 0; i < LocalArea.Count; i++)
        {
            if (i == LocalArea.Count - 1)
            {
                if (!whereToFindVariable.ContainsKey(LocalArea[i]))
                {
                    whereToFindVariable.Add(LocalArea[i], new VariableInformation(LocalArea[i]));
                }
            }

            if (whereToFindVariable.ContainsKey(LocalArea[i]))
            {
                whereToFindVariable = whereToFindVariable[LocalArea[i]].localVars;
            }
            else
            {
                //  Console.WriteLine("Return err3");
                return;
            }
        }
    }

    private VariableInformation? GetVariable()
    {
        var whereToFindVariable = variablesDictionary;
        for (int i = 0; i < LocalArea.Count; i++)
        {
            if (i == LocalArea.Count - 1)
            {
                if (whereToFindVariable.ContainsKey(LocalArea[i]))
                {
                    // Console.WriteLine("Return var");

                    return whereToFindVariable[LocalArea[i]];
                }

                //   Console.WriteLine("Return err1");
                return null;
            }

            if (whereToFindVariable.ContainsKey(LocalArea[i]))
            {
                if (whereToFindVariable[LocalArea[i]].getType() != Types.Function &&
                    whereToFindVariable[LocalArea[i]].getType() != Types.Tuple)
                    ErrorOccurs = true;

                whereToFindVariable = whereToFindVariable[LocalArea[i]].localVars;
            }
            else
            {
                //  Console.WriteLine("Return err3");
                return null;
            }
        }

        return null;
    }
}