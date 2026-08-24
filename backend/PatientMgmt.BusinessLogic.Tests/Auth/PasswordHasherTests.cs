using PatientMgmt.BusinessLogic.Auth;
using Xunit;

namespace PatientMgmt.BusinessLogic.Tests.Auth
{
    public class PasswordHasherTests
    {
        private readonly PasswordHasher _hasher = new();

        [Fact]
        public void Hash_SamePassword_ProducesDifferentHashesEachTime()
        {
            var h1 = _hasher.Hash("CorrectHorseBattery1!");
            var h2 = _hasher.Hash("CorrectHorseBattery1!");

            Assert.NotEqual(h1, h2); // salting
        }

        [Fact]
        public void Verify_RoundTrips_Correctly()
        {
            var hash = _hasher.Hash("CorrectHorseBattery1!");

            Assert.True(_hasher.Verify("CorrectHorseBattery1!", hash));
            Assert.False(_hasher.Verify("WrongPassword!", hash));
        }

        [Fact]
        public void Verify_MalformedStoredHash_ReturnsFalseInsteadOfThrowing()
        {
            Assert.False(_hasher.Verify("anything", "not-a-valid-hash"));
        }
    }
}
