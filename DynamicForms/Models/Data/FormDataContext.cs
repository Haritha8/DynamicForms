using Newtonsoft.Json.Linq;
namespace DynamicForms.Models.Data
{
    public class FormDataContext
    {
        public JObject Root { get; }
        private readonly string _basePath;
        public FormDataContext(JObject root, string basePath = null)
        {
            Root = root;
            _basePath = basePath;
        }
        // path like "data.currentEntry.toolingSetupPer"
            private string ResolvePath(string path)

            {

                if (string.IsNullOrEmpty(_basePath))

                    return path;

                if (string.IsNullOrEmpty(path))

                    return _basePath;

                return _basePath + "." + path;

            }

            // path like "data.currentEntry.toolingSetupPer" OR relative like "toolingSetupPer"

            public object GetValue(string path)

            {

                var token = GetToken(path);

                return token?.ToObject<object>();

            }

            public void SetValue(string path, object value)

            {

                var token = GetToken(path);

                if (token == null) return;

                token.Replace(value == null ? JValue.CreateNull() : JToken.FromObject(value));

            }

            // NEW: raw token access (used by repeater & save logic)

            public JToken GetToken(string path)

            {

                var effectivePath = ResolvePath(path);

                return string.IsNullOrEmpty(effectivePath)

                    ? null

                    : Root.SelectToken(effectivePath);

            }

            // NEW: create a child context bound to a base path, e.g. "data.logs[0]"

            public FormDataContext CreateChildContext(string basePath)
            {

                return new FormDataContext(Root, basePath);

            }

        }

    }
 