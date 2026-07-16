using hoangstore.Models;
using hoangstore.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace hoangstore.Data
{
    public static class MockSeeder
    {
        public static async Task SeedMockData(IServiceProvider serviceProvider)
        {
            var db = serviceProvider.GetRequiredService<ApplicationDbContext>();

            var categoryNames = new[]
            {
                "Áo",
                "Quần",
                "Giày",
                "Phụ kiện",
                "Váy"
            };

            foreach (var categoryName in categoryNames)
            {
                var exists = await db.Categories.AnyAsync(x =>
                    !x.IsDeleted &&
                    x.Category_Name == categoryName);

                if (!exists)
                {
                    db.Categories.Add(new Category
                    {
                        Category_Name = categoryName
                    });
                }
            }

            await db.SaveChangesAsync();

            var products = new List<Product>
            {
                CreateProduct("Áo thun Basic Nam", "Áo thun cotton mềm mại, phù hợp mặc hằng ngày.", "Áo", ProductGender.Male, true, "/images/products/ao-thun-basic-nam.jpg", 199000m, 0),
                CreateProduct("Áo Polo Nam Premium", "Áo polo nam lịch sự, thoáng mát.", "Áo", ProductGender.Male, true, "/images/products/ao-polo-nam.jpg", 329000m, 10),
                CreateProduct("Áo Thun Nữ Form Rộng", "Áo thun nữ trẻ trung, thoải mái.", "Áo", ProductGender.Female, true, "/images/products/ao-thun-nu.jpg", 229000m, 0),
                CreateProduct("Váy Nữ Dáng Chữ A", "Váy nữ kiểu dáng thanh lịch.", "Váy", ProductGender.Female, true, "/images/products/vay-nu-chu-a.jpg", 399000m, 15),

                CreateProduct("Quần Jeans Nam Slim Fit", "Quần jeans nam dáng slim hiện đại.", "Quần", ProductGender.Male, false, "/images/products/quan-jeans-nam.jpg", 459000m, 0),
                CreateProduct("Quần Kaki Nam", "Quần kaki nam dễ phối đồ.", "Quần", ProductGender.Male, false, "/images/products/quan-kaki-nam.jpg", 399000m, 10),
                CreateProduct("Áo Sơ Mi Nữ", "Áo sơ mi nữ thanh lịch.", "Áo", ProductGender.Female, false, "/images/products/ao-so-mi-nu.jpg", 379000m, 0),
                CreateProduct("Quần Jeans Nữ", "Quần jeans nữ co giãn nhẹ.", "Quần", ProductGender.Female, false, "/images/products/quan-jeans-nu.jpg", 429000m, 10),

                CreateProduct("Áo Hoodie Nam Essential", "Áo hoodie nam chất nỉ mềm, kiểu dáng tối giản.", "Áo", ProductGender.Male, false, "/images/products/ao-hoodie-nam.jpg", 449000m, 20),
                CreateProduct("Áo Khoác Bomber Nam", "Áo bomber nam cá tính, phù hợp phong cách đường phố.", "Áo", ProductGender.Male, false, "/images/products/ao-bomber-nam.jpg", 599000m, 0),
                CreateProduct("Áo Sơ Mi Oxford Nam", "Áo sơ mi Oxford nam phù hợp đi làm và đi chơi.", "Áo", ProductGender.Male, false, "/images/products/ao-so-mi-oxford-nam.jpg", 389000m, 0),
                CreateProduct("Áo Tank Top Nam", "Áo tank top nam thoáng mát, phù hợp vận động.", "Áo", ProductGender.Male, false, "/images/products/ao-tank-top-nam.jpg", 179000m, 10),

                CreateProduct("Quần Jogger Nam Utility", "Quần jogger nam năng động với thiết kế túi tiện dụng.", "Quần", ProductGender.Male, false, "/images/products/quan-jogger-nam.jpg", 369000m, 0),
                CreateProduct("Quần Short Nam Casual", "Quần short nam thoải mái dành cho mùa hè.", "Quần", ProductGender.Male, false, "/images/products/quan-short-nam.jpg", 279000m, 15),
                CreateProduct("Áo Croptop Nữ", "Áo croptop nữ trẻ trung, dễ phối trang phục.", "Áo", ProductGender.Female, false, "/images/products/ao-croptop-nu.jpg", 249000m, 0),
                CreateProduct("Áo Len Nữ Cổ Tròn", "Áo len nữ mềm mại, giữ ấm tốt.", "Áo", ProductGender.Female, false, "/images/products/ao-len-nu.jpg", 419000m, 10),

                CreateProduct("Quần Tây Nữ Thanh Lịch", "Quần tây nữ dáng suông phù hợp môi trường công sở.", "Quần", ProductGender.Female, false, "/images/products/quan-tay-nu.jpg", 389000m, 0),
                CreateProduct("Quần Short Nữ", "Quần short nữ thiết kế đơn giản, thoải mái.", "Quần", ProductGender.Female, false, "/images/products/quan-short-nu.jpg", 259000m, 0),
                CreateProduct("Váy Nữ Hoa Nhí", "Váy nữ họa tiết hoa nhí nhẹ nhàng.", "Váy", ProductGender.Female, false, "/images/products/vay-hoa-nhi.jpg", 429000m, 20),
                CreateProduct("Váy Nữ Công Sở", "Váy nữ thanh lịch phù hợp đi làm.", "Váy", ProductGender.Female, false, "/images/products/vay-cong-so.jpg", 479000m, 0),

                CreateProduct("Túi Đeo Chéo Unisex", "Túi đeo chéo nhỏ gọn và tiện dụng.", "Phụ kiện", ProductGender.Unisex, false, "/images/products/tui-deo-cheo.jpg", 299000m, 10),
                CreateProduct("Mũ Lưỡi Trai Unisex", "Mũ lưỡi trai có thể điều chỉnh kích thước.", "Phụ kiện", ProductGender.Unisex, false, "/images/products/mu-luoi-trai.jpg", 179000m, 0),
                CreateProduct("Sneaker Trắng Unisex", "Giày sneaker trắng tối giản, dễ phối đồ.", "Giày", ProductGender.Unisex, false, "/images/products/sneaker-trang.jpg", 699000m, 15),
                CreateProduct("Giày Chạy Bộ Unisex", "Giày chạy bộ nhẹ, đế êm hỗ trợ vận động.", "Giày", ProductGender.Unisex, false, "/images/products/giay-chay-bo.jpg", 849000m, 0)
            };

            foreach (var product in products)
            {
                var exists = await db.Products.AnyAsync(x =>
                    x.Product_Name == product.Product_Name);

                if (exists) continue;

                var categoryName = product.Category!.Category_Name;

                product.CategoryId = await db.Categories
                    .Where(x =>
                        !x.IsDeleted &&
                        x.Category_Name == categoryName)
                    .Select(x => x.CategoryId)
                    .FirstAsync();

                product.Category = null;
                db.Products.Add(product);
            }

            await db.SaveChangesAsync();
        }

        private static Product CreateProduct(
            string name,
            string description,
            string categoryName,
            ProductGender gender,
            bool isFeatured,
            string imageUrl,
            decimal price,
            int discountPercent)
        {
            return new Product
            {
                Product_Name = name,
                Product_Description = description,
                Image_Url = imageUrl,
                IsFeatured = isFeatured,
                Gender = gender,
                Category = new Category
                {
                    Category_Name = categoryName
                },
                ProductVariants = CreateVariants(
                    name,
                    price,
                    discountPercent)
            };
        }

        private static List<ProductVariant> CreateVariants(
            string productName,
            decimal price,
            int discountPercent)
        {
            var isShoe =
                productName.Contains("Sneaker") ||
                productName.Contains("Giày");

            var isAccessory =
                productName.Contains("Túi") ||
                productName.Contains("Mũ");

            var sizes = isShoe
                ? new[] { "39", "40", "41" }
                : isAccessory
                    ? new[] { "Freesize" }
                    : new[] { "S", "M", "L" };

            return sizes.Select((size, index) =>
                new ProductVariant
                {
                    Size = size,
                    Color = index switch
                    {
                        0 => "Đen",
                        1 => "Trắng",
                        _ => "Xám"
                    },
                    Quantity = 20,
                    Price = price + index * 20000,
                    DiscountPercent = discountPercent
                })
                .ToList();
        }
    }
}
