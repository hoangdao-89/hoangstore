using hoangstore.Data;
using hoangstore.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace hoangstore.Models.Services
{
    /// <summary>
    /// Tự động kiểm tra các đơn VNPAY đang chờ thanh toán.
    /// Nếu quá thời gian thanh toán thì hủy đơn và hoàn lại tồn kho.
    /// </summary>
    public sealed class PendingVnPayOrderCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<PendingVnPayOrderCleanupService> _logger;

        private static readonly TimeSpan PaymentTimeout =
            TimeSpan.FromMinutes(15);

        private static readonly TimeSpan CheckInterval =
            TimeSpan.FromMinutes(1);

        public PendingVnPayOrderCleanupService(
            IServiceScopeFactory scopeFactory,
            ILogger<PendingVnPayOrderCleanupService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "Dịch vụ kiểm tra đơn VNPAY hết hạn đã bắt đầu.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CancelExpiredOrdersAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Có lỗi khi kiểm tra đơn VNPAY hết hạn.");
                }

                try
                {
                    await Task.Delay(
                        CheckInterval,
                        stoppingToken);
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }

            _logger.LogInformation(
                "Dịch vụ kiểm tra đơn VNPAY hết hạn đã dừng.");
        }

        private async Task CancelExpiredOrdersAsync(
            CancellationToken cancellationToken)
        {
            await using var scope =
                _scopeFactory.CreateAsyncScope();

            var db = scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

            var expirationTime =
                DateTime.Now.Subtract(PaymentTimeout);

            var expiredOrders = await db.Orders
                .Include(order => order.OrderDetails)
                .Where(order =>
                    order.Status == OrderStatus.Pending &&
                    order.PaymentMethod == "VNPAY" &&
                    order.OrderDate <= expirationTime)
                .ToListAsync(cancellationToken);

            if (expiredOrders.Count == 0)
            {
                return;
            }

            await using var transaction =
                await db.Database.BeginTransactionAsync(
                    cancellationToken);

            try
            {
                var variantIds = expiredOrders
                    .SelectMany(order => order.OrderDetails)
                    .Select(detail => detail.ProductVariantId)
                    .Distinct()
                    .ToList();

                var variants = await db.ProductVariants
                    .Where(variant =>
                        variantIds.Contains(variant.Id))
                    .ToDictionaryAsync(
                        variant => variant.Id,
                        cancellationToken);

                foreach (var order in expiredOrders)
                {
                    // Bảo đảm chỉ hoàn kho cho đơn vẫn còn Pending.
                    if (order.Status != OrderStatus.Pending)
                    {
                        continue;
                    }

                    foreach (var detail in order.OrderDetails)
                    {
                        if (variants.TryGetValue(
                                detail.ProductVariantId,
                                out var variant))
                        {
                            variant.Quantity += detail.Quantity;
                        }
                    }

                    order.Status = OrderStatus.Cancelled;

                    _logger.LogInformation(
                        "Đã hủy đơn VNPAY {OrderId} vì quá hạn thanh toán.",
                        order.Id);
                }

                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                throw;
            }
        }
    }
}