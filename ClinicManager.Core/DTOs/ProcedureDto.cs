namespace ClinicManager.Core.DTOs;

/// <summary>
/// Procedura medyczna zwracana z warstwy serwisowej.
/// </summary>
public record ProcedureDto(int Id, string Name, string? Description, decimal DefaultCost);
