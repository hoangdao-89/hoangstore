using hoangstore.Data;
using hoangstore.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using hoangstore.Models.Services;
using QuestPDF.Infrastructure;

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
builder.Services.AddScoped<IVnPayService, VnPayService>();
builder.Services.AddHostedService<
    PendingVnPayOrderCleanupService>();
builder.Services.AddHostedService<SoftDeleteCleanupService>();
QuestPDF.Settings.License = LicenseType.Community;

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
   
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
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

    await SystemSeeder.SeedSystemData(services);

    if (builder.Configuration.GetValue<bool>("SeedDemoData"))
    {
        await MockSeeder.SeedMockData(services);
    }
}
app.Run();
