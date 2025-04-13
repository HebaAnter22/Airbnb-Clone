//using API.Data;
//using API.Models;
//using Microsoft.EntityFrameworkCore;

//namespace API.Services
//{
//    public class AmenityService : IAmenityService
//    {
//        private readonly AppDbContext _context;

//        public AmenityService(AppDbContext context)
//        {
//            _context = context;
//        }

//        public async Task<IEnumerable<Amenity>> GetAllAmenitiesAsync()
//        {
//            return await _context.Amenities.ToListAsync();
//        }

//        public async Task<IEnumerable<Amenity>> GetAmenitiesByCategoryAsync(string category)
//        {
//            return await _context.Amenities
//                .Where(a => a.Category == category)
//                .ToListAsync();
//        }

//        public async Task<Amenity> GetAmenityByIdAsync(int id)
//        {
//            return await _context.Amenities.FindAsync(id);
//        }

//        public async Task<Amenity> AddAmenityAsync(Amenity amenity)
//        {
//            _context.Amenities.Add(amenity);
//            await _context.SaveChangesAsync();
//            return amenity;
//        }

//        public async Task<Amenity> UpdateAmenityAsync(Amenity amenity)
//        {
//            _context.Entry(amenity).State = EntityState.Modified;
//            await _context.SaveChangesAsync();
//            return amenity;
//        }

//        public async Task<bool> DeleteAmenityAsync(int id)
//        {
//            var amenity = await _context.Amenities.FindAsync(id);
//            if (amenity == null)
//                return false;

//            _context.Amenities.Remove(amenity);
//            await _context.SaveChangesAsync();
//            return true;
//        }
//    }
//} 