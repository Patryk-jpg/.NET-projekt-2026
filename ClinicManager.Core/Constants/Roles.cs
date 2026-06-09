namespace ClinicManager.Core.Constants;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Lekarz = "Lekarz";
    public const string Rejestratorka = "Rejestratorka";

    public static readonly IReadOnlyList<string> All = [Admin, Lekarz, Rejestratorka];
}
