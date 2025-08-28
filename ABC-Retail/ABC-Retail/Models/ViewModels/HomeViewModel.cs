using System.Collections.Generic;

namespace ABC_Retail.Models.ViewModels
{
    public class HomeViewModel
    {
        public IEnumerable<ABC_Retail.Models.Product> FeaturedProducts { get; set; } = new List<ABC_Retail.Models.Product>();
        public int CustomerCount { get; set; }
        public int ProductCount { get; set; }
        public int OrderCount { get; set; }
        public int UploadCount { get; set; }
       
    }
}
