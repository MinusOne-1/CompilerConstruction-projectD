using Compiler.Core.CodeAnalysis.LexicalAnalysis.LexerTokens.Library;

namespace Compiler.Core.CodeAnalysis.LexicalAnalysis.LexerTokens;

public enum PunctuatorTokens
{
    TkRoundOpen = Tokens.TkRoundOpen,
    TkRoundClose = Tokens.TkRoundClose,
    TkCurlyOpen = Tokens.TkCurlyOpen,
    TkCurlyClose = Tokens.TkCurlyClose,
    TkSquareOpen = Tokens.TkSquareOpen,
    TkSquareClose = Tokens.TkSquareClose,
    TkSemicolon = Tokens.TkSemicolon,
    TkColon = Tokens.TkColon,
    TkComma = Tokens.TkComma
}

public enum LiteralTypesIdentifiers
{
    TkIntLiteralIdentifier = Tokens.TkIntLiteralIdentifier,
    TkRealLiteralIdentifier = Tokens.TkRealLiteralIdentifier,
    TkStringLiteralIdentifier = Tokens.TkStringLiteralIdentifier,
    TkBoolLiteralIdentifier = Tokens.TkBoolLiteralIdentifier,
    TkArrayIdentifier = Tokens.TkArrayIdentifier,
    TkTupleIdentifier = Tokens.TkTupleIdentifier,
    TkEmptyIdentifier = Tokens.TkEmptyIdentifier,
}

public enum KeywordTokens
{
    TkType = Tokens.TkType,
    TkEnd = Tokens.TkEnd,
    TkReturn = Tokens.TkReturn,
    TkVar = Tokens.TkVar,
    TkRoutine = Tokens.TkRoutine,
    TkFor = Tokens.TkFor,
    TkWhile = Tokens.TkWhile,
    TkLoop = Tokens.TkLoop,
    TkIn = Tokens.TkIn,
    TkReverse = Tokens.TkReverse,
    TkIf = Tokens.TkIf,
    TkThen = Tokens.TkThen,
    TkElse = Tokens.TkElse,
    TkArray = Tokens.TkArray,
    TkPrint = Tokens.TkPrint,
    TkIs = Tokens.TkIs,
    TkFunc = Tokens.TkFunc,
    TkConsequence = Tokens.TkConsequence,
    TkBreak = Tokens.TkBreak,
}

public enum OperatorTokens
{
    TkAssign = Tokens.TkAssign,
    TkDot = Tokens.TkDot,
    TkMinus = Tokens.TkMinus,
    TkPlus = Tokens.TkPlus,
    TkMultiply = Tokens.TkMultiply,
    TkDivide = Tokens.TkDivide,
    TkPercent = Tokens.TkPercent,
    TkAnd = Tokens.TkAnd,
    TkOr = Tokens.TkOr,
    TkXor = Tokens.TkXor,
    TkRange = Tokens.TkRange,
    TkIs = Tokens.TkIs
}

internal enum Comparators
{
    TkLeq = Tokens.TkLeq,
    TkGeq = Tokens.TkGeq,
    TkLess = Tokens.TkLess,
    TkGreater = Tokens.TkGreater,
    TkEqual = Tokens.TkEqual,
    TkNotEqual = Tokens.TkNotEqual
}