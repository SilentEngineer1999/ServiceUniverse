using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PassBuy.Models
{
    public class EducationProvider
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public string EduCode { get; set; } = null!;

        [Required]
        public string Name { get; set; } = null!;
    }
}
