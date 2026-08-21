namespace LibraryAppPrototype.Services;

// Error handling tanpa exception (PRD 7.2).
// Code  : SELALU diisi ID aturan ("BR-01", "BR-05", ...) atau kode teknis ("NOT_FOUND", "CONFLICT").
// Message: Bahasa Indonesia — langsung tampil di ErrorAlert.
public record OperationResult(bool Succeeded, string? Code, string? Message)
{
    public static OperationResult Ok() => new(true, null, null);
    public static OperationResult Fail(string code, string message) => new(false, code, message);
}

public record OperationResult<T>(bool Succeeded, T? Value, string? Code, string? Message)
    : OperationResult(Succeeded, Code, Message)
{
    public static OperationResult<T> Ok(T value) => new(true, value, null, null);
    public static new OperationResult<T> Fail(string code, string message) => new(false, default, code, message);
}
