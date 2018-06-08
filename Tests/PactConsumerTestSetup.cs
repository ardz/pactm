using Newtonsoft.Json;
using PactNet;
using PactNet.Mocks.MockHttpService;
using PactNet.Mocks.MockHttpService.Models;
using System.Collections.Generic;
using System.Net;
using Tests.ApiTestClient;
using Xunit.Abstractions;

namespace Tests
{
    public class PactConsumerTestSetup
    {
        private readonly ITestOutputHelper _output;
        public string Consumer { get; private set; }
        public string Provider { get; private set; }
        public static string PactFilename { get; private set; }
        public const string PactBrokerUri = "https://pecktest.pact.dius.com.au/";
        private static string MockMessageServiceBaseUri => $"http://localhost:{MockServerPort}";
        public string PactsDirectory { get; private set; }
        public static ProviderServiceRequest Request;
        public static Client Client;
        public static IPactBuilder PactBuilder { get; private set; }
        public static IMockProviderService MockMessageService { get; private set; }
        public static int MockServerPort { get; set; }
        public static string PactVersion { get; set; }

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
        public PactConsumerTestSetup SetPactVersion(string version)
        {
            PactVersion = version;

            return this;
        }

        public PactConsumerTestSetup SetConsumerProviderNames(string cosumer, string provider)
        {
            Provider = provider.Replace(" ", "_").ToLower();
            Consumer = cosumer.Replace(" ", "_").ToLower();

            PactFilename = $"{Consumer}-{Provider}";

            PactBuilder
                .ServiceConsumer(cosumer)
                .HasPactWith(provider);

            _output.WriteLine("Starting PACT File creation for " + Provider + " and " + Consumer);

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
            var pactPublisher = new PactPublisher(PactBrokerUri,
                new PactUriOptions("vUSQ9aXyftgjK5yuTkUcpertuiP5Pk", "2OcpDlI0uHV8Y5tbVuyvtxTyS0gdDfRw"));
            pactPublisher.PublishToBroker($"{PactsDirectory}\\{PactFilename}.json",
                PactVersion, new[] {"master"});

            _output.WriteLine("Finished publishing PACT to broker (" + PactBrokerUri + ")");

            return this;
        }
    }
}