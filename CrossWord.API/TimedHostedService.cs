using CrossWord.Models;
using CrossWord.Scraper.MySQLDbService;

namespace CrossWord.API
{
    internal class TimedHostedService : IHostedService, IDisposable
    {
        private readonly ILogger logger;
        private Timer timer;

        public IServiceProvider Services { get; }

        public TimedHostedService(IServiceProvider services, ILogger<TimedHostedService> logger)
        {
            this.Services = services;
            this.logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Timed Background Service is starting.");

            timer = new Timer(DoWork, null, TimeSpan.Zero,
                TimeSpan.FromSeconds(10));

            return Task.CompletedTask;
        }

        private void DoWork(object state)
        {
            logger.LogInformation("Timed Background Service is working.");

            using (var scope = Services.CreateScope())
            {
                var scopedServices = scope.ServiceProvider;
                var db = scopedServices.GetRequiredService<WordHintDbContext>();

                try
                {
                    var model = CrossBoardCreator.GetCrossWordModelFromUrl("http-random");
                    var board = model.ToCrossBoard();

                    // add in database
                    var newTemplate = new Scraper.MySQLDbService.Models.CrosswordTemplate()
                    {
                        Rows = model.Size.Rows,
                        Cols = model.Size.Cols,
                        Grid = model.Grid
                    };

                    db.CrosswordTemplates.Add(newTemplate);
                    db.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex,
                        $"An error occurred writing to the database. Error: {ex.Message}");
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Timed Background Service is stopping.");

            timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            timer?.Dispose();
        }
    }
}