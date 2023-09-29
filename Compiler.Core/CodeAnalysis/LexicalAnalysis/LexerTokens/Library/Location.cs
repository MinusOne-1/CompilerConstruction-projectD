namespace Compiler.Core.CodeAnalysis.LexicalAnalysis.LexerTokens.Library;

public class Location
{
    private int startLine; // start line
    private int startColumn; // start column
    private int endLine; // end line
    private int endColumn; // end column

    public int StartLine
    {
        get { return startLine; }
    }

    public int StartColumn
    {
        get { return startColumn; }
    }

    public int EndLine
    {
        get { return endLine; }
    }

    public int EndColumn
    {
        get { return endColumn; }
    }

    public Location()
    {
    }

    public Location(int sl, int sc, int el, int ec)
    {
        startLine = sl;
        startColumn = sc;
        endLine = el;
        endColumn = ec;
    }

    public Location Merge(Location last)
    {
        return new Location(this.startLine, this.startColumn, last.endLine, last.endColumn);
    }

    public override string ToString()
    {
        return (this.startLine.ToString() + ':' + this.startColumn.ToString() + "-" + this.endLine + ":" +
                this.EndColumn);
    }
}