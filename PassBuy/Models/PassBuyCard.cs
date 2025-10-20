using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PassBuy.Models
{
    public class PassBuyCard
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("User")]
        public Guid UserId { get; set; }

        [Required]
        public CardType CardType { get; set; }

        public DateTime DateApproved { get; set; } = DateTime.UtcNow;

        public PassBuyCardApplication? Application { get; set; }
    }
}
