using System.Threading.Tasks;

namespace Hicore.Parsers
{
  public class ResponseTextParser
  {
    public ResponseTextParser(string ns, HicoreSocket socket)
    {
      this.Parser = (IParser) new OpenedParser();
      this.Namespace = ns;
      this.Socket = socket;
    }

    public IParser Parser { get; set; }

    public string Text { get; set; }

    public string Namespace { get; }

    public HicoreSocket Socket { get; }

    public Task ParseAsync()
    {
      return this.Parser.ParseAsync(this);
    }
  }
}
