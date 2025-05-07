using Microsoft.AspNetCore.Mvc;
using WebApplication1.Services;
using WebApplication1.Models.DTOs;
using WebApplication1.Exceptions;

namespace WebApplication1.Controllers
{
    [Route("api/appointments")]
    [ApiController]
    public class AppointmentsController : ControllerBase
    {
        private readonly IAppointmentService _svc;
        public AppointmentsController(IAppointmentService svc) => _svc = svc;

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var dto = await _svc.GetByIdAsync(id);
                return Ok(dto);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateAppointmentDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                await _svc.AddAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = dto.AppointmentId }, new { dto.AppointmentId });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { Message = ex.Message });
            }
            catch (ConflictException ex)
            {
                return Conflict(new { Message = ex.Message });
            }
        }
    }
}