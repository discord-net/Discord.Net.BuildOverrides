using BuildOverrideService.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildOverrideService.Routes
{
    public class ModifyOverride : RestModuleBase
    {
        [Route("/overrides/{name}", "PATCH")]
        public async Task<RestResult> ExecuteAsync(string name)
        {
            var authHeader = Request.Headers.Get("Authorization");

            if (authHeader == null)
                return RestResult.Unauthorized;

            var auth = await DatabaseSerivce.GetAuthorizationAsync(authHeader);

            if (auth == null)
                return RestResult.Forbidden;

            if (!Request.HasEntityBody)
                return RestResult.BadRequest;

            var result = await OverrideService.ModifyOverrideAsync(name, Request.InputStream, auth.Name ?? "Unknown");
            return RestResult.OK.WithData(result);
        }
    }
}
