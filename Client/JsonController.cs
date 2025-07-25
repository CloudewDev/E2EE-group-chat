using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JsonController_ns
{
    public class JsonData
    {
        [JsonPropertyName("type")]
        public int Type { get; set; }

        [JsonPropertyName("from")]
        public string From { get; set; }

        [JsonPropertyName("to")]
        public string To { get; set; }

        [JsonPropertyName("body")]
        public string Body { get; set; }

        [JsonPropertyName("iv")]
        public string? IVec { get; set; }
    }
    public class JsonController
    {	
		public enum MSG_TYPE {message, dh, sender_key, enter, leave}
		public string BuildJson(MSG_TYPE type, string from, string to, string body, string? iv) 
		{
            JsonData json_to_send = new JsonData
            {
                Type = (int)type,
                From = from,
                To = to,
                Body = body,
                IVec = iv
            };
			string data_to_send = JsonSerializer.Serialize(json_to_send);
			return data_to_send;

        }

        public int ParseTypeFromJson(string input)
        {
            JsonData parsed = JsonSerializer.Deserialize<JsonData>(input);
            int type = parsed.Type;
            return type;
        }

        public string ParseFromFromJson(string input)
        {
            JsonData parsed = JsonSerializer.Deserialize<JsonData>(input);
            string from = parsed.From;
            return from;
        }

        public string ParseToFromJson(string input)
        {
            JsonData parsed = JsonSerializer.Deserialize<JsonData>(input);
            string to = parsed.To;
            return to;
        }
        public string ParseBodyFromJson(string input)
		{
            JsonData parsed = JsonSerializer.Deserialize<JsonData>(input);
            string body = parsed.Body;
            return body;
        }

        public string ParseIVFromJson(string input)
        {
            JsonData parsed = JsonSerializer.Deserialize<JsonData>(input);
            string iv = parsed.IVec;
            return iv;
        }

    }

}
