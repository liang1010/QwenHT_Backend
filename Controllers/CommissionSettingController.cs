using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QwenHT.Data;
using QwenHT.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace QwenHT.Controllers
{
    [ApiController]
    [Route("api/commission")]
    [Authorize(Policy = "NavigationAccess")] // Use custom policy based on navigation permissions
    public class CommissionSettingController(ApplicationDbContext _context) : ControllerBase
    {
        [HttpGet("setting")]
        public async Task<ActionResult<IEnumerable<OptionValue>>> GetOptionValues()
        {
            var query = _context.OptionValues.AsQueryable();


            List<string> strings = new List<string>() {
"INCENTIVE_AMOUNT_MF",
"INCENTIVE_AMOUNT_NMM",
"INCENTIVE_HOURS_MF",
"INCENTIVE_HOURS_NMF",
"INCENTIVE_AMOUNT_NMF",
"INCENTIVE_AMOUNT_MM",
"INCENTIVE_HOURS_NMM",
"INCENTIVE_HOURS_MM",
"TREATMENT_PERCENT",
"PRODUCT_PERCENT_TIER_1",
"PRODUCT_PERCENT_TIER_2",
"PRODUCT_TARGET" };

            query = query.Where(ov => strings.Contains(ov.Category));
            var optionValues = await query.ToListAsync();


            return Ok(optionValues);
        }
    }
}
