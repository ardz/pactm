using PactNet.Infrastructure.Outputters;
using Xunit.Abstractions;

namespace Tests
{
    // XUnit output helper class for use with PACT library outputters.
    public class XUnitOutput : IOutput
    {
        private readonly ITestOutputHelper _output;

        public XUnitOutput(ITestOutputHelper output)
        {
            _output = output;
        }

        public void WriteLine(string line)
        {
            _output.WriteLine(line);
        }
    }
}
