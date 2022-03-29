using EdgeDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildOverrideService.DatabaseModels
{
    [EdgeDBType]
    public class Authorization
    {
        [EdgeDBProperty("key")]
        public string? Key { get; set; }

        [EdgeDBProperty("name")]
        public string? Name { get; set; }
    }
}
