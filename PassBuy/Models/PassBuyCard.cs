using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PassBuy.Models
{
    public enum CardType
    {
        Standard,
        EducationConcession,
        YouthConcession,
        PensionerConcession,
        TransportEmployeeConcession
    }

    public class PassBuyCard
    {
        [Key]
        public int Id { get; set; } = null!;

        [ForeignKey("User")]
        public User User { get; set; } = null!;

        [Required]
        public CardType CardType { get; set; } = null!;

        public DateTime DateApproved { get; set; } = DateTime.UtcNow;

        [ForeignKey("ConcessionApplication")]
        public ConcessionApplication? ConcessionApplication { get; set; } = null!;
    }
}
