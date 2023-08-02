namespace CrossWord.API
{
    public class QueuedHostedService : BackgroundService
    {
        private readonly ILogger logger;

        public QueuedHostedService(IBackgroundTaskQueue taskQueue,
            ILoggerFactory loggerFactory)
        {
            this.TaskQueue = taskQueue;
            this.logger = loggerFactory.CreateLogger<QueuedHostedService>();
        }

        public IBackgroundTaskQueue TaskQueue { get; }

        protected async override Task ExecuteAsync(
            CancellationToken cancellationToken)
        {
            logger.LogInformation("Queued Hosted Service is starting.");

            while (!cancellationToken.IsCancellationRequested)
            {
                var workItem = await TaskQueue.DequeueAsync(cancellationToken);

                try
                {
                    await workItem(cancellationToken);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex,
                       $"Error occurred executing {nameof(workItem)}.");
                }
            }

            logger.LogInformation("Queued Hosted Service is stopping.");
        }
    }
}