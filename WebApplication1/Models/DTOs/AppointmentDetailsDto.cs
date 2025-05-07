namespace WebApplication1.Models.DTOs
{
    public class AppointmentDetailsDto
    {
        public DateTime Date { get; set; }
        public PatientDto Patient { get; set; } = new PatientDto();
        public DoctorDto Doctor { get; set; } = new DoctorDto();
        public List<AppointmentServiceDto> AppointmentServices { get; set; } = new List<AppointmentServiceDto>();
    }
}