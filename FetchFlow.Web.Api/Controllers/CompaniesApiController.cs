using Microsoft.AspNetCore.Mvc;

namespace FetchFlow.Web.Api.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class CompaniesApiController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public CompaniesApiController(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<IActionResult> CompanyList()
        {
            try
            {
                var databaseServiceUrl = _configuration["Services:DatabaseService"];
                var response = await _httpClient.GetAsync($"{databaseServiceUrl}/companies");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return Ok(content);
                }

                return StatusCode((int)response.StatusCode, "Database service'e erişilemedi");
            }
            catch (Exception ex)
            {
                return BadRequest($"Hata: {ex.Message}");
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateCompany([FromBody] object company)
        {
            try
            {
                var databaseServiceUrl = _configuration["Services:DatabaseService"];
                var json = System.Text.Json.JsonSerializer.Serialize(company);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{databaseServiceUrl}/companies", content);

                if (response.IsSuccessStatusCode)
                {
                    return Ok("Şirket Eklendi");
                }

                return StatusCode((int)response.StatusCode, "Şirket eklenirken hata oluştu");
            }
            catch (Exception ex)
            {
                return BadRequest($"Hata: {ex.Message}");
            }
        }

        [HttpPost("batch")]
        public async Task<IActionResult> CreateCompaniesBatch([FromBody] object companies)
        {
            try
            {
                var databaseServiceUrl = _configuration["Services:DatabaseService"];
                var json = System.Text.Json.JsonSerializer.Serialize(companies);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{databaseServiceUrl}/companies/batch", content);

                if (response.IsSuccessStatusCode)
                {
                    var resp = await response.Content.ReadAsStringAsync();
                    return Ok(resp);
                }

                return StatusCode((int)response.StatusCode, "Toplu şirket işleminde hata oluştu");
            }
            catch (Exception ex)
            {
                return BadRequest($"Hata: {ex.Message}");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCompany(int id)
        {
            try
            {
                var databaseServiceUrl = _configuration["Services:DatabaseService"];
                var response = await _httpClient.DeleteAsync($"{databaseServiceUrl}/companies/{id}");

                if (response.IsSuccessStatusCode)
                {
                    return Ok("Şirket Silindi");
                }

                return StatusCode((int)response.StatusCode, "Şirket silinirken hata oluştu");
            }
            catch (Exception ex)
            {
                return BadRequest($"Hata: {ex.Message}");
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCompany(int id)
        {
            try
            {
                var databaseServiceUrl = _configuration["Services:DatabaseService"];
                var response = await _httpClient.GetAsync($"{databaseServiceUrl}/companies/{id}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return Ok(content);
                }

                return StatusCode((int)response.StatusCode, "Şirket bulunamadı");
            }
            catch (Exception ex)
            {
                return BadRequest($"Hata: {ex.Message}");
            }
        }

        [HttpPut]
        public async Task<IActionResult> UpdateCompany([FromBody] object company)
        {
            try
            {
                var databaseServiceUrl = _configuration["Services:DatabaseService"];
                var json = System.Text.Json.JsonSerializer.Serialize(company);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PutAsync($"{databaseServiceUrl}/companies", content);

                if (response.IsSuccessStatusCode)
                {
                    return Ok("Şirket Güncellendi");
                }

                return StatusCode((int)response.StatusCode, "Şirket güncellenirken hata oluştu");
            }
            catch (Exception ex)
            {
                return BadRequest($"Hata: {ex.Message}");
            }
        }

        [HttpPost("sync-companies")]
        public async Task<IActionResult> TriggerCompanySync()
        {
            try
            {
                var workerServiceUrl = _configuration["Services:WorkerService"];
                var response = await _httpClient.PostAsync($"{workerServiceUrl}/api/jobs/sync-kap-companies", null);

                if (response.IsSuccessStatusCode)
                {
                    return Ok("KAP şirket senkronizasyonu başlatıldı. İşlem background'da çalışıyor.");
                }

                return StatusCode((int)response.StatusCode, "Job tetiklenirken hata oluştu");
            }
            catch (Exception ex)
            {
                return BadRequest($"Senkronizasyon başlatılamadı: {ex.Message}");
            }
        }

        [HttpGet("count")]
        public async Task<IActionResult> GetCompanyCount()
        {
            try
            {
                var databaseServiceUrl = _configuration["Services:DatabaseService"];
                var response = await _httpClient.GetAsync($"{databaseServiceUrl}/companies/count");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    return Ok(content);
                }

                return StatusCode((int)response.StatusCode, "Şirket sayısı alınırken hata oluştu");
            }
            catch (Exception ex)
            {
                return BadRequest($"Hata: {ex.Message}");
            }
        }
    }
}

