using hoangstore.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace hoangstore.Data
{
    public class ApplicationDbContext
        : IdentityDbContext<ApplicationUser>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options,
            IHttpContextAccessor httpContextAccessor)
            : base(options)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public DbSet<Category> Categories { get; set; }

        public DbSet<Product> Products { get; set; }

        public DbSet<ProductVariant> ProductVariants { get; set; }

        public DbSet<Cart> Carts { get; set; }

        public DbSet<CartItem> CartItems { get; set; }

        public DbSet<Order> Orders { get; set; }

        public DbSet<OrderDetail> OrderDetails { get; set; }
        

        public override int SaveChanges()
        {
            ApplyAuditInformation();

            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            ApplyAuditInformation();

            return await base.SaveChangesAsync(
                cancellationToken);
        }

        private void ApplyAuditInformation()
        {
            var currentUserName =
                _httpContextAccessor
                    .HttpContext?
                    .User?
                    .Identity?
                    .Name
                ?? "System Auto";

            var currentDate = DateTime.Now;

            var entries = ChangeTracker
                .Entries()
                .Where(entry =>
                    entry.Entity is IAuditable &&
                    (
                        entry.State == EntityState.Added ||
                        entry.State == EntityState.Modified ||
                        entry.State == EntityState.Deleted
                    ))
                .ToList();

            foreach (var entry in entries)
            {
                var auditable =
                    (IAuditable)entry.Entity;

                switch (entry.State)
                {
                    case EntityState.Added:
                        ApplyAddedAudit(
                            auditable,
                            currentUserName,
                            currentDate);

                        break;

                    case EntityState.Modified:
                        ApplyModifiedAudit(
                            entry,
                            auditable,
                            currentUserName,
                            currentDate);

                        break;

                    case EntityState.Deleted:
                        ApplyDeletedAudit(
                            entry,
                            auditable,
                            currentUserName,
                            currentDate);

                        break;
                }
            }
        }

        private static void ApplyAddedAudit(
            IAuditable auditable,
            string currentUserName,
            DateTime currentDate)
        {
            auditable.CreatedBy =
                currentUserName;

            auditable.CreatedDate =
                currentDate;

            /*
             * Các cột này có thể đang là NOT NULL
             * trong SQL Server nên phải gán giá trị
             * khi tạo mới.
             */
            auditable.ModifiedBy =
                string.Empty;

            auditable.ModifiedDate =
                null;

            auditable.DeletedBy =
                string.Empty;

            auditable.DeletedDate =
                null;

            auditable.IsDeleted =
                false;
        }

        private static void ApplyModifiedAudit(
            Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry,
            IAuditable auditable,
            string currentUserName,
            DateTime currentDate)
        {
            /*
             * Không cho form hoặc controller sửa lại
             * thông tin người tạo và ngày tạo.
             */
            entry.Property(
                    nameof(IAuditable.CreatedBy))
                .IsModified = false;

            entry.Property(
                    nameof(IAuditable.CreatedDate))
                .IsModified = false;

            auditable.ModifiedBy =
                currentUserName;

            auditable.ModifiedDate =
                currentDate;

            /*
             * Hỗ trợ trường hợp controller tự đặt:
             *
             * IsDeleted = true
             *
             * thay vì gọi Remove().
             */
            var isDeletedProperty =
                entry.Property(
                    nameof(IAuditable.IsDeleted));

            var wasDeleted =
                isDeletedProperty.OriginalValue is bool originalValue &&
                originalValue;

            var isBeingDeleted =
                auditable.IsDeleted &&
                !wasDeleted;

            if (isBeingDeleted)
            {
                auditable.DeletedBy =
                    currentUserName;

                auditable.DeletedDate =
                    currentDate;
            }
            else
            {
                /*
                 * Khi chỉ sửa dữ liệu thông thường,
                 * không cho thay đổi thông tin xóa.
                 */
                entry.Property(
                        nameof(IAuditable.DeletedBy))
                    .IsModified = false;

                entry.Property(
                        nameof(IAuditable.DeletedDate))
                    .IsModified = false;
            }
        }

        private static void ApplyDeletedAudit(
            Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry,
            IAuditable auditable,
            string currentUserName,
            DateTime currentDate)
        {
            /*
             * Chuyển DELETE vật lý thành UPDATE
             * để thực hiện xóa mềm.
             */
            entry.State =
                EntityState.Modified;

            entry.Property(
                    nameof(IAuditable.CreatedBy))
                .IsModified = false;

            entry.Property(
                    nameof(IAuditable.CreatedDate))
                .IsModified = false;

            auditable.IsDeleted =
                true;

            auditable.DeletedBy =
                currentUserName;

            auditable.DeletedDate =
                currentDate;

            auditable.ModifiedBy =
                currentUserName;

            auditable.ModifiedDate =
                currentDate;
        }
    }
}