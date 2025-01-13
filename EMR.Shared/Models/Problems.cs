using Newtonsoft.Json;

namespace EMR.Shared.Models;

public class Problems
{
    [JsonProperty("title")] public string Title { get; set; }

    [JsonProperty("message")] public string Message { get; set; }

    [JsonProperty("succeeded")] public bool Succeeded { get; set; }
}