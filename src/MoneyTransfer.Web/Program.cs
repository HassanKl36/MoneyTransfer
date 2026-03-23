using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MoneyTransfer.Infrastructure.Data;
using MoneyTransfer.Infrastructure.Identity;
using MoneyTransfer.Web.Infrastructure;
using MoneyTransfer.Web.Services;
using MoneyTransfer.Application.Common.Interfaces;
using MoneyTransfer.Application.Services.Authentication;
using MoneyTransfer.Infrastructure.Services.Authentication;
using MoneyTransfer.Application.Services.Clients;
using MoneyTransfer.Infrastructure.Services.Clients;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<MoneyTransferDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequiredLength = 6;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireDigit = false;

        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<MoneyTransferDbContext>()
    .AddDefaultTokenProviders();

// Configure auth cookie paths (so unauthenticated users go to /Account/Login)
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("OrgPortal", policy =>
        policy.RequireRole("Admin", "Staff"));

    options.AddPolicy("CustomerPortal", policy =>
        policy.RequireRole("Customer"));
});

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentOrganization, CurrentOrganization>();
builder.Services.AddScoped<ICurrentClient, CurrentClient>();
builder.Services.AddScoped<IClientService, ClientService>();

builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    await RoleSeeder.SeedAsync(roleManager);
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");



app.Run();
