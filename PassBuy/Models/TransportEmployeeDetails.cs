using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PassBuy.Models
{
    public class TransportEmploymentDetails
    {
        [Key, ForeignKey("ConcessionApplication")] // PK = FK because the relationship is 1:1
        public ConcessionApplication ConcessionApplication { get; set; } = null!;

        [ForeignKey("TransportEmployer")]
        public TransportEmployer TransportEmployer { get; set; } = null!;

        [Required]
        public int EmployeeNumber { get; set; } = null!;
    }
}
