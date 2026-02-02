using AdLocalAPI.Data;
using AdLocalAPI.Models;
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

            query = query
                    .Where(p => p.Activo == true);


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
        public async Task<List<Models.Plan>> GetAllPlanesUser()
        {
            List<Models.Plan> plans = new List<Models.Plan>();
            var query = _context.Plans.AsQueryable();
            query = query
                .Where(p => p.Activo == true);
            try
            {
                plans = await query.ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return plans;
            }
            return plans;
        }

        public async Task<Plan?> GetByIdAsync(int id)
        {
            return await _context.Plans
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);
        }
        public async Task<Plan?> GetByIdLongAsync(long id)
        {
            return await _context.Plans
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == id);
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
                if (plan == null) return false;

                plan.Activo = false;
                _context.Plans.Update(plan);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return false;
            }
            return true;
        }
        public async Task<Plan> GetByTipoAsync(string tipo)
        {
            return await _context.Plans
                .AsNoTracking()
                .FirstOrDefaultAsync(p =>
                    p.Tipo == tipo &&
                    p.Activo
                );
        }

        public async Task<Plan> GetByStripePriceIdAsync(string priceId)
        {
            return await _context.Plans
                .AsNoTracking()
                .FirstOrDefaultAsync(p =>
                    p.StripePriceId == priceId &&
                    p.Activo
                );
        }

    }
}
