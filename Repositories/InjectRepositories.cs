using Backend.Repositories.Appointments;
using Backend.Repositories.AppointmentsAnnexes;
using Backend.Repositories.Doctors;
using Backend.Repositories.DoctorsHasSpecialities;
using Backend.Repositories.DocumentTypes;
using Backend.Repositories.Durations;
using Backend.Repositories.Genders;
using Backend.Repositories.Offices;
using Backend.Repositories.Specialities;
using Backend.Repositories.Users;
using Backend.Services.Patients;

namespace Backend.Repositories;

public static class InjectRepositories
{
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddTransient<IGenderRepository, GenderRepository>();
        services.AddTransient<IDocumentTypeRepository, DocumentTypeRepository>();

        services.AddTransient<IOfficeRepository, OfficeRepository>();
        
        services.AddTransient<IDoctorRepository, DoctorRepository>();

        services.AddTransient<IUserRepository, UserRepository>();

        services.AddTransient<ISpecialityRepository, SpecialityRepository>();

        services.AddTransient<IDoctorHasSpecialityRepository, DoctorHasSpecialityRepository>();

        services.AddTransient<IDurationRepository, DurationRepository>();

        services.AddTransient<IPatientService, PatientService>();

        services.AddTransient<IAppointmentRepository, AppointmentRepository>();

        services.AddTransient<IAppointmentAnnexRepository, AppointmentAnnexRepository>();

        return services;
    }
}
