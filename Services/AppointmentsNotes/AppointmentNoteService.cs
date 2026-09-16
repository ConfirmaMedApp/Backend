using AutoMapper;
using Backend.DTOs.AppointmentsNotes.Requests;
using Backend.DTOs.AppointmentsNotes.Responses;
using Backend.Entities.AppointmentsNotes;
using Backend.Exceptions.BadRequest;
using Backend.Exceptions.NotFound;
using Backend.Exceptions.Unauthorized;
using Backend.Repositories.AppointmentsNotes;
using Backend.Services.CurrentUser;
using FluentValidation;

namespace Backend.Services.AppointmentsNotes;

public class AppointmentNoteService(IAppointmentNoteRepository appointmentNoteRepository, IMapper mapper, IValidator<AppointmentNoteRequestCreateDto> createValidatorDto, ICurrentUserService currentUserService) : IAppointmentNoteService
{
    public async Task<AppointmentNoteResponseDto?> CreateAsync(AppointmentNoteRequestCreateDto dto)
    {
        var validationResult = await createValidatorDto.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(" | ", validationResult.Errors.Select(e => e.ErrorMessage));
            throw new BadRequestException(errors);
        }

        var loggedUserId = currentUserService.UserId
            ?? throw new UnauthorizedException("No te encuentras autenticado");

        var appointmentNote = mapper.Map<AppointmentNote>(dto);
        appointmentNote.UserId = loggedUserId;

        var createAppointmentNote = await appointmentNoteRepository.CreateAsync(appointmentNote);
        return createAppointmentNote is null
            ? throw new BadRequestException("No se ha podido crear la nota de la cita")
            : mapper.Map<AppointmentNoteResponseDto>(createAppointmentNote);
    }

    public async Task<IEnumerable<AppointmentNoteResponseDto>> GetAllAsync(int diaryId)
    {
        var appointmentNotes = await appointmentNoteRepository.GetAllAsync(diaryId);
        return mapper.Map<IEnumerable<AppointmentNoteResponseDto>>(appointmentNotes);
    }

    public async Task<IEnumerable<AppointmentNoteResponseDto>> GetAllByPatientAsync(int patientId)
    {
        var appointmentNotes = await appointmentNoteRepository.GetAllByPatientAsync(patientId);
        return mapper.Map<IEnumerable<AppointmentNoteResponseDto>>(appointmentNotes);
    }

    public async Task<AppointmentNoteResponseDto?> GetByIdAsync(int id)
    {
        var appointmentNote = await appointmentNoteRepository.GetByIdAsync(id);
        return appointmentNote is null ? throw new NotFoundException("Nota de cita no encontrada") : mapper.Map<AppointmentNoteResponseDto>(appointmentNote);
    }
}