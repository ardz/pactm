using System.Collections.Generic;
using System.Net;
using Newtonsoft.Json;
using PactNet;
using PactNet.Mocks.MockHttpService;
using PactNet.Mocks.MockHttpService.Models;
using Tests.ApiTestClient;
using Xunit.Abstractions;

namespace Tests.Consumer
{
    /// <summary>
    /// Wrapper code for consumer Pact tests
    /// </summary>
    public class PactConsumerTestSetup
    {
        private readonly ITestOutputHelper _output;
        public string Consumer { get; private set; }
        public string Provider { get; private set; }
        public static string PactFilename { get; private set; }
        public const string PactBrokerBaseUri = "https://pecktest.pact.dius.com.au/pacts";
        public string PactUri { get; private set; }
        private static string MockMessageServiceBaseUri => $"http://localhost:{MockServerPort}";
        public string PactsDirectory { get; private set; }
        public static ProviderServiceRequest Request;
        public static Client Client;
        public static IPactBuilder PactBuilder { get; private set; }
        public static IMockProviderService MockMessageService { get; private set; }
        public static int MockServerPort { get; set; }
        public static string ConsumerServiceVersion { get; set; }

        public PactConsumerTestSetup(ITestOutputHelper output)
        {
            _output = output;
        }

        public PactConsumerTestSetup CreatePactBuilder(string pactsDirectory)
        {
            PactsDirectory = pactsDirectory;

            PactBuilder = new PactBuilder(new PactConfig
            {
                SpecificationVersion = "2.0.0",
                PactDir = pactsDirectory,
                LogDir = PactsDirectory
            });

            return this;
        }

        /// <summary>
        /// The actual version of the pact as specified by
        /// the expected interactions defined
        /// within the test methods of the derived class
        /// </summary>
        public PactConsumerTestSetup SetConsumerVersion(string version)
        {
            ConsumerServiceVersion = version;

            return this;
        }

        public PactConsumerTestSetup SetConsumerProviderNames(string consumer, string provider)
        {
            Provider = provider.Replace(" ", "_").ToLower();
            Consumer = consumer.Replace(" ", "_").ToLower();

            PactFilename = $"{Consumer}-{Provider}";

            PactBuilder
                .ServiceConsumer(consumer)
                .HasPactWith(provider);

            _output.WriteLine("Starting PACT File creation for " + Provider + " and " + Consumer);

            var consumerUri = "/consumer/" + consumer.Replace(" ", "%20");
            var providerUri = "/provider/" + provider.Replace(" ", "%20");

            PactUri = providerUri + consumerUri + "/latest";

            return this;
        }

        public PactConsumerTestSetup SetMockServerPort(int port)
        {
            MockServerPort = port;

            return this;
        }

        public PactConsumerTestSetup CreateMockService()
        {
            MockMessageService = PactBuilder.MockService(MockServerPort, new JsonSerializerSettings());

            _output.WriteLine("Created Mock Service.");

            return this;
        }

        public PactConsumerTestSetup CreateProviderServiceRequest()
        {
            Request = new ProviderServiceRequest();

            return this;
        }

        public PactConsumerTestSetup CreateTestClient()
        {
            Client = new Client(MockMessageServiceBaseUri);

            _output.WriteLine("Created Test Client at " + MockMessageServiceBaseUri);

            return this;
        }

        public PactConsumerTestSetup MockPreconditions(string given, string uponReceiving)
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

        public PactConsumerTestSetup MockExpectedResponse(dynamic messageContract)
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

        public PactConsumerTestSetup MockCheckResponse(string path)
        {
            Client.Get(path, HttpStatusCode.OK);

            return this;
        }

        public PactConsumerTestSetup MockVerifyAndClearInteractions()
        {
            MockMessageService.VerifyInteractions();
            _output.WriteLine("Interactions Verified.");
            MockMessageService.ClearInteractions();
            _output.WriteLine("Interactions Cleared.");

            return this;
        }

        public PactConsumerTestSetup WriteAndPublishPact()
        {
            PactBuilder.Build();
            _output.WriteLine("Finished writing PACT file to disk (" + PactsDirectory + ")");

            // send it up to the broker
            var pactPublisher = new PactPublisher(PactBrokerBaseUri + PactUri,
                new PactUriOptions("vUSQ9aXyftgjK5yuTkUcpertuiP5Pk", "2OcpDlI0uHV8Y5tbVuyvtxTyS0gdDfRw"));
            pactPublisher.PublishToBroker($"{PactsDirectory}\\{PactFilename}.json",
                ConsumerServiceVersion, new[] {"master"});

            _output.WriteLine("Finished publishing PACT to broker (" + PactBrokerBaseUri + PactUri + ")");

            return this;
        }
    }
}
