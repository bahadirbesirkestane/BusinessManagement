using Business.Application.Repositories;
using Business.Application.Services;
using Business.Infrastructure.Data;
using Business.Infrastructure.Identity;
using Business.Infrastructure.Repositories;
using Business.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Business.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.SignIn.RequireConfirmedAccount = false;
                options.User.RequireUniqueEmail = true;
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequiredUniqueChars = 4;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireUppercase = true;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.MaxFailedAccessAttempts = 5;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders()
            .AddErrorDescriber<TurkishIdentityErrorDescriber>()
            .AddDefaultUI();

        services.AddScoped<SignInManager<ApplicationUser>, ApplicationSignInManager>();

        services.AddAuthorization(options =>
        {
            static void AddPermissionPolicy(Microsoft.AspNetCore.Authorization.AuthorizationOptions options, string policyName, params string[] permissions)
            {
                options.AddPolicy(policyName, policy =>
                    policy.RequireAssertion(context =>
                        permissions.Any(permission => context.User.HasClaim(AppClaimTypes.Permission, permission)) ||
                        context.User.HasClaim(AppClaimTypes.Permission, AppPermissions.ProjectsManage) && policyName.Contains("Project") ||
                        context.User.HasClaim(AppClaimTypes.Permission, AppPermissions.PurchasingManage) && policyName.Contains("Purchasing")));
            }

            options.AddPolicy(AppPolicies.CanViewDashboard, policy =>
                policy.RequireClaim(AppClaimTypes.Permission, AppPermissions.DashboardView));

            options.AddPolicy(AppPolicies.CanViewProjects, policy =>
                policy.RequireClaim(AppClaimTypes.Permission, AppPermissions.ProjectsView));

            options.AddPolicy(AppPolicies.CanManageProjects, policy =>
                policy.RequireClaim(AppClaimTypes.Permission, AppPermissions.ProjectsManage));

            AddPermissionPolicy(options, AppPolicies.CanCreateProjects, AppPermissions.ProjectsCreate, AppPermissions.ProjectsManage);
            AddPermissionPolicy(options, AppPolicies.CanUpdateProjects, AppPermissions.ProjectsUpdate, AppPermissions.ProjectsManage);
            AddPermissionPolicy(options, AppPolicies.CanDeleteProjects, AppPermissions.ProjectsDelete, AppPermissions.ProjectsManage);
            AddPermissionPolicy(options, AppPolicies.CanChangeProjectStatus, AppPermissions.ProjectsChangeStatus, AppPermissions.ProjectsManage);
            AddPermissionPolicy(options, AppPolicies.CanViewTasks, AppPermissions.TasksView, AppPermissions.ProjectsView);
            AddPermissionPolicy(options, AppPolicies.CanCreateTasks, AppPermissions.TasksCreate, AppPermissions.ProjectsManage);
            AddPermissionPolicy(options, AppPolicies.CanUpdateTasks, AppPermissions.TasksUpdate, AppPermissions.ProjectsManage);
            AddPermissionPolicy(options, AppPolicies.CanDeleteTasks, AppPermissions.TasksDelete, AppPermissions.ProjectsManage);
            AddPermissionPolicy(options, AppPolicies.CanChangeTaskStatus, AppPermissions.TasksChangeStatus, AppPermissions.ProjectsManage);
            AddPermissionPolicy(options, AppPolicies.CanCompleteTasks, AppPermissions.TasksComplete, AppPermissions.ProjectsManage);

            options.AddPolicy(AppPolicies.CanViewPurchasing, policy =>
                policy.RequireClaim(AppClaimTypes.Permission, AppPermissions.PurchasingView));

            options.AddPolicy(AppPolicies.CanViewProductionUpdates, policy =>
                policy.RequireClaim(AppClaimTypes.Permission, AppPermissions.ProductionUpdatesView));

            options.AddPolicy(AppPolicies.CanViewMaterialRequests, policy =>
                policy.RequireClaim(AppClaimTypes.Permission, AppPermissions.MaterialRequestsView));

            options.AddPolicy(AppPolicies.CanRequestMaterials, policy =>
                policy.RequireClaim(AppClaimTypes.Permission, AppPermissions.MaterialRequestsManage));

            options.AddPolicy(AppPolicies.CanViewCustomers, policy =>
                policy.RequireClaim(AppClaimTypes.Permission, AppPermissions.CustomersView));

            options.AddPolicy(AppPolicies.CanManageCustomers, policy =>
                policy.RequireClaim(AppClaimTypes.Permission, AppPermissions.CustomersManage));

            options.AddPolicy(AppPolicies.CanViewInvoices, policy =>
                policy.RequireClaim(AppClaimTypes.Permission, AppPermissions.InvoicesView));

            options.AddPolicy(AppPolicies.CanManageInvoices, policy =>
                policy.RequireClaim(AppClaimTypes.Permission, AppPermissions.InvoicesManage));

            options.AddPolicy(AppPolicies.CanViewCosts, policy =>
                policy.RequireAssertion(context =>
                    context.User.IsInRole(AppRoles.Admin) ||
                    context.User.HasClaim(AppClaimTypes.Permission, AppPermissions.CostsView) ||
                    context.User.HasClaim(AppClaimTypes.Permission, AppPermissions.CostsManage)));

            options.AddPolicy(AppPolicies.CanManageCosts, policy =>
                policy.RequireAssertion(context =>
                    context.User.IsInRole(AppRoles.Admin) ||
                    context.User.HasClaim(AppClaimTypes.Permission, AppPermissions.CostsManage)));

            options.AddPolicy(AppPolicies.CanManagePurchasing, policy =>
                policy.RequireClaim(AppClaimTypes.Permission, AppPermissions.PurchasingManage));

            AddPermissionPolicy(options, AppPolicies.CanCreatePurchasing, AppPermissions.PurchasingCreate, AppPermissions.PurchasingManage);
            AddPermissionPolicy(options, AppPolicies.CanUpdatePurchasing, AppPermissions.PurchasingUpdate, AppPermissions.PurchasingManage);
            AddPermissionPolicy(options, AppPolicies.CanDeletePurchasing, AppPermissions.PurchasingDelete, AppPermissions.PurchasingManage);
            AddPermissionPolicy(options, AppPolicies.CanChangePurchasingStatus, AppPermissions.PurchasingChangeStatus, AppPermissions.PurchasingManage);

            options.AddPolicy(AppPolicies.CanViewSuppliers, policy =>
                policy.RequireClaim(AppClaimTypes.Permission, AppPermissions.SuppliersView));

            options.AddPolicy(AppPolicies.CanManageSuppliers, policy =>
                policy.RequireClaim(AppClaimTypes.Permission, AppPermissions.SuppliersManage));

            options.AddPolicy(AppPolicies.CanViewMaterials, policy =>
                policy.RequireClaim(AppClaimTypes.Permission, AppPermissions.MaterialsView));

            options.AddPolicy(AppPolicies.CanManageMaterials, policy =>
                policy.RequireClaim(AppClaimTypes.Permission, AppPermissions.MaterialsManage));

            options.AddPolicy(AppPolicies.CanViewStock, policy =>
                policy.RequireAssertion(context =>
                    context.User.IsInRole(AppRoles.Admin) ||
                    context.User.HasClaim(AppClaimTypes.Permission, AppPermissions.StockView) ||
                    context.User.HasClaim(AppClaimTypes.Permission, AppPermissions.StockManage)));

            options.AddPolicy(AppPolicies.CanManageStock, policy =>
                policy.RequireAssertion(context =>
                    context.User.IsInRole(AppRoles.Admin) ||
                    context.User.HasClaim(AppClaimTypes.Permission, AppPermissions.StockManage)));

            options.AddPolicy(AppPolicies.CanViewUsers, policy =>
                policy.RequireClaim(AppClaimTypes.Permission, AppPermissions.UsersView));

            options.AddPolicy(AppPolicies.CanManageUsers, policy =>
                policy.RequireClaim(AppClaimTypes.Permission, AppPermissions.UsersManage));
        });

        services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
        services.AddScoped(typeof(ICrudService<>), typeof(CrudService<>));
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IProjectTemplateService, ProjectTemplateService>();
        services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
        services.AddScoped<IPurchaseOrderTemplateService, PurchaseOrderTemplateService>();
        services.AddScoped<IMaterialRequestService, MaterialRequestService>();
        services.AddScoped<IMaterialRequestTemplateService, MaterialRequestTemplateService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<ISupplierService, SupplierService>();
        services.AddScoped<IMaterialService, MaterialService>();
        services.AddScoped<ILookupService, LookupService>();
        services.AddScoped<IPersonalNoteService, PersonalNoteService>();
        services.AddScoped<IPersonalTaskService, PersonalTaskService>();
        services.AddScoped<IRecordActivityService, RecordActivityService>();
        services.AddScoped<IProjectTimelineService, ProjectTimelineService>();
        services.AddScoped<IAdminRecoveryCodeService, AdminRecoveryCodeService>();

        return services;
    }
}
