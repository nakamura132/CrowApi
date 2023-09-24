using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace CrowApi.Controllers
{
    /// <summary>
    /// 非公開の開発・検証用コントローラー
    /// </summary>
    [Route( "-/[controller]" )]
    [ApiController]
    public class InfoController : ControllerBase
    {
        private readonly IEnumerable<EndpointDataSource> _endpointDataSources;

        public InfoController( IEnumerable<EndpointDataSource> endpointDataSources )
        {
            _endpointDataSources = endpointDataSources;
        }

        /// <summary>
        /// すべてのエンドポイントをリスト化し返却します
        /// </summary>
        /// <returns>エンドポイントのリスト</returns>
        [HttpGet("endpoints")]
        public IActionResult ListAllEndpoints()
        {
            var endpoints = _endpointDataSources
                .SelectMany(es => es.Endpoints)
                .OfType<RouteEndpoint>();

            var output = endpoints.Select(
                e =>
                {
                    var controller = e.Metadata
                        .OfType<ControllerActionDescriptor>()
                        .FirstOrDefault();
                    var action = controller != null
                        ? $"{controller.ControllerName}.{controller.ActionName}"
                        : null;
                    var controllerMethod = controller != null
                        ? $"{controller.ControllerTypeInfo.FullName}:{controller.MethodInfo.Name}"
                        : null;
                    var endpointInfo = new
                    {
                        Method = e.Metadata.OfType<HttpMethodMetadata>().FirstOrDefault()?.HttpMethods?[0],
                        Route = $"/{e.RoutePattern?.RawText?.TrimStart('/')}",
                        Action = action,
                        ControllerMethod = controllerMethod
                    };
                    return endpointInfo;
                });
            //return new JsonResult( output );
            return Ok( output );
        }
    }
}
