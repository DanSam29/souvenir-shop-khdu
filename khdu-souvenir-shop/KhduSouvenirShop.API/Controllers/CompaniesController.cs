using KhduSouvenirShop.API.Data;
using KhduSouvenirShop.API.Models;
using KhduSouvenirShop.API.Models.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KhduSouvenirShop.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,Manager")]
    public class CompaniesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<CompaniesController> _logger;

        public CompaniesController(AppDbContext context, ILogger<CompaniesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult> GetCompanies([FromQuery] bool onlyActive = false)
        {
            var query = _context.Companies.AsQueryable();
            if (onlyActive) query = query.Where(c => c.IsActive);
            
            var companies = await query.OrderBy(c => c.Name).ToListAsync();
            return Ok(ApiResponse<object>.SuccessResult(companies));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetCompany(int id)
        {
            var company = await _context.Companies.FindAsync(id);
            if (company == null) return NotFound(ApiResponse<object>.FailureResult("Компанію не знайдено", "NotFound"));
            return Ok(ApiResponse<object>.SuccessResult(company));
        }

        [HttpPost]
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
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Companies.Add(company);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<object>.SuccessResult(company, "Компанію створено"));
        }

        [HttpPut("{id}")]
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
            company.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(ApiResponse<object>.SuccessResult(company, "Дані компанії оновлено"));
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteCompany(int id)
        {
            var company = await _context.Companies
                .Include(c => c.IncomingDocuments)
                .Include(c => c.OutgoingDocuments)
                .FirstOrDefaultAsync(c => c.CompanyId == id);

            if (company == null) return NotFound(ApiResponse<object>.FailureResult("Компанію не знайдено", "NotFound"));

            if (company.IncomingDocuments.Any() || company.OutgoingDocuments.Any())
            {
                // М'яке видалення (деактивація), якщо є пов'язані документи
                company.IsActive = false;
                company.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return Ok(ApiResponse<object>.SuccessResult(null, "Компанію деактивовано (має пов'язані документи)"));
            }

            _context.Companies.Remove(company);
            await _context.SaveChangesAsync();
            return Ok(ApiResponse<object>.SuccessResult(null, "Компанію видалено"));
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