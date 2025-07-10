using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Infrastructure.Models;
using Domin.Entity;

namespace Domin.Entity
{
    public partial class order
    {
        public ApplicationUser User { get; set; }
    }
}
