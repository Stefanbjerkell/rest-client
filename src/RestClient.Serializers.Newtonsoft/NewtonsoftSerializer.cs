﻿using Newtonsoft.Json;

namespace RestClient.Serializers.Newtonsoft;

public class NewtonsoftSerializer : IRestClientSerializer
{
    public JsonSerializerSettings Settings { get; set; }

    public NewtonsoftSerializer(JsonSerializerSettings? settings = null)
    {
        Settings = settings ?? new JsonSerializerSettings();
    }

    public T? Deserialize<T>(string? json)
    {
        if (json is null) return default;
        return JsonConvert.DeserializeObject<T>(json);
    }

    public string? Serialize(object? item)
    {
        return JsonConvert.SerializeObject(item);
    }
}
