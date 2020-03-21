using System;
using Newtonsoft.Json;

namespace MonkeyFinder
{
    public class EmptyDocument<T>
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        public T Document { get; set; }
        public string PartitionKey { get; set; }
    }
}
