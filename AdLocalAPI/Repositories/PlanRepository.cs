using AdLocalAPI.Data;
using AdLocalAPI.Models;
using Microsoft.EntityFrameworkCore;
using Stripe;

namespace AdLocalAPI.Repositories
{
    public class PlanRepository
    {
        private readonly AppDbContext _context;

        public PlanRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Models.Plan>> GetAllAsync()
        {
            List<Models.Plan> plans = new List<Models.Plan>();
            try 
            {
                plans = await _context.Plans.ToListAsync();
            }
            catch (Exception ex) 
            {
                Console.WriteLine(ex);
                return plans;
            }            
            return plans;
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
