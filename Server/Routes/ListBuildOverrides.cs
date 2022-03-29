using BuildOverrideService.Http;
using BuildOverrideService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server.Routes
{
    public class ListBuildOverrides : RestModuleBase
    {
        [Route("/overrides", "GET")]
        public async Task<RestResult> ExecuteAsync()
        {
            var authHeader = Request.Headers.Get("Authorization");
            var auth = authHeader != null ? await DatabaseSerivce.GetAuthorizationAsync(authHeader) : null;

            // show non-public if the user is authed.
            var overrides = OverrideService.Overrides.Where(x => auth == null ? x.Value.Public : true).ToArray();

            return RestResult.OK.WithData(overrides.Select(x => x.Value));
        }
    }
}
