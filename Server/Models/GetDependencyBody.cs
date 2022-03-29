using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildOverrideService.Models
{
    public class GetDependencyBody
    {
        [JsonProperty("info")]
        public string? Info { get; set; }
    }
}
