using hoangstore.Data;
using hoangstore.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;



var builder = WebApplication.CreateBuilder(args);
//dang ky chuoi ket noi vao he thong
builder.Services.AddDbContext<ApplicationDbContext>(option => option.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultUI()
    .AddDefaultTokenProviders()
    .AddClaimsPrincipalFactory<ApplicationUserClaimsPrincipalFactory>();

// Add services to the container.
builder.Services.AddControllersWithViews();
//Kich hoat bo doc HTTP context de tu dong lay thong tin admin dang thao tac
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages();
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
     
        SystemSeeder.SeedSystemData(services).Wait();
        if (app.Environment.IsDevelopment())
        {
            MockSeeder.SeedTestData(services);
        }
    }
    catch(Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Có lỗi xảy ra trong quá trình Seed dữ liệu.");
    }
}
app.Run();
