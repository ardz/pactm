using System;
using Events;
using Events.EventData;
using Newtonsoft.Json;
using Xunit;
using Xunit.Abstractions;

namespace Tests.Provider
{
    /// <summary>
    /// Provider contract tests. These would live with the actual
    /// provider microservice code/solution
    /// </summary>
    public class ProviderTests : PactProviderTestSetup, IDisposable
    {
        private const string ProviderServiceVersion = "1.0.0";
        private const string ProviderServiceName = "Achievement Service";
        private const string ConsumerServiceName = "PLR Service";

        private readonly ITestOutputHelper _output;

        public ProviderTests(ITestOutputHelper output) : base(output)
        {
            _output = output;

            SetConsumerProviderNames(ConsumerServiceName, ProviderServiceName)
                .ProviderSetServiceVersion(ProviderServiceVersion);
        }

        public void Dispose()
        {
        }

        [Fact]
        public void CheckProviderHonoursPactWithConsumer()
        {
            // the provider test would fail due to the way the data is structured here

            //var messageContract = new CertificatePrintedData
            //{
            //    LearnerId = new Guid("3bb86580-aef6-4526-81e7-35bb22a4390e"),
            //    CertificateId = new Guid("ae553ab2-51f1-40f0-81eb-95e46205d6e5"),
            //};

            // var message = JsonConvert.SerializeObject(new EventCertificatePrinted(messageContract,
                // new Guid("2b07dd51-1a63-4834-9311-36667ec89f51"), "Achievement"));

            // the provider test should pass here due to the way the data is structured

            dynamic messageContract = new
            {
                CertificateId = "ae553ab2-51f1-40f0-81eb-95e46205d6e5",
                LearnerId = "3bb86580-aef6-4526-81e7-35bb22a4390e",
                CorrelationId = "2b07dd51-1a63-4834-9311-36667ec89f51",
                Type = "Achievement",
                NewField = "Foobar"
            };

            var message = JsonConvert.SerializeObject(messageContract);

            ProviderSetupMockResponseForMessage(message, "http://localhost:9223")
            .ProviderVerifyPact();
        }
    }
}