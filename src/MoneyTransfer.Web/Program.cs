using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MoneyTransfer.Application.Common.Interfaces;
using MoneyTransfer.Application.Services.Authentication;
using MoneyTransfer.Application.Services.Clients;
using MoneyTransfer.Application.Services.Invoices;
using MoneyTransfer.Application.Services.OrgUsers;
using MoneyTransfer.Application.Services.Payments;
using MoneyTransfer.Application.Services.Projects;
using MoneyTransfer.Infrastructure.Data;
using MoneyTransfer.Infrastructure.Identity;
using MoneyTransfer.Infrastructure.Services.Authentication;
using MoneyTransfer.Infrastructure.Services.Clients;
using MoneyTransfer.Infrastructure.Services.FinancialIdentity;
using MoneyTransfer.Infrastructure.Services.Invoices;
using MoneyTransfer.Infrastructure.Services.OrgUsers;
using MoneyTransfer.Infrastructure.Services.Payments;
using MoneyTransfer.Infrastructure.Services.Projects;
using MoneyTransfer.Web.Infrastructure;
using MoneyTransfer.Web.Services;

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
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddScoped<IClientService, ClientService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IFinancialIdentityGenerator, FinancialIdentityGenerator>();
builder.Services.AddScoped<IProjectFinancialService, ProjectFinancialService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IOrgUserService, OrgUserService>();

var app = builder.Build();

// Ensure required roles exist
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    await RoleSeeder.SeedAsync(roleManager);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
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