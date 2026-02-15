using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace RestaurantBilling.Controllers
{
    public abstract class BaseController : ControllerBase
    {
        protected int CurrentUserId
        {
            get
            {
                var userIdValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                               ?? User.FindFirst("sub")?.Value;
                if (int.TryParse(userIdValue, out int userId))
                {
                    return userId;
                }
                return 0;
            }
        }

        protected string CurrentUserEmail => User.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;
    }
}
