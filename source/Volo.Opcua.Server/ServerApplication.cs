using LibUA.Core;
using LibUA.Server;
using System.Collections.Generic;

namespace Volo.Opcua.Server
{
    public class ServerApplication : Application
    {
        private readonly ApplicationDescription _appDescription;

        public ServerApplication(AppSettings settings)
        {
            _appDescription = new ApplicationDescription(settings.ApplicationUri,
                                                           settings.ProductUri,
                                                           new LocalizedText("en-US", settings.ApplicationName),
                                                           ApplicationType.Server,
                                                           null,
                                                           null,
                                                           null);
        }
    }
}
