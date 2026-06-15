using Domin.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.ViewModel
{
    public class OrderManagerDetailsViewModel
    {

        public Order Order { get; set; }

        public List<OrderItemDetailDto> Items { get; set; }

        // بيانات المستخدم
        public string UserName { get; set; }
        public string UserEmail { get; set; }
        public string UserPhone { get; set; }
        public string UserAdress { get; set; }
        public string PaymentMethod { get; set; }
    }
    public class OrderItemManagerUpdateDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }

    public class UpdateOrderManagerViewModel
    {
        public int OrderId { get; set; }
        public List<OrderItemUpdateDto> Items { get; set; }
    }

}
