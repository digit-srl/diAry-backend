using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;

namespace DiaryCollector {

    public class WomService {
        
        private readonly IConfiguration Configuration;
        private readonly ILogger<WomService> Logger;

        public WomService(
            IConfiguration configuration,
            ILogger<WomService> logger,
            ILoggerFactory loggerFactory
        ) {
            Configuration = configuration;
            Logger = logger;

            var womSection = Configuration.GetSection("WomSetup");
            var womDomain = womSection["WomDomain"];
            var registryKeyPath = womSection["RegistryKeyPath"];
            var sourceId = womSection["SourceId"];
            var sourceKeyPath = womSection["SourceKeyPath"];

            Domain = womDomain;

            using var registryKeyStream = new FileStream(registryKeyPath, FileMode.Open);
            var womClient = new WomPlatform.Connector.Client(womDomain,
                loggerFactory, registryKeyStream);
            
            using var sourceKeyStream = new FileStream(sourceKeyPath, FileMode.Open);
            Instrument = womClient.CreateInstrument(sourceId, sourceKeyStream);

            Logger.LogInformation("Initialized WOM instrument ID {0} on domain {1}", sourceId, womDomain);
        }

        public WomPlatform.Connector.Instrument Instrument { get; }

        public string Domain { get; }

    }

}
