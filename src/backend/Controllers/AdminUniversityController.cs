using KhduSouvenirShop.API.DTOs.Admin;
using KhduSouvenirShop.API.DTOs.Requests;
using KhduSouvenirShop.API.Models;
using KhduSouvenirShop.API.Models.Common;
using KhduSouvenirShop.API.Repositories;
using KhduSouvenirShop.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;

namespace KhduSouvenirShop.API.Controllers
{
    /// <summary>
    /// Адміністративне керування верифікацією студентів: черга повторів, ручне підтвердження,
    /// відхилення та повторний запуск перевірки.
    /// Доступ: Administrator і SuperAdmin.
    /// </summary>
    [Route("api/admin/university")]
    [ApiController]
    [Authorize(Roles = "Administrator,SuperAdmin")]
    public class AdminUniversityController(
        IRepository<VerificationQueue> queueRepository,
        IUserRepository userRepository,
        IEmailService emailService,
        ILogger<AdminUniversityController> logger) : ControllerBase
    {
        private readonly IRepository<VerificationQueue> _queueRepository = queueRepository;
        private readonly IUserRepository _userRepository = userRepository;
        private readonly IEmailService _emailService = emailService;
        private readonly ILogger<AdminUniversityController> _logger = logger;
        // Студентські статуси, які адміністратор може присвоїти вручну
        private static readonly string[] ManualStatuses = { "REGULAR", "SCHOLARSHIP", "HIGH_ACHIEVER" };

        // GET: api/admin/university/verifications - черга та історія верифікацій (пагінація, фільтр за статусом)
        [HttpGet("verifications")]
        [ProducesResponseType(typeof(ApiResponse<PagedResponse<VerificationQueueItemResponse>>), StatusCodes.Status200OK)]
        public async Task<ActionResult> GetVerifications(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? status = null)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 10;

            var query = _queueRepository.Query();
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(v => v.Status == status);
            }

            var count = await query.CountAsync();
            var items = await query
                .OrderByDescending(v => v.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(v => new VerificationQueueItemResponse
                {
                    QueueId = v.QueueId,
                    UserId = v.UserId,
                    Email = v.Email,
                    FirstName = v.User != null ? v.User.FirstName : string.Empty,
                    LastName = v.User != null ? v.User.LastName : string.Empty,
                    StudentStatus = v.User != null ? v.User.StudentStatus : "NONE",
                    Attempts = v.Attempts,
                    NextAttemptAt = v.NextAttemptAt,
                    LastError = v.LastError,
                    Status = v.Status,
                    CreatedAt = v.CreatedAt
                })
                .ToListAsync();

            var response = new PagedResponse<VerificationQueueItemResponse>(items, count, pageNumber, pageSize);
            return Ok(ApiResponse<PagedResponse<VerificationQueueItemResponse>>.SuccessResult(response));
        }

        // POST: api/admin/university/verifications/{id}/approve - ручне підтвердження студентського статусу
        [HttpPost("verifications/{id:int}/approve")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> ApproveVerification(
            int id,
            [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] UpdateStudentStatusRequest? dto)
        {
            var entry = await _queueRepository.GetByIdAsync(id);
            if (entry == null)
            {
                return NotFound(ApiResponse<object>.FailureResult("Запис черги не знайдено", "NotFound"));
            }
            var user = await _userRepository.GetByIdAsync(entry.UserId);
            if (user == null)
            {
                return NotFound(ApiResponse<object>.FailureResult("Користувача не знайдено", "NotFound"));
            }

            var status = string.IsNullOrWhiteSpace(dto?.Status) ? "REGULAR" : dto!.Status.Trim().ToUpperInvariant();
            if (!ManualStatuses.Contains(status))
            {
                return BadRequest(ApiResponse<object>.FailureResult("Неприпустиме значення студентського статусу", "InvalidStatus"));
            }

            user.StudentStatus = status;
            user.GPA = dto?.GPA ?? user.GPA;
            user.StudentVerifiedAt = DateTime.UtcNow;
            user.StudentExpiresAt = dto?.ExpiresAt ?? DateTime.UtcNow.AddMonths(UniversityService.StudentStatusValidMonths);
            entry.Status = "Completed";
            entry.LastError = null;
            await _queueRepository.SaveChangesAsync();

            var discount = UniversityService.GetDiscountPercent(status);
            await _emailService.SendStudentVerificationEmailAsync(user.Email, status, "ua", discount);
            _logger.LogInformation("Запис черги {QueueId} підтверджено вручну; користувачу {UserId} присвоєно {Status}",
                id, user.UserId, status);

            return Ok(ApiResponse<object>.SuccessResult(
                new { studentStatus = user.StudentStatus, studentExpiresAt = user.StudentExpiresAt },
                "Студентський статус підтверджено вручну"));
        }

        // POST: api/admin/university/verifications/{id}/reject - відхилення верифікації
        [HttpPost("verifications/{id:int}/reject")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> RejectVerification(int id)
        {
            var entry = await _queueRepository.GetByIdAsync(id);
            if (entry == null)
            {
                return NotFound(ApiResponse<object>.FailureResult("Запис черги не знайдено", "NotFound"));
            }

            entry.Status = "Failed";
            entry.LastError = "Відхилено адміністратором";
            await _queueRepository.SaveChangesAsync();
            _logger.LogInformation("Запис черги {QueueId} відхилено адміністратором", id);

            return Ok(ApiResponse<object?>.SuccessResult(null, "Верифікацію відхилено"));
        }

        // POST: api/admin/university/verifications/{id}/retry - повторний запуск перевірки
        [HttpPost("verifications/{id:int}/retry")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> RetryVerification(int id)
        {
            var entry = await _queueRepository.GetByIdAsync(id);
            if (entry == null)
            {
                return NotFound(ApiResponse<object>.FailureResult("Запис черги не знайдено", "NotFound"));
            }

            // Повернення запису в чергу: фоновий сервіс підхопить його на найближчому циклі
            entry.Status = "Pending";
            entry.NextAttemptAt = DateTime.UtcNow;
            entry.LastError = null;
            await _queueRepository.SaveChangesAsync();
            _logger.LogInformation("Запис черги {QueueId} поставлено на повторну перевірку", id);

            return Ok(ApiResponse<object?>.SuccessResult(null, "Запит поставлено в чергу на повторну перевірку"));
        }
    }
}
