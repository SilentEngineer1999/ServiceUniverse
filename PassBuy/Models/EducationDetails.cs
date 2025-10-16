using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PassBuy.Models
{
    public class EducationDetails
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("PassBuyCardApplication")]
        public int ApplicationId { get; set; }

        [ForeignKey("EducationProvider")]
        public int EducationProviderId { get; set; }

        [Required]
        public int StudentNumber { get; set; }

        [Required]
        public int CourseCode { get; set; }

        [Required]
        public string CourseTitle { get; set; } = null!;
    }
}
