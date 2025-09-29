using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EnrolmentService.Models;

public class Enrolment
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int EnrolmentId { get; set; }

    [Required]
    public int StudentId { get; set; }

    [Required, MaxLength(20)]
    public string CourseCode { get; set; } = default!;

    [Required, MaxLength(20)]
    public string Status { get; set; } = "Active";
}