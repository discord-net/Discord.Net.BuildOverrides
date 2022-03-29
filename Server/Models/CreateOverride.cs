using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildOverrideService.Models
{
    public class CreateOverride
    {
        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("description")]
        public string? Description { get; set; }

        [JsonProperty("public")]
        public bool Public { get; set; } = true;

        [JsonProperty("dependencies")]
        public List<string> Dependencies { get; set; } = new();

        [JsonProperty("dependency_map")]
        public Dictionary<string, string> DependencyMap { get; set; } = new();
    }
}
