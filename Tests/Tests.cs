using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Events;
using Events.EventData;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Newtonsoft.Json;
using PactNet;
using PactNet.Infrastructure.Outputters;
using PactNet.Mocks.MockHttpService;
using PactNet.Mocks.MockHttpService.Models;
using Xunit;
using Xunit.Abstractions;

namespace Tests
{
    public class Tests : PactMessageSetup, IDisposable
    {
        private readonly ITestOutputHelper _output;

        private const string ServiceProvider = "Achievement Service";
        private const string ServiceVersion = "1.0.0";

        public Tests()
        {
            CreatePactBuilder()
                .SetConsumerProviderNames("PLR Service", "Achievement Service")
                .SetPactVersion("1.0")
                .SetMockServerPort(9222)
                .CreateMockService()
                .CreateTestClient();
        }

        public void Dispose()
        {
            // save the pact file to disk first
            PactBuilder.Build();

            // send it up to the broker
            var pactPublisher = new PactPublisher("https://pecktest.pact.dius.com.au/",
                new PactUriOptions("vUSQ9aXyftgjK5yuTkUcpertuiP5Pk", "2OcpDlI0uHV8Y5tbVuyvtxTyS0gdDfRw"));
            pactPublisher.PublishToBroker($"{PactDirectory}\\{PactFilename}.json",
                PactVersion, new[] { "master" });
        }

        [Fact]
        public void ConsumerSetsMessageExpectation()
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

            MockMessageService.VerifyInteractions();
            MockMessageService.ClearInteractions();
        }

        [Fact]
        public void SweetBabyJesus()
        {
            dynamic messageContract = new
            {
                CertificateId = "ae553ab2-51f1-40f0-81eb-95e46205d6e5",
                LearnerId = "3bb86580-aef6-4526-81e7-35bb22a4390e",
                CorrelationId = "2b07dd51-1a63-4834-9311-36667ec89f51",
                Type = "Achievement",
            };

            MockPreconditions(given: "There is a new achievement message in the queue",
                    uponReceiving: "A request to process the message")
                .MockExpectedResponse(messageContract)
                .MockCheckResponse()
                .MockVerifyAndClearInteractions();
        }

        [Fact]
        public async Task CheckProviderHonoursPactWithConsumer()
        {
            // arrange

            // mock the format of the message here by newing up
            // the actual integration event in the service and serialising it
            // then fire up a web server to return the message output on
            // the endpoint

            const string serviceVersion = "1.0";
            const string serviceUri = "http://localhost:9223";

            var data = new CertificatePrintedData
            {
                LearnerId = new Guid("3bb86580-aef6-4526-81e7-35bb22a4390e"),
                CertificateId = new Guid("ae553ab2-51f1-40f0-81eb-95e46205d6e5"),
            };

            var message = JsonConvert.SerializeObject(new EventCertificatePrinted(data,
                new Guid("2b07dd51-1a63-4834-9311-36667ec89f51"), "Achievement"));

            var hostBuilder = new WebHostBuilder()
                .UseUrls(serviceUri)
                .Configure(app => { app.Run(async context => { await context.Response.WriteAsync(message); }); });

            var config = new PactVerifierConfig
            {
                Outputters =
                    new
                        List<IOutput>
                        {
                            new XUnitOutput(_output)
                        },
                CustomHeader =
                    new KeyValuePair<string, string>("Authorization",
                        "Basic VGVzdA=="), //This allows the user to set a request header that will be sent with every request the verifier sends to the provider
                Verbose = true,

                ProviderVersion =
                    !string.IsNullOrEmpty(serviceVersion)
                        ? serviceVersion
                        : null, //NOTE: This is required for this feature to work
                PublishVerificationResults = !string.IsNullOrEmpty(serviceVersion)
            };

            // act
            using (var server = new TestServer(hostBuilder))
            {
                var response = await server.CreateRequest("/")
                    .SendAsync("GET");

                // assert 
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();

                Assert.Equal("Test response", content);
            }
        }
    }
}
