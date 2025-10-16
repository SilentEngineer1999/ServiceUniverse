using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PassBuy.Models
{
    public class GovIdDetails
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("PassBuyCardApplication")]
        public int ApplicationId { get; set; }

        [Required]
        public DateTime BirthDate { get; set; }
    }
}
