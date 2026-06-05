using Business.Application.Repositories;
using Business.Application.Services;
using Business.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Business.Infrastructure.Services;

public class CustomerService : CrudService<Customer>, ICustomerService
{
    public CustomerService(IRepository<Customer> repository) : base(repository)
    {
    }

    protected override IQueryable<Customer> ListQuery()
    {
        return Repository.Query().OrderBy(x => x.Name);
    }

    protected override IQueryable<Customer> DetailsQuery()
    {
        return Repository.Query()
            .Include(x => x.Projects)
            .Include(x => x.Invoices);
    }

    public Task<int> GetCountAsync(CancellationToken cancellationToken = default)
    {
        return Repository.Query().CountAsync(cancellationToken);
    }
}
