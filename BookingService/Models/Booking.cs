using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookingService.Models;

public class Booking
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int BookingId { get; set; }

    [Required]
    public int PatientId { get; set; }

    [Required]
    public DateTimeOffset TestDateTime { get; set; }

    [Required, MaxLength(20)]
    public string Status { get; set; } = "Scheduled";
}