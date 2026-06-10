namespace ClinicManager.Core.Constants;

/// <summary>
/// Limity uploadu skanow dokumentow do kartoteki pacjenta.
/// </summary>
public static class MedicalRecordUpload
{
    public const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    public static IReadOnlyList<string> AllowedExtensions { get; } = new[]
    {
        ".pdf",
        ".jpg",
        ".jpeg",
        ".png"
    };

    public static IReadOnlyList<string> AllowedContentTypes { get; } = new[]
    {
        "application/pdf",
        "image/jpeg",
        "image/png"
    };

    /// <summary>
    /// Wartosc atrybutu <c>accept</c> dla elementu <c>InputFile</c>, np.
    /// <c>.pdf,.jpg,.jpeg,.png</c>.
    /// </summary>
    public static string HtmlAcceptList => string.Join(",", AllowedExtensions);
}
