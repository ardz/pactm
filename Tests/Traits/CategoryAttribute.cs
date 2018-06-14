using System;
using Xunit.Sdk;

namespace Tests.Traits
{
    [TraitDiscoverer("Tests.Traits", "Tests")]
    [AttributeUsage(AttributeTargets.Method)]
    public abstract class CategoryAttribute : Attribute, ITraitAttribute
    {
        public abstract string Type { get; }
    }
    public class ContractAttribute : CategoryAttribute
    {
        public override string Type => "Contract";
    }

    public class ConsumerAttribute : CategoryAttribute
    {
        public override string Type => "Consumer";
    }
    public class ProviderAttribute : CategoryAttribute
    {
        public override string Type => "Provider";
    }
}