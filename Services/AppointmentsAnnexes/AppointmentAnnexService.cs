using AutoMapper;
using Backend.DTOs.Appointments.Responses;
using Backend.DTOs.AppointmentsAnnexes.Requests;
using Backend.DTOs.AppointmentsAnnexes.Responses;
using Backend.Entities.AppointmentsAnnexes;
using Backend.Repositories.AppointmentsAnnexes;
using Backend.Services.Appointments;
using Backend.Services.CloudinaryUpload;

namespace Backend.Services.AppointmentsAnnexes;

public class AppointmentAnnexService(IAppointmentAnnexRepository appointmentAnnexRepository, IAppointmentService appointmentService, ICloudinaryService cloudinaryService, IMapper mapper) : IAppointmentAnnexService
{
    public async Task<AppointmentResponseDto> CreateSeveralAsync(AppointmentAnnexRequestCreateDto[] dtos)
    {
        var appointmentId = dtos[0].AppointmentId;

        var tasks = dtos.Select(async dto =>
        {
            var cloudinaryUrl = await cloudinaryService.UploadImageAsync(dto.File, $"/appointments/{appointmentId}");

            var appointmentAnnex = mapper.Map<AppointmentAnnex>(dto);
            appointmentAnnex.File = cloudinaryUrl;
            appointmentAnnex.Name = dto.File.FileName;

            return await appointmentAnnexRepository.CreateAsync(appointmentAnnex);
        });

        var results = await Task.WhenAll(tasks);
        var appointment = await appointmentService.GetByIdAsync(appointmentId);

        return appointment;
    }

    public async Task<IEnumerable<AppointmentAnnexResponseDto>> GetAllAsync(int appointmentId)
    {
        var appointmentsAnnexes = await appointmentAnnexRepository.GetAllAsync(appointmentId);
        return mapper.Map<IEnumerable<AppointmentAnnexResponseDto>>(appointmentsAnnexes);
    }
}
