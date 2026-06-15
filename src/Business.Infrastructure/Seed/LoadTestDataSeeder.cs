using Business.Domain.Entities;
using Business.Domain.Enums;
using Business.Infrastructure.Data;
using Business.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Business.Infrastructure.Seed;

public static class LoadTestDataSeeder
{
    private const string SeedCommand = "--seed-load-test-data";
    private const string ClearCommand = "--clear-load-test-data";
    private const string ProjectCodePrefix = "YUKTEST-PRJ-";
    private const string OrderNumberPrefix = "YUKTEST-PO-";
    private const string CustomerPrefix = "Yuk Test Musteri ";
    private const string SupplierPrefix = "Yuk Test Tedarikci ";
    private const string Marker = "[YUKTEST]";

    public static async Task<bool> TryHandleCommandAsync(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        IHostEnvironment environment,
        IReadOnlyList<string> args,
        CancellationToken cancellationToken = default)
    {
        var shouldSeed = args.Contains(SeedCommand, StringComparer.OrdinalIgnoreCase);
        var shouldClear = args.Contains(ClearCommand, StringComparer.OrdinalIgnoreCase);

        if (!shouldSeed && !shouldClear)
        {
            return false;
        }

        if (!environment.IsDevelopment())
        {
            throw new InvalidOperationException("Yuk testi verisi komutlari sadece Development ortaminda calistirilabilir.");
        }

        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var options = configuration.GetSection("LoadTestData").Get<LoadTestDataOptions>() ?? new LoadTestDataOptions();

        if (shouldClear)
        {
            await ClearAsync(context, cancellationToken);
            Console.WriteLine("Yuk testi verileri temizlendi.");
            return true;
        }

        if (await HasGeneratedDataAsync(context, cancellationToken))
        {
            throw new InvalidOperationException("Yuk testi verileri zaten mevcut. Once --clear-load-test-data komutunu calistirin.");
        }

        await SeedAsync(context, userManager, options, cancellationToken);
        Console.WriteLine("Yuk testi verileri olusturuldu.");
        return true;
    }

    private static async Task SeedAsync(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        LoadTestDataOptions options,
        CancellationToken cancellationToken)
    {
        var random = new Random(options.Seed);
        var now = DateTime.UtcNow;
        var startMonth = new DateTime(now.Year, now.Month, 1).AddYears(-options.Years + 1).AddMonths(-11);
        var taskCategories = await context.TaskCategories.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken);
        var users = await userManager.Users.Where(x => x.IsActive).OrderBy(x => x.FullName).ToListAsync(cancellationToken);

        var customers = Enumerable.Range(1, options.Customers)
            .Select(index => new Customer
            {
                Name = $"{CustomerPrefix}{index:000}",
                ContactPerson = $"Yetkili {index:000}",
                Email = $"yuktest-musteri-{index:000}@local.test",
                Phone = $"0212 700 {index:0000}",
                Address = $"Yuk Test Sanayi Blok {index}",
                PaymentTerm = $"{15 + (index % 4) * 15} gun",
                Notes = Marker,
                IsActive = true,
                CreatedAt = now.AddDays(-index)
            })
            .ToList();

        var suppliers = Enumerable.Range(1, options.Suppliers)
            .Select(index => new Supplier
            {
                Name = $"{SupplierPrefix}{index:000}",
                Type = index % 2 == 0 ? "Malzeme" : "Hizmet",
                Email = $"yuktest-tedarikci-{index:000}@local.test",
                Phone = $"0262 800 {index:0000}",
                PaymentTerm = $"{30 + (index % 3) * 15} gun",
                Address = $"Yuk Test Tedarik Sitesi {index}",
                Reliability = (SupplierReliability)Math.Clamp((index % 5) + 1, 1, 5),
                Notes = Marker,
                CreatedAt = now.AddDays(-index)
            })
            .ToList();

        context.Customers.AddRange(customers);
        context.Suppliers.AddRange(suppliers);
        await context.SaveChangesAsync(cancellationToken);

        var projects = new List<Project>();
        var tasks = new List<ProjectTask>();
        var assignments = new List<ProjectTaskAssignment>();
        var orders = new List<PurchaseOrder>();
        var costs = new List<ProjectCostItem>();
        var updates = new List<ProjectUpdate>();
        var projectSequence = 1;
        var taskSequence = 1;
        var orderSequence = 1;

        for (var monthIndex = 0; monthIndex < options.Years * 12; monthIndex++)
        {
            var monthStart = startMonth.AddMonths(monthIndex);
            for (var projectIndex = 0; projectIndex < options.ProjectsPerMonth; projectIndex++)
            {
                var status = PickProjectStatus(random, monthStart, now);
                var startDate = monthStart.AddDays(random.Next(0, 25));
                var targetDate = startDate.AddDays(random.Next(15, 120));
                var project = new Project
                {
                    Code = $"{ProjectCodePrefix}{projectSequence:000000}",
                    Name = $"Yuk Test Proje {projectSequence:000000}",
                    CustomerId = customers[random.Next(customers.Count)].Id,
                    Description = $"{Marker} Performans testi icin olusturulan proje kaydi.",
                    Notes = Marker,
                    Visibility = RecordVisibility.General,
                    Status = status,
                    Priority = (ProjectPriority)random.Next(0, 4),
                    StartDate = startDate,
                    TargetEndDate = targetDate,
                    CompletedAt = status == ProjectStatus.Completed ? targetDate.AddDays(random.Next(-5, 10)) : null,
                    Budget = random.Next(250, 2500) * 1000,
                    Currency = "TRY",
                    CreatedAt = startDate.AddDays(-random.Next(1, 20)),
                    CreatedByUserId = users.Count == 0 ? null : users[random.Next(users.Count)].Id
                };
                projects.Add(project);
                projectSequence++;

                var taskCount = random.Next(options.MinTasksPerProject, options.MaxTasksPerProject + 1);
                for (var i = 0; i < taskCount; i++)
                {
                    var taskStatus = PickTaskStatus(random, project.Status, now, targetDate);
                    var taskStart = startDate.AddDays(random.Next(0, 20));
                    var taskDue = taskStart.AddDays(random.Next(5, 45));
                    var responsibleUser = users.Count == 0 ? null : users[random.Next(users.Count)];
                    var assignedUser = users.Count == 0 ? null : users[random.Next(users.Count)];
                    var task = new ProjectTask
                    {
                        Project = project,
                        TaskCategoryId = taskCategories.Count == 0 ? null : taskCategories[random.Next(taskCategories.Count)].Id,
                        Title = $"Yuk Test Gorev {taskSequence:000000}",
                        Description = $"{Marker} Liste ve detay performans testi gorevi.",
                        Visibility = RecordVisibility.General,
                        Status = taskStatus,
                        Priority = (ProjectPriority)random.Next(0, 4),
                        StartDate = taskStart,
                        DueDate = taskDue,
                        CompletedAt = taskStatus == WorkTaskStatus.Done ? taskDue.AddDays(random.Next(-2, 5)) : null,
                        SubmittedForReviewAt = taskStatus == WorkTaskStatus.InReview ? taskDue.AddDays(-1) : null,
                        ProgressPercent = CalculateProgress(taskStatus, random),
                        ResponsibleUserId = responsibleUser?.Id,
                        AssignedToUserId = assignedUser?.Id,
                        SortOrder = i + 1,
                        Notes = Marker,
                        CreatedAt = taskStart.AddDays(-random.Next(0, 10)),
                        CreatedByUserId = responsibleUser?.Id ?? assignedUser?.Id
                    };
                    tasks.Add(task);
                    taskSequence++;

                    if (users.Count > 0)
                    {
                        foreach (var user in users.OrderBy(_ => random.Next()).Take(random.Next(1, Math.Min(3, users.Count) + 1)))
                        {
                            assignments.Add(new ProjectTaskAssignment
                            {
                                ProjectTask = task,
                                UserId = user.Id,
                                CreatedAt = task.CreatedAt
                            });
                        }
                    }
                }

                var orderCount = random.Next(options.MinOrdersPerProject, options.MaxOrdersPerProject + 1);
                for (var i = 0; i < orderCount; i++)
                {
                    var orderDate = startDate.AddDays(random.Next(0, 40));
                    var orderStatus = PickOrderStatus(random, project.Status, now, orderDate);
                    var quantity = random.Next(1, 40);
                    var unitPrice = random.Next(500, 22000);
                    orders.Add(new PurchaseOrder
                    {
                        Project = project,
                        SupplierId = suppliers[random.Next(suppliers.Count)].Id,
                        OrderNumber = $"{OrderNumberPrefix}{orderSequence:000000}",
                        Visibility = RecordVisibility.General,
                        Scope = PurchaseOrderScope.Project,
                        TrackingState = 0,
                        Content = $"Yuk Test Siparis {orderSequence:000000}",
                        Quantity = quantity,
                        QuantityText = $"{quantity} adet",
                        Unit = "adet",
                        Quality = "Standart",
                        Status = orderStatus,
                        OrderDate = orderDate,
                        ExpectedArrivalDate = orderDate.AddDays(random.Next(5, 25)),
                        ArrivalDate = orderStatus == PurchaseOrderStatus.Delivered ? orderDate.AddDays(random.Next(6, 26)) : null,
                        RequestedBy = users.Count == 0 ? "Yuk Test" : users[random.Next(users.Count)].FullName,
                        RequestedByUserId = users.Count == 0 ? null : users[random.Next(users.Count)].Id,
                        PaymentTerm = $"{30 + (i % 3) * 15} gun",
                        UnitPrice = unitPrice,
                        UnitPriceText = $"{unitPrice:N2} TRY",
                        OrderTotal = quantity * unitPrice,
                        Currency = "TRY",
                        VatRate = 20,
                        Notes = Marker,
                        IsActive = true,
                        CreatedAt = orderDate,
                        CreatedByUserId = users.Count == 0 ? null : users[random.Next(users.Count)].Id
                    });
                    orderSequence++;
                }

                var costCount = random.Next(options.MinCostsPerProject, options.MaxCostsPerProject + 1);
                for (var i = 0; i < costCount; i++)
                {
                    costs.Add(new ProjectCostItem
                    {
                        Project = project,
                        Type = (CostItemType)random.Next(0, Enum.GetValues<CostItemType>().Length),
                        Description = $"{Marker} Yuk Test Maliyet {projectSequence:000000}-{i + 1:00}",
                        Amount = random.Next(5, 120) * 1000,
                        Currency = "TRY",
                        CostDate = startDate.AddDays(random.Next(0, 50)),
                        Notes = Marker,
                        Visibility = RecordVisibility.General,
                        CreatedAt = startDate.AddDays(random.Next(0, 20))
                    });
                }

                var updateCount = random.Next(1, options.MaxUpdatesPerProject + 1);
                for (var i = 0; i < updateCount; i++)
                {
                    updates.Add(new ProjectUpdate
                    {
                        Project = project,
                        Title = $"Yuk Test Guncelleme {i + 1:00}",
                        Description = Marker,
                        CreatedAt = startDate.AddDays(random.Next(0, 40))
                    });
                }
            }

            for (var generalIndex = 0; generalIndex < options.GeneralOrdersPerMonth; generalIndex++)
            {
                var orderDate = monthStart.AddDays(random.Next(0, 25));
                var orderStatus = PickOrderStatus(random, ProjectStatus.InProgress, now, orderDate);
                var quantity = random.Next(1, 25);
                var unitPrice = random.Next(300, 18000);
                orders.Add(new PurchaseOrder
                {
                    SupplierId = suppliers[random.Next(suppliers.Count)].Id,
                    OrderNumber = $"{OrderNumberPrefix}{orderSequence:000000}",
                    Visibility = RecordVisibility.General,
                    Scope = PurchaseOrderScope.General,
                    TrackingState = 0,
                    Content = $"Yuk Test Genel Siparis {orderSequence:000000}",
                    Quantity = quantity,
                    QuantityText = $"{quantity} adet",
                    Unit = "adet",
                    Quality = "Standart",
                    Status = orderStatus,
                    OrderDate = orderDate,
                    ExpectedArrivalDate = orderDate.AddDays(random.Next(5, 20)),
                    ArrivalDate = orderStatus == PurchaseOrderStatus.Delivered ? orderDate.AddDays(random.Next(6, 21)) : null,
                    RequestedBy = users.Count == 0 ? "Yuk Test" : users[random.Next(users.Count)].FullName,
                    RequestedByUserId = users.Count == 0 ? null : users[random.Next(users.Count)].Id,
                    PaymentTerm = "30 gun",
                    UnitPrice = unitPrice,
                    UnitPriceText = $"{unitPrice:N2} TRY",
                    OrderTotal = quantity * unitPrice,
                    Currency = "TRY",
                    VatRate = 20,
                    Notes = Marker,
                    IsActive = true,
                    CreatedAt = orderDate,
                    CreatedByUserId = users.Count == 0 ? null : users[random.Next(users.Count)].Id
                });
                orderSequence++;
            }
        }

        context.Projects.AddRange(projects);
        context.ProjectTasks.AddRange(tasks);
        context.ProjectTaskAssignments.AddRange(assignments);
        context.PurchaseOrders.AddRange(orders);
        context.ProjectCostItems.AddRange(costs);
        context.ProjectUpdates.AddRange(updates);
        await context.SaveChangesAsync(cancellationToken);

        Console.WriteLine($"Yuk testi verisi hazirlandi: {projects.Count} proje, {tasks.Count} gorev, {orders.Count} siparis.");
    }

    private static async Task ClearAsync(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        var projectIds = await context.Projects
            .Where(x => x.Code.StartsWith(ProjectCodePrefix))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var taskIds = await context.ProjectTasks
            .Where(x => x.Notes == Marker || (x.ProjectId.HasValue && projectIds.Contains(x.ProjectId.Value)))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var orderIds = await context.PurchaseOrders
            .Where(x => x.OrderNumber.StartsWith(OrderNumberPrefix))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        if (taskIds.Count > 0)
        {
            await context.ProjectTaskAssignments.Where(x => taskIds.Contains(x.ProjectTaskId)).ExecuteDeleteAsync(cancellationToken);
            await context.ProjectTaskUpdates.Where(x => taskIds.Contains(x.ProjectTaskId)).ExecuteDeleteAsync(cancellationToken);
            await context.RecordComments.Where(x => x.OwnerType == RecordOwnerType.ProjectTask && taskIds.Contains(x.OwnerId)).ExecuteDeleteAsync(cancellationToken);
            await context.RecordFiles.Where(x => x.OwnerType == RecordOwnerType.ProjectTask && taskIds.Contains(x.OwnerId)).ExecuteDeleteAsync(cancellationToken);
            await context.ProjectTasks.Where(x => taskIds.Contains(x.Id)).ExecuteDeleteAsync(cancellationToken);
        }

        if (orderIds.Count > 0)
        {
            await context.RecordComments.Where(x => x.OwnerType == RecordOwnerType.PurchaseOrder && orderIds.Contains(x.OwnerId)).ExecuteDeleteAsync(cancellationToken);
            await context.RecordFiles.Where(x => x.OwnerType == RecordOwnerType.PurchaseOrder && orderIds.Contains(x.OwnerId)).ExecuteDeleteAsync(cancellationToken);
            await context.Invoices.Where(x => x.PurchaseOrderId.HasValue && orderIds.Contains(x.PurchaseOrderId.Value)).ExecuteDeleteAsync(cancellationToken);
            await context.PurchaseOrders.Where(x => orderIds.Contains(x.Id)).ExecuteDeleteAsync(cancellationToken);
        }

        if (projectIds.Count > 0)
        {
            await context.ProjectUpdates.Where(x => projectIds.Contains(x.ProjectId)).ExecuteDeleteAsync(cancellationToken);
            await context.RecordComments.Where(x => x.OwnerType == RecordOwnerType.Project && projectIds.Contains(x.OwnerId)).ExecuteDeleteAsync(cancellationToken);
            await context.RecordFiles.Where(x => x.OwnerType == RecordOwnerType.Project && projectIds.Contains(x.OwnerId)).ExecuteDeleteAsync(cancellationToken);
            await context.ProjectCostItems.Where(x => x.ProjectId.HasValue && projectIds.Contains(x.ProjectId.Value)).ExecuteDeleteAsync(cancellationToken);
            await context.MaterialRequests.Where(x => x.ProjectId.HasValue && projectIds.Contains(x.ProjectId.Value)).ExecuteDeleteAsync(cancellationToken);
            await context.Invoices.Where(x => x.ProjectId.HasValue && projectIds.Contains(x.ProjectId.Value)).ExecuteDeleteAsync(cancellationToken);
            await context.Projects.Where(x => projectIds.Contains(x.Id)).ExecuteDeleteAsync(cancellationToken);
        }

        await context.Customers.Where(x => x.Name.StartsWith(CustomerPrefix)).ExecuteDeleteAsync(cancellationToken);
        await context.Suppliers.Where(x => x.Name.StartsWith(SupplierPrefix)).ExecuteDeleteAsync(cancellationToken);
    }

    private static Task<bool> HasGeneratedDataAsync(ApplicationDbContext context, CancellationToken cancellationToken)
    {
        return context.Projects.AnyAsync(x => x.Code.StartsWith(ProjectCodePrefix), cancellationToken);
    }

    private static ProjectStatus PickProjectStatus(Random random, DateTime monthStart, DateTime now)
    {
        if (monthStart < now.AddYears(-2))
        {
            return random.Next(100) < 70 ? ProjectStatus.Completed : random.Next(100) < 10 ? ProjectStatus.Cancelled : ProjectStatus.Waiting;
        }

        var bucket = random.Next(100);
        return bucket switch
        {
            < 20 => ProjectStatus.Planned,
            < 55 => ProjectStatus.InProgress,
            < 75 => ProjectStatus.Waiting,
            < 95 => ProjectStatus.Completed,
            _ => ProjectStatus.Cancelled
        };
    }

    private static WorkTaskStatus PickTaskStatus(Random random, ProjectStatus projectStatus, DateTime now, DateTime targetDate)
    {
        if (projectStatus == ProjectStatus.Completed)
        {
            return random.Next(100) < 85 ? WorkTaskStatus.Done : WorkTaskStatus.Cancelled;
        }

        if (projectStatus == ProjectStatus.Cancelled)
        {
            return random.Next(100) < 70 ? WorkTaskStatus.Cancelled : WorkTaskStatus.Done;
        }

        if (targetDate < now.AddMonths(-3))
        {
            return random.Next(100) < 55 ? WorkTaskStatus.Done : WorkTaskStatus.Waiting;
        }

        var bucket = random.Next(100);
        return bucket switch
        {
            < 22 => WorkTaskStatus.Todo,
            < 52 => WorkTaskStatus.InProgress,
            < 68 => WorkTaskStatus.Waiting,
            < 84 => WorkTaskStatus.InReview,
            < 96 => WorkTaskStatus.Done,
            _ => WorkTaskStatus.Cancelled
        };
    }

    private static PurchaseOrderStatus PickOrderStatus(Random random, ProjectStatus projectStatus, DateTime now, DateTime orderDate)
    {
        if (projectStatus == ProjectStatus.Completed || orderDate < now.AddMonths(-6))
        {
            return random.Next(100) < 70 ? PurchaseOrderStatus.Delivered : PurchaseOrderStatus.Cancelled;
        }

        var bucket = random.Next(100);
        return bucket switch
        {
            < 28 => PurchaseOrderStatus.Requested,
            < 58 => PurchaseOrderStatus.Ordered,
            < 78 => PurchaseOrderStatus.PartiallyDelivered,
            < 94 => PurchaseOrderStatus.Delivered,
            _ => PurchaseOrderStatus.Cancelled
        };
    }

    private static int CalculateProgress(WorkTaskStatus status, Random random)
    {
        return status switch
        {
            WorkTaskStatus.Todo => 0,
            WorkTaskStatus.InProgress => random.Next(15, 80),
            WorkTaskStatus.Waiting => random.Next(5, 60),
            WorkTaskStatus.InReview => random.Next(70, 95),
            WorkTaskStatus.Done => 100,
            WorkTaskStatus.Cancelled => random.Next(0, 50),
            _ => 0
        };
    }

    private sealed class LoadTestDataOptions
    {
        public int Years { get; set; } = 5;
        public int Customers { get; set; } = 180;
        public int Suppliers { get; set; } = 120;
        public int ProjectsPerMonth { get; set; } = 18;
        public int MinTasksPerProject { get; set; } = 4;
        public int MaxTasksPerProject { get; set; } = 8;
        public int MinOrdersPerProject { get; set; } = 2;
        public int MaxOrdersPerProject { get; set; } = 4;
        public int GeneralOrdersPerMonth { get; set; } = 8;
        public int MinCostsPerProject { get; set; } = 1;
        public int MaxCostsPerProject { get; set; } = 3;
        public int MaxUpdatesPerProject { get; set; } = 3;
        public int Seed { get; set; } = 20260612;
    }
}
