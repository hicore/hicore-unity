using System.Threading.Tasks;

namespace Hicore.Parsers
{
  internal class DisconnectedParser : IParser
  {
    public Task ParseAsync(ResponseTextParser rtp)
    {
      if (rtp.Text == "41" + rtp.Namespace)
        return rtp.Socket.InvokeClosedAsync();
      rtp.Parser = (IParser) new MessageEventParser();
      return rtp.ParseAsync();
    }
  }
}
