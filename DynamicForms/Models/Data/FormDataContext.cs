using Newtonsoft.Json.Linq;
namespace DynamicForms.Models.Data
{
    public class FormDataContext
    {
        public JObject Root { get; }
        public FormDataContext(JObject root)
        {
            Root = root;
        }
        // path like "data.currentEntry.toolingSetupPer"
        public JToken GetToken(string path)
        {
            return Root.SelectToken(path);
        }
        public object GetValue(string path)
        {
            var token = Root.SelectToken(path);
            return token?.ToObject<object>();
        }
        public void SetValue(string path, object value)
        {
            var token = Root.SelectToken(path);
            if (token == null) return;
            token.Replace(value == null ? JValue.CreateNull() : JToken.FromObject(value));
        }
    }
}