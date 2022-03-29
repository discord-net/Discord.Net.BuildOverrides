using BuildOverrideService.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildOverrideService.Routes
{
    public class DownloadBuildOverride : RestModuleBase
    {
        [Route("/overrides/download/{id}", "GET")]
        public async Task<RestResult> ExecuteAsync(string id)
        {
            if (!Guid.TryParse(id, out var guid))
                return RestResult.BadRequest;

            if (!OverrideService.TryGetOverride(guid, out var metadata) || metadata == null)
                return RestResult.NotFound;

            var data = await metadata.GetAsync();

            if (data == null)
                return RestResult.NotFound;

            Response.StatusCode = 200;
            Response.AddHeader("Content-Type", "application/octet-stream");
            await data.CopyToAsync(Response.OutputStream);

            Response.Close();

            return RestResult.NoAction;
        }
    }
}
