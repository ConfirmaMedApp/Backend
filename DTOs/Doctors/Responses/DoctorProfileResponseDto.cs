using Backend.DTOs.Specialities.Responses;

namespace Backend.DTOs.Doctors.Responses;

/// <summary>
/// Hoja de vida del doctor: datos del doctor, su foto, su descripción y las
/// especialidades que ejerce. Pensado para la parte del paciente.
/// </summary>
public class DoctorProfileResponseDto : DoctorResponseDto
{
    public IEnumerable<SpecialityResponseDto> Specialities { get; set; } = [];
}
