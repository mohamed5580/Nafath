using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Domin.Entity
{
    public class ApplicationUser : IdentityUser
    {


        [MaxLength(100)]
        public string? FullName { get; set; }

        [MaxLength(100)]
        public string? LastName { get; set; }

        public bool? AcceptTerms { get; set; }

        /// <summary>
        /// Raw bytes if you choose to store the image in the DB
        /// </summary>
        public byte[]? ProfilePicture { get; set; }

        /// <summary>
        /// URL or path to the stored avatar image (e.g. “/uploads/avatars/…png”)
        /// </summary>
        [MaxLength(256)]
        public string? AvatarFile { get; set; } = "user1.jpg";


        [MaxLength(10)]
        public string? Gender { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [MaxLength(10)]
        public string? AgeRange { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }


    }
}
