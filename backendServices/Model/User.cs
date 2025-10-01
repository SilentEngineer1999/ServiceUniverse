namespace backendServices.Model
{
    public class User
    {
        public int id { get; set; }
        public string firstName { get; set; } = "";
        public string lastName { get; set; } = "";
        public int age { get; set; }
        public string email { get; set; } = "";
        public string password { get; set; } = "";
    }
}