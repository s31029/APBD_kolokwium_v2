using System.ComponentModel.DataAnnotations;

namespace WebApplication1.Models.DTOs
{
    public class ServiceInputDto
    {
        [Required]
        public string ServiceName { get; set; } = string.Empty;

        [Range(0.01, double.MaxValue)]
        public decimal ServiceFee { get; set; }
    }

    public class CreateAppointmentDto
    {
        [Required]
        public int AppointmentId { get; set; }

        [Required]
        public int PatientId { get; set; }

        [Required]
        [StringLength(7)]
        public string Pwz { get; set; } = string.Empty;

        [Required]
        [MinLength(1)]
        public List<ServiceInputDto> Services { get; set; } = new List<ServiceInputDto>();
    }
}