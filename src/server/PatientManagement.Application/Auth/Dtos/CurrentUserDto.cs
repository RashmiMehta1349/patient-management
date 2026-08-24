using System;

namespace PatientManagement.Application.Auth.Dtos;

public class CurrentUserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public DateTime? LastLoginAt { get; set; }
}
