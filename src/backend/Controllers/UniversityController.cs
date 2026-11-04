using KhduSouvenirShop.API.Repositories;
using KhduSouvenirShop.API.Models;
using KhduSouvenirShop.API.Models.Common;
using KhduSouvenirShop.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KhduSouvenirShop.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,Manager")]
    public class UniversityController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly IUniversityService _universityService;
        private readonly ILogger<UniversityController> _logger;
        public UniversityController(IUserRepository userRepository, IUniversityService universityService, ILogger<UniversityController> logger)
        {
            _userRepository = userRepository;
            _universityService = universityService;
            _logger = logger;
        }
        [HttpGet("pending-verifications")]
        public async Task<ActionResult> GetPendingVerifications()
        {
            // Користувачі з університетською поштою, але без статусу студента (або з застарілим статусом)
            var users = await _userRepository.Query()
                .Where(u => (u.Email.EndsWith("@ksu.edu.ua") || u.Email.EndsWith("@student.ksu.edu.ua")) 
                             && (u.StudentStatus == "NONE" || u.StudentExpiresAt < DateTime.UtcNow))
                .Select(u => new {
                    u.UserId,
                    u.FirstName,
                    u.LastName,
                    u.Email,
                    u.StudentStatus,
                    u.StudentExpiresAt
                })
                .ToListAsync();
            return Ok(ApiResponse<object>.SuccessResult(users));
        }
        [HttpPost("verify/{userId}")]
        public async Task<ActionResult> VerifyStudent(int userId)
        {
            var result = await _universityService.VerifyAndApplyStudentStatusAsync(userId);
            if (result)
            {
                return Ok(ApiResponse<object?>.SuccessResult(null, "Статус студента успішно оновлено"));
            }
            return BadRequest(ApiResponse<object>.FailureResult("Не вдалося верифікувати студента через University API", "VerificationError"));
        }
        [HttpPatch("manual-status/{userId}")]
        [Authorize(Roles = "Administrator")]
        public async Task<ActionResult> ManualStatusUpdate(int userId, [FromBody] ManualStatusDto dto)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return NotFound();
            user.StudentStatus = dto.Status;
            user.GPA = dto.GPA;
            user.StudentVerifiedAt = DateTime.UtcNow;
            user.StudentExpiresAt = dto.ExpiresAt ?? DateTime.UtcNow.AddMonths(4);
            await _userRepository.SaveChangesAsync();
            return Ok(ApiResponse<object?>.SuccessResult(null, "Статус оновлено вручну адміністратором"));
        }
    }
    public class ManualStatusDto
    {
        public string Status { get; set; } = "REGULAR";
        public decimal? GPA { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
}