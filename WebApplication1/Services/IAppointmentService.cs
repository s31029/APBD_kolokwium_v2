using WebApplication1.Models.DTOs;

namespace WebApplication1.Services
{
    public interface IAppointmentService
    {
        Task<AppointmentDetailsDto> GetByIdAsync(int appointmentId);
        Task AddAsync(CreateAppointmentDto dto);
    }
}