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

        public PactMessageSetup MockCheckResponse()
        {
            Client.Get("/servicebusmock/message", HttpStatusCode.OK);

            return this;
        }

        public IMockProviderService Foo()
        {
            // in a real project the tests which set the expectation of the consumer would
            // probably live in a project away from the actual service itself
            // Here we set the expectation of the message format (contract) the PLR service
            // expects to receive from the achievement service

            MockMessageService.Given("There is a new a message in the achievements queue")
                .UponReceiving("A request to process the message")
                .With(new ProviderServiceRequest
                {
                    // Define what the message should contain
                    Method = HttpVerb.Get, // hide in base class
                    Path = "/servicebusmock/message",
                    Headers = new Dictionary<string, object>
                    {
                        {"Accept", "application/json"}
                    },
                    Query = ""
                })
                .WillRespondWith(new ProviderServiceResponse
                {
                    Status = 200,
                    Headers = new Dictionary<string, object>()
                    {
                        {"Content-Type", "application/json; charset=utf-8"}
                    },
                    Body =
                        new
                        {
                            CertificateId = "ae553ab2-51f1-40f0-81eb-95e46205d6e5",
                            LearnerId = "3bb86580-aef6-4526-81e7-35bb22a4390e",
                            CorrelationId = "2b07dd51-1a63-4834-9311-36667ec89f51",
                            Type = "Achievement",
                        }
                });

            Client.Get("/servicebusmock/message", HttpStatusCode.OK);

            return MockMessageService;
        }

        public PactMessageSetup MockVerifyAndClearInteractions()
        {
            MockMessageService.VerifyInteractions();
            MockMessageService.ClearInteractions();

            return this;
        }
    }
}