using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QwenHT.Data;
using QwenHT.Models;

namespace QwenHT.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Require authentication but handle authorization at action level or with custom policy
    public class StaffController(ApplicationDbContext _context) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<IEnumerable<StaffDto>>> GetStaff()
        {
            var staff = await _context.Staff
                .AsNoTracking()
                .Include(s => s.Employments)
                .Include(s => s.Compensations)
                .Include(s => s.BankAccounts)
                .ToListAsync();

            var staffDtos = new List<StaffDto>();

            foreach (var s in staff)
            {
                var staffDto = MapToDto(s);
                staffDtos.Add(staffDto);
            }

            return Ok(staffDtos);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<StaffDto>> GetStaff(Guid id)
        {
            var staff = await _context.Staff
                .AsNoTracking()
                .Include(s => s.Employments)
                .Include(s => s.Compensations)
                .Include(s => s.BankAccounts)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (staff == null)
            {
                return NotFound();
            }

            var staffDto = MapToDto(staff);
            return Ok(staffDto);
        }

        [HttpPost]
        public async Task<ActionResult<StaffDto>> CreateStaff(CreateStaffDto staffDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Get the username from JWT claim using standard User context
            var username = User?.Claims?.FirstOrDefault(c => c.Type == "username")?.Value ?? "Unknown";

            var staff = new Staff
            {
                Id = Guid.NewGuid(),
                NickName = staffDto.NickName,
                FullName = staffDto.FullName,
                Gender = staffDto.Gender,
                PhoneNo = staffDto.PhoneNo,
                Nationality = staffDto.Nationality,
                HostelName = staffDto.HostelName,
                HostelRoom = staffDto.HostelRoom,
                Reference = staffDto.Reference,
                Status = 1, // Set as active by default
                CreatedBy = username, // Set the creator from JWT claim
                CreatedAt = DateTimeOffset.UtcNow,
                LastUpdated = DateTimeOffset.UtcNow,
                LastModifiedBy = username // Also set as last modified by initial creator
            };

            _context.Staff.Add(staff);
            await _context.SaveChangesAsync();

            // Add compensation details if provided
            if (staffDto.FootRatePerHour.HasValue || staffDto.BodyRatePerHour.HasValue ||
                staffDto.CommissionBasePercentage.HasValue || staffDto.GuaranteeIncome.HasValue ||
                staffDto.IsRate.HasValue || staffDto.IsCommissionPercentage.HasValue || staffDto.IsGuaranteeIncome.HasValue)
            {
                var compensation = new StaffCompensation
                {
                    Id = Guid.NewGuid(),
                    StaffId = staff.Id,
                    IsRate = staffDto.IsRate ?? false,
                    FootRatePerHour = staffDto.FootRatePerHour ?? 0,
                    BodyRatePerHour = staffDto.BodyRatePerHour ?? 0,
                    IsCommissionPercentage = staffDto.IsCommissionPercentage ?? false,
                    CommissionBasePercentage = staffDto.CommissionBasePercentage ?? 0,
                    IsGuaranteeIncome = staffDto.IsGuaranteeIncome ?? false,
                    GuaranteeIncome = staffDto.GuaranteeIncome ?? 0
                };
                _ = _context.StaffCompensations.Add(compensation);
                await _context.SaveChangesAsync();
            }

            var employment = new StaffEmployment
            {
                Id = Guid.NewGuid(),
                StaffId = staff.Id,
                Outlet = staffDto.Outlet,
                Type = staffDto.Type,
                CheckIn = staffDto.CheckIn,
                CheckOut = staffDto.CheckOut,

            };
            _ = _context.StaffEmployments.Add(employment);
            await _context.SaveChangesAsync();


            var bank = new BankAccount
            {
                Id = Guid.NewGuid(),
                StaffId = staff.Id,
                BankName = staffDto.BankName,
                AccountHolderName = staffDto.AccountHolderName,
                AccountNumber = staffDto.AccountNumber

            };
            _ = _context.BankAccounts.Add(bank);
            await _context.SaveChangesAsync();

            var createdStaffDto = MapToDto(staff);
            return CreatedAtAction(nameof(GetStaff), new { id = staff.Id }, createdStaffDto);
        }

        [HttpPost("{id}")]
        public async Task<IActionResult> UpdateStaff(Guid id, UpdateStaffDto staffDto)
        {
            var staff = await _context.Staff
                .Include(s => s.Employments)
                .Include(s => s.Compensations)
                .Include(s => s.BankAccounts)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (staff == null)
            {
                return NotFound();
            }

            // Get the username from JWT claim using standard User context
            var username = User?.Claims?.FirstOrDefault(c => c.Type == "username")?.Value ?? "Unknown";

            // Update basic staff properties
            staff.NickName = staffDto.NickName;
            staff.Gender = staffDto.Gender;
            staff.FullName = staffDto.FullName;
            staff.PhoneNo = staffDto.PhoneNo;
            staff.Nationality = staffDto.Nationality;
            staff.HostelName = staffDto.HostelName;
            staff.HostelRoom = staffDto.HostelRoom;
            staff.Reference = staffDto.Reference;
            staff.Status = staffDto.Status;
            staff.LastUpdated = DateTimeOffset.UtcNow;
            staff.LastModifiedBy = username; // Set the modifier from JWT claim

            // Update employment details if provided
            if (staffDto.CheckIn.HasValue || staffDto.CheckOut.HasValue || !string.IsNullOrEmpty(staffDto.Outlet) || !string.IsNullOrEmpty(staffDto.Type))
            {
                var existingEmployment = staff.Employments?.FirstOrDefault();
                if (existingEmployment == null)
                {
                    // Create new employment record
                    var newEmployment = new StaffEmployment
                    {
                        Id = Guid.NewGuid(),
                        StaffId = staff.Id,
                        Outlet = staffDto.Outlet ?? "Default",
                        Type = staffDto.Type ?? "Therapist",
                        CheckIn = staffDto.CheckIn,
                        CheckOut = staffDto.CheckOut
                    };
                    _ = _context.StaffEmployments.Add(newEmployment);
                }
                else
                {
                    // Update existing employment record
                    existingEmployment.Outlet = !string.IsNullOrEmpty(staffDto.Outlet) ? staffDto.Outlet : existingEmployment.Outlet;
                    existingEmployment.Type = !string.IsNullOrEmpty(staffDto.Type) ? staffDto.Type : existingEmployment.Type;
                    existingEmployment.CheckIn = staffDto.CheckIn;
                    existingEmployment.CheckOut = staffDto.CheckOut;
                }
            }

            // Update compensation details if provided
            if (staffDto.FootRatePerHour.HasValue || staffDto.BodyRatePerHour.HasValue ||
                staffDto.CommissionBasePercentage.HasValue || staffDto.GuaranteeIncome.HasValue ||
                staffDto.IsRate.HasValue || staffDto.IsCommissionPercentage.HasValue || staffDto.IsGuaranteeIncome.HasValue)
            {
                var existingCompensation = staff.Compensations?.FirstOrDefault();
                if (existingCompensation == null)
                {
                    // Create new compensation record
                    var newCompensation = new StaffCompensation
                    {
                        Id = Guid.NewGuid(),
                        StaffId = staff.Id,
                        IsRate = staffDto.IsRate ?? false,
                        FootRatePerHour = staffDto.FootRatePerHour ?? 0,
                        BodyRatePerHour = staffDto.BodyRatePerHour ?? 0,
                        IsCommissionPercentage = staffDto.IsCommissionPercentage ?? false,
                        CommissionBasePercentage = staffDto.CommissionBasePercentage ?? 0,
                        IsGuaranteeIncome = staffDto.IsGuaranteeIncome ?? false,
                        GuaranteeIncome = staffDto.GuaranteeIncome ?? 0
                    };
                    _ = _context.StaffCompensations.Add(newCompensation);
                }
                else
                {
                    // Update existing compensation record
                    existingCompensation.IsRate = staffDto.IsRate ?? existingCompensation.IsRate;
                    existingCompensation.FootRatePerHour = staffDto.FootRatePerHour ?? existingCompensation.FootRatePerHour;
                    existingCompensation.BodyRatePerHour = staffDto.BodyRatePerHour ?? existingCompensation.BodyRatePerHour;
                    existingCompensation.IsCommissionPercentage = staffDto.IsCommissionPercentage ?? existingCompensation.IsCommissionPercentage;
                    existingCompensation.CommissionBasePercentage = staffDto.CommissionBasePercentage ?? existingCompensation.CommissionBasePercentage;
                    existingCompensation.IsGuaranteeIncome = staffDto.IsGuaranteeIncome ?? existingCompensation.IsGuaranteeIncome;
                    existingCompensation.GuaranteeIncome = staffDto.GuaranteeIncome ?? existingCompensation.GuaranteeIncome;
                }
            }

            // Update bank account details if provided
            if (!string.IsNullOrEmpty(staffDto.BankName) || !string.IsNullOrEmpty(staffDto.AccountHolderName) ||
                !string.IsNullOrEmpty(staffDto.AccountNumber))
            {
                var existingBankAccount = staff.BankAccounts?.FirstOrDefault();
                if (existingBankAccount == null)
                {
                    // Create new bank account record
                    var newBankAccount = new BankAccount
                    {
                        Id = Guid.NewGuid(),
                        StaffId = staff.Id,
                        BankName = staffDto.BankName ?? "",
                        AccountHolderName = staffDto.AccountHolderName ?? "",
                        AccountNumber = staffDto.AccountNumber ?? ""
                    };
                    _ = _context.BankAccounts.Add(newBankAccount);
                }
                else
                {
                    // Update existing bank account record
                    existingBankAccount.BankName = staffDto.BankName ?? existingBankAccount.BankName;
                    existingBankAccount.AccountHolderName = staffDto.AccountHolderName ?? existingBankAccount.AccountHolderName;
                    existingBankAccount.AccountNumber = staffDto.AccountNumber ?? existingBankAccount.AccountNumber;
                }
            }

            _context.Staff.Update(staff);
            await _context.SaveChangesAsync();

            return Ok(MapToDto(staff));
        }

        [HttpPost("{id}/delete")]
        public async Task<IActionResult> DeleteStaff(Guid id)
        {
            var staff = await _context.Staff.FindAsync(id);
            if (staff == null)
            {
                return NotFound();
            }

            // Instead of hard delete, we'll mark as inactive (status = 0)
            staff.Status = 0; // Inactive
            staff.LastUpdated = DateTimeOffset.UtcNow;

            _context.Staff.Update(staff);
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpGet("paginated")]
        public async Task<ActionResult<PaginatedResponse<StaffDto>>> GetStaffPaginated(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? sortField = null,
            [FromQuery] string? sortDirection = null,
            [FromQuery] string? searchTerm = null)
        {
            // Validate pagination parameters
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            // Build the base query using the DbContext WITH INCLUDES for related entities
            IQueryable<Staff> query = _context.Staff
                .AsNoTracking()
                .Include(s => s.Employments)
                .Include(s => s.Compensations)
                .Include(s => s.BankAccounts);

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(s =>
                    EF.Functions.Like(s.FullName, $"%{searchTerm}%") ||
                    EF.Functions.Like(s.NickName, $"%{searchTerm}%") ||
                    EF.Functions.Like(s.PhoneNo, $"%{searchTerm}%") ||
                    EF.Functions.Like(s.Nationality, $"%{searchTerm}%")
                );
            }

            // Determine the order to apply (before pagination)
            IOrderedQueryable<Staff> orderedQuery = query switch
            {
                _ when sortField?.ToLower() == "fullname" => sortDirection?.ToLower() == "desc"
                    ? query.OrderByDescending(s => s.FullName)
                    : query.OrderBy(s => s.FullName),
                _ when sortField?.ToLower() == "nickname" => sortDirection?.ToLower() == "desc"
                    ? query.OrderByDescending(s => s.NickName ?? "")
                    : query.OrderBy(s => s.NickName ?? ""),
                _ when sortField?.ToLower() == "phonenumber" => sortDirection?.ToLower() == "desc"
                    ? query.OrderByDescending(s => s.PhoneNo ?? "")
                    : query.OrderBy(s => s.PhoneNo ?? ""),
                _ when sortField?.ToLower() == "nationality" => sortDirection?.ToLower() == "desc"
                    ? query.OrderByDescending(s => s.Nationality ?? "")
                    : query.OrderBy(s => s.Nationality ?? ""),
                _ when sortField?.ToLower() == "status" => sortDirection?.ToLower() == "desc"
                    ? query.OrderByDescending(s => s.Status)
                    : query.OrderBy(s => s.Status),
                _ => query.OrderByDescending(s => s.Status).ThenBy(s=>s.NickName) // Default sort
            };

            // Get total count of matching records before pagination
            var totalCount = await orderedQuery.CountAsync();

            // Apply pagination (Skip and Take)
            var pagedStaff = await orderedQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Convert to DTOs
            var staffDtos = new List<StaffDto>();
            foreach (var s in pagedStaff)
            {
                var staffDto = MapToDto(s);
                staffDtos.Add(staffDto);
            }

            var response = new PaginatedResponse<StaffDto>
            {
                Data = staffDtos,
                TotalCount = totalCount,
                PageSize = pageSize,
                CurrentPage = page
            };

            return Ok(response);
        }

        private StaffDto MapToDto(Staff staff)
        {
            var staffDto = new StaffDto
            {
                Id = staff.Id,
                NickName = staff.NickName,
                FullName = staff.FullName,
                Gender = staff.Gender,
                PhoneNo = staff.PhoneNo,
                Nationality = staff.Nationality,
                HostelName = staff.HostelName,
                HostelRoom = staff.HostelRoom,
                Reference = staff.Reference,
                Status = staff.Status,
                CreatedBy = staff.CreatedBy,
                CreatedAt = staff.CreatedAt,
                LastUpdated = staff.LastUpdated,
                LastModifiedBy = staff.LastModifiedBy
            };

            // Add employment details if they exist
            if (staff.Employments != null && staff.Employments.Any())
            {
                var employment = staff.Employments.First(); // Get the first employment record
                staffDto.Outlet = employment.Outlet;
                staffDto.Type = employment.Type;
                staffDto.CheckIn = employment.CheckIn;
                staffDto.CheckOut = employment.CheckOut;
            }

            // Add compensation details if they exist
            if (staff.Compensations != null && staff.Compensations.Any())
            {
                var compensation = staff.Compensations.First(); // Get the first compensation record
                staffDto.IsRate = compensation.IsRate;
                staffDto.FootRatePerHour = compensation.FootRatePerHour;
                staffDto.BodyRatePerHour = compensation.BodyRatePerHour;
                staffDto.IsCommissionPercentage = compensation.IsCommissionPercentage;
                staffDto.CommissionBasePercentage = compensation.CommissionBasePercentage;
                staffDto.IsGuaranteeIncome = compensation.IsGuaranteeIncome;
                staffDto.GuaranteeIncome = compensation.GuaranteeIncome;
            }

            // Add bank account details if they exist
            if (staff.BankAccounts != null && staff.BankAccounts.Any())
            {
                var bankAccount = staff.BankAccounts.First(); // Get the first bank account record
                staffDto.BankName = bankAccount.BankName;
                staffDto.AccountHolderName = bankAccount.AccountHolderName;
                staffDto.AccountNumber = bankAccount.AccountNumber;
            }

            return staffDto;
        }
    }
}