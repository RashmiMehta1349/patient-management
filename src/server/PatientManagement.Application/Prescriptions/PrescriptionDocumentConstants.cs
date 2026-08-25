namespace PatientManagement.Application.Prescriptions;

/// <summary>
/// Fixed clinic/doctor header and footer content for the generated prescription PDF (R5 — no UI
/// path to edit this, per BRD: "Clinic/doctor header and footer details are fixed/hardcoded for
/// this deployment, not editable through the UI"). Placeholder values below — replace with real
/// clinic/doctor details before go-live. Kept in this one clearly-named file so a developer can
/// find and swap them in one place.
/// </summary>
public static class PrescriptionDocumentConstants
{
    public const string ClinicName = "[Clinic Name]";

    public const string DoctorName = "Dr. [Doctor Name], MBBS";

    public const string ClinicAddressLine = "[Clinic Address], [City], [State] [PIN] · Phone: [Phone Number]";

    public const string FooterNote = "This prescription is computer-generated and valid without a physical signature. For queries, contact the clinic during working hours.";
}
