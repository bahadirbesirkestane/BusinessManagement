using Business.Domain.Common;
using Business.Domain.Entities;
using Business.Domain.Enums;
using Business.Application.Common;
using Business.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Business.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    private readonly ICurrentUserService? _currentUserService;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ICurrentUserService currentUserService)
        : base(options)
    {
        _currentUserService = currentUserService;
    }

    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectTask> ProjectTasks => Set<ProjectTask>();
    public DbSet<ProjectUpdate> ProjectUpdates => Set<ProjectUpdate>();
    public DbSet<ProjectTaskUpdate> ProjectTaskUpdates => Set<ProjectTaskUpdate>();
    public DbSet<ProjectCostItem> ProjectCostItems => Set<ProjectCostItem>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderTemplate> PurchaseOrderTemplates => Set<PurchaseOrderTemplate>();
    public DbSet<PurchaseOrderTemplateLine> PurchaseOrderTemplateLines => Set<PurchaseOrderTemplateLine>();
    public DbSet<MaterialRequest> MaterialRequests => Set<MaterialRequest>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Material> Materials => Set<Material>();
    public DbSet<MaterialCategoryDefinition> MaterialCategoryDefinitions => Set<MaterialCategoryDefinition>();
    public DbSet<StockItem> StockItems => Set<StockItem>();
    public DbSet<RecordComment> RecordComments => Set<RecordComment>();
    public DbSet<RecordFile> RecordFiles => Set<RecordFile>();
    public DbSet<TaskCategory> TaskCategories => Set<TaskCategory>();
    public DbSet<ProjectTaskAssignment> ProjectTaskAssignments => Set<ProjectTaskAssignment>();
    public DbSet<ProjectTemplate> ProjectTemplates => Set<ProjectTemplate>();
    public DbSet<ProjectTemplateTask> ProjectTemplateTasks => Set<ProjectTemplateTask>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<AdminRecoveryCode> AdminRecoveryCodes => Set<AdminRecoveryCode>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        ConfigureIdentity(builder);
        ConfigureBusinessEntities(builder);
        SeedLookups(builder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StampEntities();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        StampEntities();
        return base.SaveChanges();
    }

    private void StampEntities()
    {
        var entries = ChangeTracker.Entries<BaseEntity>();
        var userId = _currentUserService?.UserId;
        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
                entry.Entity.CreatedByUserId ??= userId;
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
                entry.Entity.UpdatedByUserId = userId;
                entry.Property(x => x.CreatedAt).IsModified = false;
                entry.Property(x => x.CreatedByUserId).IsModified = false;
            }
        }
    }

    private static void ConfigureIdentity(ModelBuilder builder)
    {
        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(x => x.FullName).HasMaxLength(160);
            entity.HasOne<Department>().WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<AdminRecoveryCode>(entity =>
        {
            entity.HasIndex(x => new { x.UserId, x.UsedAt });
            entity.Property(x => x.UserId).HasMaxLength(450).IsRequired();
            entity.Property(x => x.CodeHash).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Salt).HasMaxLength(64).IsRequired();
            entity.Property(x => x.UsedIpAddress).HasMaxLength(80);
            entity.Property(x => x.Note).HasMaxLength(240);
            entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<IdentityRole>().HasData(
            new IdentityRole
            {
                Id = "1f9d0c0b-8a4a-49f5-8ef4-c7f49d490001",
                Name = AppRoles.Admin,
                NormalizedName = "YÖNETİCİ",
                ConcurrencyStamp = "1f9d0c0b-8a4a-49f5-8ef4-c7f49d490001"
            },
            new IdentityRole
            {
                Id = "1f9d0c0b-8a4a-49f5-8ef4-c7f49d490002",
                Name = AppRoles.Manager,
                NormalizedName = "MÜDÜR",
                ConcurrencyStamp = "1f9d0c0b-8a4a-49f5-8ef4-c7f49d490002"
            },
            new IdentityRole
            {
                Id = "1f9d0c0b-8a4a-49f5-8ef4-c7f49d490003",
                Name = AppRoles.Purchasing,
                NormalizedName = "SATIN ALMA",
                ConcurrencyStamp = "1f9d0c0b-8a4a-49f5-8ef4-c7f49d490003"
            },
            new IdentityRole
            {
                Id = "1f9d0c0b-8a4a-49f5-8ef4-c7f49d490004",
                Name = AppRoles.ProjectUser,
                NormalizedName = "PROJE KULLANICISI",
                ConcurrencyStamp = "1f9d0c0b-8a4a-49f5-8ef4-c7f49d490004"
            },
            new IdentityRole
            {
                Id = "1f9d0c0b-8a4a-49f5-8ef4-c7f49d490005",
                Name = AppRoles.Workshop,
                NormalizedName = "USTA",
                ConcurrencyStamp = "1f9d0c0b-8a4a-49f5-8ef4-c7f49d490005"
            });
    }

    private static void ConfigureBusinessEntities(ModelBuilder builder)
    {
        builder.Entity<Project>(entity =>
        {
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(32).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(220).IsRequired();
            entity.Property(x => x.CustomerName).HasMaxLength(180);
            entity.Property(x => x.Currency).HasMaxLength(3);
            entity.Property(x => x.Budget).HasColumnType("decimal(18,2)");
            entity.HasOne(x => x.Customer).WithMany(x => x.Projects).HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.SetNull);
            entity.HasMany(x => x.Tasks).WithOne(x => x.Project).HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.SetNull);
            entity.HasMany(x => x.PurchaseOrders).WithOne(x => x.Project).HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.SetNull);
            entity.HasMany(x => x.MaterialRequests).WithOne(x => x.Project).HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.SetNull);
            entity.HasMany(x => x.Updates).WithOne(x => x.Project).HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(x => x.CostItems).WithOne(x => x.Project).HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.SetNull);
            entity.HasMany(x => x.Invoices).WithOne(x => x.Project).HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.SetNull);
        });

        foreach (var entityType in builder.Model.GetEntityTypes()
                     .Where(x => typeof(BaseEntity).IsAssignableFrom(x.ClrType)))
        {
            builder.Entity(entityType.ClrType).Property<string?>(nameof(BaseEntity.CreatedByUserId)).HasMaxLength(450);
            builder.Entity(entityType.ClrType).Property<string?>(nameof(BaseEntity.UpdatedByUserId)).HasMaxLength(450);
        }

        builder.Entity<ProjectTask>(entity =>
        {
            entity.Property(x => x.Title).HasMaxLength(220).IsRequired();
            entity.Property(x => x.ManualProjectName).HasMaxLength(220);
            entity.Property(x => x.ManualCustomerName).HasMaxLength(220);
            entity.Property(x => x.AssignedToUserId).HasMaxLength(450);
            entity.Property(x => x.ResponsibleUserId).HasMaxLength(450);
            entity.Property(x => x.WbsCode).HasMaxLength(40);
            entity.HasIndex(x => new { x.ProjectId, x.ParentTaskId, x.SortOrder });
            entity.HasOne(x => x.ParentTask).WithMany(x => x.SubTasks).HasForeignKey(x => x.ParentTaskId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.AssignedToUserId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ResponsibleUserId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.TaskCategory).WithMany(x => x.Tasks).HasForeignKey(x => x.TaskCategoryId).OnDelete(DeleteBehavior.SetNull);
            entity.HasMany(x => x.Updates).WithOne(x => x.ProjectTask).HasForeignKey(x => x.ProjectTaskId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ProjectTemplate>(entity =>
        {
            entity.HasIndex(x => x.Name).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(180).IsRequired();
            entity.Property(x => x.Code).HasMaxLength(40);
            entity.Property(x => x.Description).HasMaxLength(1000);
            entity.HasMany(x => x.Tasks).WithOne(x => x.ProjectTemplate).HasForeignKey(x => x.ProjectTemplateId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<ProjectTemplateTask>(entity =>
        {
            entity.Property(x => x.Title).HasMaxLength(220).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(1000);
            entity.Property(x => x.WbsCode).HasMaxLength(40);
            entity.Property(x => x.DefaultAssignedUserId).HasMaxLength(450);
            entity.Property(x => x.DefaultResponsibleUserId).HasMaxLength(450);
            entity.HasIndex(x => new { x.ProjectTemplateId, x.ParentTemplateTaskId, x.SortOrder });
            entity.HasOne(x => x.ParentTemplateTask).WithMany(x => x.SubTasks).HasForeignKey(x => x.ParentTemplateTaskId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.TaskCategory).WithMany().HasForeignKey(x => x.TaskCategoryId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.DefaultAssignedUserId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.DefaultResponsibleUserId).OnDelete(DeleteBehavior.NoAction);
        });

        builder.Entity<ProjectTaskAssignment>(entity =>
        {
            entity.HasIndex(x => new { x.ProjectTaskId, x.UserId }).IsUnique();
            entity.Property(x => x.UserId).HasMaxLength(450).IsRequired();
            entity.HasOne(x => x.ProjectTask).WithMany(x => x.Assignments).HasForeignKey(x => x.ProjectTaskId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.NoAction);
        });

        builder.Entity<TaskCategory>(entity =>
        {
            entity.HasIndex(x => x.Name).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Color).HasMaxLength(24);
        });

        builder.Entity<Department>(entity =>
        {
            entity.HasIndex(x => x.Name).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(500);
        });

        builder.Entity<ProjectUpdate>(entity =>
        {
            entity.Property(x => x.Title).HasMaxLength(220).IsRequired();
        });

        builder.Entity<ProjectTaskUpdate>(entity =>
        {
            entity.Property(x => x.Title).HasMaxLength(220).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(2000);
            entity.HasIndex(x => new { x.ProjectTaskId, x.CreatedAt });
        });

        builder.Entity<ProjectCostItem>(entity =>
        {
            entity.Property(x => x.Description).HasMaxLength(320).IsRequired();
            entity.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            entity.Property(x => x.Currency).HasMaxLength(3);
            entity.HasOne(x => x.PurchaseOrder).WithMany().HasForeignKey(x => x.PurchaseOrderId).OnDelete(DeleteBehavior.NoAction);
        });

        builder.Entity<PurchaseOrder>(entity =>
        {
            entity.HasIndex(x => x.OrderNumber).IsUnique();
            entity.Property(x => x.OrderNumber).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Content).HasMaxLength(600).IsRequired();
            entity.Property(x => x.Quantity).HasColumnType("decimal(18,3)");
            entity.Property(x => x.QuantityText).HasMaxLength(80);
            entity.Property(x => x.Unit).HasMaxLength(40);
            entity.Property(x => x.Quality).HasMaxLength(120);
            entity.Property(x => x.RequestedBy).HasMaxLength(120);
            entity.Property(x => x.RequestedByUserId).HasMaxLength(450);
            entity.Property(x => x.PaymentTerm).HasMaxLength(80);
            entity.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
            entity.Property(x => x.UnitPriceText).HasMaxLength(120);
            entity.Property(x => x.OrderTotal).HasColumnType("decimal(18,2)");
            entity.Property(x => x.Currency).HasMaxLength(3);
            entity.Property(x => x.VatRate).HasColumnType("decimal(5,2)");
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.RequestedByUserId).OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(x => x.Supplier).WithMany(x => x.PurchaseOrders).HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.Material).WithMany(x => x.PurchaseOrders).HasForeignKey(x => x.MaterialId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<PurchaseOrderTemplate>(entity =>
        {
            entity.HasIndex(x => x.Name).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(180).IsRequired();
            entity.Property(x => x.Code).HasMaxLength(40);
            entity.Property(x => x.Description).HasMaxLength(1000);
            entity.Property(x => x.DefaultPaymentTerm).HasMaxLength(80);
            entity.Property(x => x.DefaultCurrency).HasMaxLength(3);
            entity.Property(x => x.DefaultVatRate).HasColumnType("decimal(5,2)");
            entity.HasOne(x => x.DefaultSupplier).WithMany().HasForeignKey(x => x.DefaultSupplierId).OnDelete(DeleteBehavior.SetNull);
            entity.HasMany(x => x.Lines).WithOne(x => x.PurchaseOrderTemplate).HasForeignKey(x => x.PurchaseOrderTemplateId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<PurchaseOrderTemplateLine>(entity =>
        {
            entity.Property(x => x.Content).HasMaxLength(600).IsRequired();
            entity.Property(x => x.Quantity).HasColumnType("decimal(18,3)");
            entity.Property(x => x.QuantityText).HasMaxLength(80);
            entity.Property(x => x.Unit).HasMaxLength(40);
            entity.Property(x => x.Quality).HasMaxLength(120);
            entity.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
            entity.Property(x => x.OrderTotal).HasColumnType("decimal(18,2)");
            entity.Property(x => x.Notes).HasMaxLength(2000);
            entity.HasIndex(x => new { x.PurchaseOrderTemplateId, x.SortOrder });
            entity.HasOne(x => x.Material).WithMany().HasForeignKey(x => x.MaterialId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<MaterialRequest>(entity =>
        {
            entity.Property(x => x.RequestedItem).HasMaxLength(420).IsRequired();
            entity.Property(x => x.Quantity).HasColumnType("decimal(18,3)");
            entity.Property(x => x.QuantityText).HasMaxLength(80);
            entity.Property(x => x.Unit).HasMaxLength(40);
            entity.Property(x => x.Quality).HasMaxLength(120);
            entity.Property(x => x.RequestedByUserId).HasMaxLength(450);
            entity.HasOne(x => x.Material).WithMany(x => x.MaterialRequests).HasForeignKey(x => x.MaterialId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Customer>(entity =>
        {
            entity.HasIndex(x => x.Name).IsUnique();
            entity.HasIndex(x => x.TaxNumber);
            entity.Property(x => x.Name).HasMaxLength(220).IsRequired();
            entity.Property(x => x.TaxNumber).HasMaxLength(40);
            entity.Property(x => x.TaxOffice).HasMaxLength(120);
            entity.Property(x => x.Email).HasMaxLength(180);
            entity.Property(x => x.Phone).HasMaxLength(80);
            entity.Property(x => x.ContactPerson).HasMaxLength(160);
            entity.Property(x => x.Website).HasMaxLength(240);
            entity.Property(x => x.PaymentTerm).HasMaxLength(80);
        });

        builder.Entity<Invoice>(entity =>
        {
            entity.HasIndex(x => new { x.Type, x.InvoiceNumber }).IsUnique();
            entity.Property(x => x.InvoiceNumber).HasMaxLength(60).IsRequired();
            entity.Property(x => x.SubTotal).HasColumnType("decimal(18,2)");
            entity.Property(x => x.VatTotal).HasColumnType("decimal(18,2)");
            entity.Property(x => x.DiscountTotal).HasColumnType("decimal(18,2)");
            entity.Property(x => x.GrandTotal).HasColumnType("decimal(18,2)");
            entity.Property(x => x.Currency).HasMaxLength(3);
            entity.Property(x => x.PaymentTerm).HasMaxLength(80);
            entity.HasOne(x => x.Customer).WithMany(x => x.Invoices).HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.Supplier).WithMany(x => x.Invoices).HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(x => x.PurchaseOrder).WithMany(x => x.Invoices).HasForeignKey(x => x.PurchaseOrderId).OnDelete(DeleteBehavior.SetNull);
            entity.HasMany(x => x.Lines).WithOne(x => x.Invoice).HasForeignKey(x => x.InvoiceId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<InvoiceLine>(entity =>
        {
            entity.Property(x => x.Description).HasMaxLength(420).IsRequired();
            entity.Property(x => x.Quantity).HasColumnType("decimal(18,3)");
            entity.Property(x => x.Unit).HasMaxLength(40);
            entity.Property(x => x.UnitPrice).HasColumnType("decimal(18,2)");
            entity.Property(x => x.VatRate).HasColumnType("decimal(5,2)");
            entity.Property(x => x.DiscountAmount).HasColumnType("decimal(18,2)");
            entity.Property(x => x.LineTotal).HasColumnType("decimal(18,2)");
            entity.HasOne(x => x.Material).WithMany(x => x.InvoiceLines).HasForeignKey(x => x.MaterialId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Supplier>(entity =>
        {
            entity.HasIndex(x => x.Name).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(220).IsRequired();
            entity.Property(x => x.Type).HasMaxLength(120);
            entity.Property(x => x.Email).HasMaxLength(180);
            entity.Property(x => x.Phone).HasMaxLength(80);
            entity.Property(x => x.PaymentTerm).HasMaxLength(80);
            entity.Property(x => x.Website).HasMaxLength(240);
        });

        builder.Entity<Material>(entity =>
        {
            entity.HasIndex(x => new { x.Name, x.Category });
            entity.Property(x => x.Name).HasMaxLength(220).IsRequired();
            entity.Property(x => x.CategoryName).HasMaxLength(120);
            entity.Property(x => x.Type).HasMaxLength(120);
            entity.Property(x => x.Grade).HasMaxLength(120);
            entity.Property(x => x.Surface).HasMaxLength(120);
            entity.Property(x => x.Dimensions).HasMaxLength(120);
            entity.Property(x => x.Unit).HasMaxLength(40);
        });

        builder.Entity<StockItem>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(220).IsRequired();
            entity.Property(x => x.Thickness).HasMaxLength(80);
            entity.Property(x => x.Dimensions).HasMaxLength(120);
            entity.Property(x => x.Quantity).HasColumnType("decimal(18,3)");
            entity.Property(x => x.QuantityText).HasMaxLength(80);
            entity.Property(x => x.Unit).HasMaxLength(40);
            entity.Property(x => x.Location).HasMaxLength(120);
            entity.HasOne(x => x.Material).WithMany(x => x.StockItems).HasForeignKey(x => x.MaterialId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<MaterialCategoryDefinition>(entity =>
        {
            entity.HasIndex(x => x.Name).IsUnique();
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(500);
        });

        builder.Entity<RecordComment>(entity =>
        {
            entity.HasIndex(x => new { x.OwnerType, x.OwnerId, x.CreatedAt });
            entity.Property(x => x.CommentText).HasMaxLength(2000).IsRequired();
        });

        builder.Entity<RecordFile>(entity =>
        {
            entity.HasIndex(x => new { x.OwnerType, x.OwnerId, x.CreatedAt });
            entity.Property(x => x.OriginalFileName).HasMaxLength(260).IsRequired();
            entity.Property(x => x.StoredFileName).HasMaxLength(260).IsRequired();
            entity.Property(x => x.RelativePath).HasMaxLength(520).IsRequired();
            entity.Property(x => x.ContentType).HasMaxLength(160);
            entity.Property(x => x.Description).HasMaxLength(500);
        });
    }

    private static void SeedLookups(ModelBuilder builder)
    {
        var createdAt = new DateTime(2026, 5, 26, 0, 0, 0, DateTimeKind.Utc);

        builder.Entity<Material>().HasData(
            new Material { Id = Guid.Parse("2d222bd2-4b85-45ad-ae70-000000000001"), Name = "Paslanmaz", Category = MaterialCategory.Stainless, CategoryName = "Paslanmaz", CreatedAt = createdAt },
            new Material { Id = Guid.Parse("2d222bd2-4b85-45ad-ae70-000000000002"), Name = "Çelik", Category = MaterialCategory.Steel, CategoryName = "Çelik", CreatedAt = createdAt },
            new Material { Id = Guid.Parse("2d222bd2-4b85-45ad-ae70-000000000003"), Name = "Alüminyum", Category = MaterialCategory.Aluminum, CategoryName = "Alüminyum", CreatedAt = createdAt },
            new Material { Id = Guid.Parse("2d222bd2-4b85-45ad-ae70-000000000004"), Name = "Bronz", Category = MaterialCategory.Bronze, CategoryName = "Bronz", CreatedAt = createdAt },
            new Material { Id = Guid.Parse("2d222bd2-4b85-45ad-ae70-000000000005"), Name = "Plastikler", Category = MaterialCategory.Plastic, CategoryName = "Plastikler", CreatedAt = createdAt },
            new Material { Id = Guid.Parse("2d222bd2-4b85-45ad-ae70-000000000006"), Name = "Rulman", Category = MaterialCategory.Bearing, CategoryName = "Rulman", CreatedAt = createdAt },
            new Material { Id = Guid.Parse("2d222bd2-4b85-45ad-ae70-000000000007"), Name = "Motor", Category = MaterialCategory.Motor, CategoryName = "Motor", CreatedAt = createdAt },
            new Material { Id = Guid.Parse("2d222bd2-4b85-45ad-ae70-000000000008"), Name = "Redüktör", Category = MaterialCategory.Gearbox, CategoryName = "Redüktör", CreatedAt = createdAt },
            new Material { Id = Guid.Parse("2d222bd2-4b85-45ad-ae70-000000000009"), Name = "Cıvata", Category = MaterialCategory.Bolt, CategoryName = "Cıvata", CreatedAt = createdAt },
            new Material { Id = Guid.Parse("2d222bd2-4b85-45ad-ae70-000000000010"), Name = "Kılavuz", Category = MaterialCategory.Tap, CategoryName = "Kılavuz", CreatedAt = createdAt },
            new Material { Id = Guid.Parse("2d222bd2-4b85-45ad-ae70-000000000011"), Name = "Paslanmaz saç yüzey", Category = MaterialCategory.StainlessSheetSurface, CategoryName = "Paslanmaz saç yüzey", CreatedAt = createdAt });

        builder.Entity<MaterialCategoryDefinition>().HasData(
            new MaterialCategoryDefinition { Id = Guid.Parse("9f1f88a7-7732-4c70-b2a1-000000000001"), Name = "Paslanmaz", CreatedAt = createdAt },
            new MaterialCategoryDefinition { Id = Guid.Parse("9f1f88a7-7732-4c70-b2a1-000000000002"), Name = "Çelik", CreatedAt = createdAt },
            new MaterialCategoryDefinition { Id = Guid.Parse("9f1f88a7-7732-4c70-b2a1-000000000003"), Name = "Alüminyum", CreatedAt = createdAt },
            new MaterialCategoryDefinition { Id = Guid.Parse("9f1f88a7-7732-4c70-b2a1-000000000004"), Name = "Bronz", CreatedAt = createdAt },
            new MaterialCategoryDefinition { Id = Guid.Parse("9f1f88a7-7732-4c70-b2a1-000000000005"), Name = "Plastikler", CreatedAt = createdAt },
            new MaterialCategoryDefinition { Id = Guid.Parse("9f1f88a7-7732-4c70-b2a1-000000000006"), Name = "Rulman", CreatedAt = createdAt },
            new MaterialCategoryDefinition { Id = Guid.Parse("9f1f88a7-7732-4c70-b2a1-000000000007"), Name = "Motor", CreatedAt = createdAt },
            new MaterialCategoryDefinition { Id = Guid.Parse("9f1f88a7-7732-4c70-b2a1-000000000008"), Name = "Redüktör", CreatedAt = createdAt },
            new MaterialCategoryDefinition { Id = Guid.Parse("9f1f88a7-7732-4c70-b2a1-000000000009"), Name = "Cıvata", CreatedAt = createdAt },
            new MaterialCategoryDefinition { Id = Guid.Parse("9f1f88a7-7732-4c70-b2a1-000000000010"), Name = "Kılavuz", CreatedAt = createdAt },
            new MaterialCategoryDefinition { Id = Guid.Parse("9f1f88a7-7732-4c70-b2a1-000000000011"), Name = "Paslanmaz saç yüzey", CreatedAt = createdAt });

        builder.Entity<TaskCategory>().HasData(
            new TaskCategory { Id = Guid.Parse("7c49d41c-1dc3-45cf-8d4b-000000000001"), Name = "Satış", Color = "#2563eb", CreatedAt = createdAt },
            new TaskCategory { Id = Guid.Parse("7c49d41c-1dc3-45cf-8d4b-000000000002"), Name = "Teklif", Color = "#7c3aed", CreatedAt = createdAt },
            new TaskCategory { Id = Guid.Parse("7c49d41c-1dc3-45cf-8d4b-000000000003"), Name = "Üretim", Color = "#16a34a", CreatedAt = createdAt },
            new TaskCategory { Id = Guid.Parse("7c49d41c-1dc3-45cf-8d4b-000000000004"), Name = "Montaj", Color = "#ea580c", CreatedAt = createdAt });
    }
}
