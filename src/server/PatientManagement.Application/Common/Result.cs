namespace PatientManagement.Application.Common;

/// <summary>Simple success/failure result wrapper for Application-layer command handlers.</summary>
public class Result<T>
{
    public bool Succeeded { get; }
    public T? Value { get; }
    public string? Error { get; }

    /// <summary>True when the failure represents a "record does not exist" outcome (404),
    /// as distinct from a validation failure (400). Defaults to false, so existing
    /// Success/Failure factories remain fully backward compatible.</summary>
    public bool IsNotFound { get; }

    private Result(bool succeeded, T? value, string? error, bool isNotFound = false)
    {
        Succeeded = succeeded;
        Value = value;
        Error = error;
        IsNotFound = isNotFound;
    }

    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(string error) => new(false, default, error);
    public static Result<T> NotFound(string error) => new(false, default, error, isNotFound: true);
}
