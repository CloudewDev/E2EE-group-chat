using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JsonController_ns
{
    public class JsonData
    {
        [JsonPropertyName("type")]
        public int Type { get; set; }

        [JsonPropertyName("who")]
        public string Who { get; set; }

        [JsonPropertyName("body")]
        public string Body { get; set; }
    }
    public class JsonController
    {	
		public enum MSG_TYPE {Message, DH, Key, Announce}
		public string BuildJson(MSG_TYPE type, string who, string body) 
		{
            JsonData json_to_send = new JsonData
            {
                Type = (int)type,
                Who = who,
                Body = body
            };
			string data_to_send = JsonSerializer.Serialize(json_to_send);
			return data_to_send;

        }

		public string ParseBodyFromJson(string input)
		{
            JsonData parsed = JsonSerializer.Deserialize<JsonData>(input);
            string body = parsed.Body;
            return body;
        }
	}

}
