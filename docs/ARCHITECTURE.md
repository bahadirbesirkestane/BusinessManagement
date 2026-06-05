# Business Management MVC Architecture

## Project structure

- `src/Business.Domain`: Entity and enum layer. It has no dependency on MVC, EF Core, SQL Server, or Identity.
- `src/Business.Application`: Application-level contracts and use cases. It references Domain and is reserved for services, DTOs, validation, and business workflows.
- `src/Business.Infrastructure`: EF Core, SQL Server, Identity user/role, DbContext, persistence mappings, seed data, and migrations.
- `src/Business.Web`: ASP.NET Core MVC UI, controllers, Razor views, authentication screens, and app configuration.

## Core model

- `Project` is the center of the system. A project owns tasks, purchase orders, updates, and cost items.
- `PurchaseOrder` can be general or project-based. Project relation is optional so the same table supports the old "Genel Sipariş" sheet.
- `Supplier` stores vendor type, contact, payment term, address, website, reliability, and notes.
- `Material` stores product/material families and quality/grade metadata from the Excel "Ürün kalite" sheet.
- `StockItem` supports the existing sheet-material and bar/pipe stock sheets.
- `ProjectCostItem` supports cost tracking and can be linked back to a purchase order.
- `ProjectTask` keeps project work separate from purchases.
- `ProjectUpdate` is for project timeline notes and status history.

## Excel mapping notes

- `Tedarikçi listesi` maps to `Supplier`.
- `Genel Sipariş` maps to `PurchaseOrder` with `Scope = General` and no `ProjectId`.
- Project-code sheets such as `26-009`, `24-029`, and `25-017` map to `Project` plus project-scoped `PurchaseOrder` rows.
- `Genel Sipariş Durumu` maps to project status and high-level order state.
- `Ürün kalite` seeds material categories such as stainless, steel, aluminum, bronze, plastics, bearings, motor, gearbox, bolts, taps, and stainless sheet surface.
- `Mil Boru stok` and `Saç malzeme stok` map to `StockItem`.

## Database

Default database provider is SQL Server LocalDB:

```text
Server=(localdb)\MSSQLLocalDB;Database=BusinessManagementMvcDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True
```

Identity roles seeded by default:

- `Admin`
- `Manager`
- `Purchasing`
- `ProjectUser`
