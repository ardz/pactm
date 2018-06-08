using System.Collections.Generic;
using System.Net;
using Newtonsoft.Json;
using PactNet;
using PactNet.Mocks.MockHttpService;
using PactNet.Mocks.MockHttpService.Models;
using Tests.ApiTestClient;

namespace Tests
{
    public class PactMessageSetup
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

        public PactMessageSetup CreatePactBuilder()
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
        public PactMessageSetup SetPactVersion(string version)
        {
            PactVersion = version;

            return this;
        }

        public PactMessageSetup SetConsumerProviderNames(string cosumer, string provider)
        {
            Provider = provider.Replace(" ", "_").ToLower();
            Consumer = cosumer.Replace(" ", "_").ToLower();

            PactFilename = $"{Consumer}-{Provider}";

            PactBuilder
                .ServiceConsumer(cosumer)
                .HasPactWith(provider);

            return this;
        }

        public PactMessageSetup SetMockServerPort(int port)
        {
            MockServerPort = port;

            return this;
        }

        public PactMessageSetup CreateMockService()
        {
            MockMessageService = PactBuilder.MockService(MockServerPort, new JsonSerializerSettings());

            return this;
        }

        public PactMessageSetup CreateProviderServiceRequest()
        {
            Request = new ProviderServiceRequest();

            return this;
        }

        public PactMessageSetup CreateTestClient()
        {
            Client = new Client(MockMessageServiceBaseUri);

            return this;
        }

        public PactMessageSetup MockPreconditions(string given, string uponReceiving)
        {
            MockMessageService.Given(given)
                .UponReceiving(uponReceiving)
                .With(new ProviderServiceRequest
                {
                    Method = HttpVerb.Get,
                    Path = "/servicebusmock/message",
                    Headers = new Dictionary<string, object>
                    {
                        {"Accept", "application/json"}
                    },
                    Query = ""
                });

            return this;
        }

        public PactMessageSetup MockExpectedResponse(dynamic messageContract)
        {
            MockMessageService
                .WillRespondWith(new ProviderServiceResponse
                {
                    Status = 200,
                    Headers = new Dictionary<string, object>()
                    {
                        {"Content-Type", "application/json; charset=utf-8"}
                    },
                    Body = messageContract
                });

            return this;
        }

        public PactMessageSetup MockCheckResponse(string path)
        {
            Client.Get(path, HttpStatusCode.OK);

            return this;
        }

        public PactMessageSetup MockVerifyAndClearInteractions()
        {
            MockMessageService.VerifyInteractions();
            MockMessageService.ClearInteractions();

            return this;
        }
    }
}