using Moq;
using PatientMgmt.BusinessLogic.Patients;
using PatientMgmt.BusinessLogic.Tests.Auth;
using PatientMgmt.DataAccess.Repositories;
using PatientMgmt.Domain.Entities;
using Xunit;

namespace PatientMgmt.BusinessLogic.Tests.Patients
{
    public class PatientServiceTests
    {
        private readonly Mock<IPatientRepository> _repo = new();
        private readonly FakeClock _clock = new();

        private PatientService CreateSut() => new(_repo.Object, _clock);

        [Fact]
        public async Task CreateAsync_ValidInput_CreatesPatientWithGeneratedPatientCode()
        {
            _repo.Setup(r => r.GetPatientCountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);
            _repo.Setup(r => r.FindPossibleDuplicateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Patient?)null);

            var sut = CreateSut();
            var result = await sut.CreateAsync("Jane Doe", new DateTime(1990, 1, 1), null, "Female", "5551234567", null, null);

            Assert.True(result.Success);
            Assert.Equal("P-00001", result.Patient!.PatientCode);
            Assert.False(result.PossibleDuplicateWarning);
            _repo.Verify(r => r.CreateAsync(It.Is<Patient>(p => p.FullName == "Jane Doe"), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_MissingFullName_IsRejected()
        {
            var sut = CreateSut();
            var result = await sut.CreateAsync("", new DateTime(1990, 1, 1), null, "Female", "5551234567", null, null);

            Assert.False(result.Success);
            Assert.Contains(result.Errors, e => e.Field == "fullName");
            _repo.Verify(r => r.CreateAsync(It.IsAny<Patient>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_MissingPhone_IsRejected()
        {
            var sut = CreateSut();
            var result = await sut.CreateAsync("Jane Doe", new DateTime(1990, 1, 1), null, "Female", "", null, null);

            Assert.False(result.Success);
            Assert.Contains(result.Errors, e => e.Field == "phoneNumber");
        }

        [Fact]
        public async Task CreateAsync_DateOfBirthInFuture_IsRejected()
        {
            var sut = CreateSut();
            var futureDob = _clock.UtcNow.AddDays(1);
            var result = await sut.CreateAsync("Jane Doe", futureDob, null, "Female", "5551234567", null, null);

            Assert.False(result.Success);
            Assert.Contains(result.Errors, e => e.Field == "dateOfBirth");
        }

        [Fact]
        public async Task CreateAsync_AgeOnlyEntry_StoresApproxAgeAndEntryDate()
        {
            _repo.Setup(r => r.GetPatientCountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(4);
            _repo.Setup(r => r.FindPossibleDuplicateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Patient?)null);

            var sut = CreateSut();
            var result = await sut.CreateAsync("John Smith", null, 45, "Male", "5559876543", null, null);

            Assert.True(result.Success);
            Assert.Null(result.Patient!.DateOfBirth);
            Assert.Equal(45, result.Patient.ApproxAgeAtEntry);
            Assert.Equal(_clock.UtcNow, result.Patient.EntryDate);
            Assert.Equal("P-00005", result.Patient.PatientCode);
        }

        [Fact]
        public async Task CreateAsync_MissingBothDobAndAge_IsRejected()
        {
            var sut = CreateSut();
            var result = await sut.CreateAsync("Jane Doe", null, null, "Female", "5551234567", null, null);

            Assert.False(result.Success);
            Assert.Contains(result.Errors, e => e.Field == "dateOfBirth");
        }

        [Fact]
        public async Task CreateAsync_InvalidGender_IsRejected()
        {
            var sut = CreateSut();
            var result = await sut.CreateAsync("Jane Doe", new DateTime(1990, 1, 1), null, "Alien", "5551234567", null, null);

            Assert.False(result.Success);
            Assert.Contains(result.Errors, e => e.Field == "gender");
        }

        [Fact]
        public async Task CreateAsync_DuplicateNameAndPhone_ReturnsWarningButStillSaves()
        {
            var existing = new Patient { Id = Guid.NewGuid(), FullName = "Jane Doe", PhoneNumber = "5551234567" };
            _repo.Setup(r => r.GetPatientCountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
            _repo.Setup(r => r.FindPossibleDuplicateAsync("Jane Doe", "5551234567", It.IsAny<CancellationToken>()))
                .ReturnsAsync(existing);

            var sut = CreateSut();
            var result = await sut.CreateAsync("Jane Doe", new DateTime(1990, 1, 1), null, "Female", "5551234567", null, null);

            Assert.True(result.Success);
            Assert.True(result.PossibleDuplicateWarning);
            _repo.Verify(r => r.CreateAsync(It.IsAny<Patient>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_ValidInput_UpdatesFieldsAndRefreshesUpdatedAt()
        {
            var existing = new Patient
            {
                Id = Guid.NewGuid(),
                PatientCode = "P-00001",
                FullName = "Old Name",
                PhoneNumber = "5550000000",
                Gender = Gender.Female,
                DateOfBirth = new DateTime(1980, 1, 1),
                CreatedAt = _clock.UtcNow.AddDays(-10),
                UpdatedAt = _clock.UtcNow.AddDays(-10)
            };
            _repo.Setup(r => r.GetByIdAsync(existing.Id, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

            var sut = CreateSut();
            var result = await sut.UpdateAsync(existing.Id, "New Name", new DateTime(1980, 1, 1), null, "Female", "5551112222", null, null);

            Assert.True(result.Success);
            Assert.Equal("New Name", result.Patient!.FullName);
            Assert.Equal("5551112222", result.Patient.PhoneNumber);
            Assert.Equal(_clock.UtcNow, result.Patient.UpdatedAt);
            _repo.Verify(r => r.UpdateAsync(It.IsAny<Patient>(), It.IsAny<CancellationToken>()), Times.Once);
            // Editing must not re-run the duplicate check (§3.2).
            _repo.Verify(r => r.FindPossibleDuplicateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_PatientNotFound_ReturnsIdError()
        {
            _repo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync((Patient?)null);

            var sut = CreateSut();
            var result = await sut.UpdateAsync(Guid.NewGuid(), "Name", new DateTime(1980, 1, 1), null, "Male", "5551112222", null, null);

            Assert.False(result.Success);
            Assert.Contains(result.Errors, e => e.Field == "id");
        }

        [Fact]
        public async Task CheckDuplicateAsync_ExistingMatch_ReturnsPossibleDuplicateTrue()
        {
            var existing = new Patient { Id = Guid.NewGuid(), PatientCode = "P-00002", FullName = "Jane Doe", PhoneNumber = "5551234567" };
            _repo.Setup(r => r.FindPossibleDuplicateAsync("Jane Doe", "5551234567", It.IsAny<CancellationToken>()))
                .ReturnsAsync(existing);

            var sut = CreateSut();
            var result = await sut.CheckDuplicateAsync("Jane Doe", "5551234567");

            Assert.True(result.PossibleDuplicate);
            Assert.Equal(existing.Id, result.ExistingPatient!.Id);
        }

        [Fact]
        public async Task CheckDuplicateAsync_NoMatch_ReturnsPossibleDuplicateFalse()
        {
            _repo.Setup(r => r.FindPossibleDuplicateAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Patient?)null);

            var sut = CreateSut();
            var result = await sut.CheckDuplicateAsync("Unique Name", "5559999999");

            Assert.False(result.PossibleDuplicate);
        }
    }
}
