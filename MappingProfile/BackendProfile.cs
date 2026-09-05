using AutoMapper;
using Backend.DTOs.Appointments.Responses;
using Backend.DTOs.Appointments.Requests;
using Backend.DTOs.AppointmentsAnnexes.Requests;
using Backend.DTOs.AppointmentsAnnexes.Responses;
using Backend.DTOs.Doctors.Requests;
using Backend.DTOs.Doctors.Responses;
using Backend.DTOs.DoctorsHasSpecialities.Requests;
using Backend.DTOs.DocumentTypes.Responses;
using Backend.DTOs.Durations.Responses;
using Backend.DTOs.Genders.Responses;
using Backend.DTOs.Offices.Requests;
using Backend.DTOs.Offices.Responses;
using Backend.DTOs.Patients.Requests;
using Backend.DTOs.Patients.Responses;
using Backend.DTOs.Specialities.Requests;
using Backend.DTOs.Specialities.Responses;
using Backend.DTOs.Users.Requests;
using Backend.DTOs.Users.Responses;
using Backend.Entities.Appointments;
using Backend.Entities.AppointmentsAnnexes;
using Backend.Entities.Doctors;
using Backend.Entities.DoctorsHasSpecialities;
using Backend.Entities.DocumentTypes;
using Backend.Entities.Durations;
using Backend.Entities.Genders;
using Backend.Entities.Offices;
using Backend.Entities.Patients;
using Backend.Entities.Specialities;
using Backend.Entities.Users;

namespace Backend.MappingProfile;

public class BackendProfile : Profile
{
    public BackendProfile()
    {
        CreateMap<Gender, GenderResponseDto>().ReverseMap();
        CreateMap<DocumentType, DocumentTypeResponseDto>().ReverseMap();
        
        CreateMap<Office, OfficeResponseDto>().ReverseMap();
        CreateMap<Office, OfficeMinimalDto>().ReverseMap();
        CreateMap<Office, OfficeRequestCreateDto>().ReverseMap();
        CreateMap<Office, OfficeRequestUpdateDto>().ReverseMap();
        
        CreateMap<Doctor, DoctorResponseDto>().ReverseMap();
        CreateMap<Doctor, DoctorMinimalDto>().ReverseMap();
        CreateMap<DoctorFlatDto, DoctorResponseDto>()
            .ForMember(dest => dest.DocumentType, opt => opt.MapFrom(src =>
                new DocumentTypeResponseDto
                {
                    Id = src.DocumentTypeId,
                    Name = src.DocumentTypeName
                }));
        CreateMap<Doctor, DoctorRequestCreateDto>().ReverseMap();
        CreateMap<Doctor, DoctorRequestUpdateDto>().ReverseMap();

        CreateMap<User, UserResponseDto>().ReverseMap();
        CreateMap<UserFlatDto, UserResponseDto>()
            .ForMember(dest => dest.Office, opt => opt.MapFrom(src =>
                new OfficeMinimalDto
                {
                    Id = src.OfficeId,
                    Name = src.OfficeName,
                    Nit = src.OfficeNit
                }))
            .ForMember(dest => dest.Doctor, opt => opt.MapFrom(src =>
                new DoctorMinimalDto
                {
                    Id = src.DoctorId,
                    Name = src.DoctorName,
                    LastName = src.DoctorLastName,
                    Document = src.DoctorDocument
                }));
        CreateMap<User, UserRequestCreateDto>().ReverseMap();
        CreateMap<User, UserRequestUpdateDto>().ReverseMap();

        CreateMap<Speciality, SpecialityResponseDto>().ReverseMap();
        CreateMap<Speciality, SpecialityMinimalDto>().ReverseMap();
        CreateMap<Speciality, SpecialityRequestCreateDto>().ReverseMap();
        CreateMap<Speciality, SpecialityRequestUpdatedDto>().ReverseMap();

        CreateMap<DoctorHasSpeciality, DoctorSpecialityRequestCreateUpdateDto>().ReverseMap();

        CreateMap<Duration, DurationResponseDto>()
            .ForMember(dest => dest.Interval, opt => opt.MapFrom(src => src.Interval.ToString(@"hh\:mm\:ss")))
            .ReverseMap()
            .ForMember(dest => dest.Interval, opt => opt.MapFrom(src => TimeSpan.Parse(src.Interval)));

        CreateMap<Patient, PatientResponseDto>().ReverseMap();
        CreateMap<Patient, PatientMinimalDto>().ReverseMap();
        CreateMap<PatientFlatDto, PatientResponseDto>()
            .ForMember(dest => dest.DocumentType, opt => opt.MapFrom(src =>
                new DocumentTypeResponseDto
                {
                    Id = src.DocumentTypeId,
                    Name = src.DocumentTypeName
                }))
            .ForMember(dest => dest.Gender, opt => opt.MapFrom(src =>
                new GenderResponseDto
                {
                    Id = src.GenderId,
                    Name = src.GenderName
                }));
        CreateMap<Patient, PatientRequestCreateDto>().ReverseMap();
        CreateMap<Patient, PatientRequestUpdateDto>().ReverseMap();

        CreateMap<Appointment, SeveralAppointmentsRequestCreateDto>().ReverseMap();
        CreateMap<Appointment, RescheduleToSlotRequestDto>().ReverseMap();
        CreateMap<Appointment, AssignAppointmentRequestDto>().ReverseMap();
        CreateMap<
            (DateOnly CalendarDate, string StatusDay, string Color),
            OccupationAppointmentsPerMonthResponseDto
        >()
        .ForMember(
            d => d.CalendarDate,
            o => o.MapFrom(s => s.CalendarDate.ToString("yyyy-MM-dd"))
        )
        .ForMember(
            d => d.StatusDay,
            o => o.MapFrom(s => s.StatusDay)
        )
        .ForMember(
            d => d.Color,
            o => o.MapFrom(s => s.Color)
        );
        CreateMap<Appointment, AppointmentResponseDto>().ReverseMap();
        CreateMap<Appointment, AppointmentMinimalDto>().ReverseMap();
        CreateMap<AppointmentFlatDto, AppointmentResponseDto>()
            .ForMember(dest => dest.Duration, opt => opt.MapFrom(src =>
                new DurationResponseDto
                {
                    Id = src.DurationId,
                    Interval = src.DurationInterval
                }))
            .ForMember(dest => dest.Doctor, opt => opt.MapFrom(src =>
                new DoctorMinimalDto
                {
                    Id = src.DoctorId,
                    Name = src.DoctorName,
                    LastName = src.DoctorLastname,
                    Document = src.DoctorDocument
                }))
            .ForMember(dest => dest.Speciality, opt => opt.MapFrom(src =>
                new SpecialityMinimalDto
                {
                    Id = src.SpecialityId,
                    Name = src.SpecialityName,
                    Code = src.SpecialityCode
                }))
            .ForMember(dest => dest.Patient, opt => opt.Ignore())
            .AfterMap((src, dest) =>
            {
                if (src.PatientId.HasValue)
                {
                    dest.Patient = new PatientMinimalDto
                    {
                        Id = src.PatientId.Value,
                        Name = src.PatientName ?? string.Empty,
                        Lastname = src.PatientLastname ?? string.Empty,
                        Document = src.PatientDocument ?? string.Empty
                    };
                }
                else
                {
                    dest.Patient = null;
                }
            });

        CreateMap<AppointmentAnnex, AppointmentAnnexResponseDto>().ReverseMap();
        CreateMap<AppointmentAnnexFlatDto, AppointmentAnnexResponseDto>()
            .ForMember(dest => dest.Appointment, opt => opt.MapFrom(src =>
                new AppointmentMinimalDto
                {
                    Id = src.AppointmentId,
                    DateAppointment = src.AppointmentDateAppointment,
                    StartHour = src.AppointmentStartHour
                }
            ));
        CreateMap<AppointmentAnnex, AppointmentAnnexRequestCreateDto>().ReverseMap();
    }
}