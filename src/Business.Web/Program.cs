using Microsoft.AspNetCore.DataProtection;
using Business.Application.Common;
using Business.Infrastructure;
using Business.Infrastructure.Data;
using Business.Infrastructure.Seed;
using Business.Web.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var dataProtectionKeysPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtectionKeys");
Directory.CreateDirectory(dataProtectionKeysPath);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.Configure<SmtpEmailOptions>(builder.Configuration.GetSection("Email:Smtp"));
builder.Services.Configure<AdminTwoFactorOptions>(builder.Configuration.GetSection("Security:AdminTwoFactor"));
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddControllersWithViews();

var app = builder.Build();

if (app.Configuration.GetValue("DatabaseMigration:ApplyOnStartup", false))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.MigrateAsync();
}

await IdentitySeeder.SeedIdentityAsync(app.Services, builder.Configuration);
await DemoDataSeeder.SeedDemoDataAsync(app.Services, builder.Configuration);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    await next();
});

app.UseStatusCodePagesWithReExecute("/Home/Status", "?code={0}");
app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated != true ||
        !context.RequestServices.GetRequiredService<Microsoft.Extensions.Options.IOptions<AdminTwoFactorOptions>>().Value.RequireForAdmins)
    {
        await next();
        return;
    }

    var path = context.Request.Path;
    if (path.StartsWithSegments("/Identity/Account/Manage/TwoFactorSetup") ||
        path.StartsWithSegments("/Identity/Account/Logout") ||
        path.StartsWithSegments("/Identity/Account/Login") ||
        path.StartsWithSegments("/css") ||
        path.StartsWithSegments("/js") ||
        path.StartsWithSegments("/lib") ||
        path.StartsWithSegments("/favicon.ico"))
    {
        await next();
        return;
    }

    var userManager = context.RequestServices.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<Business.Infrastructure.Identity.ApplicationUser>>();
    var user = await userManager.GetUserAsync(context.User);
    if (user is not null &&
        await userManager.IsInRoleAsync(user, Business.Infrastructure.Identity.AppRoles.Admin) &&
        !await userManager.GetTwoFactorEnabledAsync(user))
    {
        context.Response.Redirect("/Identity/Account/Manage/TwoFactorSetup");
        return;
    }

    await next();
});

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

app.Run();
