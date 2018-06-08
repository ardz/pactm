using System.Collections.Generic;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using PactNet;
using PactNet.Infrastructure.Outputters;
using Xunit.Abstractions;

namespace Tests
{
    public class PactProviderTestSetup
    {
        private string _mockUri;
        private string _providerServiceVersion;
        private string _providerServiceName;
        private string _consumerServiceName;
        private readonly ITestOutputHelper _output;
        private IPactVerifier _pactVerifier;
        private PactVerifierConfig _pactVerifierConfig;
        private string _pactUri;
        private IWebHostBuilder _hostBuilder;

        public PactProviderTestSetup(ITestOutputHelper output)
        {
            _output = output;
        }

        public PactProviderTestSetup ProviderSetServiceName(string providerServiceName)
        {
            _providerServiceName = providerServiceName;

            return this;
        }

        public PactProviderTestSetup ProviderSetConsumerServiceName(string consumerServiceName)
        {
            _consumerServiceName = consumerServiceName;

            return this;
        }

        public PactProviderTestSetup ProviderSetServiceVersion(string version)
        {
            _providerServiceVersion = version;

            return this;
        }

        public PactProviderTestSetup ProviderSetPactUri(string uri)
        {
            _pactUri = uri;

            return this;
        }

        /// <summary>
        ///     Mocks the message structure on the endpoint
        /// </summary>
        /// <param name="message">The actual message structure</param>
        /// <param name="url">Endpoint to use for mocking the actual message structure</param>
        /// <returns></returns>
        public PactProviderTestSetup ProviderSetupMockResponseForMessage(string message, string url)
        {
            _mockUri = url;

            _hostBuilder = new WebHostBuilder()
                .UseUrls(_mockUri)
                .Configure(app => { app.Run(async context => { await context.Response.WriteAsync(message); }); });

            return this;
        }

        public PactProviderTestSetup ProviderVerifyPact()
        {
            _pactVerifierConfig = new PactVerifierConfig
            {
                Outputters =
                    new
                        List<IOutput>
                        {
                            new XUnitOutput(_output)
                        },
                CustomHeader =
                    // This allows the user to set a request header that will be
                    // sent with every request the verifier sends to the provider
                    new KeyValuePair<string, string>("Authorization",
                        "Basic VGVzdA=="),
                Verbose = true,

                ProviderVersion =
                    !string.IsNullOrEmpty(_providerServiceVersion)
                        ? _providerServiceVersion
                        : null,
                // note: This is required for this feature to work
                PublishVerificationResults = !string.IsNullOrEmpty(_providerServiceVersion)
            };

            using (new TestServer(_hostBuilder))
            {
                _pactVerifier = new PactVerifier(_pactVerifierConfig);
                _pactVerifier
                    //.ProviderState($"{serviceUri}/provider-states")
                    .ServiceProvider(_providerServiceName, _mockUri)
                    .HonoursPactWith(_consumerServiceName)
                    .PactUri(
                        _pactUri,
                        new PactUriOptions("vUSQ9aXyftgjK5yuTkUcpertuiP5Pk", "2OcpDlI0uHV8Y5tbVuyvtxTyS0gdDfRw"))
                    .Verify();
            }

            return this;
        }
    }
}