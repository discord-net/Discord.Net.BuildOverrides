using BuildOverrideService.DatabaseModels;
using EdgeDB;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildOverrideService.Services
{
    public class DatabaseService
    {
        private readonly IServiceProvider _provider;
        private EdgeDBClient _client
            => _provider.GetRequiredService<EdgeDBClient>();

        public DatabaseService(IServiceProvider provider)
        {
            _provider = provider;
        }

        public async Task<Authorization?> GetAuthorizationAsync(string key)
        {
            var query = QueryBuilder.Select<Authorization>().Filter(x => x.Key == key).Limit(1).Build();

            var result = await _client.QuerySingleAsync<Authorization>(query.QueryText, query.Parameters.ToDictionary(x => x.Key, x => x.Value));

            return result;
        }
    }
}
