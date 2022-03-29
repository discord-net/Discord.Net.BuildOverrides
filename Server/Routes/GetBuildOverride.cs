using BuildOverrideService.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildOverrideService.Routes
{
    public class GetBuildOverride : RestModuleBase
    {
        [Route("/overrides/{name}", "GET")]
        public Task<RestResult> ExecuteAsync(string name)
        {
            if (!OverrideService.TryGetOverride(name, out var metadata) || metadata == null)
                return Task.FromResult(RestResult.NotFound);

            return Task.FromResult(RestResult.OK.WithData(metadata));
        }
    }
}
