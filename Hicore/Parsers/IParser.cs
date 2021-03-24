using System.Threading.Tasks;

namespace Hicore.Parsers
{
  public interface IParser
  {
    Task ParseAsync(ResponseTextParser rtp);
  }
}
