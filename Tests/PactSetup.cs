using Newtonsoft.Json;
using PactNet;
using PactNet.Mocks.MockHttpService;
using PactNet.Mocks.MockHttpService.Models;
using Tests.ApiTestClient;

namespace Tests
{
    public class PactSetup
    {
        public const string PactDirectory = @"C:\PactFiles";
        public static ProviderServiceRequest Request;
        public static Client Client;
        public static IPactBuilder PactBuilder { get; private set; }
        public static IMockProviderService MockMessageService { get; private set; }
        public static int MockServerPort { get; set; }
        private static string Consumer { get; set; }
        private static string Provider { get; set; }
        public static string PactFilename { get; set; }
        private static string MockMessageServiceBaseUri => $"http://localhost:{MockServerPort}";
        public static string PactVersion { get; set; }

        public PactSetup CreatePactBuilder()
        {
            PactBuilder = new PactBuilder(new PactConfig
            {
                SpecificationVersion = "2.0.0",
                PactDir = PactDirectory,
                LogDir = PactDirectory
            });

            return this;
        }

        /// <summary>
        /// The actual version of the pact as specified by
        /// the expected interactions defined
        /// within the test methods of the derived class
        /// </summary>
        public PactSetup SetPactVersion(string version)
        {
            PactVersion = version;

            return this;
        }

        public PactSetup SetConsumerProviderNames(string cosumer, string provider)
        {
            Provider = provider.Replace(" ", "_").ToLower();
            Consumer = cosumer.Replace(" ", "_").ToLower();

            PactFilename = $"{Consumer}-{Provider}";

            PactBuilder
                .ServiceConsumer(cosumer)
                .HasPactWith(provider);

            return this;
        }

        public PactSetup SetMockServerPort(int port)
        {
            MockServerPort = port;

            return this;
        }

        public PactSetup CreateMockService()
        {
            MockMessageService = PactBuilder.MockService(MockServerPort);

            MockMessageService = PactBuilder.MockService(MockServerPort, new JsonSerializerSettings());

            return this;
        }

        public PactSetup CreateRequest()
        {
            Request = new ProviderServiceRequest();

            return this;
        }

        public PactSetup CreateTestClient()
        {
            Client = new Client(MockMessageServiceBaseUri);

            return this;
        }
    }
}