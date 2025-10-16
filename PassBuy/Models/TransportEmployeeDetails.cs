using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PassBuy.Models
{
    public class TransportEmploymentDetails
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("PassBuyCardApplication")]
        public int ApplicationId { get; set; }

        [ForeignKey("TransportEmployer")]
        public Guid TransportEmployerId { get; set; }

        [Required]
        public int EmployeeNumber { get; set; }
    }
}
