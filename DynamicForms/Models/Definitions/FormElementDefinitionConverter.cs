using System;
using System.Collections.Generic;
using DynamicForms.Models.Definitions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace DynamicForms.Models.Definitions
{
    // Custom converter to create the right subclass based on "elementType"
    public class FormElementDefinitionConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(FormElementDefinition).IsAssignableFrom(objectType);
        }
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
                return null;
            var jo = JObject.Load(reader);
            var elementType = (string)jo["elementType"];
            FormElementDefinition target;
            switch (elementType)
            {
                case "Form":
                    target = new FormDefinition();
                    break;
                case "Section":
                    target = new SectionDefinition();
                    break;
                case "Field":
                    target = new FieldDefinition();
                    break;
                case "Action":
                    target = new ActionDefinition();
                    break;
                case "Repeater":
                    target = new RepeaterDefinition();
                    break;
                default:
                    throw new InvalidOperationException($"Unknown elementType: {elementType}");
            }
            serializer.Populate(jo.CreateReader(), target);
            return target;
        }
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            // Not needed for this prototype
            throw new NotImplementedException();
        }
    }
    // Root wrapper to plug in the converter easily, if you like that style:
    public static class FormDefinitionFactory
    {
        public static FormDefinition FromJson(string json)
        {
            var settings = new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> { new FormElementDefinitionConverter() },
                NullValueHandling = NullValueHandling.Ignore
            };
            return JsonConvert.DeserializeObject<FormDefinition>(json, settings);
        }
    }
}