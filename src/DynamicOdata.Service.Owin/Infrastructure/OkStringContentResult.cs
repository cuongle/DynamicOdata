using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Results;

namespace DynamicOdata.Service.Owin.Infrastructure
{
  internal class OkStringContentResult : OkResult
  {
    private readonly string _content;

    public OkStringContentResult(HttpRequestMessage request,
      string content) : base(request)
    {
      _content = content;
    }

    public OkStringContentResult(ApiController controller, string content) : base(controller)
    {
      _content = content;
    }

    public override async Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken)
    {
      var result = await base.ExecuteAsync(cancellationToken);
      result.Content = new StringContent(_content, Encoding.UTF8);

      return await Task.FromResult(result);
    }
  }
}