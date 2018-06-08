using System;
using System.Collections.Generic;
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
using Xunit;
using Xunit.Abstractions;

namespace Tests
{
    public class ConsumerTests : PactConsumerTestSetup, IDisposable
    {
        private readonly ITestOutputHelper _output;

        private const string ServiceConsumer = "PLR Service";
        private const string ServiceProvider = "Achievement Service";
        private const string ServiceVersion = "1.0.0";

        public ConsumerTests(ITestOutputHelper output) : base(output)
        {
            _output = output;

            CreatePactBuilder(@"C:\PactFiles")
                .SetConsumerProviderNames(ServiceConsumer, ServiceProvider)
                .SetConsumerServiceVersion("1.0.0")
                .SetMockServerPort(9222)
                .CreateMockService()
                .CreateTestClient();
        }

        public void Dispose()
        {
            WriteAndPublishPact();
        }

        [Fact]
        public void ConsumerSetsMessageExpectation()
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
                .MockCheckResponse("/servicebusmock/message")
                .MockVerifyAndClearInteractions();
        }

        [Fact]
        public async Task CheckProviderHonoursPactWithConsumer()
        {
            // arrange

            // mock the format of the message here by newing up
            // the actual integration event in the service and serialising it
            // and sending to an on the fly web server to mock the message output on
            // the endpoint, then call the PACT verify methods to check
            // the format is correct based on the contract stored on the broker

            const string serviceVersion = "1.0";
            const string serviceUri = "http://localhost:9223";

            // the consumer test would fail due to the way the data is
            // structured here

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

            using (var server = new TestServer(hostBuilder))
            {
                var response = await server.CreateRequest("/")
                    .SendAsync("GET");

                // assert 
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();

                Assert.Equal("Test response", content);
            }

            // example pact verify code, inside the using?

            IPactVerifier pactVerifier = new PactVerifier(config);
            pactVerifier
                //.ProviderState($"{serviceUri}/provider-states")
                .ServiceProvider("Achievement Service", serviceUri)
                .HonoursPactWith("PLR Service")
                .PactUri(
                    "https://pecktest.pact.dius.com.au/pacts/provider/Achievement%20Service/consumer/Plr%20Service/latest",
                    new PactUriOptions("vUSQ9aXyftgjK5yuTkUcpertuiP5Pk", "2OcpDlI0uHV8Y5tbVuyvtxTyS0gdDfRw"))
                //Assert
                .Verify();
        }
    }
}
