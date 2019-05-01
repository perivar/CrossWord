using System;
using System.Threading;
using System.Threading.Tasks;
using CrossWord.Models;
using CrossWord.Scraper.MySQLDbService;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CrossWord.API
{
    internal class TimedHostedService : IHostedService, IDisposable
    {
        private readonly ILogger _logger;
        private Timer _timer;

        public IServiceProvider Services { get; }

        public TimedHostedService(IServiceProvider services, ILogger<TimedHostedService> logger)
        {
            Services = services;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Timed Background Service is starting.");

            _timer = new Timer(DoWork, null, TimeSpan.Zero,
                TimeSpan.FromSeconds(10));

            return Task.CompletedTask;
        }

        private void DoWork(object state)
        {
            _logger.LogInformation("Timed Background Service is working.");

            using (var scope = Services.CreateScope())
            {
                var scopedServices = scope.ServiceProvider;
                var db = scopedServices.GetRequiredService<WordHintDbContext>();

                try
                {
                    var model = CrossBoardCreator.GetCrossWordModelFromUrl("http-random");
                    var board = model.ToCrossBoard();

                    // add in database
                    var newTemplate = new CrossWord.Scraper.MySQLDbService.Models.CrosswordTemplate()
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
                    _logger.LogError(ex,
                        "An error occurred writing to the " +
                        $"database. Error: {ex.Message}");
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Timed Background Service is stopping.");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}