namespace backendServices.Models
{
    public class Utility
    {
        public int UtilityId { get; set; }
        public int UserId { get; set; }   // comes from JWT token
        public int GasUsage { get; set; }
        public int GasRate { get; set; }
        public int WaterUsage { get; set; }
        public int WaterRate { get; set; }
        public int ElectricityUsage { get; set; }
        public int ElectricityRate { get; set; }
        public DateTime DueDate { get; set; }
        public int Penalty { get; set; }
        public string Status { get; set; } = "unpaid";

        public int TotalBill =>
            GasUsage * GasRate + WaterUsage * WaterRate + ElectricityUsage * ElectricityRate;
    }
}
