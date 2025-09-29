# Microservices Demo (No Docker)

This folder runs **3 .NET 8 Web APIs** locally using your installed PostgreSQL.

### Prereqs
- .NET 8 SDK
- PostgreSQL running on localhost:5432 with user `postgres` / password `postgres`

### 1) Create databases (once)
Create these DBs in Postgres: `patients_db`, `bookings_db`, `enrolments_db`.

### 2) Apply EF migrations & run
Open three terminals in VS Code:

Terminal 1 (PatientService)
```
cd PatientService
dotnet tool install --global dotnet-ef
dotnet ef migrations add Init
dotnet ef database update
dotnet run
```
Browse: http://localhost:5001/swagger

Terminal 2 (BookingService)
```
cd BookingService
dotnet ef migrations add Init
dotnet ef database update
dotnet run
```
Browse: http://localhost:5002/swagger

Terminal 3 (EnrolmentService)
```
cd EnrolmentService
dotnet ef migrations add Init
dotnet ef database update
dotnet run
```
Browse: http://localhost:5003/swagger

### 3) Quick Postman/Swagger Test
1. POST Patient (PatientService) → copy `patientId`
2. POST Booking (BookingService) with that `patientId`
3. PATCH Reschedule / Cancel
4. POST Enrolment (EnrolmentService) with `studentId` (can reuse patientId)

### Notes
- Ports are fixed via `launchSettings.json` to avoid HTTPS cert prompts.
- If your Postgres password/user differ, edit each `appsettings.json`.
