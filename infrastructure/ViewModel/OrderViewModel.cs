using Domin.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.ViewModel
{
    public class OrderDetailsViewModel
    {
        public Order Order { get; set; }

        public List<OrderItemDetailDto> Items { get; set; }
    }

}
