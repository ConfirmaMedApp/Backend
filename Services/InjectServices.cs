using Backend.Repositories.Patients;
using Backend.Services.Appointments;
using Backend.Services.Auth;
using Backend.Services.CurrentUser;
using Backend.Services.Doctors;
using Backend.Services.DoctorsHasSpecialities;
using Backend.Services.DocumentTypes;
using Backend.Services.Durations;
using Backend.Services.Genders;
using Backend.Services.Offices;
using Backend.Services.Specialities;
using Backend.Services.Users;
using Backend.Services.MailerSend;
using Backend.Services.CloudinaryUpload;
using Backend.Services.AppointmentsAnnexes;
using Backend.Services.AppointmentsNotes;

namespace Backend.Services;

public static class InjectServices
{
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddScoped<IGenderService, GenderService>();
        services.AddScoped<IDocumentTypeService, DocumentTypeService>();

        services.AddScoped<IOfficeService, OfficeService>();
        
        services.AddScoped<IDoctorService, DoctorService>();

        services.AddScoped<IUserService, UserService>();

        services.AddScoped<IAuthService, AuthService>();

        services.AddScoped<ISpecialityService, SpecialityService>();

        services.AddScoped<IDoctorHasSpecialityService, DoctorHasSpecialityService>();

        services.AddScoped<IDurationService, DurationService>();

        services.AddScoped<IPatientRepository, PatientRepository>();

        services.AddScoped<IAppointmentService, AppointmentService>();
        
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        services.AddScoped<IMailerSenderService, MailerSenderService>();

        services.AddScoped<ICloudinaryService, CloudinaryService>();

        services.AddScoped<IAppointmentAnnexService, AppointmentAnnexService>();

        services.AddScoped<IAppointmentNoteService, AppointmentNoteService>();

        return services;
    }
}
