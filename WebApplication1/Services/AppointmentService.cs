using Microsoft.Data.SqlClient;
using WebApplication1.Models.DTOs;
using WebApplication1.Exceptions;

namespace WebApplication1.Services
{
    public class AppointmentService : IAppointmentService
    {
        private readonly string _connectionString;

        public AppointmentService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("Default") ?? string.Empty;
        }

        public async Task<AppointmentDetailsDto> GetByIdAsync(int appointmentId)
        {
            const string sql = @"
                SELECT a.date, p.first_name, p.last_name, p.date_of_birth,
                       d.doctor_id, d.pwz,
                       s.name, aps.service_fee
                  FROM Appointment a
                  JOIN Patient p ON a.patient_id = p.patient_id
                  JOIN Doctor d ON a.doctor_id = d.doctor_id
                  LEFT JOIN Appointment_Service aps ON a.appoitment_id = aps.appoitment_id
                  LEFT JOIN Service s ON aps.service_id = s.service_id
                 WHERE a.appoitment_id = @Id;";

            var dto = new AppointmentDetailsDto();
            bool found = false;
            await using var conn = new SqlConnection(_connectionString);
            await using var cmd  = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Id", appointmentId);
            await conn.OpenAsync();

            await using var rdr = await cmd.ExecuteReaderAsync();
            while (await rdr.ReadAsync())
            {
                if (!found)
                {
                    dto.Date = rdr.GetDateTime(0);
                    dto.Patient = new PatientDto
                    {
                        FirstName = rdr.GetString(1),
                        LastName = rdr.GetString(2),
                        DateOfBirth = rdr.GetDateTime(3)
                    };
                    dto.Doctor = new DoctorDto
                    {
                        DoctorId = rdr.GetInt32(4),
                        Pwz = rdr.GetString(5)
                    };
                    found = true;
                }
                if (!rdr.IsDBNull(6))
                {
                    dto.AppointmentServices.Add(new AppointmentServiceDto
                    {
                        Name = rdr.GetString(6),
                        ServiceFee = rdr.GetDecimal(7)
                    });
                }
            }

            if (!found)
                throw new NotFoundException($"Wizyta o ID={appointmentId} nie istnieje.");

            return dto;
        }

        public async Task AddAsync(CreateAppointmentDto dto)
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var tx = conn.BeginTransaction();
            try
            {
                await using var chkA = new SqlCommand(
                    "SELECT COUNT(1) FROM Appointment WHERE appoitment_id=@Id", conn, tx);
                chkA.Parameters.AddWithValue("@Id", dto.AppointmentId);
                if ((int)await chkA.ExecuteScalarAsync() > 0)
                    throw new ConflictException($"Wizyta o ID={dto.AppointmentId} już istnieje.");
                
                await using var chkP = new SqlCommand(
                    "SELECT COUNT(1) FROM Patient WHERE patient_id=@Pid", conn, tx);
                chkP.Parameters.AddWithValue("@Pid", dto.PatientId);
                if ((int)await chkP.ExecuteScalarAsync() == 0)
                    throw new NotFoundException($"Pacjent o ID={dto.PatientId} nie istnieje.");
                
                await using var getD = new SqlCommand(
                    "SELECT doctor_id FROM Doctor WHERE pwz=@Pwz", conn, tx);
                getD.Parameters.AddWithValue("@Pwz", dto.Pwz);
                var docObj = await getD.ExecuteScalarAsync();
                if (docObj is null)
                    throw new NotFoundException($"Lekarz o PWZ={dto.Pwz} nie znaleziony.");
                int doctorId = (int)docObj;
                
                await using var insA = new SqlCommand(@"
                    INSERT INTO Appointment(appoitment_id, patient_id, doctor_id, date)
                    VALUES(@Id,@Pid,@Did,GETDATE())", conn, tx);
                insA.Parameters.AddWithValue("@Id", dto.AppointmentId);
                insA.Parameters.AddWithValue("@Pid", dto.PatientId);
                insA.Parameters.AddWithValue("@Did", doctorId);
                await insA.ExecuteNonQueryAsync();
                
                foreach (var srv in dto.Services)
                {
                    await using var getS = new SqlCommand(
                        "SELECT service_id FROM Service WHERE name=@Name", conn, tx);
                    getS.Parameters.AddWithValue("@Name", srv.ServiceName);
                    var sid = await getS.ExecuteScalarAsync();
                    if (sid is null)
                        throw new NotFoundException($"Usługa '{srv.ServiceName}' nie istnieje.");

                    await using var insAS = new SqlCommand(@"
                        INSERT INTO Appointment_Service(appoitment_id, service_id, service_fee)
                        VALUES(@Aid,@Sid,@Fee)", conn, tx);
                    insAS.Parameters.AddWithValue("@Aid", dto.AppointmentId);
                    insAS.Parameters.AddWithValue("@Sid", (int)sid!);
                    insAS.Parameters.AddWithValue("@Fee", srv.ServiceFee);
                    await insAS.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
    }
}
