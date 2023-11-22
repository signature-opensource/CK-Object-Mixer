using CK.Core;
using CK.Object.Mixer;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace CK.Object.Mixer.Tests
{
    [TestFixture]
    public class SimpleTests
    {
        [Test]
        public void empty_configuration_behavior()
        {
            var config = ImmutableConfigurationSection.CreateFromJson( "Root", """
                {
                }
                """ );
        }
    }
}
