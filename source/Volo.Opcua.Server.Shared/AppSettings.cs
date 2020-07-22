using Newtonsoft.Json;

namespace Volo.Opcua.Server.Shared
{
    public class AppSettings
    {
        [JsonProperty("applicationName")]
        public string ApplicationName { get; set; }

        [JsonProperty("applicationUri")]
        public string ApplicationUri { get; set; }

        [JsonProperty("backlog")]
        public int Backlog { get; set; }

        [JsonProperty("certificate")]
        public string Certificate { get; set; }

        [JsonProperty("commonName")]
        public string CommonName { get; set; }

        [JsonProperty("locale")]
        public string Locale { get; set; }

        [JsonProperty("maxClients")]
        public int MaxClients { get; set; }

        [JsonProperty("monitoringInterval")]
        public int MonitoringInterval { get; set; }

        [JsonProperty("organizationalUnit")]
        public string OrganizationalUnit { get; set; }

        [JsonProperty("opcuaPort")]
        public int OpcuaPort { get; set; }

        [JsonProperty("privateKey")]
        public string PrivateKey { get; set; }

        [JsonProperty("productUri")]
        public string ProductUri { get; set; }

        [JsonProperty("rootItemName")]
        public string RootItemName { get; set; }

        [JsonProperty("timeout")]
        public int Timeout { get; set; }

        [JsonProperty("grpcPort")]
        public int GrpcPort { get; set; }

        [JsonProperty("grpcHost")]
        public string GrpcHost { get; set; }
    }
}