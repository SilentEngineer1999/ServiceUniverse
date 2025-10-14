using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PassBuy.Models
{
    public enum ConcesionType
    {
        Education,
        Youth,
        Pensioner,
        TransportEmployee
    }
    public class ConcessionApplication
    {
        [Key]
        public int Id { get; set; } = null!;

        [ForeignKey("PassBuyCard")]
        public PassBuyCard PassBuyCard { get; set; } = null!;

        [Required]
        public ConcessionType ConcessionType { get; set; } = null!;

        public DateTime DateApplied { get; set; } = DateTime.UtcNow;

        public string status { get; set; } = "Pending";

        // Details should be provided according to the type of application
        // For Education Concession: EducationDetails
        // For Youth and Pensioner Concession: GovIDDetails
        // For TransportEmployee: TransportEmployer Details
        // Providing details that do not match with the concession type is disallowed
        [ForeignKey("EducationDetails")]
        public EducationDetails? EducationDetails { get; set; }

        public GovIdDetails? GovIdDetails { get; set; }

        public TransportEmploymentDetails? TransportEmploymentDetails { get; set; }

        // Add conditions
        public void Validate()
        {
            // Disallow adding details that do not match with the Concession Type
            if (ConcessionType != ConcessionType.Education && EducationDetails != null)
                throw new InvalidOperationException(
                    "EducationDetails have been provided, or are present in the table, " +
                    "but the ConcessionType is not Education.");
            if ((ConcessionType != ConcessionType.Youth || ConcessionType == ConcessionType.Pensioner)
                && GovIdDetails != null)
                throw new InvalidOperationException(
                    "GovIDDetails have been provided, or are present in the table, " +
                    "but the ConcessionType is not Youth or Pensioner.");
            if (ConcessionType != ConcessionType.TransportEmployee && TransportEmployeeDetails != null)
                throw new InvalidOperationException(
                    "TransportEmployeeDetails have been provided, or are present in the table, " +
                    "but the ConcessionType is not Transport Employee.");
        }
    }
}
