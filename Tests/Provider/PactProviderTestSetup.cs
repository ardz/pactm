﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using PactNet;
using PactNet.Infrastructure.Outputters;
using Xunit.Abstractions;

namespace Tests.Provider
{
    /// <summary>
    /// Wrapper code for provider Pact tests
    /// </summary>
    public class PactProviderTestSetup
    {
        private readonly ITestOutputHelper _output;

        private const string PactBrokerBaseUri = "https://pecktest.pact.dius.com.au/pacts";
        private string _mockUri;
        private string _providerServiceVersion;
        private string _providerServiceName;
        private string _consumerServiceName;
        private string _pactUri;

        private IPactVerifier _pactVerifier;
        private PactVerifierConfig _pactVerifierConfig;
        private IWebHost _mockService;

        public PactProviderTestSetup(ITestOutputHelper output)
        {
            _output = output;
        }

        /// <summary>
        /// Used for working out the Uri for the PACT file on the broker
        /// Yes, not elegant but limited to the PACT API atm
        /// </summary>
        /// <param name="consumer"></param>
        /// <param name="provider"></param>
        /// <returns></returns>
        public PactProviderTestSetup SetConsumerProviderNames(string consumer, string provider)
        {
            _consumerServiceName = consumer;
            _providerServiceName = provider;

            var consumerUri = "/consumer/" + consumer.Replace(" ", "%20");
            var providerUri = "/provider/" + provider.Replace(" ", "%20");

            _pactUri = providerUri + consumerUri + "/latest";

            return this;
        }

        public PactProviderTestSetup ProviderSetServiceVersion(string version)
        {
            _providerServiceVersion = version;

            return this;
        }

        /// <summary>
        /// Mocks the message structure on the endpoint
        /// </summary>
        /// <param name="message">The actual message structure</param>
        /// <param name="uri">Endpoint to use for mocking the actual message structure</param>
        /// <returns></returns>
        public PactProviderTestSetup ProviderSetupMockResponseForMessage(string message, string uri)
        {
            _mockUri = uri;

            _mockService = WebHost.CreateDefaultBuilder(null)
                .UseUrls(_mockUri)
                .Configure(app =>
                {
                    // Add app/json header
                    app.Use(async (context, nextMiddleware) =>
                    {
                        context.Response.OnStarting(() =>
                        {
                            context.Response.Headers.Add("Content-Type", "application/json; charset=utf-8");
                            return Task.FromResult(0);
                        });
                        await nextMiddleware();
                    });
                    app.Run(async context =>
                    {
                        // mock the message/contract when the uri
                        // specified recieves a GET
                        await context
                            .Response
                            .WriteAsync(message);
                    });
                })
                .ConfigureServices(services => { services.AddMvc(); })
                .Build();

            _output.WriteLine("Mocking the following message format on " + _mockUri + " for Pact verifier:");
            _output.WriteLine(message);

            Task.Run(async () => await _mockService.StartAsync());

            return this;
        }

        public PactProviderTestSetup ProviderVerifyPact(string description, string providerState)
        {
            _pactVerifierConfig = new PactVerifierConfig
            {
                Outputters =
                    new
                        List<IOutput>
                        {
                            new XUnitOutput(_output)
                        },
                Verbose = true,

                ProviderVersion =
                    !string.IsNullOrEmpty(_providerServiceVersion)
                        ? _providerServiceVersion
                        : null,

                // note: This is required for this feature to work
                PublishVerificationResults = !string.IsNullOrEmpty(_providerServiceVersion)
            };

            _pactVerifier = new PactVerifier(_pactVerifierConfig);

            _output.WriteLine("Attempting to verify Pact.");

            _pactVerifier
                //.ProviderState($"{serviceUri}/provider-states")
                .ServiceProvider(_providerServiceName, _mockUri)
                .HonoursPactWith(_consumerServiceName)
                .PactUri(
                    PactBrokerBaseUri + _pactUri,
                    new PactUriOptions("user", "pass"))
                .Verify(description: description,
                    providerState: providerState);

            Task.Run(async () => await _mockService.StopAsync());

            return this;
        }
    }
}
