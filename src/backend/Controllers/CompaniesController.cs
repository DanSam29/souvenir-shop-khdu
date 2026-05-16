using KhduSouvenirShop.API.Data;
using KhduSouvenirShop.API.Models;
using KhduSouvenirShop.API.Models.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using System.Text.Json;

namespace KhduSouvenirShop.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Administrator,Manager")]
    public class CompaniesController(AppDbContext context, ILogger<CompaniesController> logger) : ControllerBase
    {
        private readonly AppDbContext _context = context;
        private readonly ILogger<CompaniesController> _logger = logger;

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<PagedResponse<object>>), StatusCodes.Status200OK)]
        public async Task<ActionResult> GetCompanies(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? search = null,
            [FromQuery] bool? isActive = null)
        {
            var query = _context.Companies.AsQueryable();

            if (isActive.HasValue)
                query = query.Where(c => c.IsActive == isActive.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(c => c.Name.ToLower().Contains(s) || (c.Email != null && c.Email.ToLower().Contains(s)));
            }

            var count = await query.CountAsync();
            var items = await query
                .OrderBy(c => c.Name)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var response = new PagedResponse<object>(items, count, pageNumber, pageSize);
            return Ok(ApiResponse<PagedResponse<object>>.SuccessResult(response));
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> GetCompany(int id)
        {
            var company = await _context.Companies.FindAsync(id);
            if (company == null) return NotFound(ApiResponse<object>.FailureResult("Компанію не знайдено", "NotFound"));
            return Ok(ApiResponse<object>.SuccessResult(company));
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status201Created)]
        public async Task<ActionResult> CreateCompany([FromBody] CompanyDto dto)
        {
            if (await _context.Companies.AnyAsync(c => c.Name == dto.Name))
                return BadRequest(ApiResponse<object>.FailureResult("Компанія з такою назвою вже існує", "Conflict"));

            var company = new Company
            {
                Name = dto.Name,
                ContactPerson = dto.ContactPerson,
                Phone = dto.Phone,
                Email = dto.Email,
                Address = dto.Address,
                Notes = dto.Notes,
                IsActive = true
            };

            _context.Companies.Add(company);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCompany), new { id = company.CompanyId }, ApiResponse<object>.SuccessResult(company, "Компанію створено"));
        }

        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<ActionResult> UpdateCompany(int id, [FromBody] CompanyDto dto)
        {
            var company = await _context.Companies.FindAsync(id);
            if (company == null) return NotFound(ApiResponse<object>.FailureResult("Компанію не знайдено", "NotFound"));

            if (await _context.Companies.AnyAsync(c => c.Name == dto.Name && c.CompanyId != id))
                return BadRequest(ApiResponse<object>.FailureResult("Компанія з такою назвою вже існує", "Conflict"));

            company.Name = dto.Name;
            company.ContactPerson = dto.ContactPerson;
            company.Phone = dto.Phone;
            company.Email = dto.Email;
            company.Address = dto.Address;
            company.Notes = dto.Notes;
            company.IsActive = dto.IsActive;

            await _context.SaveChangesAsync();
            return Ok(ApiResponse<object>.SuccessResult(company, "Дані компанії оновлено"));
        }

        [HttpPatch("{id}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<ActionResult> PatchCompany(int id, [FromBody] IDictionary<string, JsonElement> updates)
        {
            var company = await _context.Companies.FindAsync(id);
            if (company == null) return NotFound(ApiResponse<object>.FailureResult("Компанію не знайдено"));

            foreach (var update in updates)
            {
                var value = update.Value;
                switch (update.Key.ToLower())
                {
                    case "name": company.Name = value.GetString() ?? company.Name; break;
                    case "isactive": 
                        if (value.ValueKind == JsonValueKind.True || value.ValueKind == JsonValueKind.False) company.IsActive = value.GetBoolean();
                        else if (bool.TryParse(value.GetString(), out var a)) company.IsActive = a; 
                        break;
                    case "email": company.Email = value.GetString(); break;
                    case "phone": company.Phone = value.GetString(); break;
                }
            }

            await _context.SaveChangesAsync();
            return Ok(ApiResponse<object>.SuccessResult(company, "Дані компанії частково оновлено"));
        }

        [HttpPost("bulk-delete")]
        public async Task<ActionResult> BulkDelete([FromBody] List<int> ids)
        {
            var companies = await _context.Companies
                .Include(c => c.IncomingDocuments)
                .Include(c => c.OutgoingDocuments)
                .Where(c => ids.Contains(c.CompanyId))
                .ToListAsync();

            var deletedCount = 0;
            var deactivatedCount = 0;

            foreach (var company in companies)
            {
                if (company.IncomingDocuments.Count != 0 || company.OutgoingDocuments.Count != 0)
                {
                    company.IsActive = false;
                    deactivatedCount++;
                }
                else
                {
                    _context.Companies.Remove(company);
                    deletedCount++;
                }
            }

            await _context.SaveChangesAsync();
            return Ok(ApiResponse<object>.SuccessResult(new { deletedCount, deactivatedCount }, $"Видалено {deletedCount}, деактивовано {deactivatedCount} компаній"));
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<ActionResult> DeleteCompany(int id)
        {
            var company = await _context.Companies
                .Include(c => c.IncomingDocuments)
                .Include(c => c.OutgoingDocuments)
                .FirstOrDefaultAsync(c => c.CompanyId == id);

            if (company == null) return NotFound(ApiResponse<object>.FailureResult("Компанію не знайдено", "NotFound"));

            if (company.IncomingDocuments.Count != 0 || company.OutgoingDocuments.Count != 0)
            {
                // М'яке видалення (деактивація), якщо є пов'язані документи
                company.IsActive = false;
                await _context.SaveChangesAsync();
                return Ok(ApiResponse<object?>.SuccessResult(null, "Компанію деактивовано (має пов'язані документи)"));
            }

            _context.Companies.Remove(company);
            await _context.SaveChangesAsync();
            return Ok(ApiResponse<object?>.SuccessResult(null, "Компанію видалено"));
        }
    }

    public class CompanyDto
    {
        public string Name { get; set; } = string.Empty;
        public string? ContactPerson { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? Notes { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
