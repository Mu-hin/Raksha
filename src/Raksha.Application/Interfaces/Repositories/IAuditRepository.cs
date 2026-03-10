using Raksha.Domain.Entities;
using Raksha.Domain.Interfaces;

namespace Raksha.Application.Interfaces.Repositories
{
    public interface IAuditRepository : INoSqlRepository<AuditLog, Guid>
    {
    }
}
