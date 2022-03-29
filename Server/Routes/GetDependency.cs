using BuildOverrideService.Http;
using BuildOverrideService.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BuildOverrideService.Routes
{
    public class GetDependency : RestModuleBase
    {
        [Route("/overrides/{id}/dependency", "POST")]
        public async Task<RestResult> ExecuteAsync(string id)
        {
            if (!Guid.TryParse(id, out var guid))
                return RestResult.BadRequest;

            if (!OverrideService.TryGetOverride(guid, out var metadata) || metadata == null)
                return RestResult.BadRequest;

            var body = GetBody<GetDependencyBody>();

            if (body == null)
                return RestResult.BadRequest;

            // parse it 

            var reg = Regex.Match(body.Info!, @"(.*?), Version=(.*?),");

            if (!reg.Success)
                return RestResult.NotFound;

            var depName = reg.Groups[1].Value;
            var version = reg.Groups[2].Value;

            if (metadata.DependencyMap.TryGetValue(depName, out var mappedValue))
                depName = mappedValue;

            if (version.Count(x => x == '.') == 3 && version.Split('.')[3] == "0")
                version = Regex.Replace(version, @"\.0$", x => "");

            if (!metadata.Dependencies.Contains(depName))
                return RestResult.Forbidden;

            var dep = await DependencyService.GetDependencyAsync(depName!, version);

            if (dep == null)
                return RestResult.NotFound;

            Response.StatusCode = 200;
            Response.AddHeader("Content-Type", "application/octet-stream");
            await dep.CopyToAsync(Response.OutputStream);

            Response.Close();

            return RestResult.NoAction;
        }
    }
}
