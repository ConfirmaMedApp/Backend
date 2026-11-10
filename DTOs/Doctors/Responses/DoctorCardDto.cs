using Backend.DTOs.Specialities.Responses;

namespace Backend.DTOs.Doctors.Responses;

/// <summary>
/// Vista ligera de un doctor para listados públicos (p. ej. selección de doctor
/// en la app del paciente): solo id, nombre, apellido, foto y sus especialidades.
/// </summary>
public class DoctorCardDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public IEnumerable<SpecialityMinimalDto> Specialities { get; set; } = [];
}
