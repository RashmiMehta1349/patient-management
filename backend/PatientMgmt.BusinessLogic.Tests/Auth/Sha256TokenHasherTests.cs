using PatientMgmt.BusinessLogic.Auth;
using Xunit;

namespace PatientMgmt.BusinessLogic.Tests.Auth
{
    public class Sha256TokenHasherTests
    {
        [Fact]
        public void Hash_IsDeterministic_ForLookupByHash()
        {
            var hasher = new Sha256TokenHasher();
            Assert.Equal(hasher.Hash("abc"), hasher.Hash("abc"));
        }

        [Fact]
        public void Hash_DifferentInputs_ProduceDifferentHashes()
        {
            var hasher = new Sha256TokenHasher();
            Assert.NotEqual(hasher.Hash("abc"), hasher.Hash("abd"));
        }
    }
}
