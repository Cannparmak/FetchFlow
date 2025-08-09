## FetchFlow

Üç servisten oluşan basit bir mikro servis mimarisi:

- **FetchFlow.Web.Api**: Dış dünyaya açık API. Database ve Worker servisleriyle konuşur.
- **FetchFlow.Database.Service**: EF Core + MySQL ile veri erişim katmanı (şirket kayıtları).
- **FetchFlow.Worker.Service**: KAP şirket verilerini toplayıp Database servisine kaydeden arka plan işleri (Hangfire).

### Gereksinimler

- .NET 8 SDK
- MySQL 8+ (yerel veya uzak)

### Proje Yapısı

```
FetchFlow/
  FetchFlow.sln
  FetchFlow.Web.Api/
    Controllers/CompaniesApiController.cs
    appsettings.json (Services: DatabaseService, WorkerService)
  FetchFlow.Database.Service/
    Context/ApiContext.cs
    Controllers/CompaniesController.cs
    Entities/Company.cs
    Migrations/*
    appsettings.json (ConnectionStrings: DefaultConnection)
  FetchFlow.Worker.Service/
    Controllers/JobsController.cs
    KAPJob.cs
    appsettings.json (Services: DatabaseService)
```

### Konfigürasyon

- `FetchFlow.Database.Service/appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=Staj;User=root;Password=12345678;"
  }
}
```

Database adını ve kullanıcı/parolayı kendi ortamınıza göre güncelleyin (ör. `Database=FetchFlow`).

- `FetchFlow.Web.Api/appsettings.json`

```json
{
  "Services": {
    "DatabaseService": "http://localhost:6001",
    "WorkerService": "http://localhost:6003"
  }
}
```

- `FetchFlow.Worker.Service/appsettings.json`

```json
{
  "Services": {
    "DatabaseService": "http://localhost:6001"
  }
}
```

Varsayılan portlar: Database 6001, Web.Api 6002, Worker 6003.

### Kurulum ve Çalıştırma

1) Bağımlılıkları indirip derleyin

```bash
dotnet restore
dotnet build -c Debug
```

2) Veritabanını oluşturun (Migration’ları uygulayın)

```bash
cd FetchFlow.Database.Service
dotnet ef database update
cd ..
```

3) Servisleri başlatın (ayrı terminallerde)

```bash
cd FetchFlow.Database.Service && dotnet run --urls "http://localhost:6001"
```

```bash
cd FetchFlow.Worker.Service && dotnet run --urls "http://localhost:6003"
```

```bash
cd FetchFlow.Web.Api && dotnet run --urls "http://localhost:6002"
```

Web.Api geliştirme modunda Swagger UI açıktır: `http://localhost:6002/swagger`

### API Özeti

- FetchFlow.Database.Service (6001)
  - `GET /companies`
  - `GET /companies/{id}`
  - `POST /companies` (body: Company)
  - `POST /companies/batch` (body: Company[])
  - `PUT /companies` (body: Company)
  - `DELETE /companies/{id}`
  - `GET /companies/count`

- FetchFlow.Worker.Service (6003)
  - `POST /api/jobs/sync-kap-companies` (KAP şirket senkronizasyonu tetikler)
  - `POST /api/jobs/sync-kap-companies/immediate`
  - `POST /api/jobs/sync-kap-companies/schedule?delayMinutes=10`
  - Hangfire Dashboard (Development): `/hangfire`
  - Zamanlanmış görev: Saat başı `sync-kap-companies-hourly`

- FetchFlow.Web.Api (6002)
  - `GET /companies` → Database Service proxy
  - `GET /companies/{id}` → Database Service proxy
  - `POST /companies` → Database Service proxy
  - `POST /companies/batch` → Database Service proxy
  - `PUT /companies` → Database Service proxy
  - `DELETE /companies/{id}` → Database Service proxy
  - `POST /companies/sync-companies` → Worker Service tetikler
  - `GET /companies/count` → Database Service proxy

### Örnek İstekler

```bash
# KAP senkronizasyonunu başlat (Worker Service)
curl -X POST http://localhost:6003/api/jobs/sync-kap-companies

# Web.Api üzerinden şirket listesi
curl http://localhost:6002/companies
```

### Geliştirme Notları

- Yeni Entity ekledikten sonra Migration oluşturma:

```bash
cd FetchFlow.Database.Service
dotnet ef migrations add <MigrationName>
dotnet ef database update
```

- Sadece isimlendirme/namespace değişiklikleri yapılmıştır; rotalar ve konfig anahtarları korunmuştur.

