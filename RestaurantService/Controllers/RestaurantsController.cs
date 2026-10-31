
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantService.Data;
using RestaurantService.Models;

namespace RestaurantService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RestaurantsController : ControllerBase
    {
        private readonly RestaurantDbContext _context;

        public RestaurantsController(RestaurantDbContext context)
        {
            _context = context;
        }

        // 1. GET: api/restaurants
        // Get all restaurants
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Restaurant>>> GetAllRestaurants()
        {
            var restaurants = await _context.Restaurants
                .AsNoTracking()
                .ToListAsync();

            return Ok(restaurants);
        }

        // 2. GET: api/restaurants/1
        // Get restaurant by ID
        [HttpGet("{id:int}")]
        public async Task<ActionResult<Restaurant>> GetRestaurantById(int id)
        {
            var restaurant = await _context.Restaurants
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == id);

            if (restaurant == null)
            {
                return NotFound(new
                {
                    Message = "Restaurant not found"
                });
            }

            return Ok(restaurant);
        }

        // 3. POST: api/restaurants
        // Create a new restaurant
        [HttpPost]
        public async Task<ActionResult<Restaurant>> CreateRestaurant(
            [FromBody] Restaurant restaurant)
        {
            if (string.IsNullOrWhiteSpace(restaurant.Name))
            {
                return BadRequest(new
                {
                    Message = "Restaurant name is required"
                });
            }

            restaurant.Id = 0;
            restaurant.CreatedAt = DateTime.UtcNow;

            _context.Restaurants.Add(restaurant);
            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetRestaurantById),
                new { id = restaurant.Id },
                restaurant);
        }

        // 4. PUT: api/restaurants/1
        // Update an existing restaurant
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateRestaurant(
            int id,
            [FromBody] Restaurant updatedRestaurant)
        {
            var restaurant = await _context.Restaurants
                .FindAsync(id);

            if (restaurant == null)
            {
                return NotFound(new
                {
                    Message = "Restaurant not found"
                });
            }

            if (string.IsNullOrWhiteSpace(updatedRestaurant.Name))
            {
                return BadRequest(new
                {
                    Message = "Restaurant name is required"
                });
            }

            restaurant.Name = updatedRestaurant.Name;
            restaurant.Location = updatedRestaurant.Location;
            restaurant.PhoneNumber = updatedRestaurant.PhoneNumber;
            restaurant.Email = updatedRestaurant.Email;
            restaurant.IsActive = updatedRestaurant.IsActive;

            // Id and CreatedAt should not change

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Restaurant updated successfully",
                Restaurant = restaurant
            });
        }

        // 5. DELETE: api/restaurants/1
        // Delete restaurant by ID
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteRestaurant(int id)
        {
            var restaurant = await _context.Restaurants
                .FindAsync(id);

            if (restaurant == null)
            {
                return NotFound(new
                {
                    Message = "Restaurant not found"
                });
            }

            _context.Restaurants.Remove(restaurant);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Restaurant deleted successfully"
            });
        }
    }
}
