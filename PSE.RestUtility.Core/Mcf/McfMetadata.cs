﻿using Newtonsoft.Json;

namespace PSE.RestUtility.Core.Mcf
{
    public class McfMetadata
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("uri")]
        public string Uri { get; set; }
        [JsonProperty("type")]
        public string Type { get; set; }
    }
}