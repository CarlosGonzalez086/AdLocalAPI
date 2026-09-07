using AdLocalAPI.Helpers;
using Xunit;

namespace AdLocalAPI.Tests
{
    public class CorsPolicySecurityTests
    {
        private readonly string[] _explicitOrigins = new[]
        {
            "https://adlocal.store",
            "https://admin.adlocal.store"
        };

        [Theory]
        [InlineData("http://localhost:3000")]
        [InlineData("http://localhost:4321")]
        [InlineData("http://localhost:5173")]
        [InlineData("https://localhost:7045")]
        [InlineData("http://127.0.0.1:3000")]
        [InlineData("http://127.0.0.1:8080")]
        public void IsOriginAllowed_LocalDevelopment_AllowedOnAnyPort(string origin)
        {
            var isAllowed = CorsSecurityPolicy.IsOriginAllowed(origin, _explicitOrigins);
            Assert.True(isAllowed);
        }

        [Theory]
        [InlineData("https://adlocal.store")]
        [InlineData("https://admin.adlocal.store")]
        [InlineData("https://panel.adlocal.store")]
        [InlineData("https://api.adlocal.store")]
        public void IsOriginAllowed_ProductionAndSubdomains_Allowed(string origin)
        {
            var isAllowed = CorsSecurityPolicy.IsOriginAllowed(origin, _explicitOrigins);
            Assert.True(isAllowed);
        }

        [Theory]
        [InlineData("https://adlocal-preview-branch.vercel.app")]
        [InlineData("https://staging-panel.vercel.app")]
        [InlineData("https://adlocal-worker.workers.dev")]
        public void IsOriginAllowed_PreviewDeployments_Allowed(string origin)
        {
            var isAllowed = CorsSecurityPolicy.IsOriginAllowed(origin, _explicitOrigins);
            Assert.True(isAllowed);
        }

        [Theory]
        [InlineData("https://evil-attacker.com")]
        [InlineData("https://phishing-adlocal.store.evil.com")]
        [InlineData("https://adlocal.store.fake.org")]
        [InlineData("https://notadlocal.store")]
        [InlineData("http://attacker-site.io")]
        public void IsOriginAllowed_UntrustedExternalDomains_Rejected(string origin)
        {
            var isAllowed = CorsSecurityPolicy.IsOriginAllowed(origin, _explicitOrigins);
            Assert.False(isAllowed);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("not-a-valid-uri:::")]
        public void IsOriginAllowed_MalformedOrEmptyOrigin_SafelyRejected(string? origin)
        {
            var isAllowed = CorsSecurityPolicy.IsOriginAllowed(origin, _explicitOrigins);
            Assert.False(isAllowed);
        }
    }
}
