namespace Backend.Entities.DoctorsHasSpecialities;

public class DoctorHasSpeciality
{
    public int Id { get; set; }
    public int DoctorId { get; set; }
    public int SpecialityId { get; set; }
    public bool Status { get; set; }
}
