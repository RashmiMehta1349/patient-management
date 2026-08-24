namespace PatientMgmt.BusinessLogic.Auth
{
    /// <summary>
    /// Deterministic hashing for high-entropy random tokens (password reset tokens), distinct
    /// from the salted adaptive password hasher: tokens are already random/unguessable, so a
    /// fast deterministic hash (SHA-256) is sufficient and allows direct lookup by hash.
    /// </summary>
    public interface ITokenHasher
    {
        string Hash(string rawToken);
    }
}
