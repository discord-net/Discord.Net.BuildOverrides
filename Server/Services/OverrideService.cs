using BuildOverrideService.Models;
using Discord;
using Discord.Webhook;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildOverrideService.Services
{
    public class OverrideMetadata
    {
        [JsonProperty("id")]
        public Guid Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string? Description { get; set; }

        [JsonProperty("created_at")]
        public DateTimeOffset CreatedAt { get; set; }

        [JsonProperty("last_updated")]
        public DateTimeOffset LastUpdated { get; set; }

        [JsonProperty("dependency_map")]
        public Dictionary<string, string> DependencyMap { get; set; } = new();

        [JsonProperty("public")]
        public bool Public { get; set; }

        [JsonProperty("dependencies")]
        public List<string> Dependencies { get; set; } = new();

        public OverrideMetadata(string name)
        {
            Name = name;
            Id = Guid.NewGuid();
        }

        public async Task<Stream?> GetAsync()
        {
            var file = Path.Combine(OverrideService.OverridePath, $"{Name}.bin");

            if (!File.Exists(file))
                return null;

            var ms = new MemoryStream();

            using (var fs = File.OpenRead(file))
            {
                await fs.CopyToAsync(ms);
            }

            ms.Position = 0;

            return ms;
        }
    }

    public class OverrideService
    {
        public static string OverridePath = Path.Combine(Environment.CurrentDirectory, "overrides");
        public static string OverrideMetadataPath = Path.Combine(OverridePath, "overrides.json");
        public ConcurrentDictionary<Guid, OverrideMetadata> Overrides { get; private set; } = new();

        private object _lock = new();
        private readonly IServiceProvider _provider;
        private JsonSerializer _serializer
            => _provider.GetRequiredService<JsonSerializer>();

        private DiscordWebhookClient _webhook
            => _provider.GetRequiredService<DiscordWebhookClient>();

        public OverrideService(IServiceProvider provider)
        {
            _provider = provider;

            LoadOverrides();
        }

        private void LoadOverrides()
        {
            if (!File.Exists(OverrideMetadataPath))
            {
                Overrides = new();
                SaveOverrides();
                return;
            }

            lock (_lock)
            {
                using(var fs = File.OpenRead(OverrideMetadataPath))
                using(var textReader = new StreamReader(fs))
                using(var reader = new JsonTextReader(textReader))
                {
                    Overrides = _serializer.Deserialize<ConcurrentDictionary<Guid, OverrideMetadata>>(reader)!;
                }
            }
        }

        private void SaveOverrides()
        {
            lock (_lock)
            {
                using(var fs = File.OpenWrite(OverrideMetadataPath))
                using (var writer = new StreamWriter(fs))
                {
                    _serializer.Serialize(writer, Overrides);
                }
            }
        }

        private void AddOverride(OverrideMetadata metadata)
        {
            Overrides.TryAdd(metadata.Id, metadata);
            SaveOverrides();
        }

        public bool TryGetOverride(Guid id, out OverrideMetadata? metadata)
            => Overrides.TryGetValue(id, out metadata);

        public bool TryGetOverride(string name, out OverrideMetadata? metadata)
        {
            metadata = Overrides.FirstOrDefault(x => x.Value.Name == name).Value;
            return metadata != null;
        }

        public async Task<OverrideMetadata> CreateOverrideAsync(CreateOverride metadata, Stream stream, string creator)
        {
            var meta = new OverrideMetadata(metadata.Name!)
            {
                Description = metadata.Description,
                CreatedAt = DateTimeOffset.UtcNow,
                LastUpdated = DateTimeOffset.UtcNow,
                Public = metadata.Public,
                Dependencies = metadata.Dependencies,
                DependencyMap = metadata.DependencyMap
            };

            AddOverride(meta);

            using (var fs = File.OpenWrite(Path.Combine(OverridePath, $"{metadata.Name}.bin")))
            {
                stream.Position = 0;
                await stream.CopyToAsync(fs);
                await fs.FlushAsync();
            }

            if (metadata.Public)
            {
                var embed = new EmbedBuilder()
                    .WithTitle("New build override created!")
                    .WithColor(Color.Green)
                    .WithCurrentTimestamp()
                    .WithDescription($"The build override `{metadata.Name}` was created")
                    .AddField("Author", creator);

                if (metadata.Description != null)
                    embed.AddField("Description", metadata.Description);

                if (metadata.Dependencies.Any())
                    embed.AddField("Dependencies", $"```\n{string.Join(",\n", metadata.Dependencies)}```");

                if (metadata.DependencyMap.Any())
                    embed.AddField("Dependency Map", $"```\n{string.Join(",\n", metadata.DependencyMap.Select(x => $"{x.Key} -> {x.Value}"))}```");

                await _webhook.SendMessageAsync(embeds: new[]
                {
                    embed.Build()
                });
            }

            return meta;
        }

        public async Task<OverrideMetadata?> ModifyOverrideAsync(string name, Stream data, string updater)
        {
            if (!TryGetOverride(name, out var meta) || meta == null)
                return null;

            Overrides[meta.Id].LastUpdated = DateTimeOffset.UtcNow;

            var file = Path.Combine(OverridePath, $"{meta.Name}.bin");

            File.Delete(file);

            using(var fs = File.OpenWrite(file))
            {
                //data.Position = 0;
                await data.CopyToAsync(fs);
                await fs.FlushAsync();
            }

            SaveOverrides();

            if (meta.Public)
            {
                var embed = new EmbedBuilder()
                    .WithTitle("Build override updated")
                    .WithCurrentTimestamp()
                    .WithColor(Color.Orange)
                    .WithDescription($"The build override `{name}` was updated")
                    .AddField("Updator", updater);

                await _webhook.SendMessageAsync(embeds: new[] { embed.Build() });
            }

            return Overrides[meta.Id];
        }
    }
}
