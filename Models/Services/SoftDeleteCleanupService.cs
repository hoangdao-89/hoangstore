using hoangstore.Data;
using Microsoft.EntityFrameworkCore;

namespace hoangstore.Models.Services
{
    public class SoftDeleteCleanupService : BackgroundService
    {
        private const int RetentionDays = 30;
        private static readonly TimeSpan CleanupInterval = TimeSpan.FromHours(24);

        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<SoftDeleteCleanupService> _logger;

        public SoftDeleteCleanupService(
            IServiceScopeFactory scopeFactory,
            IWebHostEnvironment environment,
            ILogger<SoftDeleteCleanupService> logger)
        {
            _scopeFactory = scopeFactory;
            _environment = environment;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await CleanupAsync(stoppingToken);

            using var timer = new PeriodicTimer(CleanupInterval);
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await CleanupAsync(stoppingToken);
            }
        }

        private async Task CleanupAsync(CancellationToken cancellationToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var expiredDate = DateTime.Now.AddDays(-RetentionDays);

                var products = await db.Products
                    .AsNoTracking()
                    .Where(product =>
                        product.IsDeleted &&
                        product.DeletedDate.HasValue &&
                        product.DeletedDate <= expiredDate &&
                        !product.ProductVariants.Any(variant =>
                            db.OrderDetails.Any(detail =>
                                detail.ProductVariantId == variant.Id)))
                    .Select(product => new
                    {
                        product.ProductId,
                        product.Image_Url,
                        VariantIds = product.ProductVariants
                            .Select(variant => variant.Id)
                            .ToList(),
                        VariantImages = product.ProductVariants
                            .Select(variant => variant.Variant_Image_Url)
                            .ToList()
                    })
                    .ToListAsync(cancellationToken);

                if (products.Count == 0)
                {
                    await DeleteEmptyCategoriesAsync(db, expiredDate, cancellationToken);
                    return;
                }

                var productIds = products.Select(product => product.ProductId).ToList();
                var variantIds = products.SelectMany(product => product.VariantIds).ToList();

                await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

                if (variantIds.Count > 0)
                {
                    await db.CartItems
                        .Where(item => variantIds.Contains(item.ProductVariantId))
                        .ExecuteDeleteAsync(cancellationToken);

                    await db.ProductVariants
                        .Where(variant => variantIds.Contains(variant.Id))
                        .ExecuteDeleteAsync(cancellationToken);
                }

                var deletedProducts = await db.Products
                    .Where(product => productIds.Contains(product.ProductId))
                    .ExecuteDeleteAsync(cancellationToken);

                await DeleteEmptyCategoriesAsync(db, expiredDate, cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                foreach (var image in products
                    .Select(product => product.Image_Url)
                    .Concat(products.SelectMany(product => product.VariantImages))
                    .Where(image => !string.IsNullOrWhiteSpace(image))
                    .Distinct())
                {
                    DeleteLocalImage(image!);
                }

                _logger.LogInformation(
                    "Đã xóa vĩnh viễn {ProductCount} sản phẩm sau {RetentionDays} ngày.",
                    deletedProducts,
                    RetentionDays);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Có lỗi khi dọn dữ liệu đã xóa mềm.");
            }
        }

        private static async Task DeleteEmptyCategoriesAsync(
            ApplicationDbContext db,
            DateTime expiredDate,
            CancellationToken cancellationToken)
        {
            await db.Categories
                .Where(category =>
                    category.IsDeleted &&
                    category.DeletedDate.HasValue &&
                    category.DeletedDate <= expiredDate &&
                    !db.Products.Any(product =>
                        product.CategoryId == category.CategoryId))
                .ExecuteDeleteAsync(cancellationToken);
        }

        private void DeleteLocalImage(string imageUrl)
        {
            if (Uri.TryCreate(imageUrl, UriKind.Absolute, out _)) return;

            var relativePath = imageUrl
                .TrimStart('~')
                .TrimStart('/')
                .Replace('/', Path.DirectorySeparatorChar);

            if (string.IsNullOrWhiteSpace(relativePath)) return;

            var webRoot = Path.GetFullPath(_environment.WebRootPath);
            var fullPath = Path.GetFullPath(Path.Combine(webRoot, relativePath));

            if (!fullPath.StartsWith(webRoot, StringComparison.OrdinalIgnoreCase)) return;
            if (!File.Exists(fullPath)) return;

            try
            {
                File.Delete(fullPath);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Không thể xóa ảnh {ImagePath}.",
                    fullPath);
            }
        }
    }
}
