using System;
using Xunit;
using Xunit.Abstractions;

namespace Tests.Consumer
{
    /// <inheritdoc cref="PactConsumerTestSetup" />
    /// <summary>
    /// Consumer tests. Tests specify the contracts they
    /// expect the Providers to adhere to.
    /// </summary>
    public class ConsumerTests : PactConsumerTestSetup, IDisposable
    {
        private readonly ITestOutputHelper _output;

        private const string ServiceConsumer = "PLR Service";
        private const string ServiceProvider = "Achievement Service";
        private const string ConsumerVersion = "1.0.1";

        public ConsumerTests(ITestOutputHelper output) : base(output)
        {
            _output = output;

            CreatePactBuilder(@"C:\PactFiles")
                .SetConsumerProviderNames(ServiceConsumer, ServiceProvider)
                .SetConsumerVersion(ConsumerVersion)
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
                NewField = "Foobar"
            };
            
            MockPreconditions(given: "There is a new achievement message in the queue",
                    uponReceiving: "A request to process the message")
                .MockExpectedResponse(messageContract)
                .MockCheckResponse("/servicebusmock/message")
                .MockVerifyAndClearInteractions();
        }
    }
}
