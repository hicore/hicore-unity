using Newtonsoft.Json;
using System.Threading.Tasks;
using Hicore.Arguments;

namespace Hicore.Parsers
{
  internal class OpenedParser : IParser
  {
    public Task ParseAsync(ResponseTextParser rtp)
    {
      if (rtp.Text.StartsWith("0{\"sid\":\""))
      {
        OpenedArgs args = (OpenedArgs) JsonConvert.DeserializeObject<OpenedArgs>(rtp.Text.TrimStart('0'));
        return rtp.Socket.InvokeOpenedAsync(args);
      }
      rtp.Parser = (IParser) new ConnectedParser();
      return rtp.ParseAsync();
    }
  }
}
