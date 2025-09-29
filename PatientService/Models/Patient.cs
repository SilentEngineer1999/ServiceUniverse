using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PatientService.Models;

public class Patient
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int PatientId { get; set; }

    [Required, MaxLength(120)]
    public string FullName { get; set; } = default!;

    [Required]
    public DateOnly DateOfBirth { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    [EmailAddress, MaxLength(200)]
    public string? Email { get; set; }

    [MaxLength(50)]
    public string? CitizenId { get; set; }
}