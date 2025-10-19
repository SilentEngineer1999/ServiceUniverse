using System.ComponentModel.DataAnnotations;

namespace HealthApi.Models
{
    // ===== Models / DTOs =====
    public class User
    {
        [Key] public string Id { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string PasswordHash { get; set; } = default!;
        public string PasswordSalt { get; set; } = default!;
        public string Role { get; set; } = "patient";
    }

    public class Doctor
    {
        [Key] public string Id { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string Specialty { get; set; } = default!;
        public List<string> Slots { get; set; } = new();
    }

    public class Appointment
    {
        [Key] public string Id { get; set; } = default!;
        public string PatientName { get; set; } = default!;
        public string DoctorId { get; set; } = default!;
        public string Time { get; set; } = default!;
    }

    public class RefreshToken
    {
        [Key] public int Id { get; set; }
        public string Token { get; set; } = default!;
        public string UserId { get; set; } = default!;
        public DateTime CreatedUtc { get; set; }
    }

    public record CreateAppointmentDto
    {
        public string PatientName { get; set; } = default!;
        public string DoctorId { get; set; } = default!;
        public string Time { get; set; } = default!;
    }

    public record AppointmentDto
    {
        public string Id { get; set; } = default!;
        public string PatientName { get; set; } = default!;
        public string DoctorId { get; set; } = default!;
        public string? DoctorName { get; set; }
        public string Time { get; set; } = default!;
    }

    public record SignupDto(string Name, string Email, string Password, string? Role);
    public record LoginDto(string Email, string Password);
    public record RefreshDto(string RefreshToken);
}
