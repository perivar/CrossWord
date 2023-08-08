using CrossWord.Models;
using CrossWord.Scraper.MySQLDbService;

namespace CrossWord.API
{
    public class TimedHostedService : BackgroundService
    {
        private readonly ILogger<TimedHostedService> _logger;
        private int _executionCount;

        public IServiceProvider Services { get; }

        public TimedHostedService(IServiceProvider services, ILogger<TimedHostedService> logger)
        {
            Services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Timed Hosted Service running.");

            // When the timer should have no due-time, then do the work once now.
            await DoWork();

            using PeriodicTimer timer = new(TimeSpan.FromSeconds(1));

            try
            {
                while (await timer.WaitForNextTickAsync(stoppingToken))
                {
                    await DoWork();
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Timed Hosted Service is stopping.");
            }
        }

        private async Task<int> DoWork()
        {
            int count = Interlocked.Increment(ref _executionCount);

            _logger.LogInformation("Timed Hosted Service is working. Count: {Count}", count);

            using var scope = Services.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var db = scopedServices.GetRequiredService<WordHintDbContext>();

            try
            {
                var model = await CrossBoardCreator.GetCrossWordModelFromUrlAsync("http-random");
                var board = model.ToCrossBoard();

                // add in database
                var newTemplate = new Scraper.MySQLDbService.Models.CrosswordTemplate()
                {
                    Rows = model.Size.Rows,
                    Cols = model.Size.Cols,
                    Grid = model.Grid
                };

                db.CrosswordTemplates.Add(newTemplate);
                return await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    $"An error occurred writing to the database. Error: {ex.Message}");
            }

            return -1;
        }
    }
}