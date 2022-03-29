using BuildOverrideService.Http;
using BuildOverrideService.Models;
using HttpMultipartParser;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildOverrideService.Routes
{
    public class AddOverride : RestModuleBase
    {
        [Route("/override", "POST")]
        public async Task<RestResult> ExecuteAsync()
        {
            var authHeader = Request.Headers.Get("Authorization");

            if (authHeader == null)
                return RestResult.Unauthorized;

            var auth = await DatabaseSerivce.GetAuthorizationAsync(authHeader);

            if (auth == null)
                return RestResult.Forbidden;

            var body = await MultipartFormDataParser.ParseAsync(Request.InputStream).ConfigureAwait(false);

            var fileContent = body.Files.FirstOrDefault(x => x.Name != null && x.Name == "binary");

            var rawMetadata = body.GetParameterValue("metadata");

            CreateOverride metadata;

            try
            {
                metadata = JsonConvert.DeserializeObject<CreateOverride>(rawMetadata)!;
            }
            catch(Exception x)
            {
                Logger.GetLogger<AddOverride>().Error("Failed to read body", Severity.Rest, x);
                return RestResult.BadRequest;
            }

            if (fileContent == null)
                return RestResult.BadRequest;

            var result = await OverrideService.CreateOverrideAsync(metadata, fileContent.Data, auth.Name ?? "Unknown");

            return RestResult.OK.WithData(result);
        }
    }
}
