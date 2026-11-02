namespace Backend.DTOs.DoctorsHasSpecialities.Responses;

public sealed class DoctorSpecialityPriceResponseDto
{
    public int Id { get; set; }
    public int DoctorId { get; set; }
    public int SpecialityId { get; set; }
    public decimal? PriceUsd { get; set; }
    public bool Status { get; set; }
}
