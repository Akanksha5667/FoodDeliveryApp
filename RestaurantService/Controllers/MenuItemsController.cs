
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RestaurantService.Data;
using RestaurantService.Models;

namespace RestaurantService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MenuItemsController : ControllerBase
    {
        private readonly RestaurantDbContext _context;

        public MenuItemsController(RestaurantDbContext context)
        {
            _context = context;
        }

        // 1. GET: api/menuitems
        [HttpGet]
        public async Task<IActionResult> GetAllMenuItems()
        {
            var menuItems = await _context.MenuItems
                .AsNoTracking()
                .ToListAsync();

            return Ok(menuItems);
        }

        // 2. GET: api/menuitems/1
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetMenuItemById(int id)
        {
            var menuItem = await _context.MenuItems
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (menuItem == null)
            {
                return NotFound(new
                {
                    Message = "Menu item not found"
                });
            }

            return Ok(menuItem);
        }

        // 3. GET: api/menuitems/restaurant/1
        [HttpGet("restaurant/{restaurantId:int}")]
        public async Task<IActionResult> GetMenuByRestaurant(
            int restaurantId)
        {
            var restaurantExists = await _context.Restaurants
                .AnyAsync(x => x.Id == restaurantId);

            if (!restaurantExists)
            {
                return NotFound(new
                {
                    Message = "Restaurant not found"
                });
            }

            var menuItems = await _context.MenuItems
                .AsNoTracking()
                .Where(x => x.RestaurantId == restaurantId)
                .ToListAsync();

            return Ok(menuItems);
        }

        // 4. POST: api/menuitems
        [HttpPost]
        public async Task<IActionResult> CreateMenuItem(
            [FromBody] MenuItem menuItem)
        {
            var restaurantExists = await _context.Restaurants
                .AnyAsync(x => x.Id == menuItem.RestaurantId);

            if (!restaurantExists)
            {
                return BadRequest(new
                {
                    Message = "Restaurant does not exist"
                });
            }

            if (string.IsNullOrWhiteSpace(menuItem.Name))
            {
                return BadRequest(new
                {
                    Message = "Food name is required"
                });
            }

            if (menuItem.Price <= 0)
            {
                return BadRequest(new
                {
                    Message = "Price must be greater than zero"
                });
            }

            menuItem.Id = 0;
            menuItem.CreatedAt = DateTime.UtcNow;

            _context.MenuItems.Add(menuItem);

            await _context.SaveChangesAsync();

            return CreatedAtAction(
                nameof(GetMenuItemById),
                new { id = menuItem.Id },
                menuItem);
        }

        // 5. PUT: api/menuitems/1
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateMenuItem(
            int id,
            [FromBody] MenuItem updatedMenuItem)
        {
            var menuItem = await _context.MenuItems
                .FindAsync(id);

            if (menuItem == null)
            {
                return NotFound(new
                {
                    Message = "Menu item not found"
                });
            }

            if (string.IsNullOrWhiteSpace(updatedMenuItem.Name)
                || updatedMenuItem.Price <= 0)
            {
                return BadRequest(new
                {
                    Message = "Valid name and price are required"
                });
            }

            // Check restaurant if it is being changed
            var restaurantExists = await _context.Restaurants
                .AnyAsync(x => x.Id == updatedMenuItem.RestaurantId);

            if (!restaurantExists)
            {
                return BadRequest(new
                {
                    Message = "Restaurant does not exist"
                });
            }

            menuItem.RestaurantId = updatedMenuItem.RestaurantId;
            menuItem.Name = updatedMenuItem.Name;
            menuItem.Description = updatedMenuItem.Description;
            menuItem.Price = updatedMenuItem.Price;
            menuItem.Category = updatedMenuItem.Category;
            menuItem.IsAvailable = updatedMenuItem.IsAvailable;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Menu item updated successfully",
                MenuItem = menuItem
            });
        }

        // 6. DELETE: api/menuitems/1
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteMenuItem(int id)
        {
            var menuItem = await _context.MenuItems
                .FindAsync(id);

            if (menuItem == null)
            {
                return NotFound(new
                {
                    Message = "Menu item not found"
                });
            }

            _context.MenuItems.Remove(menuItem);

            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Menu item deleted successfully"
            });
        }
    }
}
