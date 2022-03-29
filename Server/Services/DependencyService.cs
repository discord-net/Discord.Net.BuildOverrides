using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BuildOverrideService.Services
{
    public class DependencyService
    {
        public static string DependencyDir = Path.Combine(Environment.CurrentDirectory, "dependencies");

        private readonly IServiceProvider _provider;
        public DependencyService(IServiceProvider provider)
        {
            _provider = provider;
        }

        public async Task<Stream?> GetDependencyAsync(string name, string version)
        {
            var path = Path.Combine(DependencyDir, $"{name}-{version}.bin");

            if(File.Exists(path))
            {
                var ms = new MemoryStream();

                using(var fs = File.OpenRead(path))
                {
                    await fs.CopyToAsync(ms);
                }

                ms.Position = 0;

                return ms;
            }

            // download it

            using(var client = new HttpClient())
            {
                var result = await client.GetAsync($"https://api.nuget.org/v3-flatcontainer/{name}/{version}/{name.ToLowerInvariant()}.{version}.nupkg");

                if (!result.IsSuccessStatusCode)
                    return null;

                var content = await result.Content.ReadAsStreamAsync();

                using (var zip = new ZipArchive(content, ZipArchiveMode.Read))
                {
                    Stream? output = null;

                    var libs = zip.Entries.Select(x => (Regex.Match(x.FullName, @"lib\/(.*?)\/"), x)).Where(x => x.Item1.Success).OrderByDescending(x => x.Item1.Groups[1].Value);

                    var std = libs.Where(x => x.Item1.Groups[1].Value.StartsWith("netstandard"));
                    var core = libs.Where(x => x.Item1.Groups[1].Value.StartsWith("netcoreapp"));

                    var mostRecent = libs.FirstOrDefault(y =>
                    {
                        var isstd = std.Any(x => x.Item1.Groups[1].Value == y.Item1.Groups[1].Value);
                        var iscore = core.Any(x => x.Item1.Groups[1].Value == y.Item1.Groups[1].Value);

                        return !isstd && !iscore;
                    });

                    if (mostRecent.Item1 != null)
                    {
                        output = mostRecent.x.Open();
                    }
                    else
                        output = core.FirstOrDefault().x?.Open() ?? std.FirstOrDefault().x?.Open();

                    if (output == null)
                        return null;

                    var ms = new MemoryStream();

                    await output.CopyToAsync(ms);

                    ms.Position = 0;

                    using (var fs = File.OpenWrite(path))
                    {
                        await ms.CopyToAsync(fs);
                        await fs.FlushAsync();
                    }

                    ms.Position = 0;
                    return ms;
                }
            }
        }
    }
}
