using hoangstore.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace hoangstore.Data
{
    public class SystemSeeder
    {
        public static async Task SeedSystemData(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            string[] roleNames = { "Admin", "Khách hàng" };
            foreach (var roleName in roleNames)
            {
                //check asyn xem role da ton tai chua
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            var admin = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            string adminEmail = "admin123@gmail.com";
            //check xem co ton tai tronbg AspNetUsers chua
            var checkAdmin = await admin.FindByEmailAsync(adminEmail);
            if (checkAdmin == null)
            {
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
                // Tao user va mat khau
                var createUserAdmin = await admin.CreateAsync(newAdmin, "Hoang123@");
                //tao thanh conmg -> gan quyen admin
                if (createUserAdmin.Succeeded)
                {
                    await admin.AddToRoleAsync(newAdmin, "Admin");
                }

            }
        }
    }
}
