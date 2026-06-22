using Microsoft.EntityFrameworkCore;
using hoangstore.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace hoangstore.Data
{
    public static class SeedData
    {
        public static void Initialize(IServiceProvider serviceProvider)
        {
            using (var context = new ApplicationDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>()))
            {
                // 1. Nếu đã có sản phẩm thì dừng, không làm trùng dữ liệu
                if (context.Products.Any())
                {
                    return;
                }

                // 2. TẠO MỒI CATEGORY TRƯỚC (Vì Product của Hoàng bắt buộc phải có CategoryId)
                var defaultCategory = context.Categories.FirstOrDefault(c => c.Category_Name == "Áo Local Brand");
                if (defaultCategory == null)
                {
                    defaultCategory = new Category
                    {
                        // Sửa lại tên thuộc tính của bảng Category nếu bên bạn viết khác nhé
                        Category_Name = "Áo Local Brand",
                        IsDelete = false,
                        CreatedDate = DateTime.Now,
                        CreatedBy = "System"
                    };
                    context.Categories.Add(defaultCategory);
                    context.SaveChanges(); // Lưu để sinh ra defaultCategory.Id (hoặc Category_Id) thật
                }

                // Lấy ID vừa sinh ra của Category
                // LƯU Ý: Nếu khóa chính bảng Category của Hoàng đặt tên là Category_Id thì sửa thành defaultCategory.Category_Id nhé
                int cateId = defaultCategory.Category_Id;

                // 3. TẠO SẢN PHẨM 1 (Khớp 100% thuộc tính Model của Hoàng)
                var p1 = new Product
                {
                    Product_Name = "Áo T-Shirt Local Brand Oversize Cotton 100%",
                    Product_Description = "Chất liệu cotton 100% co giãn 4 chiều, dày dặn, thấm hút mồ hôi cực tốt.",
                    Price = 299000,
                    Quantity = 35, // Tổng số lượng thô
                    Image_Url = "https://images.unsplash.com/photo-1617137968427-85924c800a22?q=80&w=500&auto=format&fit=crop",
                    IsFeatured = true,
                    CreatedDate = DateTime.Now,
                    CreatedBy = "Hoàng Đào Xuân",
                    IsDelete = false,
                    DeleteDate = DateTime.Now, // Điền bù vì trường này không cho phép NULL
                    CategoryId = cateId // Gán mã danh mục chuẩn chỉ
                };
                context.Products.Add(p1);
                context.SaveChanges(); // Lưu để kích nổ Product_Id cho p1

                // Bơm biến thể con dựa trên Model của Hoàng
                var variantsOfP1 = new List<ProductVariant>
                {
                    new ProductVariant { ProductId = p1.Product_Id, Size = "M", Color = "Đen", Price = 299000, Quantity = 15 },
                    new ProductVariant { ProductId = p1.Product_Id, Size = "L", Color = "Đen", Price = 299000, Quantity = 20 }
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
                    DeleteDate = DateTime.Now, // Điền bù vì trường này không cho phép NULL
                    CategoryId = cateId // Gán mã danh mục chuẩn chỉ
                };
                context.Products.Add(p2);
                context.SaveChanges(); // Lưu để kích nổ Product_Id cho p2

                var variantsOfP2 = new List<ProductVariant>
                {
                    new ProductVariant { ProductId = p2.Product_Id, Size = "L", Color = "Đen", Price = 450000, Quantity = 8 },
                    new ProductVariant { ProductId = p2.Product_Id, Size = "XL", Color = "Đen", Price = 450000, Quantity = 5 }
                };
                context.ProductVariants.AddRange(variantsOfP2);

                // CHỐT SỔ LƯU TOÀN BỘ XUỐNG SQL SERVER
                context.SaveChanges();
            }
        }
    }
}