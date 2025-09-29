using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BookingService.Data;
using BookingService.Models;
using BookingService.Services;

namespace BookingService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BookingsController(AppDbContext db, PatientClient patientClient, ILogger<BookingsController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Booking dto, CancellationToken ct)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        var exists = await patientClient.PatientExistsAsync(dto.PatientId, ct);
        if (!exists) return NotFound(new { message = "Patient not found in PatientService." });

        db.Bookings.Add(dto);
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Created booking {Id} for patient {Pid}", dto.BookingId, dto.PatientId);
        return CreatedAtAction(nameof(GetById), new { id = dto.BookingId }, dto);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var b = await db.Bookings.FindAsync(new object[]{ id }, ct);
        return b is null ? NotFound() : Ok(b);
    }

    [HttpPatch("{id:int}/reschedule")]
    public async Task<IActionResult> Reschedule(int id, [FromBody] DateTimeOffset newDateTime, CancellationToken ct)
    {
        var b = await db.Bookings.FindAsync(new object[]{ id }, ct);
        if (b is null) return NotFound();
        if (b.Status == "Cancelled") return BadRequest(new { message = "Cannot reschedule a cancelled booking." });
        b.TestDateTime = newDateTime;
        await db.SaveChangesAsync(ct);
        return Ok(b);
    }

    [HttpPatch("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id, CancellationToken ct)
    {
        var b = await db.Bookings.FindAsync(new object[]{ id }, ct);
        if (b is null) return NotFound();
        if (b.Status == "Cancelled") return Conflict(new { message = "Already cancelled." });
        b.Status = "Cancelled";
        await db.SaveChangesAsync(ct);
        return Ok(b);
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to, CancellationToken ct)
    {
        var q = db.Bookings.AsNoTracking().AsQueryable();
        if (from is not null) q = q.Where(b => b.TestDateTime >= from);
        if (to is not null) q = q.Where(b => b.TestDateTime <= to);
        var items = await q.OrderBy(b => b.TestDateTime).Take(200).ToListAsync(ct);
        return Ok(items);
    }
}