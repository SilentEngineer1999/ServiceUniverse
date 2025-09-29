namespace BookingService.Services;

public class PatientClient(HttpClient http)
{
    public async Task<bool> PatientExistsAsync(int id, CancellationToken ct = default)
    {
        var resp = await http.GetAsync($"/api/patients/{id}", ct);
        return resp.IsSuccessStatusCode;
    }
}