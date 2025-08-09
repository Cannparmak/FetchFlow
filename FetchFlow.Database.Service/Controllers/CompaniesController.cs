using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FetchFlow.Database.Service.Context;
using FetchFlow.Database.Service.Entities;

namespace FetchFlow.Database.Service.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class CompaniesController : ControllerBase
    {
        private readonly ApiContext _context;
        private readonly ILogger<CompaniesController> _logger;

        public CompaniesController(ApiContext context, ILogger<CompaniesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetCompanies()
        {
            try
            {
                var companies = await _context.Companies.ToListAsync();
                return Ok(companies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting companies");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCompany(int id)
        {
            try
            {
                var company = await _context.Companies.FindAsync(id);
                if (company == null)
                {
                    return NotFound();
                }
                return Ok(company);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting company with id {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateCompany([FromBody] Company company)
        {
            try
            {
                Company? existing = null;

                if (!string.IsNullOrWhiteSpace(company.MkkMemberOid))
                {
                    existing = await _context.Companies
                        .FirstOrDefaultAsync(c => c.MkkMemberOid == company.MkkMemberOid);
                }

                if (existing == null && !string.IsNullOrWhiteSpace(company.StockCode))
                {
                    existing = await _context.Companies
                        .FirstOrDefaultAsync(c => c.StockCode == company.StockCode);
                }

                if (existing != null)
                {
                    // Var olan kaydı güncelle
                    existing.MkkMemberOid = company.MkkMemberOid;
                    existing.KapMemberTitle = company.KapMemberTitle;
                    existing.RelatedMemberTitle = company.RelatedMemberTitle;
                    existing.StockCode = company.StockCode;
                    existing.CityName = company.CityName;
                    existing.RelatedMemberOid = company.RelatedMemberOid;
                    existing.KapMemberType = company.KapMemberType;

                    await _context.SaveChangesAsync();
                    return Ok(existing);
                }
                else
                {
                    // Yeni kayıt ekle
                    _context.Companies.Add(company);
                    await _context.SaveChangesAsync();
                    return CreatedAtAction(nameof(GetCompany), new { id = company.Id }, company);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating company");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("batch")]
        public async Task<IActionResult> CreateCompaniesBatch([FromBody] List<Company> companies)
        {
            try
            {
                if (companies == null || companies.Count == 0)
                {
                    return Ok("0 companies processed");
                }

                var insertCount = 0;
                var updateCount = 0;

                // 1) MKK OID ile eşleşenleri topla ve mevcutları çek
                var oids = companies
                    .Where(c => !string.IsNullOrWhiteSpace(c.MkkMemberOid))
                    .Select(c => c.MkkMemberOid)
                    .Distinct()
                    .ToList();

                var existingByOid = oids.Count > 0
                    ? await _context.Companies
                        .Where(c => oids.Contains(c.MkkMemberOid))
                        .ToDictionaryAsync(c => c.MkkMemberOid)
                    : new Dictionary<string, Company>();

                // 2) StockCode ile eşleşenleri topla ve mevcutları çek (OID'i olmayanlar için)
                var stockCodes = companies
                    .Where(c => string.IsNullOrWhiteSpace(c.MkkMemberOid) && !string.IsNullOrWhiteSpace(c.StockCode))
                    .Select(c => c.StockCode!)
                    .Distinct()
                    .ToList();

                var existingByStock = stockCodes.Count > 0
                    ? await _context.Companies
                        .Where(c => stockCodes.Contains(c.StockCode!))
                        .ToDictionaryAsync(c => c.StockCode!)
                    : new Dictionary<string, Company>();

                foreach (var item in companies)
                {
                    Company? existing = null;

                    if (!string.IsNullOrWhiteSpace(item.MkkMemberOid))
                    {
                        existing = existingByOid.GetValueOrDefault(item.MkkMemberOid);
                    }

                    if (existing == null && !string.IsNullOrWhiteSpace(item.StockCode))
                    {
                        existing = existingByStock.GetValueOrDefault(item.StockCode!);
                    }

                    if (existing != null)
                    {
                        existing.MkkMemberOid = item.MkkMemberOid;
                        existing.KapMemberTitle = item.KapMemberTitle;
                        existing.RelatedMemberTitle = item.RelatedMemberTitle;
                        existing.StockCode = item.StockCode;
                        existing.CityName = item.CityName;
                        existing.RelatedMemberOid = item.RelatedMemberOid;
                        existing.KapMemberType = item.KapMemberType;
                        updateCount++;
                    }
                    else
                    {
                        _context.Companies.Add(item);
                        insertCount++;
                    }
                }

                var saved = await _context.SaveChangesAsync();
                return Ok(new { Inserted = insertCount, Updated = updateCount, SavedChanges = saved });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating companies batch");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut]
        public async Task<IActionResult> UpdateCompany([FromBody] Company company)
        {
            try
            {
                _context.Companies.Update(company);
                await _context.SaveChangesAsync();
                return Ok(company);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating company");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCompany(int id)
        {
            try
            {
                var company = await _context.Companies.FindAsync(id);
                if (company == null)
                {
                    return NotFound();
                }

                _context.Companies.Remove(company);
                await _context.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting company with id {Id}", id);
                return StatusCode(500, "Internal server error");
            }
        }


        [HttpGet("count")]
        public async Task<IActionResult> GetCompanyCount()
        {
            try
            {
                var companyCount = await _context.Companies.CountAsync();
                var lastCompany = await _context.Companies
                    .OrderByDescending(c => c.Id)
                    .FirstOrDefaultAsync();

                return Ok(new
                {
                    TotalCompanies = companyCount,
                    LastRecordId = lastCompany?.Id ?? 0,
                    LastSyncTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    Message = $"Veritabanında {companyCount} şirket kayıtlı"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting company count");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}

