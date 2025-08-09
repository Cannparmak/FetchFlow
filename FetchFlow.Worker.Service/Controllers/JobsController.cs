using Hangfire;
using Microsoft.AspNetCore.Mvc;
using FetchFlow.Worker.Service.Jobs;

namespace FetchFlow.Worker.Service.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class JobsController : ControllerBase
    {
        private readonly ILogger<JobsController> _logger;

        public JobsController(ILogger<JobsController> logger)
        {
            _logger = logger;
        }

        [HttpPost("sync-kap-companies")]
        public IActionResult TriggerKapSync()
        {
            try
            {
                BackgroundJob.Enqueue<KAPJob>(job => job.SyncKapCompaniesAsync());
                _logger.LogInformation("KAP sync job queued successfully");
                return Ok("KAP şirket senkronizasyonu job'ı kuyruğa eklendi");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error queuing KAP sync job");
                return StatusCode(500, "Job kuyruğa eklenirken hata oluştu");
            }
        }

        [HttpPost("sync-kap-companies/immediate")]
        public IActionResult TriggerKapSyncImmediate()
        {
            try
            {
                BackgroundJob.Enqueue<KAPJob>(job => job.SyncKapCompaniesAsync());
                _logger.LogInformation("Immediate KAP sync job queued successfully");
                return Ok("KAP şirket senkronizasyonu hemen başlatıldı");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error queuing immediate KAP sync job");
                return StatusCode(500, "Job başlatılırken hata oluştu");
            }
        }

        [HttpPost("sync-kap-companies/schedule")]
        public IActionResult ScheduleKapSync([FromQuery] int delayMinutes = 10)
        {
            try
            {
                BackgroundJob.Schedule<KAPJob>(
                    job => job.SyncKapCompaniesAsync(),
                    TimeSpan.FromMinutes(delayMinutes));
                
                _logger.LogInformation("KAP sync job scheduled for {Minutes} minutes", delayMinutes);
                return Ok($"KAP şirket senkronizasyonu {delayMinutes} dakika sonra çalışacak şekilde zamanlandı");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scheduling KAP sync job");
                return StatusCode(500, "Job zamanlanırken hata oluştu");
            }
        }

        [HttpGet("status")]
        public IActionResult GetJobStatus()
        {
            try
            {
                // Simple status check
                return Ok(new
                {
                    Status = "Worker Service Active",
                    Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    Message = "Job service çalışıyor"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting job status");
                return StatusCode(500, "Status alınırken hata oluştu");
            }
        }
    }
} 