using AcxiomCRM.Data;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var b = WebApplication.CreateBuilder(args);

// ==================== DATABASE ====================

b.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        b.Configuration.GetConnectionString("Default")));

// ==================== IDENTITY ====================

b.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;

    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan =
        TimeSpan.FromMinutes(10);
    options.Lockout.AllowedForNewUsers = true;

    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// ==================== AUTHENTICATION COOKIE ====================

b.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/Denied";

    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;

    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);

    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = 401;
        }
        else
        {
            context.Response.Redirect(context.RedirectUri);
        }

        return Task.CompletedTask;
    };

    options.Events.OnRedirectToAccessDenied = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = 403;
        }
        else
        {
            context.Response.Redirect(context.RedirectUri);
        }

        return Task.CompletedTask;
    };
});

// ==================== SERVICES ====================

b.Services.AddHttpContextAccessor();


// Register the interface and implementation together.
b.Services.AddScoped<IAuditService, AuditService>();

b.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(
        new AutoValidateAntiforgeryTokenAttribute());
});

// ==================== BUILD APPLICATION ====================

var app = b.Build();

// ==================== MIDDLEWARE ====================

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

// ==================== ROUTING ====================

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// ==================== DATABASE MIGRATION AND SEEDING ====================

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var db = services.GetRequiredService<AppDbContext>();

    await db.Database.MigrateAsync();

    var roleManager =
        services.GetRequiredService<RoleManager<IdentityRole>>();

    var userManager =
        services.GetRequiredService<UserManager<AppUser>>();

    string[] roles =
    {
        "Admin",
        "Manager",
        "SalesExecutive"
    };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            var result =
                await roleManager.CreateAsync(new IdentityRole(role));

            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Could not create role {role}: " +
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }

    var initialUsers = new[]
    {
        (Email: "admin@acxiom.com", Role: "Admin"),
        (Email: "manager@acxiom.com", Role: "Manager")
    };

    foreach (var (email, role) in initialUsers)
    {
        var user = await userManager.FindByEmailAsync(email);

        if (user == null)
        {
            user = new AppUser
            {
                UserName = email,
                Email = email,
                FullName = role,
                EmailConfirmed = true,
                IsActive = true
            };

            var createResult =
                await userManager.CreateAsync(user, "Admin@123");

            if (!createResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Could not create user {email}: " +
                    string.Join(", ",
                        createResult.Errors.Select(e => e.Description)));
            }
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            var roleResult =
                await userManager.AddToRoleAsync(user, role);

            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Could not assign role {role} to {email}: " +
                    string.Join(", ",
                        roleResult.Errors.Select(e => e.Description)));
            }
        }
    }
}

// ==================== RUN ====================

await app.RunAsync();