using System.Text.RegularExpressions;
using Compiler.Core.CodeAnalysis.LexicalAnalysis.LexerTokens;
using Compiler.Core.CodeAnalysis.LexicalAnalysis.LexerTokens.Library;

namespace Compiler.Core.CodeAnalysis.LexicalAnalysis;

public class Lexer
{
    private readonly string _programText;
    private int _position;

    public List<Token> ProgramTokens { get; } = new();
    
    public Lexer(string programText)
    {
        _programText = programText;
        _position = 0;

        FinalStateAutomata(programText);
    }

    private void FinalStateAutomata(string text)
    {
        var buffer = "";
        var lineNumber = 1;
        var columnNumber = 1;
        text += ' ';
        foreach (var symbol in text + ' ')
        {
            var preset = CheckForEnum(buffer);
            Token token = null;

            if (TokenRegexes.Comments.IsMatch(buffer))
            {
                lineNumber += Regex.Matches(buffer, "\r").Count;
                buffer = "";
            }
            else if (preset != Tokens.TkUnknown)
            {

                if (Enum.IsDefined(typeof(LiteralTypesIdentifiers), (int)preset) && !char.IsLetter(symbol))
                    token = new EnumeratedTk<LiteralTypesIdentifiers>(preset);
                else if (Enum.IsDefined(typeof(KeywordTokens), (int)preset) && !char.IsLetter(symbol))
                    token = new EnumeratedTk<KeywordTokens>(preset);

                else if (TokenRegexes.Comparators.IsMatch(buffer))
                    token = new EnumeratedTk<Comparators>(preset);

                else if (TokenRegexes.Operators.IsMatch(buffer) && buffer + symbol is not (".." or "//" or "/*" or "/="))
                    token = new EnumeratedTk<OperatorTokens>(preset);

                else if (TokenRegexes.Puncuators.IsMatch(buffer) &&
                         !TokenRegexes.Comparators.IsMatch(symbol.ToString()))
                    token = new EnumeratedTk<PunctuatorTokens>(preset);

            }
            // Makes literal tokens of types integer and real
            else if (TokenRegexes.Numbers.IsMatch(buffer) &&
                     !(char.IsNumber(symbol) || char.IsLetter(symbol) || symbol == '.'))
            {
                if (long.TryParse(buffer, out var integer)) token = new IntTk(integer);
                else if (double.TryParse(buffer, out var real)) token = new RealTk(real);
            }
            else if (TokenRegexes.Strings.IsMatch(buffer))
            {
                token = new StringTk(buffer);
            }
            else if (bool.TryParse(buffer, out var boolean))
            {
                token = new BoolTk(boolean);
            }
            else if (TokenRegexes.Identifiers.IsMatch(buffer) && !(char.IsLetterOrDigit(symbol) || symbol == '_'))
            {
                token = new IdentifierTk(buffer);
            }
            else if (TokenRegexes.Whitespaces.IsMatch(buffer))
            {
                if (buffer.Contains('\n'))
                {
                    lineNumber++;
                    columnNumber = 0;
                }

                buffer = "";
            }
            else if (TokenRegexes.Unknown.IsMatch(buffer) && TokenRegexes.Whitespaces.IsMatch(symbol.ToString()))
            {
                token = new UnknownTk(buffer);
            }

            if (token is not null)
            {
                token.Span = new Location(lineNumber, columnNumber - buffer.Length, lineNumber, columnNumber - 1);
                ProgramTokens.Add(token);
                token.TokenValue = buffer;
                buffer = "";
            }

            buffer += symbol;
            columnNumber++;
        }
    }

    private static Tokens CheckForEnum(string inputWord)
    {
        return inputWord switch
        { 
            //KeyWords:
            "type" => Tokens.TkType,
            "is" => Tokens.TkIs,
            "end" => Tokens.TkEnd,
            "return" => Tokens.TkReturn,
            "var" => Tokens.TkVar,
            "for" => Tokens.TkFor,
            "while" => Tokens.TkWhile,
            "loop" => Tokens.TkLoop,
            "in" => Tokens.TkIn,
            "if" => Tokens.TkIf,
            "then" => Tokens.TkThen,
            "else" => Tokens.TkElse,
            "print" => Tokens.TkPrint,
            "func" => Tokens.TkFunc,
            
            "Real" => Tokens.TkRealLiteralIdentifier,
            "Integer" => Tokens.TkIntLiteralIdentifier,
            "String" => Tokens.TkStringLiteralIdentifier,
            "Bool" => Tokens.TkBoolLiteralIdentifier,
            "Array" => Tokens.TkArrayIdentifier,
            "Tuple" => Tokens.TkTupleIdentifier,
            "Empty" => Tokens.TkEmptyIdentifier,

            //Punctuators:
            "(" => Tokens.TkRoundOpen,
            ")" => Tokens.TkRoundClose,
            "{" => Tokens.TkCurlyOpen,
            "}" => Tokens.TkCurlyClose,
            "[" => Tokens.TkSquareOpen,
            "]" => Tokens.TkSquareClose,
            ";" => Tokens.TkSemicolon,
            ":" => Tokens.TkColon,
            "," => Tokens.TkComma,

            //Operators:
            ":=" => Tokens.TkAssign,
            "." => Tokens.TkDot,
            "-" => Tokens.TkMinus,
            "+" => Tokens.TkPlus,
            "*" => Tokens.TkMultiply,
            "/" => Tokens.TkDivide,
            "%" => Tokens.TkPercent,
            "and" => Tokens.TkAnd,
            "or" => Tokens.TkOr,
            "xor" => Tokens.TkXor,
            ".." => Tokens.TkRange,

            // Comparators:
            "<=" => Tokens.TkLeq,
            ">=" => Tokens.TkGeq,
            "<" => Tokens.TkLess,
            ">" => Tokens.TkGreater,
            "=" => Tokens.TkEqual,
            "!=" => Tokens.TkNotEqual,
            _ => Tokens.TkUnknown
        };
    }
}