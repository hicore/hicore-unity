using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Hicore.Arguments;

namespace Hicore.Parsers
{
  internal class MessageEventParser : IParser
  {
    public Task ParseAsync(ResponseTextParser rtp)
    {
      Regex regex = new Regex("^42" + rtp.Namespace + "\\d*\\[\"([*\\s\\w-]+)\",([\\s\\S]*)\\]$");
      if (regex.IsMatch(rtp.Text))
      {
        GroupCollection groups = regex.Match(rtp.Text).Groups;
        string index = groups[1].Value;
        ResponseArgs args = new ResponseArgs()
        {
          Text = groups[2].Value,
          RawText = rtp.Text
        };
        if (rtp.Socket.EventHandlers.ContainsKey(index))
          rtp.Socket.EventHandlers[index](args);
        else
          rtp.Socket.InvokeUnhandledEvent(index, args);
        rtp.Socket.InvokeReceivedEvent(index, args);
        return Task.CompletedTask;
      }
      rtp.Parser = (IParser) new MessageAckParser();
      return rtp.ParseAsync();
    }
  }
}
