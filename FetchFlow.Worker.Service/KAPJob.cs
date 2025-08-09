using HtmlAgilityPack;
using System.Text.RegularExpressions;
using System.Text.Json;
using FetchFlow.Database.Service.Entities;

namespace FetchFlow.Worker.Service.Jobs
{
    public class KAPJob
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<KAPJob> _logger;
        private readonly IConfiguration _configuration;

        public KAPJob(
            IHttpClientFactory httpClientFactory,
            ILogger<KAPJob> logger,
            IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task SyncKapCompaniesAsync()
        {
            _logger.LogInformation("KAP şirket senkronizasyonu başlatıldı.");

            try
            {
                var companies = await FetchFromKapAsync();
                
                if (companies?.Any() == true)
                {
                    _logger.LogInformation($"{companies.Count} KAP şirketi çekildi.");
                    await SaveCompaniesToDatabaseAsync(companies);
                    _logger.LogInformation($"{companies.Count} şirket başarıyla senkronize edildi.");
                }
                else
                {
                    _logger.LogError("KAP sitesine erişilemedi.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "KAP şirket senkronizasyonu sırasında hata oluştu.");
                throw;
            }
        }

        private async Task<List<Company>> FetchFromKapAsync()
        {
            _logger.LogInformation("KAP sitesinden veri çekiliyor...");

            try
            {
                var handler = new HttpClientHandler()
                {
                    AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
                };
                
                using var httpClient = new HttpClient(handler);
                
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");
                httpClient.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");

                var response = await httpClient.GetAsync("https://www.kap.org.tr/tr/bist-sirketler");
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError($"KAP sitesi erişim hatası: {response.StatusCode}");
                    return new List<Company>();
                }

                var html = await response.Content.ReadAsStringAsync();
                var companies = await ExtractCompaniesFromJavaScript(html);
                
                if (companies.Any())
                {
                    _logger.LogInformation($"KAP'tan {companies.Count} şirket verisi çekildi.");
                    return companies;
                }
                else
                {
                    _logger.LogWarning("KAP'tan şirket verisi çıkarılamadı");
                    return new List<Company>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "KAP veri çekme hatası");
                return new List<Company>();
            }
        }

        private async Task<List<Company>> ExtractCompaniesFromJavaScript(string html)
        {
            var companies = new List<Company>();

            try
            {
                var scriptMatches = Regex.Matches(html, @"<script[^>]*>(.*?)</script>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                
                foreach (Match scriptMatch in scriptMatches)
                {
                    var scriptContent = scriptMatch.Groups[1].Value;
                    
                    if (scriptContent.Contains("self.__next_f.push") && scriptContent.Contains("mkkMemberOid"))
                    {
                        var jsonMatches = Regex.Matches(scriptContent, @"\{[^}]*\\""mkkMemberOid\\"":\\""([^\\""]+)\\""[^}]*\\""kapMemberTitle\\"":\\""([^\\""]+)\\""[^}]*\\""relatedMemberTitle\\"":(\\""([^\\""]*?)\\""|(null))[^}]*\\""stockCode\\"":\\""([^\\""]*?)\\""[^}]*\\""cityName\\"":\\""([^\\""]+)\\""[^}]*\\""relatedMemberOid\\"":(\\""([^\\""]*?)\\""|(null))[^}]*\\""kapMemberType\\"":\\""([^\\""]*?)\\""[^}]*\}", RegexOptions.IgnoreCase | RegexOptions.Singleline);
                        
                        foreach (Match match in jsonMatches)
                        {
                            try
                            {
                                var company = new Company
                                {
                                    MkkMemberOid = CleanText(match.Groups[1].Value),
                                    KapMemberTitle = CleanText(match.Groups[2].Value),
                                    RelatedMemberTitle = match.Groups[4].Success ? CleanText(match.Groups[4].Value) : null,
                                    StockCode = CleanText(match.Groups[6].Value),
                                    CityName = CleanText(match.Groups[7].Value),
                                    RelatedMemberOid = match.Groups[9].Success ? CleanText(match.Groups[9].Value) : null,
                                    KapMemberType = CleanText(match.Groups[11].Value)
                                };

                                if (!string.IsNullOrWhiteSpace(company.KapMemberTitle))
                                {
                                    companies.Add(company);
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning($"Şirket parse hatası: {ex.Message}");
                            }
                        }

                        if (companies.Any())
                        {
                            break;
                        }
                    }
                }

                if (!companies.Any())
                {
                    var jsonArrayMatches = Regex.Matches(html, @"\{[^}]*""mkkMemberOid""[^}]*\}", RegexOptions.IgnoreCase);
                    
                    foreach (Match jsonMatch in jsonArrayMatches)
                    {
                        try
                        {
                            var jsonStr = jsonMatch.Value;
                            
                            var mkkOid = ExtractJsonValue(jsonStr, "mkkMemberOid");
                            var kapTitle = ExtractJsonValue(jsonStr, "kapMemberTitle");
                            var relatedTitle = ExtractJsonValue(jsonStr, "relatedMemberTitle");
                            var stockCode = ExtractJsonValue(jsonStr, "stockCode");
                            var cityName = ExtractJsonValue(jsonStr, "cityName");
                            var relatedOid = ExtractJsonValue(jsonStr, "relatedMemberOid");
                            var memberType = ExtractJsonValue(jsonStr, "kapMemberType");

                            if (!string.IsNullOrWhiteSpace(stockCode) && !string.IsNullOrWhiteSpace(kapTitle))
                            {
                                var company = new Company
                                {
                                    MkkMemberOid = CleanText(mkkOid),
                                    KapMemberTitle = CleanText(kapTitle),
                                    RelatedMemberTitle = CleanText(relatedTitle),
                                    StockCode = CleanText(stockCode),
                                    CityName = CleanText(cityName),
                                    RelatedMemberOid = CleanText(relatedOid),
                                    KapMemberType = CleanText(memberType)
                                };

                                companies.Add(company);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning($"JSON parse hatası: {ex.Message}");
                        }
                    }
                }

                _logger.LogInformation($"Toplam {companies.Count} şirket çıkarıldı.");
                
                await Task.Delay(100);
                
                return companies.GroupBy(c => c.StockCode).Select(g => g.First()).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "JavaScript parsing hatası");
                return new List<Company>();
            }
        }

        private string ExtractJsonValue(string jsonStr, string key)
        {
            var pattern = $@"""{key}""\s*:\s*""([^""]*)""";
            var match = Regex.Match(jsonStr, pattern, RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value : "";
        }

        private string CleanText(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            text = text.Replace("\\/", "/")
                      .Replace("\\\"", "\"")
                      .Replace("\\\\", "\\");
                      
            text = text.Replace("\\u0130", "İ")
                      .Replace("\\u015E", "Ş")
                      .Replace("\\u011E", "Ğ")
                      .Replace("\\u00C7", "Ç")
                      .Replace("\\u00DC", "Ü")
                      .Replace("\\u00D6", "Ö")
                      .Replace("\\u0131", "ı")
                      .Replace("\\u015F", "ş")
                      .Replace("\\u011F", "ğ")
                      .Replace("\\u00E7", "ç")
                      .Replace("\\u00FC", "ü")
                      .Replace("\\u00F6", "ö");

            text = Regex.Replace(text, @"[/\\]", " ", RegexOptions.Compiled);
            text = Regex.Replace(text, @"\s+", " ", RegexOptions.Compiled);
            
            return text.Trim();
        }

        private async Task SaveCompaniesToDatabaseAsync(List<Company> companies)
        {
            try
                {
                var httpClient = _httpClientFactory.CreateClient();
                var databaseServiceUrl = _configuration["Services:DatabaseService"];
                
                // Delete all existing companies first
                await httpClient.DeleteAsync($"{databaseServiceUrl}/companies/all");
                
                // Add new companies in batches
                var batchSize = 50;
                for (int i = 0; i < companies.Count; i += batchSize)
                {
                    var batch = companies.Skip(i).Take(batchSize).ToList();
                    var json = JsonSerializer.Serialize(batch);
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                    
                    var response = await httpClient.PostAsync($"{databaseServiceUrl}/companies/batch", content);
                    
                    if (!response.IsSuccessStatusCode)
                        {
                        _logger.LogError($"Batch {i / batchSize + 1} kaydedilirken hata oluştu: {response.StatusCode}");
                        }
                    else
                    {
                        _logger.LogInformation($"Batch {i / batchSize + 1} başarıyla kaydedildi ({batch.Count} şirket)");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Şirketler Database Service'e kaydedilirken hata oluştu.");
                throw;
            }
        }

        private string ExtractMkkMemberOid(HtmlNode? cell)
        {
            if (cell == null) return "";
            
            var links = cell.SelectNodes(".//a[@href]");
            if (links != null)
            {
                foreach (var link in links)
        {
                    var href = link.GetAttributeValue("href", "");
                    var match = Regex.Match(href, @"memberOid=([^&]+)");
                    if (match.Success)
                    {
                        return match.Groups[1].Value;
                    }
                }
            }
            return "";
        }

        private string ExtractRelatedMemberOid(HtmlNode? cell)
        {
            if (cell == null) return "";
            
            var links = cell.SelectNodes(".//a[@href]");
            if (links != null)
            {
                foreach (var link in links)
                {
                    var href = link.GetAttributeValue("href", "");
                    var match = Regex.Match(href, @"memberOid=([^&]+)");
                    if (match.Success)
                    {
                        return match.Groups[1].Value;
                    }
                }
            }
            return "";
        }

        private string DetermineKapMemberType(IList<HtmlNode> cells)
        {
            if (cells.Count >= 2 && !string.IsNullOrEmpty(cells[1]?.InnerText?.Trim()))
            {
                return "Bağlı Üye";
            }
            return "Ana Üye";
        }
    }

    // KAP JSON verisi için yardımcı sınıf
    public class KAPCompanyData
    {
        public string? mkkMemberOid { get; set; }
        public string? kapMemberTitle { get; set; }
        public string? relatedMemberTitle { get; set; }
        public string? stockCode { get; set; }
        public string? cityName { get; set; }
        public string? relatedMemberOid { get; set; }
        public string? kapMemberType { get; set; }
    }
} 