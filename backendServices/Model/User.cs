namespace backendServices.Model
{
    public class User
    {
        public required string firstName { get; set; }
        public required string lastName { get; set; }
        public required int age { get; set; }
        public required string email { get; set; }
        public required string password { get; set; }
    }
}