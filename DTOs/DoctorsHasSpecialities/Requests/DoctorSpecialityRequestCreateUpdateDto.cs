namespace Backend.DTOs.DoctorsHasSpecialities.Requests;

public class DoctorSpecialityRequestCreateUpdateDto
{
    public int SpecialityId { get; set; }
    public int[]? DoctorIds { get; set; }
}
