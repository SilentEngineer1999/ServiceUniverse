# HealthService.AllInOne (merged backend)
Single process backend with:
- Auth: /api/auth/signup, /api/auth/login, /api/auth/refresh, /api/auth/me
- Data: /api/doctors, /api/appointments (GET), /api/appointments (POST protected)

Run:
```bash
cd backend/HealthService.AllInOne
dotnet restore
dotnet run   # http://localhost:5199
```
