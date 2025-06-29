using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nafath.ViewModel
{
    public class RolesViewModel
    {
        public List<IdentityRole> Roles { get; set; }
        public NewRoles NewRole { get; set; }

    }
    public class NewRoles
    {
        public string RoleId { get; set; } // ID of the role, used for editing existing roles
        [Required(ErrorMessageResourceType =typeof(Resource.ResourceData),ErrorMessageResourceName = "RoleName" )]
        public string RoleName { get; set; } // Name of the new role to be created
    }
}
