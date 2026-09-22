using AutoMapper;
using Backend.DTOs.Appointments.Requests;
using Backend.DTOs.Appointments.Responses;
using Backend.DTOs.MailerSend;
using Backend.DTOs.Offices.Responses;
using Backend.DTOs.Patients.Responses;
using Backend.Exceptions.BadRequest;
using Backend.Exceptions.NotFound;
using Backend.Exceptions.Unauthorized;
using Backend.Helpers;
using Backend.Repositories.Appointments;
using Backend.Services.Appointments.VideoCalls;
using Backend.Services.CurrentUser;
using Backend.Services.MailerSend;
using Backend.Services.Offices;
using Backend.Services.Patients;
using Backend.Services.Users;
using FluentValidation;

namespace Backend.Services.Appointments;

public class AppointmentService(
    IAppointmentRepository appointmentRepository,
    IPatientService patientService,
    IOfficeService officeService,
    ICurrentUserService currentUserService,
    IUserService userService,
    IMailerSenderService mailSenderService,
    IAppointmentVideoCallService videoCallService,
    ILogger<AppointmentService> logger,
    IValidator<SeveralAppointmentsRequestCreateDto> createValidatorDto,
    IValidator<RescheduleToSlotRequestDto> rescheduleToSlotValidatorDto,
    IMapper mapper) : IAppointmentService
{
    public async Task<AppointmentResponseDto> AssignAppointmentAsync(AssignAppointmentRequestDto dto)
    {
        var appointmentInfo = await GetByIdAsync(dto.AppointmentId);

        if (appointmentInfo.IsOccuped)
        {
            throw new BadRequestException("La cita ya se encuentra ocupada");
        }

        var appointment = await appointmentRepository.AssignAppointmentAsync(dto.AppointmentId, dto.PatientId);

        // Send email notification to patient
        var patientInfo = await patientService.GetByIdAsync(dto.PatientId);
        var userInfo = await userService.GetByIdAsync(appointmentInfo.UserId);
        var officeId = userInfo!.Office.Id;
        var officeInfo = await officeService.GetByIdAsync(officeId);

        string? videoCallLink = null;
        if (patientInfo != null)
        {
            try
            {
                var provision = await videoCallService.ProvisionForAsync(
                    dto.AppointmentId,
                    $"{patientInfo.Name} {patientInfo.Lastname}");
                videoCallLink = provision.PatientLink;
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "No se pudo provisionar la videollamada para la cita {AppointmentId}; se enviará el correo sin link",
                    dto.AppointmentId);
            }

            await SendConfirmationEmailAsync(patientInfo, officeInfo, appointmentInfo, videoCallLink);
        }

        // Re-fetch para que la respuesta traiga room_name/url/created_at ya persistidas por la provisión
        var fresh = await appointmentRepository.GetByIdAsync(dto.AppointmentId) ?? appointment;
        var response = mapper.Map<AppointmentResponseDto>(fresh);
        response.VideoCallLink = videoCallLink;
        return response;
    }

    public async Task<int> CreateSeveralAppointmentsAsync(SeveralAppointmentsRequestCreateDto dto)
    {
        var loggedUserId = currentUserService.UserId!;

        if (loggedUserId == null)
            throw new UnauthorizedException("No te encuentras autenticado");
        
        var validationResult = await createValidatorDto.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(" | ", validationResult.Errors.Select(e => e.ErrorMessage));
            throw new BadRequestException(errors);
        }

        var diary = await appointmentRepository.CreateSeveralAppointmentsAsync(dto.Dates.ToArray(), dto.StartHour, dto.EndHour, dto.DurationId, dto.DoctorId, dto.SpecialityId, loggedUserId);
        return mapper.Map<int>(diary);
    }

    public async Task<AppointmentResponseDto> RescheduleToSlotAsync(RescheduleToSlotRequestDto dto)
    {
        var validationResult = await rescheduleToSlotValidatorDto.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(" | ", validationResult.Errors.Select(e => e.ErrorMessage));
            throw new BadRequestException(errors);
        }

        var reschedule = await appointmentRepository.RescheduleToSlotAsync(dto.OldAppointmentId, dto.NewAppointmentId);

        // Al liberar el slot viejo, borramos su room en Daily y limpiamos las columnas en DB.
        // El nuevo slot NO se auto-provisiona (evita consumir cupo mensual sin decisión explícita);
        // el admin debe llamar POST /appointments/{id}/video/provision si quiere videollamada.
        try
        {
            await videoCallService.DeprovisionAsync(dto.OldAppointmentId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "No se pudo deprovisionar la videollamada de la cita {OldId} durante el reagendamiento",
                dto.OldAppointmentId);
        }

        return mapper.Map<AppointmentResponseDto>(reschedule);
    }

    public async Task<IEnumerable<AppointmentResponseDto>> GetAllAsync(string dateSelected, int? specialityId, int? doctorId, bool? isOccuped, int? limit, int? offset)
    {
        var appointments = await appointmentRepository.GetAllAsync(dateSelected, specialityId, doctorId, isOccuped, limit, offset);
        return mapper.Map<IEnumerable<AppointmentResponseDto>>(appointments);
    }

    public async Task<AppointmentResponseDto> GetByIdAsync(int id)
    {
        var appointment = await appointmentRepository.GetByIdAsync(id);
        return mapper.Map<AppointmentResponseDto>(appointment) ?? throw new NotFoundException("Cita no encontrada");
    }

    public async Task<IEnumerable<AppointmentResponseDto>> GetByPatientNeedAppointmentAsync(int specialityId, string startHour, string dateSelected)
    {
        var appointments = await appointmentRepository.GetByPatientNeedAppointmentAsync(specialityId, startHour, dateSelected);
        return mapper.Map<IEnumerable<AppointmentResponseDto>>(appointments);
    }

    public async Task<IEnumerable<OccupationAppointmentsPerMonthResponseDto>> GetOccupationAppointmentsPerMonthAsync(int year, int month, int? doctorId)
    {
        var days = await appointmentRepository.GetOccupationAppointmentsPerMonthAsync(year, month, doctorId);
        return mapper.Map<IEnumerable<OccupationAppointmentsPerMonthResponseDto>>(days);
    }

    public async Task<IEnumerable<AppointmentResponseDto>> GetRecommendationsForPatientAsync(int specialityId, string dateSelected)
    {
        var appointments = await appointmentRepository.GetRecommendationsForPatientAsync(specialityId, dateSelected);
        return mapper.Map<IEnumerable<AppointmentResponseDto>>(appointments);
    }

    public async Task SendConfirmationEmailAsync(PatientResponseDto patientResponseDto, OfficeResponseDto officeResponseDto, AppointmentResponseDto appointmentResponseDto, string? videoCallLink = null)
    {
        var templateDto = new AppointmentConfirmationEmailDto
        {
            ToName = $"{patientResponseDto.Name} {patientResponseDto.Lastname}",
            OfficeName = officeResponseDto.Name,
            OfficeNit = officeResponseDto.Nit,
            OfficeAddress = officeResponseDto.Address,
            OfficeBrandUrl = officeResponseDto.Brand,
            DoctorName = $"Dr. {appointmentResponseDto.Doctor.Name} {appointmentResponseDto.Doctor.LastName}".Trim(),
            SpecialityName = appointmentResponseDto.Speciality.Name,
            DateAppointment = appointmentResponseDto.DateAppointment,
            StartHour = appointmentResponseDto.StartHour,
            EndHour = appointmentResponseDto.EndHour,
            VideoCallLink = videoCallLink
        };

        var htmlContent = AppointmentEmailTemplateHelper
            .BuildConfirmationEmail(templateDto);

        await mailSenderService.SendEmailAssignationAsync(
            patientResponseDto.Email.Trim(),
            templateDto.ToName,
            "Confirmación de cita médica",
            htmlContent
        );
    }
    
    public async Task SendReminderEmailAsync(PatientResponseDto patientResponseDto, OfficeResponseDto officeResponseDto, AppointmentResponseDto appointmentResponseDto, string? videoCallLink = null)
    {
        var templateDto = new AppointmentReminderEmailDto
        {
            ToName = $"{patientResponseDto.Name} {patientResponseDto.Lastname}",
            OfficeName = officeResponseDto.Name,
            OfficeNit = officeResponseDto.Nit,
            OfficeAddress = officeResponseDto.Address,
            OfficeBrandUrl = officeResponseDto.Brand,
            DoctorName = $"Dr. {appointmentResponseDto.Doctor.Name} {appointmentResponseDto.Doctor.LastName}".Trim(),
            SpecialityName = appointmentResponseDto.Speciality.Name,
            DateAppointment = appointmentResponseDto.DateAppointment,
            StartHour = appointmentResponseDto.StartHour,
            EndHour = appointmentResponseDto.EndHour,
            VideoCallLink = videoCallLink
        };

        var htmlContent = AppointmentEmailTemplateHelper
            .BuildReminderEmail(templateDto);

        await mailSenderService.SendEmailReminderAsync(
            patientResponseDto.Email.Trim(),
            templateDto.ToName,
            "Recordatorio de cita médica",
            htmlContent
        );
    }

    public async Task<IEnumerable<AppointmentResponseDto>> GetAllByUserAsync(string dateSelected, int? specialityId, bool? isOccuped, int? limit, int? offset)
    {
        var loggedUserId = currentUserService.UserId
            ?? throw new UnauthorizedException("No te encuentras autenticado");

        var appointments = await appointmentRepository.GetAllByUserAsync(dateSelected, loggedUserId, specialityId, isOccuped, limit, offset);
        return mapper.Map<IEnumerable<AppointmentResponseDto>>(appointments);
    }

    public async Task<AppointmentVideoProvisionResultDto> ProvisionVideoCallAsync(int appointmentId)
    {
        var appointment = await GetByIdAsync(appointmentId);

        if (appointment.Patient is null)
            throw new BadRequestException("La cita no tiene paciente asignado");

        var patientDisplayName = $"{appointment.Patient.Name} {appointment.Patient.Lastname}";
        var result = await videoCallService.ProvisionForAsync(appointmentId, patientDisplayName);

        var patientInfo = await patientService.GetByIdAsync(appointment.Patient.Id);
        if (patientInfo != null)
        {
            var userInfo = await userService.GetByIdAsync(appointment.UserId);
            var officeInfo = await officeService.GetByIdAsync(userInfo!.Office.Id);
            await SendConfirmationEmailAsync(patientInfo, officeInfo, appointment, result.PatientLink);
        }

        return result;
    }

    public async Task<DoctorMeetingTokenResultDto> IssueDoctorMeetingTokenAsync(int appointmentId)
    {
        var appointment = await GetByIdAsync(appointmentId);
        var doctorDisplayName = $"Dr. {appointment.Doctor.Name} {appointment.Doctor.LastName}".Trim();
        return await videoCallService.IssueDoctorTokenAsync(appointmentId, doctorDisplayName);
    }

    public async Task DeprovisionVideoCallAsync(int appointmentId)
    {
        _ = await GetByIdAsync(appointmentId);
        await videoCallService.DeprovisionAsync(appointmentId);
    }
}
