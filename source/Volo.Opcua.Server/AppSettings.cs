using Newtonsoft.Json;

namespace Volo.Opcua.Server
{
    public class AppSettings
    {
        [JsonProperty("applicationUri")]
        public string ApplicationUri { get; set; }

        [JsonProperty("productUri")]
        public string ProductUri { get; set; }

        [JsonProperty("applicationName")]
        public string ApplicationName { get; set; }

        [JsonProperty("port")]
        public int Port { get; set; }

        [JsonProperty("certificate")]
        public string Certificate { get; set; }

        [JsonProperty("privateKey")]
        public string PrivateKey { get; set; }

        [JsonProperty("commonName")]
        public string CommonName { get; set; }

        [JsonProperty("organizationalUnit")]
        public string OrganizationalUnit { get; set; }
    }
}