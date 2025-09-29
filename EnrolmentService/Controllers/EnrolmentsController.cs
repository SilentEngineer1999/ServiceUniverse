using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EnrolmentService.Data;
using EnrolmentService.Models;

namespace EnrolmentService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EnrolmentsController(AppDbContext db, ILogger<EnrolmentsController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Enrolment dto, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        db.Enrolments.Add(dto);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Created enrolment {Id} for student {Sid}", dto.EnrolmentId, dto.StudentId);
        return CreatedAtAction(nameof(GetById), new { id = dto.EnrolmentId }, dto);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var e = await db.Enrolments.FindAsync(new object[]{ id }, ct);
        return e is null ? NotFound() : Ok(e);
    }

    [HttpPatch("{id:int}/withdraw")]
    public async Task<IActionResult> Withdraw(int id, CancellationToken ct)
    {
        var e = await db.Enrolments.FindAsync(new object[]{ id }, ct);
        if (e is null) return NotFound();
        if (e.Status == "Withdrawn") return Conflict(new { message = "Already withdrawn." });
        e.Status = "Withdrawn";
        await db.SaveChangesAsync(ct);
        return Ok(e);
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int? studentId, [FromQuery] string? course, CancellationToken ct)
    {
        var q = db.Enrolments.AsNoTracking().AsQueryable();
        if (studentId is not null) q = q.Where(e => e.StudentId == studentId);
        if (!string.IsNullOrWhiteSpace(course)) q = q.Where(e => e.CourseCode == course);
        var items = await q.Take(200).ToListAsync(ct);
        return Ok(items);
    }
}