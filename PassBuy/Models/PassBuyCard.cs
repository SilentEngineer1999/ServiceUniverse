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

        // Top-up settings
        public string TopUpMode { get; set; } = "manual"; // manual | auto | scheduled

        public decimal? AutoThreshold { get; set; }

        public decimal? TopUpAmount { get; set; }

        public string? TopUpSchedule { get; set; } // weekly | monthly

        public string BankAccount { get; set; }
    }
}
