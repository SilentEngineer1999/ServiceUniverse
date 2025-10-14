using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PassBuy.Models
{
    public class GovIdDetails
    {
        [Key, ForeignKey("ConcessionApplication")] // PK = FK because the relationship is 1:1
        public ConcessionApplication ConcessionApplication { get; set; } = null!;

        [Required]
        public DateTime BirthDate { get; set; } = null!;
    }
}
