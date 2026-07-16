using hoangstore.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace hoangstore.Data
{
    public class SystemSeeder
    {
        public static async Task SeedSystemData(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();

            string[] roleNames = { "Admin", "Khách hàng" };

            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    var createRoleResult = await roleManager.CreateAsync(new IdentityRole(roleName));

                    if (!createRoleResult.Succeeded)
                    {
                        var errors = string.Join(", ", createRoleResult.Errors.Select(error => error.Description));
                        throw new InvalidOperationException($"Không thể tạo role '{roleName}': {errors}");
                    }
                }
            }

            var adminEmail = configuration["SystemAdmin:Email"];
            var adminPassword = configuration["SystemAdmin:Password"];

            if (string.IsNullOrWhiteSpace(adminEmail))
            {
                throw new InvalidOperationException("Chưa cấu hình SystemAdmin:Email.");
            }

            var existingAdmin = await userManager.FindByEmailAsync(adminEmail);

            if (existingAdmin == null)
            {
                if (string.IsNullOrWhiteSpace(adminPassword))
                {
                    throw new InvalidOperationException("Chưa cấu hình SystemAdmin:Password trong User Secrets.");
                }

                var newAdmin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    FirstName = "Hoàng",
                    LastName = "Đào Xuân",
                    Address = "Hà Nội",
                    PhoneNumber = "0985770904",
                    PhoneNumberConfirmed = true
                };

                var createAdminResult = await userManager.CreateAsync(newAdmin, adminPassword);

                if (!createAdminResult.Succeeded)
                {
                    var errors = string.Join(", ", createAdminResult.Errors.Select(error => error.Description));
                    throw new InvalidOperationException($"Không thể tạo admin gốc: {errors}");
                }

                var addRoleResult = await userManager.AddToRoleAsync(newAdmin, "Admin");

                if (!addRoleResult.Succeeded)
                {
                    var errors = string.Join(", ", addRoleResult.Errors.Select(error => error.Description));
                    throw new InvalidOperationException($"Không thể gán quyền Admin: {errors}");
                }

                return;
            }

            if (!await userManager.IsInRoleAsync(existingAdmin, "Admin"))
            {
                var addRoleResult = await userManager.AddToRoleAsync(existingAdmin, "Admin");

                if (!addRoleResult.Succeeded)
                {
                    var errors = string.Join(", ", addRoleResult.Errors.Select(error => error.Description));
                    throw new InvalidOperationException($"Không thể gán lại quyền Admin: {errors}");
                }
            }
        }
    }
}