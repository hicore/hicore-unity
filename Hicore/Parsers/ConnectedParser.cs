using System.Threading.Tasks;

namespace Hicore.Parsers
{
  internal class ConnectedParser : IParser
  {
    public Task ParseAsync(ResponseTextParser rtp)
    {
      if (rtp.Text == "40" + rtp.Namespace)
        return rtp.Socket.InvokeConnectedAsync();
      if (rtp.Text == "40")
        return Task.CompletedTask;
      rtp.Parser = (IParser) new DisconnectedParser();
      return rtp.ParseAsync();
    }
  }
}
