using PatientManagement.Infrastructure.Services;
using Xunit;

namespace PatientManagement.Tests.Unit.Auth;

public class PasswordHasherServiceTests
{
    private readonly PasswordHasherService _hasher = new();

    [Fact]
    public void HashThenVerify_RoundTripSucceeds()
    {
        var hash = _hasher.HashPassword("CorrectHorseBatteryStaple1!");

        Assert.True(_hasher.VerifyPassword(hash, "CorrectHorseBatteryStaple1!"));
    }

    [Fact]
    public void Verify_FailsAgainstTamperedHash()
    {
        var hash = _hasher.HashPassword("CorrectHorseBatteryStaple1!");
        var tampered = hash.Substring(0, hash.Length - 4) + "abcd";

        Assert.False(_hasher.VerifyPassword(tampered, "CorrectHorseBatteryStaple1!"));
    }

    [Fact]
    public void Verify_FailsForWrongPassword()
    {
        var hash = _hasher.HashPassword("CorrectHorseBatteryStaple1!");

        Assert.False(_hasher.VerifyPassword(hash, "WrongPassword"));
    }
}
