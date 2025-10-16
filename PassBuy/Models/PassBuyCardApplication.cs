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
    public class PassBuyCardApplication
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey("User")]
        public Guid UserId { get; set; }

        [Required]
        public CardType CardType { get; set; }

        public DateTime DateApplied { get; set; } = DateTime.UtcNow;

        public string status { get; set; } = "Pending";

        // Details should be provided according to the type of application
        // For Education Concession: EducationDetails
        // For Youth and Pensioner Concession: GovIDDetails
        // For TransportEmployee: TransportEmployer Details
        // Providing details that do not match with the concession type is disallowed
        public EducationDetails? EducationDetails { get; set; }

        public GovIdDetails? GovIdDetails { get; set; }

        public TransportEmploymentDetails? TransportEmploymentDetails { get; set; }

        // Add conditions
        public void Validate()
        {
            // Disallow adding details that do not match with the Concession Type
            if (CardType != CardType.EducationConcession && EducationDetails != null)
                throw new InvalidOperationException(
                    "EducationDetails have been provided, or are present in the table, " +
                    "but the ConcessionType is not Education.");
            if ((CardType != CardType.YouthConcession || CardType == CardType.PensionerConcession)
                && GovIdDetails != null)
                throw new InvalidOperationException(
                    "GovIDDetails have been provided, or are present in the table, " +
                    "but the ConcessionType is not Youth or Pensioner.");
            if (CardType != CardType.TransportEmployeeConcession && TransportEmploymentDetails != null)
                throw new InvalidOperationException(
                    "TransportEmployeeDetails have been provided, or are present in the table, " +
                    "but the ConcessionType is not Transport Employee.");
        }
    }
}
