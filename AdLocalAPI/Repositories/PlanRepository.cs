using AdLocalAPI.Data;
using Microsoft.EntityFrameworkCore;


namespace AdLocalAPI.Repositories
{
    public class PlanRepository
    {
        private readonly AppDbContext _context;

        public PlanRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<object> GetAllAsync(
            int page,
            int pageSize,
            string orderBy,
            string search
        )
        {
            var query = _context.Plans.AsQueryable();


            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(p =>
                    EF.Functions.ILike(p.Nombre, $"%{search}%")
                );
            }


            query = orderBy switch
            {
                "recent" => query.OrderByDescending(p => p.FechaCreacion),
                "old" => query.OrderBy(p => p.FechaCreacion),
                "az" => query.OrderBy(p => p.Nombre),
                "za" => query.OrderByDescending(p => p.Nombre),
                _ => query.OrderByDescending(p => p.FechaCreacion)
            };

            var totalRecords = await query.CountAsync();

            var plans = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new
            {
                totalRecords,
                page,
                pageSize,
                data = plans
            };
        }

        public async Task<Models.Plan> GetByIdAsync(int id) 
        {
            Models.Plan plan = new Models.Plan();
            try 
            {
                plan = await _context.Plans.FindAsync(id);
            }
            catch (Exception ex) 
            {
                Console.WriteLine($"Could not find {ex.Message}");
                return plan;
            }
            return plan;
        }
        public async Task<Models.Plan> CreateAsync(Models.Plan plan)
        {
            try
            {
                _context.Plans.Add(plan);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex) 
            {
                Console.WriteLine(ex);
                return null;
            }
            return plan;
        }
        public async Task<Models.Plan> UpdateAsync(Models.Plan plan)
        {
            try
            {
                _context.Plans.Update(plan);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return plan;
            }
            return plan;
        }
        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                var plan = await _context.Plans.FindAsync(id);
                if (plan != null)
                {
                    _context.Plans.Remove(plan);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return false;
            }
            return true;
        }
    }
}
