using Business.Application.Repositories;
using Business.Application.Services;
using Business.Domain.Entities;

namespace Business.Infrastructure.Services;

public class MaterialService : CrudService<Material>, IMaterialService
{
    public MaterialService(IRepository<Material> repository) : base(repository)
    {
    }

    protected override IQueryable<Material> ListQuery()
    {
        return Repository.Query().OrderBy(x => x.Name);
    }
}
