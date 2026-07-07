namespace Domin.Entity
{

    public class VwUsers
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Role { get; set; }
        public bool AcceptTerms { get; set; } 
        public string? AvatarFile { get; set; }
    }


}
