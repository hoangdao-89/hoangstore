using Microsoft.EntityFrameworkCore;
using hoangstore.Models;
using Microsoft.Extensions.DependencyInjection;

namespace hoangstore.Data
{
    public static class MockSeeder
    {
        public static void SeedTestData(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

            // 1. Nếu đã có sản phẩm thì dừng, không làm trùng dữ liệu
            if (context.Products.Any())
            {
                return;
            }

            // 2. TẠO MỒI CATEGORY TRƯỚC
            var defaultCategory = context.Categories.FirstOrDefault(c => c.Category_Name == "Áo Local Brand");
            if (defaultCategory == null)
            {
                defaultCategory = new Category
                {
                    Category_Name = "Áo Local Brand",
                    IsDelete = false,
                    CreatedDate = DateTime.Now,
                    CreatedBy = "System"
                };
                context.Categories.Add(defaultCategory);
                context.SaveChanges();
            }

            int cateId = defaultCategory.CategoryId;

            // 3. TẠO SẢN PHẨM 1
            var p1 = new Product
            {
                Product_Name = "Áo T-Shirt Local Brand Oversize Cotton 100%",
                Product_Description = "Chất liệu cotton 100% co giãn 4 chiều, dày dặn, thấm hút mồ hôi cực tốt.",
                Price = 299000,
                Quantity = 35,
                Image_Url = "https://images.unsplash.com/photo-1617137968427-85924c800a22?q=80&w=500&auto=format&fit=crop",
                IsFeatured = true,
                CreatedDate = DateTime.Now,
                CreatedBy = "Hoàng Đào Xuân",
                IsDelete = false,
                DeleteDate = DateTime.Now,
                CategoryId = cateId
            };
            context.Products.Add(p1);
            context.SaveChanges();

            var variantsOfP1 = new List<ProductVariant>
            {
                new ProductVariant { ProductId = p1.ProductId, Size = "M", Color = "Đen", Price = 299000, Quantity = 15 },
                new ProductVariant { ProductId = p1.ProductId, Size = "L", Color = "Đen", Price = 299000, Quantity = 20 }
            };
            context.ProductVariants.AddRange(variantsOfP1);

            // 4. TẠO SẢN PHẨM 2
            var p2 = new Product
            {
                Product_Name = "Áo Hoodie Premium Heavyweight Black",
                Product_Description = "Áo hoodie nỉ bông định lượng cao, form dáng boxy trendy.",
                Price = 450000,
                Quantity = 13,
                Image_Url = "https://images.unsplash.com/photo-1556821840-3a63f95609a7?q=80&w=500&auto=format&fit=crop",
                IsFeatured = false,
                CreatedDate = DateTime.Now,
                CreatedBy = "Hoàng Đào Xuân",
                IsDelete = false,
                DeleteDate = DateTime.Now,
                CategoryId = cateId
            };
            context.Products.Add(p2);
            context.SaveChanges();

            var variantsOfP2 = new List<ProductVariant>
            {
                new ProductVariant { ProductId = p2.ProductId, Size = "L", Color = "Đen", Price = 450000, Quantity = 8 },
                new ProductVariant { ProductId = p2.ProductId, Size = "XL", Color = "Đen", Price = 450000, Quantity = 5 }
            };
            context.ProductVariants.AddRange(variantsOfP2);

            // CHỐT SỔ LƯU TOÀN BỘ XUỐNG SQL SERVER
            context.SaveChanges();
        }
    }
}