namespace PatientMgmt.Domain.Options
{
    /// <summary>Configuration-driven SMTP/email provider settings (appsettings.json section "Smtp").</summary>
    public class SmtpOptions
    {
        public const string SectionName = "Smtp";

        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 587;
        public bool UseSsl { get; set; } = true;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FromAddress { get; set; } = "no-reply@patientmgmt.local";
        public string FromDisplayName { get; set; } = "Patient Management App";

        /// <summary>Base URL of the frontend used to build the reset link, e.g. https://app.example.com</summary>
        public string FrontendBaseUrl { get; set; } = "https://localhost:4200";
    }
}
