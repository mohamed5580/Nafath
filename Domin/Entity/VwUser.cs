namespace Domin.Entity
{

    public class VwUsers
    {
        public string Id { get; set; }
        public string Name { get; set; }  
        public string Email { get; set; }
        public string? Role { get; set; }
        public bool AcceptTerms { get; set; } 
        public string AvatarUrl { get; set; }
    }


}