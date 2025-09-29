using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PatientService.Data;
using PatientService.Models;

namespace PatientService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PatientsController(AppDbContext db, ILogger<PatientsController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Patient dto)
    {
        if (!ModelState.IsValid) return ValidationProblem(ModelState);

        if (!string.IsNullOrWhiteSpace(dto.CitizenId))
        {
            var exists = await db.Patients.AnyAsync(p => p.CitizenId == dto.CitizenId);
            if (exists) return Conflict(new { message = "CitizenId already exists." });
        }

        db.Patients.Add(dto);
        await db.SaveChangesAsync();
        logger.LogInformation("Created patient {Id}", dto.PatientId);
        return CreatedAtAction(nameof(GetById), new { id = dto.PatientId }, dto);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById([FromRoute] int id)
    {
        var patient = await db.Patients.FindAsync(id);
        return patient is null ? NotFound() : Ok(patient);
    }

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string? name)
    {
        IQueryable<Patient> q = db.Patients.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(name))
        {
            q = q.Where(p => p.FullName.ToLower().Contains(name.ToLower()));
        }
        var results = await q.Take(100).ToListAsync();
        return Ok(results);
    }
}