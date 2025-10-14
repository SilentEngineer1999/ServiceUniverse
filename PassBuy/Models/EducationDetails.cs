using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PassBuy.Models
{
    public class EducationDetails
    {
        [Key, ForeignKey("ConcessionApplication")] // PK = FK because the relationship is 1:1
        public ConcessionApplication ConcessionApplication { get; set; } = null!;

        [ForeignKey("EducationProvider")]
        public EducationProvider EducationProvider { get; set; } = null!;

        [Required]
        public int StudentNumber { get; set; } = null!;

        [Required]
        public int CourseCode { get; set; } = null!;

        [Required]
        public string CourseTitle { get; set; } = null!;
    }
}
