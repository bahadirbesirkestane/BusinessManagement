using System.Security.Claims;
using Business.Domain.Entities;
using Business.Domain.Enums;
using Business.Infrastructure.Identity;

namespace Business.Web.Extensions;

public static class RecordVisibilityExtensions
{
    public static bool CanViewAdminOnlyRecords(this ClaimsPrincipal user)
    {
        return user.IsInRole(AppRoles.Admin);
    }

    public static RecordVisibility NormalizeRecordVisibility(this ClaimsPrincipal user, RecordVisibility visibility)
    {
        return user.CanViewAdminOnlyRecords() ? visibility : RecordVisibility.General;
    }

    public static IQueryable<Project> ApplyRecordVisibility(this IQueryable<Project> query, ClaimsPrincipal user)
    {
        return user.CanViewAdminOnlyRecords()
            ? query
            : query.Where(x => x.Visibility == RecordVisibility.General);
    }

    public static IQueryable<ProjectTask> ApplyRecordVisibility(this IQueryable<ProjectTask> query, ClaimsPrincipal user)
    {
        return user.CanViewAdminOnlyRecords()
            ? query
            : query.Where(x =>
                x.Visibility == RecordVisibility.General &&
                (x.Project == null || x.Project.Visibility == RecordVisibility.General));
    }

    public static IQueryable<PurchaseOrder> ApplyRecordVisibility(this IQueryable<PurchaseOrder> query, ClaimsPrincipal user)
    {
        return user.CanViewAdminOnlyRecords()
            ? query
            : query.Where(x =>
                x.Visibility == RecordVisibility.General &&
                (x.Project == null || x.Project.Visibility == RecordVisibility.General));
    }

    public static IQueryable<ProjectCostItem> ApplyRecordVisibility(this IQueryable<ProjectCostItem> query, ClaimsPrincipal user)
    {
        return user.CanViewAdminOnlyRecords()
            ? query
            : query.Where(x =>
                x.Visibility == RecordVisibility.General &&
                (x.Project == null || x.Project.Visibility == RecordVisibility.General) &&
                (x.PurchaseOrder == null || x.PurchaseOrder.Visibility == RecordVisibility.General));
    }

    public static IQueryable<MaterialRequest> ApplyProjectRecordVisibility(this IQueryable<MaterialRequest> query, ClaimsPrincipal user)
    {
        return user.CanViewAdminOnlyRecords()
            ? query
            : query.Where(x => x.Project == null || x.Project.Visibility == RecordVisibility.General);
    }

    public static bool IsVisibleTo(this Project project, ClaimsPrincipal user)
    {
        return user.CanViewAdminOnlyRecords() || project.Visibility == RecordVisibility.General;
    }

    public static bool IsVisibleTo(this ProjectTask task, ClaimsPrincipal user)
    {
        return user.CanViewAdminOnlyRecords() ||
               (task.Visibility == RecordVisibility.General &&
                (task.Project is null || task.Project.Visibility == RecordVisibility.General));
    }

    public static bool IsVisibleTo(this PurchaseOrder order, ClaimsPrincipal user)
    {
        return user.CanViewAdminOnlyRecords() ||
               (order.Visibility == RecordVisibility.General &&
                (order.Project is null || order.Project.Visibility == RecordVisibility.General));
    }

    public static bool IsVisibleTo(this ProjectCostItem item, ClaimsPrincipal user)
    {
        return user.CanViewAdminOnlyRecords() ||
               (item.Visibility == RecordVisibility.General &&
                (item.Project is null || item.Project.Visibility == RecordVisibility.General) &&
                (item.PurchaseOrder is null || item.PurchaseOrder.Visibility == RecordVisibility.General));
    }
}
