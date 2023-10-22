using Compiler.Core.CodeAnalysis.LexicalAnalysis.LexerTokens.Library;

namespace Compiler.Core.CodeAnalysis.LexicalAnalysis.LexerTokens;

public abstract class Token
{
    public Location? Span { get; set; }
    public abstract Tokens TokenId { get; }
    public string TokenValue { get; set; }

    public override string ToString()
    {
        return (TokenId.ToString() + " '" + TokenValue + "' on " + (Span == null ? "" : Span.ToString()));
    }
}

public class UnknownTk : Token
{
    public string Value { get; }

    public UnknownTk(string value)
    {
        Value = value;
    }

    public override Tokens TokenId => Tokens.TkUnknown;
}

public class IdentifierTk : Token
{
    public string Value { get; }

    public IdentifierTk(string value)
    {
        Value = value;
    }

    public override Tokens TokenId => Tokens.TkIdentifier;
}

/// <summary>
///     This generic class is recommended to use for Keyword, Type, Operator, Punctuator, and comparator tokens
/// </summary>
/// <typeparam name="T"></typeparam>
public class EnumeratedTk<T> : Token where T : Enum
{
    private readonly int _tokenId;

    public EnumeratedTk(Tokens value)
    {
        if (Enum.IsDefined(typeof(T), (int)value)) _tokenId = (int)value;
        else throw new ArgumentOutOfRangeException($"Enum {typeof(T)} doesn't have value {value}");
    }

    public override Tokens TokenId => (Tokens)_tokenId;
}

#region LiteralTokens

public class IntTk : Token
{
    public long Value { get; }

    public IntTk(String value)
    {
        TokenValue = value;
    }

    public override Tokens TokenId => Tokens.TkIntLiteral;
}
public class StringTk : Token
{
    public string Value { get; }

    public StringTk(string value)
    {
        TokenValue = value;
    }

    public override Tokens TokenId => Tokens.TkStringLiteral;
}

public class RealTk : Token
{
    public double Value { get; }

    public RealTk(string value)
    {
        TokenValue = value;
    }

    public override Tokens TokenId => Tokens.TkRealLiteral;
}

public class BoolTk : Token
{
    public bool Value { get; }

    public BoolTk(String value)
    {
        TokenValue = value;
    }

    public override Tokens TokenId => Tokens.TkBoolLiteral;
}

#endregion