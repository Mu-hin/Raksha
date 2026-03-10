using Raksha.Application.Interfaces.Repositories;
using Raksha.Domain.Entities;
using Raksha.Infrastructure.Data;

namespace Raksha.Infrastructure.Repositories
{
    public class AuditRepository : NoSqlRepository<AuditLog, Guid>, IAuditRepository
    {
        public AuditRepository(MongoDbContext mongoDbContext) : base(mongoDbContext)
        {
        }
    }
}
