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

        if (patientInfo != null)
        {
            await SendConfirmationEmailAsync(patientInfo, officeInfo, appointmentInfo);
        }

        return mapper.Map<AppointmentResponseDto>(appointment);
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

    public async Task SendConfirmationEmailAsync(PatientResponseDto patientResponseDto, OfficeResponseDto officeResponseDto, AppointmentResponseDto appointmentResponseDto)
    {
        var templateDto = new AppointmentConfirmationEmailDto
        {
            ToName = $"{patientResponseDto.Name} {patientResponseDto.Lastname}",
            OfficeName = officeResponseDto.Name,
            OfficeNit = officeResponseDto.Nit,
            OfficeAddress = officeResponseDto.Address,
            OfficeBrandUrl = officeResponseDto.Brand,
            DateAppointment = appointmentResponseDto.DateAppointment,
            StartHour = appointmentResponseDto.StartHour,
            EndHour = appointmentResponseDto.EndHour
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
    
    public async Task SendReminderEmailAsync(PatientResponseDto patientResponseDto, OfficeResponseDto officeResponseDto, AppointmentResponseDto appointmentResponseDto)
    {
        var templateDto = new AppointmentReminderEmailDto
        {
            ToName = $"{patientResponseDto.Name} {patientResponseDto.Lastname}",
            OfficeName = officeResponseDto.Name,
            OfficeNit = officeResponseDto.Nit,
            OfficeAddress = officeResponseDto.Address,
            OfficeBrandUrl = officeResponseDto.Brand,
            DateAppointment = appointmentResponseDto.DateAppointment,
            StartHour = appointmentResponseDto.StartHour,
            EndHour = appointmentResponseDto.EndHour
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
}
