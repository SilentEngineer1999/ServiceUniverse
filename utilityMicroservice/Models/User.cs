namespace backendServices.Models
{
    public class User
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public int Age { get; set; }
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public byte[] Salt { get; set; } = null!;
    }
}
