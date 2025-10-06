namespace ABC_Retail.Services
{
    public class CustomerDto
    {
        public string id { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        public string surname { get; set; } = string.Empty;
        public string username { get; set; } = string.Empty;
        public string email { get; set; } = string.Empty;
        public string shippingAddress { get; set; } = string.Empty;
    }

    public class OrderDto
    {
        public string id { get; set; } = string.Empty;
        public string details { get; set; } = string.Empty;
        public string status { get; set; } = string.Empty;
        public DateTimeOffset orderDate { get; set; }
    }

    public class ProductDto
    {
        public string id { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        public string description { get; set; } = string.Empty;
        public double price { get; set; }
        public int stock { get; set; }
        public string imageUrl { get; set; } = string.Empty;
    }
}
