using Microsoft.EntityFrameworkCore;

namespace TableFlow.Api.Data
{
    public class TableFlowDbContext : DbContext
    {
        public TableFlowDbContext(DbContextOptions<TableFlowDbContext> options) : base(options)
        {
            
        }
    }
}