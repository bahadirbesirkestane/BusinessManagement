using Business.Domain.Entities;
using Business.Domain.Enums;
using Business.Infrastructure.Data;
using Business.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Business.Infrastructure.Seed;

public static class DemoDataSeeder
{
    public static async Task SeedDemoDataAsync(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        if (!configuration.GetValue("SeedDemoData:Enabled", false))
        {
            return;
        }

        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        if (await context.Projects.AnyAsync(x => x.Code.StartsWith("DEMO-PRJ-")))
        {
            return;
        }

        var random = new Random(20260603);
        var now = DateTime.UtcNow;

        await SeedDepartmentsAsync(context, now);
        var users = await SeedUsersAsync(userManager, context);
        var customers = await SeedCustomersAsync(context, now);
        var suppliers = await SeedSuppliersAsync(context, now);
        var materials = await SeedMaterialsAsync(context, now);
        await context.SaveChangesAsync();

        var projects = SeedProjects(context, customers, users, random, now);
        await context.SaveChangesAsync();

        SeedTasks(context, projects, customers, users, random, now);
        SeedPurchaseOrders(context, projects, suppliers, materials, users, random, now);
        SeedMaterialRequests(context, projects, materials, users, random, now);
        SeedStockItems(context, materials, random, now);
        SeedProjectCosts(context, projects, random, now);
        SeedProjectUpdates(context, projects, random, now);
        await context.SaveChangesAsync();

        await SeedInvoicesAsync(context, customers, suppliers, projects, materials, random, now);
        await context.SaveChangesAsync();
    }

    private static async Task SeedDepartmentsAsync(ApplicationDbContext context, DateTime now)
    {
        var departments = new[]
        {
            "Yönetim",
            "Satın Alma",
            "Proje",
            "Üretim",
            "Montaj",
            "Muhasebe"
        };

        foreach (var name in departments)
        {
            if (!await context.Departments.AnyAsync(x => x.Name == name))
            {
                context.Departments.Add(new Department
                {
                    Name = name,
                    Description = $"{name} departmanı",
                    CreatedAt = now
                });
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task<List<ApplicationUser>> SeedUsersAsync(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
    {
        var departments = await context.Departments.ToDictionaryAsync(x => x.Name, x => x.Id);
        (string FullName, string Email, string Role, string Department)[] userSeeds =
        {
            new("Demo Yönetici", "demo.yonetici@business.local", AppRoles.Manager, "Yönetim"),
            new("Demo Satın Alma", "demo.satinalma@business.local", AppRoles.Purchasing, "Satın Alma"),
            new("Demo Proje Sorumlusu", "demo.proje@business.local", AppRoles.ProjectUser, "Proje"),
            new("Demo Usta 1", "demo.usta1@business.local", AppRoles.Workshop, "Üretim"),
            new("Demo Usta 2", "demo.usta2@business.local", AppRoles.Workshop, "Montaj"),
            new("Demo Muhasebe", "demo.muhasebe@business.local", AppRoles.ProjectUser, "Muhasebe"),
            new("Demo Planlama", "demo.planlama@business.local", AppRoles.ProjectUser, "Proje"),
            new("Demo Depo", "demo.depo@business.local", AppRoles.Workshop, "Satın Alma")
        };

        var users = new List<ApplicationUser>();
        foreach (var seed in userSeeds)
        {
            var user = await userManager.FindByEmailAsync(seed.Email);
            if (user is null)
            {
                user = new ApplicationUser
                {
                    UserName = seed.Email,
                    Email = seed.Email,
                    EmailConfirmed = true,
                    FullName = seed.FullName,
                    DepartmentId = departments.GetValueOrDefault(seed.Department),
                    IsActive = true
                };

                var result = await userManager.CreateAsync(user, "Demo123!");
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException(string.Join("; ", result.Errors.Select(x => x.Description)));
                }
            }

            if (!await userManager.IsInRoleAsync(user, seed.Role))
            {
                await userManager.AddToRoleAsync(user, seed.Role);
            }

            users.Add(user);
        }

        return users;
    }

    private static async Task<List<Customer>> SeedCustomersAsync(ApplicationDbContext context, DateTime now)
    {
        var sectors = new[] { "Makina", "Otomotiv", "Gıda", "Ambalaj", "Enerji", "Tekstil", "Kimya", "Metal" };
        var customers = new List<Customer>();
        for (var i = 1; i <= 28; i++)
        {
            var customerName = $"DEMO Müşteri {i:00} {sectors[i % sectors.Length]}";
            var customer = await context.Customers.FirstOrDefaultAsync(x => x.Name == customerName);
            if (customer is null)
            {
                customer = new Customer
                {
                    Name = customerName,
                    TaxNumber = $"10{i:00000000}",
                    TaxOffice = "İstanbul",
                    Email = $"musteri{i:00}@demo.local",
                    Phone = $"0212 555 {i:0000}",
                    ContactPerson = $"Yetkili {i:00}",
                    Address = $"Organize Sanayi Bölgesi No:{i}",
                    Website = $"https://demo-musteri-{i:00}.local",
                    PaymentTerm = $"{15 + (i % 4) * 15} gün",
                    IsActive = true,
                    Notes = "Demo veri",
                    CreatedAt = now.AddDays(-i)
                };
                context.Customers.Add(customer);
            }

            customers.Add(customer);
        }

        return customers;
    }

    private static async Task<List<Supplier>> SeedSuppliersAsync(ApplicationDbContext context, DateTime now)
    {
        var types = new[] { "Malzeme", "Rulman", "Motor", "Sac", "Kesim", "Boya", "Hırdavat", "Nakliye" };
        var suppliers = new List<Supplier>();
        for (var i = 1; i <= 24; i++)
        {
            var supplierName = $"DEMO Tedarikçi {i:00} {types[i % types.Length]}";
            var supplier = await context.Suppliers.FirstOrDefaultAsync(x => x.Name == supplierName);
            if (supplier is null)
            {
                supplier = new Supplier
                {
                    Name = supplierName,
                    Type = types[i % types.Length],
                    Email = $"tedarikci{i:00}@demo.local",
                    Phone = $"0262 444 {i:0000}",
                    PaymentTerm = $"{30 + (i % 3) * 15} gün",
                    Address = $"Sanayi Sitesi Blok {i}",
                    Website = $"https://demo-tedarikci-{i:00}.local",
                    Reliability = (SupplierReliability)((i % 5) + 1),
                    Notes = i % 7 == 0 ? "Alternatif tedarikçi olarak değerlendiriliyor." : "Demo veri",
                    CreatedAt = now.AddDays(-i)
                };
                context.Suppliers.Add(supplier);
            }

            suppliers.Add(supplier);
        }

        return suppliers;
    }

    private static async Task<List<Material>> SeedMaterialsAsync(ApplicationDbContext context, DateTime now)
    {
        (string Name, MaterialCategory Category, string Type, string Grade, string Surface, string Dimensions, string Unit)[] materialSeeds =
        {
            ("DEMO Mil 1040", MaterialCategory.Steel, "Mil", "1040", "Taşlanmış", "Ø20-Ø80", "mm"),
            ("DEMO Boru Paslanmaz", MaterialCategory.Stainless, "Boru", "304", "Parlak", "20x2-100x5", "mt"),
            ("DEMO Profil Alüminyum", MaterialCategory.Aluminum, "Profil", "6061", "Eloksal", "20x20-80x80", "mt"),
            ("DEMO Bronz Burç", MaterialCategory.Bronze, "Burç", "CuSn12", "İşlenmiş", "Ø10-Ø60", "adet"),
            ("DEMO Polyamid Levha", MaterialCategory.Plastic, "Levha", "PA6", "Doğal", "10-50 mm", "kg"),
            ("DEMO Rulman 6205", MaterialCategory.Bearing, "Rulman", "6205", "Kapalı", "25x52x15", "adet"),
            ("DEMO Motor 1.5kW", MaterialCategory.Motor, "Motor", "IE3", "Flanşlı", "1.5kW", "adet"),
            ("DEMO Redüktör 1/30", MaterialCategory.Gearbox, "Redüktör", "Helisel", "Boyalı", "1/30", "adet"),
            ("DEMO Civata M8", MaterialCategory.Bolt, "Civata", "8.8", "Galvaniz", "M8x30", "adet"),
            ("DEMO Kılavuz M10", MaterialCategory.Tap, "Kılavuz", "HSS", "Kaplamalı", "M10", "adet"),
            ("DEMO Sac 304", MaterialCategory.StainlessSheetSurface, "Sac", "304", "2B", "1-5 mm", "kg"),
            ("DEMO Lazer Kesim Hizmeti", MaterialCategory.Other, "Hizmet", "DXF", "Kesim", "Proje bazlı", "saat")
        };

        var materials = await context.Materials.Where(x => x.Name.StartsWith("DEMO ")).ToListAsync();
        foreach (var seed in materialSeeds)
        {
            var material = materials.FirstOrDefault(x => x.Name == seed.Name);
            if (material is null)
            {
                material = new Material
                {
                    Name = seed.Name,
                    Category = seed.Category,
                    CategoryName = seed.Category.ToString(),
                    Type = seed.Type,
                    Grade = seed.Grade,
                    Surface = seed.Surface,
                    Dimensions = seed.Dimensions,
                    Unit = seed.Unit,
                    Description = "Demo malzeme",
                    CreatedAt = now
                };
                context.Materials.Add(material);
                materials.Add(material);
            }
        }

        return materials;
    }

    private static List<Project> SeedProjects(ApplicationDbContext context, IReadOnlyList<Customer> customers, IReadOnlyList<ApplicationUser> users, Random random, DateTime now)
    {
        var projectTypes = new[] { "Konveyör", "Fikstür", "Revizyon", "Hat Kurulumu", "Makina İmalatı", "Pano", "Montaj", "Yedek Parça" };
        var statuses = new[] { ProjectStatus.Planned, ProjectStatus.InProgress, ProjectStatus.Waiting, ProjectStatus.Completed, ProjectStatus.Cancelled };
        var priorities = new[] { ProjectPriority.Low, ProjectPriority.Normal, ProjectPriority.High, ProjectPriority.Critical };
        var projects = new List<Project>();

        for (var i = 1; i <= 72; i++)
        {
            var startDate = DateTime.Today.AddDays(random.Next(-120, 45));
            var targetDate = startDate.AddDays(random.Next(10, 95));
            var status = statuses[random.Next(statuses.Length)];
            var project = new Project
            {
                Code = $"DEMO-PRJ-{i:000}",
                Name = $"{projectTypes[i % projectTypes.Length]} Projesi {i:000}",
                CustomerId = customers[random.Next(customers.Count)].Id,
                Description = "Demo veri ile oluşturulan proje kaydı.",
                Status = status,
                Priority = priorities[random.Next(priorities.Length)],
                StartDate = startDate,
                TargetEndDate = targetDate,
                CompletedAt = status == ProjectStatus.Completed ? targetDate.AddDays(random.Next(-3, 8)) : null,
                Budget = random.Next(80, 900) * 1000,
                Currency = "TRY",
                Notes = "Performans ve listeleme testi için demo proje.",
                CreatedAt = now.AddDays(-random.Next(1, 140)),
                CreatedByUserId = users[random.Next(users.Count)].Id
            };
            context.Projects.Add(project);
            projects.Add(project);
        }

        return projects;
    }

    private static void SeedTasks(ApplicationDbContext context, IReadOnlyList<Project> projects, IReadOnlyList<Customer> customers, IReadOnlyList<ApplicationUser> users, Random random, DateTime now)
    {
        var categories = context.TaskCategories.Local.Any()
            ? context.TaskCategories.Local.ToList()
            : context.TaskCategories.ToList();
        var taskNames = new[] { "Teknik çizim kontrolü", "Malzeme listesi hazırlama", "Teklif revizyonu", "Satın alma takibi", "Üretim planı", "Kaynak kontrolü", "Montaj hazırlığı", "Kalite kontrol", "Sevkiyat planı", "Müşteri bilgilendirme" };
        var statuses = new[] { WorkTaskStatus.Todo, WorkTaskStatus.InProgress, WorkTaskStatus.Waiting, WorkTaskStatus.InReview, WorkTaskStatus.Done, WorkTaskStatus.Cancelled };
        var priorities = new[] { ProjectPriority.Low, ProjectPriority.Normal, ProjectPriority.High, ProjectPriority.Critical };

        for (var i = 1; i <= 360; i++)
        {
            var project = projects[random.Next(projects.Count)];
            var status = statuses[random.Next(statuses.Length)];
            var progress = status switch
            {
                WorkTaskStatus.Done => 100,
                WorkTaskStatus.Cancelled => random.Next(0, 50),
                WorkTaskStatus.Todo => 0,
                _ => random.Next(10, 90)
            };

            var task = new ProjectTask
            {
                ProjectId = i % 12 == 0 ? null : project.Id,
                CustomerId = i % 12 == 0 ? customers[random.Next(customers.Count)].Id : null,
                TaskCategoryId = categories.Count == 0 ? null : categories[random.Next(categories.Count)].Id,
                Title = $"{taskNames[i % taskNames.Length]} #{i:000}",
                Description = "Demo görev açıklaması.",
                Status = status,
                Priority = priorities[random.Next(priorities.Length)],
                StartDate = DateTime.Today.AddDays(random.Next(-45, 15)),
                DueDate = DateTime.Today.AddDays(random.Next(-20, 60)),
                CompletedAt = status == WorkTaskStatus.Done ? now.AddDays(-random.Next(1, 20)) : null,
                SubmittedForReviewAt = status == WorkTaskStatus.InReview ? now.AddDays(-random.Next(0, 5)) : null,
                ProgressPercent = progress,
                ResponsibleUserId = users[random.Next(users.Count)].Id,
                AssignedToUserId = users[random.Next(users.Count)].Id,
                Notes = "Demo görev",
                CreatedAt = now.AddDays(-random.Next(1, 90)),
                CreatedByUserId = users[random.Next(users.Count)].Id
            };
            context.ProjectTasks.Add(task);

            foreach (var user in users.OrderBy(_ => random.Next()).Take(random.Next(1, 4)))
            {
                task.Assignments.Add(new ProjectTaskAssignment
                {
                    ProjectTaskId = task.Id,
                    UserId = user.Id,
                    CreatedAt = task.CreatedAt
                });
            }
        }
    }

    private static void SeedPurchaseOrders(ApplicationDbContext context, IReadOnlyList<Project> projects, IReadOnlyList<Supplier> suppliers, IReadOnlyList<Material> materials, IReadOnlyList<ApplicationUser> users, Random random, DateTime now)
    {
        var statuses = new[] { PurchaseOrderStatus.Requested, PurchaseOrderStatus.Ordered, PurchaseOrderStatus.PartiallyDelivered, PurchaseOrderStatus.Delivered, PurchaseOrderStatus.Cancelled };
        for (var i = 1; i <= 280; i++)
        {
            var project = i % 5 == 0 ? null : projects[random.Next(projects.Count)];
            var material = materials[random.Next(materials.Count)];
            var quantity = random.Next(1, 80);
            var unitPrice = random.Next(150, 25000);
            var status = statuses[random.Next(statuses.Length)];
            var orderDate = DateTime.Today.AddDays(-random.Next(1, 100));
            context.PurchaseOrders.Add(new PurchaseOrder
            {
                ProjectId = project?.Id,
                SupplierId = suppliers[random.Next(suppliers.Count)].Id,
                MaterialId = material.Id,
                OrderNumber = $"DEMO-PO-{i:0000}",
                Scope = project is null ? PurchaseOrderScope.General : PurchaseOrderScope.Project,
                TrackingState = random.Next(0, 2),
                Content = $"{material.Name} siparişi #{i:000}",
                Quantity = quantity,
                QuantityText = $"{quantity} {material.Unit ?? "adet"}",
                Unit = material.Unit,
                Quality = material.Grade,
                Status = status,
                OrderDate = orderDate,
                ExpectedArrivalDate = orderDate.AddDays(random.Next(3, 25)),
                ArrivalDate = status == PurchaseOrderStatus.Delivered ? orderDate.AddDays(random.Next(4, 22)) : null,
                RequestedBy = users[random.Next(users.Count)].FullName,
                PaymentTerm = $"{random.Next(1, 5) * 15} gün",
                UnitPrice = unitPrice,
                UnitPriceText = $"{unitPrice:N2} TRY",
                OrderTotal = quantity * unitPrice,
                Currency = "TRY",
                VatRate = 20,
                Notes = "Demo sipariş",
                IsActive = true,
                CreatedAt = now.AddDays(-random.Next(1, 100)),
                CreatedByUserId = users[random.Next(users.Count)].Id
            });
        }
    }

    private static void SeedMaterialRequests(ApplicationDbContext context, IReadOnlyList<Project> projects, IReadOnlyList<Material> materials, IReadOnlyList<ApplicationUser> users, Random random, DateTime now)
    {
        var statuses = new[] { MaterialRequestStatus.Requested, MaterialRequestStatus.Approved, MaterialRequestStatus.Ordered, MaterialRequestStatus.Fulfilled, MaterialRequestStatus.Cancelled };
        for (var i = 1; i <= 180; i++)
        {
            var material = materials[random.Next(materials.Count)];
            var quantity = random.Next(1, 60);
            context.MaterialRequests.Add(new MaterialRequest
            {
                ProjectId = i % 7 == 0 ? null : projects[random.Next(projects.Count)].Id,
                MaterialId = material.Id,
                RequestedItem = $"{material.Name} ihtiyacı #{i:000}",
                Quantity = quantity,
                QuantityText = $"{quantity} {material.Unit ?? "adet"}",
                Unit = material.Unit,
                Quality = material.Grade,
                Status = statuses[random.Next(statuses.Length)],
                NeededBy = DateTime.Today.AddDays(random.Next(-15, 70)),
                RequestedByUserId = users[random.Next(users.Count)].Id,
                Notes = "Demo ihtiyaç kaydı",
                CreatedAt = now.AddDays(-random.Next(1, 80))
            });
        }
    }

    private static void SeedStockItems(ApplicationDbContext context, IReadOnlyList<Material> materials, Random random, DateTime now)
    {
        var locations = new[] { "A-01", "A-02", "B-01", "B-04", "C-02", "Kesimhane", "Montaj Rafı" };
        var statuses = new[] { StockStatus.InStock, StockStatus.Reserved, StockStatus.LowStock, StockStatus.OutOfStock };
        for (var i = 1; i <= 220; i++)
        {
            var material = materials[random.Next(materials.Count)];
            var quantity = random.Next(0, 160);
            context.StockItems.Add(new StockItem
            {
                MaterialId = material.Id,
                Name = $"{material.Type ?? material.Name} stok kalemi {i:000}",
                Thickness = $"{random.Next(1, 20)} mm",
                Dimensions = $"{random.Next(20, 160)}x{random.Next(20, 160)}",
                Quantity = quantity,
                QuantityText = $"{quantity} {material.Unit ?? "adet"}",
                Unit = material.Unit,
                Status = quantity == 0 ? StockStatus.OutOfStock : quantity < 10 ? StockStatus.LowStock : statuses[random.Next(statuses.Length)],
                Location = locations[random.Next(locations.Length)],
                Notes = "Demo stok",
                CreatedAt = now.AddDays(-random.Next(1, 120))
            });
        }
    }

    private static void SeedProjectCosts(ApplicationDbContext context, IReadOnlyList<Project> projects, Random random, DateTime now)
    {
        var descriptions = new[] { "Ek işçilik", "Dış hizmet", "Nakliye", "Montaj gideri", "Genel atölye gideri", "Kesim hizmeti" };
        var types = new[] { CostItemType.Labor, CostItemType.OutsourcedService, CostItemType.Shipping, CostItemType.Overhead, CostItemType.Other };
        for (var i = 1; i <= 150; i++)
        {
            context.ProjectCostItems.Add(new ProjectCostItem
            {
                ProjectId = i % 10 == 0 ? null : projects[random.Next(projects.Count)].Id,
                Type = types[random.Next(types.Length)],
                Description = $"{descriptions[i % descriptions.Length]} #{i:000}",
                Amount = random.Next(2, 85) * 1000,
                Currency = "TRY",
                CostDate = DateTime.Today.AddDays(-random.Next(1, 100)),
                Notes = "Demo maliyet",
                CreatedAt = now.AddDays(-random.Next(1, 100))
            });
        }
    }

    private static void SeedProjectUpdates(ApplicationDbContext context, IReadOnlyList<Project> projects, Random random, DateTime now)
    {
        var titles = new[] { "Üretim başladı", "Malzeme bekleniyor", "Montaj tamamlandı", "Müşteri revizyon istedi", "Kontrol notu eklendi", "Sevkiyat planlandı" };
        foreach (var project in projects)
        {
            for (var i = 0; i < random.Next(2, 6); i++)
            {
                context.ProjectUpdates.Add(new ProjectUpdate
                {
                    ProjectId = project.Id,
                    Title = titles[random.Next(titles.Length)],
                    Description = "Demo proje güncellemesi.",
                    CreatedAt = now.AddDays(-random.Next(1, 60))
                });
            }
        }
    }

    private static async Task SeedInvoicesAsync(ApplicationDbContext context, IReadOnlyList<Customer> customers, IReadOnlyList<Supplier> suppliers, IReadOnlyList<Project> projects, IReadOnlyList<Material> materials, Random random, DateTime now)
    {
        var statuses = new[] { InvoiceStatus.Draft, InvoiceStatus.Issued, InvoiceStatus.PartiallyPaid, InvoiceStatus.Paid, InvoiceStatus.Overdue };
        for (var i = 1; i <= 90; i++)
        {
            var type = i % 3 == 0 ? InvoiceType.Purchase : InvoiceType.Sales;
            var lineCount = random.Next(1, 4);
            var invoice = new Invoice
            {
                CustomerId = type == InvoiceType.Sales ? customers[random.Next(customers.Count)].Id : null,
                SupplierId = type == InvoiceType.Purchase ? suppliers[random.Next(suppliers.Count)].Id : null,
                ProjectId = i % 5 == 0 ? null : projects[random.Next(projects.Count)].Id,
                InvoiceNumber = $"DEMO-INV-{i:0000}",
                Type = type,
                Status = statuses[random.Next(statuses.Length)],
                IssueDate = DateTime.Today.AddDays(-random.Next(1, 120)),
                DueDate = DateTime.Today.AddDays(random.Next(-20, 60)),
                Currency = "TRY",
                PaymentTerm = $"{random.Next(1, 5) * 15} gün",
                Notes = "Demo fatura",
                CreatedAt = now.AddDays(-random.Next(1, 120))
            };

            for (var lineIndex = 1; lineIndex <= lineCount; lineIndex++)
            {
                var material = materials[random.Next(materials.Count)];
                var quantity = random.Next(1, 20);
                var unitPrice = random.Next(300, 18000);
                var lineTotal = quantity * unitPrice;
                invoice.Lines.Add(new InvoiceLine
                {
                    MaterialId = material.Id,
                    Description = $"{material.Name} fatura satırı",
                    Quantity = quantity,
                    Unit = material.Unit,
                    UnitPrice = unitPrice,
                    VatRate = 20,
                    DiscountAmount = 0,
                    LineTotal = lineTotal,
                    CreatedAt = now
                });
                invoice.SubTotal += lineTotal;
            }

            invoice.VatTotal = invoice.SubTotal * 0.20m;
            invoice.GrandTotal = invoice.SubTotal + invoice.VatTotal - invoice.DiscountTotal;
            if (invoice.Status == InvoiceStatus.Paid)
            {
                invoice.PaidAt = invoice.IssueDate.AddDays(random.Next(5, 35));
            }

            context.Invoices.Add(invoice);
        }

        await Task.CompletedTask;
    }
}
