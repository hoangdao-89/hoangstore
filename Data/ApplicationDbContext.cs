using hoangstore.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.General;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.Diagnostics.Contracts;
namespace hoangstore.Data
{
    public class ApplicationDbContext: IdentityDbContext<ApplicationUser>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ApplicationDbContext (DbContextOptions<ApplicationDbContext> options, IHttpContextAccessor httpContextAccessor) : base(options) {
            _httpContextAccessor = httpContextAccessor;
        }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<ProductVariant> ProductVariants { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }
        //ghi lich su ngam khi goi SaveChangesAsync
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            // Tự động lấy tên tài khoản (Email) của Admin/Nhân viên đang thao tác trên trình duyệt
            var currentUserName = _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "System Auto";

            // Tìm tất cả các thực thể có kế thừa bộ khung IAuditable đang trong trạng thái chờ lưu (Thêm, Sửa, Xóa)
            var entries = ChangeTracker.Entries()
                .Where(e => e.Entity is IAuditable &&
                           (e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted));

            foreach (var entry in entries)
            {
                var auditable = (IAuditable)entry.Entity;

                if (entry.State == EntityState.Added)
                {
                    // Khi Thêm mới (Create): Tự điền người tạo và ngày tạo
                    auditable.CreatedBy = currentUserName;
                    auditable.CreatedDate = DateTime.Now;
                }
                else if (entry.State == EntityState.Modified)
                {
                    // Khi Chỉnh sửa (Edit): Không cho sửa dữ liệu lúc tạo ban đầu
                    entry.Property("CreatedBy").IsModified = false;
                    entry.Property("CreatedDate").IsModified = false;

                    auditable.ModifiedBy = currentUserName;
                    auditable.ModifiedDate = DateTime.Now;
                }
                else if (entry.State == EntityState.Deleted)
                {
                    // Khi bấm Xóa (Delete) -> Bẻ lái sang lệnh Cập nhật để làm XÓA MỀM (SOFT DELETE)
                    entry.State = EntityState.Modified;

                    entry.Property("CreatedBy").IsModified = false;
                    entry.Property("CreatedDate").IsModified = false;

                    // Bật cờ ẩn đi và lưu lại thông tin vết người xóa
                    auditable.IsDeleted = true;
                    auditable.DeletedBy = currentUserName;
                    auditable.DeletedDate = DateTime.Now;
                }
            }

            // Đẩy dữ liệu đã được nạp đủ lịch sử xuống SQL Server
            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}
