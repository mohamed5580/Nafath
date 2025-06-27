using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Nafath.Models
{
    using Microsoft.AspNetCore.Identity;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Diagnostics.CodeAnalysis;

    public class ApplicationUser : IdentityUser
    {
        [MaxLength(100)]
        public string FullName { get; set; }

        [MaxLength(100)]
        public string LastName { get; set; }

        public bool AcceptTerms { get; set; }

        /// <summary>
        /// Raw bytes if you choose to store the image in the DB
        /// </summary>
        public byte[]? ProfilePicture { get; set; }

        /// <summary>
        /// URL or path to the stored avatar image (e.g. “/uploads/avatars/…png”)
        /// </summary>
        [MaxLength(256)]
        public string? AvatarUrl { get; set; }

        /// <summary>
        /// Used only for uploading; EF will ignore this
        /// </summary>
        [NotMapped]
        public IFormFile? AvatarFile { get; set; }

        // — your other profile fields —

        [MaxLength(50)]
        public string? City { get; set; }

        [MaxLength(10)]
        public string? Gender { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [MaxLength(20)]
        public string? MaritalStatus { get; set; }

        [MaxLength(10)]
        public string? AgeRange { get; set; }

        [MaxLength(50)]
        public string? Country { get; set; }

        [MaxLength(50)]
        public string? State { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }
    }


}
