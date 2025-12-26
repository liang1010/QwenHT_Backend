using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QwenHT.Data;
using QwenHT.Models;
using QwenHT.Utilities;
using static QwenHT.Controllers.SalesKeyInController;

namespace QwenHT.Controllers
{
    [ApiController]
    [Route("api/commission")]
    [Authorize(Policy = "NavigationAccess")] // Use custom policy based on navigation permissions
    public class CommissionTherapistController(ApplicationDbContext _context) : ControllerBase
    {



        [HttpPost("therapist/incentives")]
        public async Task<ActionResult<Incentive>> CreateIncentive(CreateIncentive menuDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Get current user from JWT claims
            var username = User?.Claims?.FirstOrDefault(c => c.Type == "username")?.Value ?? "Unknown";

            var menu = new Incentive
            {
                Id = Guid.NewGuid(),
                Amount = menuDto.Amount,
                IncentiveDate = menuDto.IncentiveDate,
                Remark = menuDto.Remark,
                Description = menuDto.Description,
                StaffId = menuDto.StaffId,
                Status = 1,
                CreatedBy = username,
                CreatedAt = DateTimeOffset.UtcNow,
                LastUpdated = DateTimeOffset.UtcNow,
                LastModifiedBy = username
            };

            _context.Incentives.Add(menu);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(CreateIncentive), new { id = menu.Id }, menu);
        }

        [HttpPost("therapist/incentives/{id}")]
        public async Task<IActionResult> UpdateMenu(Guid id, CreateIncentive menuDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var menu = await _context.Incentives.FindAsync(id);

            if (menu == null)
            {
                return NotFound();
            }

            // Get current user from JWT claims
            var username = User?.Claims?.FirstOrDefault(c => c.Type == "username")?.Value ?? "Unknown";
            menu.Description = menuDto.Description;
            menu.Remark = menuDto.Remark;
            menu.Amount = menuDto.Amount;
            menu.IncentiveDate = menuDto.IncentiveDate;
            menu.Status = 1;
            menu.LastUpdated = DateTimeOffset.UtcNow;
            menu.LastModifiedBy = username;

            _context.Incentives.Update(menu);
            await _context.SaveChangesAsync();

            return Ok(menu);
        }

        [HttpPost("therapist/incentives/{id}/delete")]
        public async Task<IActionResult> DeleteMenu(Guid id)
        {
            var menu = await _context.Incentives.FindAsync(id);

            if (menu == null)
            {
                return NotFound();
            }

            menu.Status = 0;
            _context.Incentives.Update(menu);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET api/commission/therapist/pdf - Generate therapist commission PDF report (grouped by date and menu code)
        [HttpGet("therapist/pdf")]
        public async Task<IActionResult> GetTherapistCommissionPdf(
            [FromQuery] Guid staffId,
            [FromQuery] DateTimeOffset? startDate = null,
            [FromQuery] DateTimeOffset? endDate = null)
        {

            var staff = await _context.Staffs.Where(x => x.Id == staffId).FirstOrDefaultAsync();
            // Step 1: Fetch all required sales data in a single query for efficiency
            var salesData = await GetSalesDataAsync(staffId, startDate, endDate);

            // Step 2: Group sales by date and menu code to create daily summaries
            var groupedCommissions = GroupSalesByDateAndMenu(salesData);

            // Step 3: Initialize the main report object with basic commission data
            var result = new TherapistCommissionReportDto
            {
                Commissions = groupedCommissions,
                Incentives = new List<TherapistIncentiveDto>() // Initialize with empty list
            };

            // Step 4: Retrieve staff compensation settings to determine calculation method
            var staffCompensation = await _context.StaffCompensations
                .FirstOrDefaultAsync(x => x.StaffId == staffId);

            // If no compensation setup is found, return basic commission data only
            if (staffCompensation == null)
                return Ok(result);

            // Step 5: Set compensation type flags for frontend reference
            result.IsGuaranteeIncome = staffCompensation.IsGuaranteeIncome;
            result.IsRate = staffCompensation.IsRate;
            result.IsCommissionPercentage = staffCompensation.IsCommissionPercentage;

            // Step 6: Calculate compensation details based on staff compensation setup
            if (staffCompensation.IsRate)
            {
                // Calculate rate-based commission details
                result.RateBase = GetTherapistRateBasedCommissionDto(groupedCommissions, staffCompensation, startDate);

                // If the period doesn't start on the 1st day of the month, calculate full month hours for comparison
                if (startDate.HasValue && startDate.Value.LocalDateTime.Day != 1)
                {
                    var fullMonthHours = await GetFullMonthHoursForComparisonAsync(staffId, startDate.Value);
                    result.RateBase.AllPeriodHrs = Math.Round(fullMonthHours, 2);
                }
            }
            else if (staffCompensation.IsCommissionPercentage)
            {
                // Calculate percentage-based commission details
                result.CommissionPercentage = GetTherapistPercentageBasedCommissionDto(groupedCommissions, staffCompensation, startDate);

                // If the period doesn't start on the 1st day of the month, calculate full month hours for comparison
                if (startDate.HasValue && startDate.Value.LocalDateTime.Day != 1)
                {
                    var fullMonthHours = await GetFullMonthHoursForComparisonAsync(staffId, startDate.Value);
                    result.CommissionPercentage.AllPeriodHrs = Math.Round(fullMonthHours, 2);
                }
            }

            // Step 7: Calculate guarantee income if applicable
            // Only calculate if guarantee is enabled and the reporting period doesn't start on the 1st of the month
            if (staffCompensation.IsGuaranteeIncome && startDate.HasValue && startDate.Value.LocalDateTime.Day != 1)
            {
                result.GuaranteeIncome = await CalculateTherapistGuaranteeIncomeAsync(staffId, staffCompensation, startDate.Value, endDate);
            }


            // Step 8: Get Incentive
            var local = startDate.Value.LocalDateTime;
            var day = local.Day == 1
                ? 15
                : DateTime.DaysInMonth(local.Year, local.Month);

            var incentiveDate = new DateTimeOffset(
                local.Year,
                local.Month,
                day,
                0, 0, 0,
                startDate.Value.Offset
            );

            var incentive = await _context.Incentives.Where(x => x.StaffId == staffId && x.Status == 1 && x.IncentiveDate == endDate).ToListAsync();


            if (incentive.Count > 0)
            {
                result.Incentives = new List<TherapistIncentiveDto>();

                foreach (var item in incentive)
                {

                    result.Incentives.Add(new TherapistIncentiveDto
                    {
                        IncentiveDate = item.IncentiveDate,
                        Amount = item.Amount,
                        Description = item.Description,
                        Id = item.Id,
                        Remark = item.Remark
                    });
                }



                result.TotalIncentive = result.Incentives.Sum(x => x.Amount);
            }

            if (result.IsRate)
            {
                result.TotalPayout = result.TotalIncentive + result.RateBase.TotalCommission;
            }
            else if (result.IsCommissionPercentage)
            {

                result.TotalPayout = result.TotalIncentive + result.CommissionPercentage.TotalCommission;
            }

            // Generate PDF
            var pdfBytes = PdfGenerator.GenerateTherapistCommissionReport(
                staff.NickName ?? staff.FullName,
                startDate, // Default to 30 days if not provided
                endDate,
                result);

            // Return PDF as file
            var fileName = $"Therapist-Commission-{(staff.NickName ?? staff.FullName).Replace(" ", "_")}-{(startDate?.ToString("yyyyMMdd") ?? DateTime.Now.ToString("yyyyMMdd"))}-{(endDate?.ToString("yyyyMMdd") ?? DateTime.Now.ToString("yyyyMMdd"))}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }

        // GET api/commission/therapist/pdf - Generate therapist commission PDF report (grouped by date and menu code)
        [HttpGet("therapist/insertPayout")]
        public async Task<IActionResult> InsertPayout(
            [FromQuery] Guid staffId,
            [FromQuery] DateTimeOffset? startDate = null,
            [FromQuery] DateTimeOffset? endDate = null)
        {
            var username = User?.Claims?.FirstOrDefault(c => c.Type == "username")?.Value ?? "Unknown";
            var staff = await _context.Staffs.Where(x => x.Id == staffId).FirstOrDefaultAsync();
            // Step 1: Fetch all required sales data in a single query for efficiency
            var salesData = await GetSalesDataAsync(staffId, startDate, endDate);

            // Step 2: Group sales by date and menu code to create daily summaries
            var groupedCommissions = GroupSalesByDateAndMenu(salesData);

            // Step 3: Initialize the main report object with basic commission data
            var result = new TherapistCommissionReportDto
            {
                Commissions = groupedCommissions,
                Incentives = new List<TherapistIncentiveDto>() // Initialize with empty list
            };

            // Step 4: Retrieve staff compensation settings to determine calculation method
            var staffCompensation = await _context.StaffCompensations
                .FirstOrDefaultAsync(x => x.StaffId == staffId);

            // If no compensation setup is found, return basic commission data only
            if (staffCompensation == null)
                return Ok(result);

            // Step 5: Set compensation type flags for frontend reference
            result.IsGuaranteeIncome = staffCompensation.IsGuaranteeIncome;
            result.IsRate = staffCompensation.IsRate;
            result.IsCommissionPercentage = staffCompensation.IsCommissionPercentage;

            // Step 6: Calculate compensation details based on staff compensation setup
            if (staffCompensation.IsRate)
            {
                // Calculate rate-based commission details
                result.RateBase = GetTherapistRateBasedCommissionDto(groupedCommissions, staffCompensation, startDate);

                // If the period doesn't start on the 1st day of the month, calculate full month hours for comparison
                if (startDate.HasValue && startDate.Value.LocalDateTime.Day != 1)
                {
                    var fullMonthHours = await GetFullMonthHoursForComparisonAsync(staffId, startDate.Value);
                    result.RateBase.AllPeriodHrs = Math.Round(fullMonthHours, 2);
                }
            }
            else if (staffCompensation.IsCommissionPercentage)
            {
                // Calculate percentage-based commission details
                result.CommissionPercentage = GetTherapistPercentageBasedCommissionDto(groupedCommissions, staffCompensation, startDate);

                // If the period doesn't start on the 1st day of the month, calculate full month hours for comparison
                if (startDate.HasValue && startDate.Value.LocalDateTime.Day != 1)
                {
                    var fullMonthHours = await GetFullMonthHoursForComparisonAsync(staffId, startDate.Value);
                    result.CommissionPercentage.AllPeriodHrs = Math.Round(fullMonthHours, 2);
                }
            }

            // Step 7: Calculate guarantee income if applicable
            // Only calculate if guarantee is enabled and the reporting period doesn't start on the 1st of the month
            if (staffCompensation.IsGuaranteeIncome && startDate.HasValue && startDate.Value.LocalDateTime.Day != 1)
            {
                result.GuaranteeIncome = await CalculateTherapistGuaranteeIncomeAsync(staffId, staffCompensation, startDate.Value, endDate);
            }


            // Step 8: Get Incentive
            var local = startDate.Value.LocalDateTime;
            var day = local.Day == 1
                ? 15
                : DateTime.DaysInMonth(local.Year, local.Month);

            var incentiveDate = new DateTimeOffset(
                local.Year,
                local.Month,
                day,
                0, 0, 0,
                startDate.Value.Offset
            );

            var incentive = await _context.Incentives.Where(x => x.StaffId == staffId && x.Status == 1 && x.IncentiveDate == endDate).ToListAsync();


            if (incentive.Count > 0)
            {
                result.Incentives = new List<TherapistIncentiveDto>();

                foreach (var item in incentive)
                {

                    result.Incentives.Add(new TherapistIncentiveDto
                    {
                        IncentiveDate = item.IncentiveDate,
                        Amount = item.Amount,
                        Description = item.Description,
                        Id = item.Id,
                        Remark = item.Remark
                    });
                }



                result.TotalIncentive = result.Incentives.Sum(x => x.Amount);
            }

            if (result.IsRate)
            {
                result.TotalPayout = result.TotalIncentive + result.RateBase.TotalCommission;
            }
            else if (result.IsCommissionPercentage)
            {

                result.TotalPayout = result.TotalIncentive + result.CommissionPercentage.TotalCommission;
            }

            var existings = await _context.TherapistPayouts.Where(x => x.StaffId == staffId && x.PayoutDate == endDate && x.Status == 1).ToListAsync();
            if (existings.Count > 0)
            {
                foreach (var item in existings)
                {
                    item.Status = 0;
                }
                _context.TherapistPayouts.UpdateRange(existings);
                await _context.SaveChangesAsync();
            }

            var therapistPayout = new TherapistPayout
            {
                Id = Guid.NewGuid(),
                PayoutDate = (DateTimeOffset)endDate,
                StaffId = staffId,
                FootAmount = result.IsRate ? result.RateBase.TotalFootCommission : 0,
                BodyAmount = result.IsRate ? result.RateBase.TotalBodyCommission : 0,
                StaffAmount = result.IsRate ? result.RateBase.TotalStaffCommission : result.CommissionPercentage.TotalStaffCommission,
                ExtraAmount = result.IsRate ? result.RateBase.TotalExtraCommission : result.CommissionPercentage.TotalExtraCommission,
                IncentiveAmount = result.TotalIncentive,
                TotalAmount = result.TotalPayout,
                Status = 1,
                CreatedBy = username,
                CreatedAt = DateTimeOffset.UtcNow,
                LastUpdated = DateTimeOffset.UtcNow,
                LastModifiedBy = username
            };
            await _context.TherapistPayouts.AddAsync(therapistPayout);
            await _context.SaveChangesAsync();


            return Ok();
        }

        [HttpGet("therapist/staff/active")]
        public async Task<ActionResult<IEnumerable<ActiveStaffDto>>> GetActiveStaff()
        {
            var activeStaff = await _context.Staffs
                .Where(s => s.Status == 1 &&
        s.Employments.Any(x => x.Type.ToLower() == "therapist")) // Only return staff with status = 1
                .Select(s => new ActiveStaffDto
                {
                    Id = s.Id,
                    NickName = s.NickName,
                    FullName = s.FullName
                })
                .OrderBy(s => s.NickName)
                .ToListAsync();

            return Ok(activeStaff);
        }

        /// <summary>
        /// Retrieves therapist commission data grouped by date and menu code for a specified staff member and date range.
        /// Includes detailed compensation calculations based on staff compensation setup (rate-based, percentage-based,
        /// or guarantee income).
        /// </summary>
        /// <param name="staffId">The ID of the staff member to retrieve commission data for</param>
        /// <param name="startDate">Start date for the data retrieval period (optional)</param>
        /// <param name="endDate">End date for the data retrieval period (optional)</param>
        /// <returns>Comprehensive therapist commission report with all calculated values</returns>
        [HttpGet("therapist")]
        public async Task<ActionResult<TherapistCommissionReportDto>> GetTherapistCommission(
            [FromQuery] Guid staffId,
            [FromQuery] DateTimeOffset? startDate = null,
            [FromQuery] DateTimeOffset? endDate = null,
            [FromQuery] bool? incentive = false)
        {
            // Get the current user for CreatedBy field
            var currentUser = User?.Identity?.Name ?? "System";
            // Step 1: Fetch all required sales data in a single query for efficiency
            var salesData = await GetSalesDataAsync(staffId, startDate, endDate);

            // Step 2: Group sales by date and menu code to create daily summaries
            var groupedCommissions = GroupSalesByDateAndMenu(salesData);

            // Step 3: Initialize the main report object with basic commission data
            var result = new TherapistCommissionReportDto
            {
                Commissions = groupedCommissions,
                Incentives = new List<TherapistIncentiveDto>() // Initialize with empty list
            };

            // Step 4: Retrieve staff compensation settings to determine calculation method
            var staffCompensation = await _context.StaffCompensations
                .FirstOrDefaultAsync(x => x.StaffId == staffId);

            var staff = await _context.Staffs
                .FirstOrDefaultAsync(x => x.Id == staffId);

            // If no compensation setup is found, return basic commission data only
            if (staffCompensation == null)
                return Ok(result);

            // Step 5: Set compensation type flags for frontend reference
            result.IsGuaranteeIncome = staffCompensation.IsGuaranteeIncome;
            result.IsRate = staffCompensation.IsRate;
            result.IsCommissionPercentage = staffCompensation.IsCommissionPercentage;

            // Step 6: Calculate compensation details based on staff compensation setup
            if (staffCompensation.IsRate)
            {
                // Calculate rate-based commission details
                result.RateBase = GetTherapistRateBasedCommissionDto(groupedCommissions, staffCompensation, startDate);

                // If the period doesn't start on the 1st day of the month, calculate full month hours for comparison
                if (startDate.HasValue && startDate.Value.LocalDateTime.Day != 1)
                {
                    var fullMonthHours = await GetFullMonthHoursForComparisonAsync(staffId, startDate.Value);
                    result.RateBase.AllPeriodHrs = Math.Round(fullMonthHours, 2);
                }
            }
            else if (staffCompensation.IsCommissionPercentage)
            {
                // Calculate percentage-based commission details
                result.CommissionPercentage = GetTherapistPercentageBasedCommissionDto(groupedCommissions, staffCompensation, startDate);

                // If the period doesn't start on the 1st day of the month, calculate full month hours for comparison
                if (startDate.HasValue && startDate.Value.LocalDateTime.Day != 1)
                {
                    var fullMonthHours = await GetFullMonthHoursForComparisonAsync(staffId, startDate.Value);
                    result.CommissionPercentage.AllPeriodHrs = Math.Round(fullMonthHours, 2);
                }
            }

            // Step 7: Calculate guarantee income if applicable
            // Only calculate if guarantee is enabled and the reporting period doesn't start on the 1st of the month
            if (startDate.HasValue && startDate.Value.LocalDateTime.Day != 1)
            {
                if (incentive == true)
                {
                    bool isMalaysian = staff.Nationality.ToLower().Contains("malaysia");
                    bool isFemale = staff.Gender.ToLower().Contains("female");
                    var hrscategory =
        isMalaysian
            ? (isFemale ? "INCENTIVE_HOURS_MF" : "INCENTIVE_HOURS_MM")
            : (isFemale ? "INCENTIVE_HOURS_NMF" : "INCENTIVE_HOURS_NMM");

                    int.TryParse(_context.OptionValues.Where(x => x.Category == hrscategory).FirstOrDefaultAsync().Result.Value, out int incentivehrs);
                    var amountcategory =
        isMalaysian
            ? (isFemale ? "INCENTIVE_AMOUNT_MF" : "INCENTIVE_AMOUNT_MM")
            : (isFemale ? "INCENTIVE_AMOUNT_NMF" : "INCENTIVE_AMOUNT_NMM");

                    int.TryParse(_context.OptionValues.Where(x => x.Category == amountcategory).FirstOrDefaultAsync().Result.Value, out int incentiveAmount);

                    Console.WriteLine(incentiveAmount);

                    if (result.IsRate && !staffCompensation.IsGuaranteeIncome)
                    {
                        if (result.RateBase.AllPeriodHrs >= incentivehrs)
                        {
                            var exist = await _context.Incentives.Where(x => x.StaffId == staffId && x.IncentiveDate == endDate.Value && x.Description == "EXCEED TREATMENT HOURS" && x.Status == 1).FirstOrDefaultAsync();

                            if (exist == null)
                                _context.Incentives.Add(new Incentive
                                {
                                    Id = Guid.NewGuid(),
                                    Amount = (decimal)result.RateBase.AllPeriodHrs * incentiveAmount,
                                    IncentiveDate = endDate.Value,
                                    Remark = "EXCEED " + incentivehrs + " TREATMENT HOURS INCENTIVE(" + result.RateBase.AllPeriodHrs + " x " + incentiveAmount + ")",
                                    Description = "EXCEED TREATMENT HOURS",
                                    StaffId = staffId,
                                    Status = 1,
                                    CreatedBy = currentUser,
                                    CreatedAt = DateTimeOffset.UtcNow,
                                    LastUpdated = DateTimeOffset.UtcNow,
                                    LastModifiedBy = currentUser
                                });
                            else
                            {
                                exist.Amount = (decimal)result.RateBase.AllPeriodHrs * incentiveAmount;
                                exist.Remark = "EXCEED " + incentivehrs + " TREATMENT HOURS INCENTIVE(" + result.RateBase.AllPeriodHrs + " x " + incentiveAmount + ")";
                                exist.LastUpdated = DateTimeOffset.UtcNow;
                                exist.LastModifiedBy = currentUser;

                                _context.Incentives.Update(exist);
                            }
                            _context.SaveChanges();
                        }
                    }


                }

                if (staffCompensation.IsGuaranteeIncome)
                {
                    result.GuaranteeIncome = await CalculateTherapistGuaranteeIncomeAsync(staffId, staffCompensation, startDate.Value, endDate);

                    if (result.GuaranteeIncome.GuaranteeIncomePaid > 0)
                    {
                        var exist = await _context.Incentives.Where(x => x.StaffId == staffId && x.IncentiveDate == endDate.Value && x.Description == "GUARANTEE INCOME" && x.Status == 1).FirstOrDefaultAsync();

                        if (exist == null)
                            _context.Incentives.Add(new Incentive
                            {
                                Id = Guid.NewGuid(),
                                Amount = result.GuaranteeIncome.GuaranteeIncomePaid,
                                IncentiveDate = endDate.Value,
                                Remark = "GUARANTEE INCOME AMOUNT " + staffCompensation.GuaranteeIncome,
                                Description = "GUARANTEE INCOME",
                                StaffId = staffId,
                                Status = 1,
                                CreatedBy = currentUser,
                                CreatedAt = DateTimeOffset.UtcNow,
                                LastUpdated = DateTimeOffset.UtcNow,
                                LastModifiedBy = currentUser
                            });
                        else
                        {
                            exist.Amount = result.GuaranteeIncome.GuaranteeIncomePaid;
                            exist.Remark = "GUARANTEE INCOME AMOUNT " + staffCompensation.GuaranteeIncome;
                            exist.LastUpdated = DateTimeOffset.UtcNow;
                            exist.LastModifiedBy = currentUser;

                            _context.Incentives.Update(exist);
                        }
                        _context.SaveChanges();
                    }
                }
            }




            // Step 8: Get Incentive
            var local = startDate.Value.LocalDateTime;
            var day = local.Day == 1
                ? 15
                : DateTime.DaysInMonth(local.Year, local.Month);

            var incentiveDate = new DateTimeOffset(
                local.Year,
                local.Month,
                day,
                0, 0, 0,
                startDate.Value.Offset
            );

            var _incentive = await _context.Incentives.Where(x => x.StaffId == staffId && x.Status == 1 && x.IncentiveDate == endDate).ToListAsync();


            if (_incentive.Count > 0)
            {
                result.Incentives = new List<TherapistIncentiveDto>();

                foreach (var item in _incentive)
                {

                    result.Incentives.Add(new TherapistIncentiveDto
                    {
                        IncentiveDate = item.IncentiveDate,
                        Amount = item.Amount,
                        Description = item.Description,
                        Id = item.Id,
                        Remark = item.Remark
                    });
                }



                result.TotalIncentive = result.Incentives.Sum(x => x.Amount);
            }

            if (result.IsRate)
            {
                result.TotalPayout = result.TotalIncentive + result.RateBase.TotalCommission;
            }
            else if (result.IsCommissionPercentage)
            {

                result.TotalPayout = result.TotalIncentive + result.CommissionPercentage.TotalCommission;
            }



            return Ok(result);
        }

        /// <summary>
        /// Calculates the total hours for the full month when a partial period is requested.
        /// Used for comparison purposes in reports.
        /// </summary>
        private async Task<double> GetFullMonthHoursForComparisonAsync(Guid staffId, DateTimeOffset startDate)
        {
            var startOfMonth = new DateTime(startDate.Year, startDate.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1); // Last day of month
            var fullMonthSales = await GetSalesDataAsync(staffId, startOfMonth, endOfMonth);
            var fullMonthGrouped = GroupSalesByDateAndMenu(fullMonthSales);
            return (fullMonthGrouped.Sum(x => x.FootMins) + fullMonthGrouped.Sum(x => x.BodyMins)) / 60.00;
        }

        /// <summary>
        /// Calculates guarantee income details based on the full month performance split into two periods
        /// (1st half: 1-15, 2nd half: 16-end of month)
        /// </summary>
        private async Task<TherapistGuaranteeIncomeCalculationDto> CalculateTherapistGuaranteeIncomeAsync(
            Guid staffId,
            StaffCompensation staffCompensation,
            DateTimeOffset startDate,
            DateTimeOffset? endDate)
        {
            var startOfMonth = new DateTime(startDate.Year, startDate.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1); // Last day of month

            var fullMonthSales = await GetSalesDataAsync(staffId, startOfMonth, endOfMonth);
            var fullMonthGrouped = GroupSalesByDateAndMenu(fullMonthSales);

            return CalculateTherapistGuaranteeIncomeCalculationDto(fullMonthGrouped, staffCompensation, endDate ?? endOfMonth);
        }

        private async Task<List<TherapistSalesCommissionDto>> GetSalesDataAsync(Guid staffId, DateTimeOffset? startDate, DateTimeOffset? endDate)
        {
            var query = _context.Sales
           .Include(s => s.Menu)
           .Where(s => s.StaffId == staffId && s.Status == 1) // Filter by staff and active sales
           .AsQueryable();

            if (startDate.HasValue)
                query = query.Where(s => s.SalesDate >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(s => s.SalesDate <= endDate.Value); // Use <= since we normalized to date

            var data = await query
                .Select(s => new TherapistSalesCommissionDto
                {
                    SalesDate = s.SalesDate,
                    MenuCode = s.Menu.Code,
                    FootMins = s.Menu.FootMins,
                    BodyMins = s.Menu.BodyMins,
                    StaffCommission = s.StaffCommission,
                    ExtraCommission = s.ExtraCommission,
                    Price = s.StaffCommission != 0 || s.ExtraCommission != 0 ? 0 : s.Price
                })
                .ToListAsync();

            return data;
        }
        private TherapistRateBasedCommissionDto GetTherapistRateBasedCommissionDto(
    List<DailyTherapistCommissionSummaryDto> commissions,
    StaffCompensation comp,
    DateTimeOffset? startDate)
        {
            var response = new TherapistRateBasedCommissionDto
            {
                SelectedPeriodHrs = Math.Round((commissions.Sum(x => x.FootMins) + commissions.Sum(x => x.BodyMins)) / 60.00, 2),
                TotalFootMins = commissions.Sum(x => x.FootMins),
                TotalBodyMins = commissions.Sum(x => x.BodyMins),
                TotalStaffCommission = commissions.Sum(x => x.StaffCommission),
                TotalExtraCommission = commissions.Sum(x => x.ExtraCommission)
            };

            response.TotalFootCommission = Math.Round(comp.FootRatePerHour * (response.TotalFootMins / 60m), 2);
            response.TotalBodyCommission = Math.Round(comp.BodyRatePerHour * (response.TotalBodyMins / 60m), 2); // Fixed: was FootRate!
            response.TotalCommission = response.TotalFootCommission + response.TotalBodyCommission
                                       + response.TotalStaffCommission + response.TotalExtraCommission;

            return response;
        }

        private TherapistPercentageBasedCommissionDto GetTherapistPercentageBasedCommissionDto(
            List<DailyTherapistCommissionSummaryDto> commissions,
            StaffCompensation comp,
            DateTimeOffset? startDate)
        {
            var response = new TherapistPercentageBasedCommissionDto
            {
                SelectedPeriodHrs = Math.Round((commissions.Sum(x => x.FootMins) + commissions.Sum(x => x.BodyMins)) / 60.00, 2),
                TotalStaffCommission = commissions.Sum(x => x.StaffCommission),
                TotalExtraCommission = commissions.Sum(x => x.ExtraCommission),
                TotalPrice = commissions.Sum(x => x.Price),
                Percentage = comp.CommissionBasePercentage
            };

            response.TotalCommission = (response.TotalPrice * (comp.CommissionBasePercentage / 100m))
                                       + response.TotalStaffCommission + response.TotalExtraCommission; ;
            return response;
        }

        private TherapistGuaranteeIncomeCalculationDto CalculateTherapistGuaranteeIncomeCalculationDto(
    List<DailyTherapistCommissionSummaryDto> fullMonthData,
    StaffCompensation comp,
    DateTimeOffset endDate)
        {
            var endOfMonth = new DateTime(endDate.Year, endDate.Month, DateTime.DaysInMonth(endDate.Year, endDate.Month));
            var midDate = new DateTime(endDate.Year, endDate.Month, 15);

            var firstHalf = fullMonthData.Where(x => x.SalesDate <= midDate).ToList();
            var secondHalf = fullMonthData.Where(x => x.SalesDate > midDate && x.SalesDate <= endDate).ToList();

            decimal firstCommission, secondCommission;

            if (comp.IsRate)
            {
                var first = GetTherapistRateBasedCommissionDto(firstHalf, comp, null);
                var second = GetTherapistRateBasedCommissionDto(secondHalf, comp, null);
                firstCommission = first.TotalCommission;
                secondCommission = second.TotalCommission;
            }
            else if (comp.IsCommissionPercentage)
            {
                var first = GetTherapistPercentageBasedCommissionDto(firstHalf, comp, null);
                var second = GetTherapistPercentageBasedCommissionDto(secondHalf, comp, null);
                firstCommission = first.TotalCommission;
                secondCommission = second.TotalCommission;
            }
            else
            {
                firstCommission = firstHalf.Sum(x => x.StaffCommission + x.ExtraCommission);
                secondCommission = secondHalf.Sum(x => x.StaffCommission + x.ExtraCommission);
            }

            var total = firstCommission + secondCommission;
            var guaranteePaid = comp.GuaranteeIncome > total ? comp.GuaranteeIncome - total : 0;

            return new TherapistGuaranteeIncomeCalculationDto
            {
                FirstPeriodCommission = firstCommission,
                SecondPeriodCommission = secondCommission,
                TotalCommission = total,
                GuaranteeIncomePaid = guaranteePaid
            };
        }
        private List<DailyTherapistCommissionSummaryDto> GroupSalesByDateAndMenu(List<TherapistSalesCommissionDto> rawData)
        {
            return rawData
                .GroupBy(s => new { Date = s.SalesDate.Date, Code = s.MenuCode })
                .Select(g => new DailyTherapistCommissionSummaryDto
                {
                    Id = Guid.NewGuid().ToString(),
                    SalesDate = g.Key.Date,
                    MenuCode = g.Key.Code,
                    FootMins = g.Sum(s => s.FootMins),
                    BodyMins = g.Sum(s => s.BodyMins),
                    StaffCommission = g.Sum(s => s.StaffCommission),
                    ExtraCommission = g.Sum(s => s.ExtraCommission),
                    Price = g.Sum(s => s.Price)
                })
                .OrderBy(s => s.SalesDate)
                .ToList();
        }

    }

    public class TherapistSalesCommissionDto
    {
        public DateTimeOffset SalesDate { get; set; }
        public string MenuCode { get; set; }
        public int FootMins { get; set; }
        public int BodyMins { get; set; }
        public decimal StaffCommission { get; set; }
        public decimal ExtraCommission { get; set; }
        public decimal Price { get; set; }
    }

    public class DailyTherapistCommissionSummaryDto
    {
        public string Id { get; set; } = string.Empty;
        public DateTimeOffset SalesDate { get; set; }
        public string MenuCode { get; set; } = string.Empty;
        public int FootMins { get; set; }
        public int BodyMins { get; set; }
        public decimal StaffCommission { get; set; }
        public decimal ExtraCommission { get; set; }
        public decimal Price { get; set; }
    }
    public class TherapistIncentiveDto
    {
        public Guid Id { get; set; } = Guid.Empty;
        public DateTimeOffset IncentiveDate { get; set; }
        public string? Description { get; set; } = string.Empty;
        public string? Remark { get; set; }
        public decimal Amount { get; set; }
    }

    public class TherapistCommissionReportDto
    {
        public List<DailyTherapistCommissionSummaryDto> Commissions { get; set; }
        public List<TherapistIncentiveDto> Incentives { get; set; }
        public bool IsRate { get; set; }
        public TherapistRateBasedCommissionDto RateBase { get; set; }
        public bool IsCommissionPercentage { get; set; }
        public TherapistPercentageBasedCommissionDto CommissionPercentage { get; set; }
        public bool IsGuaranteeIncome { get; set; }
        public TherapistGuaranteeIncomeCalculationDto GuaranteeIncome { get; set; }
        public decimal TotalIncentive { get; set; }
        public decimal TotalPayout { get; set; }

    }

    public class TherapistGuaranteeIncomeCalculationDto
    {
        public decimal FirstPeriodCommission { get; set; }
        public decimal SecondPeriodCommission { get; set; }
        public decimal TotalCommission { get; set; }
        public decimal GuaranteeIncomePaid { get; set; }
    }

    public class TherapistRateBasedCommissionDto
    {
        public double SelectedPeriodHrs { get; set; }
        public double AllPeriodHrs { get; set; }
        public int TotalFootMins { get; set; }
        public decimal TotalFootCommission { get; set; }
        public int TotalBodyMins { get; set; }
        public decimal TotalBodyCommission { get; set; }
        public decimal TotalStaffCommission { get; set; }
        public decimal TotalExtraCommission { get; set; }
        public decimal TotalCommission { get; set; }
    }

    public class TherapistPercentageBasedCommissionDto
    {
        public double SelectedPeriodHrs { get; set; }
        public double AllPeriodHrs { get; set; }
        public decimal TotalStaffCommission { get; set; }
        public decimal TotalExtraCommission { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal TotalCommission { get; set; }
        public decimal Percentage { get; set; }
    }
}