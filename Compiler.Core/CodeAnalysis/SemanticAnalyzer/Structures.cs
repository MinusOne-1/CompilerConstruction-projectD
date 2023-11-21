using System.Runtime.InteropServices.JavaScript;
using Compiler.Core.CodeAnalysis.SyntaxAnalysis;

namespace Compiler.Core.CodeAnalysis.SemanticAnalyzer;

public enum Types
{
    Integer,
    Real,
    Boolean,
    String,
    Array,
    Tuple,
    Empty,
    Function
}



public class VariableInformation
{
    private string name;
    private Types? varType;
    public Object? value;
    private string? viewArea;
    public bool usage;
    public Dictionary<string, VariableInformation> localVars;

    public VariableInformation(string name_, string type_ = null, Object value_ = null, string viewArea_ = null)
    {
        name = name_;
        varType = type_ == null ? Types.Empty : whatTypes(type_);
        value = value_;
        viewArea = (viewArea_ == null ? viewArea_ : "global");
        usage = false;
        localVars = new Dictionary<string, VariableInformation>();
    }
    
    public static Types? whatTypes(string type_)
    {
        return (Types)Enum.Parse(typeof(Types), type_);
    }

    public static String whatName(Node node)
    {
        return ((IdentifierNode)node).Token.TokenValue;
    }


    public String getName()
    {
        return name;
    }

    public Types? getType()
    {
        return varType;
    }

    public void setType(String type_)
    {
        if (Enum.TryParse(type_, out Types t))
            varType = (Types)Enum.Parse(typeof(Types), type_);
    }
    public void setType(Types? type_)
    {
        if (type_ != null)
            varType = type_;
    }

    public Object? getValue()
    {
        return value;
    }

    public void setValue(Object value_)
    {
        value = value_;
    }

    public string getViewArea()
    {
        return viewArea;
    }
    public void setViewArea(string new_area)
    {
        viewArea = new_area;
    }

    public override string ToString()
    {
        return name + "(" + varType + ", used=" + usage+ ")";
    }

    public bool isInitialyzed()
    {
        return value != null;
    }
}

public class FucntionInformation
{
    private String name;
    private FunctionNode body;
    private List<VariableInformation>? arguments;

    public bool
        usage;

    public FucntionInformation(String name_, FunctionNode body_)
    {
        name = name_;
        body = body_;
        arguments = CreateSignature();
        usage = false;
    }

    private List<VariableInformation> CreateSignature()
    {
        var resultSignature = new List<VariableInformation>();
        //TODO: parse Body.Parametr of function to get signature
        return resultSignature;
    }

    public String checkSignarure(List<VariableInformation> variables)
    {
        var validation = "valid";
        validation = "Function (" + name + ") needs arguments:(" + arguments.ToString() + "), wrong numer of arguments";
        for (int i = 0; i < variables.Count; i++)
        {
            if (!(arguments[i].getType() == variables[i].getType() || arguments[i].getType() == null))

                validation = "Function (" + name + ") needs arguments: (" + arguments.ToString() +
                             "), but this arguments was given: (" + variables.ToString() + ")";
        }

        return validation;
    }
}