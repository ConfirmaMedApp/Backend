namespace Backend.DTOs.DoctorsHasSpecialities.Requests;

public sealed class DoctorSpecialityPriceRequestDto
{
    public int DoctorId { get; set; }
    public int SpecialityId { get; set; }

    // Precio de la consulta en USD para ese doctor en esa especialidad.
    public decimal PriceUsd { get; set; }
}
