using Microsoft.AspNetCore.Mvc.Rendering;

namespace ABC_Retail.Models.ViewModels
{
    public class OrderCreateViewModel
    {
        public string? CustomerId { get; set; }
        public string? ProductId { get; set; }
        public int Quantity { get; set; } = 1;
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Pending";

        public List<SelectListItem> Customers { get; set; } = new();
        public List<SelectListItem> Products { get; set; } = new();

        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }
}
