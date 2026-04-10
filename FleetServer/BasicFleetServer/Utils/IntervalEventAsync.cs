namespace BasicFleetServer.Utils
{
    public class IntervalEventAsync
    {
        /// This class calls an event at a set interval and has a non blocking clock, the event is guaranteed to not be called concurrently, 
        /// if the event takes longer than the interval to execute, it will wait until the event finishes before starting the next interval.
        /// 

        public event AsyncEventSystem.AsyncEventHandler? IntervalElapsedEvent;

        private CancellationTokenSource? _cancellationTokenSource;

        protected virtual int Interval => 0;

        private protected IntervalEventAsync()
        {

        }

        public async Task StartIntervalLoop(CancellationTokenSource cancellationTokenSource)
        {
            _cancellationTokenSource = cancellationTokenSource;

            while (!cancellationTokenSource.IsCancellationRequested)
            {
                await Task.Delay(Interval, cancellationTokenSource.Token);

                await AsyncEventSystem.InvokeEventAsync(IntervalElapsedEvent!, this);
            }
        }

        public void StopIntervalLoop()
        {
            _cancellationTokenSource?.Cancel();
        }
    }
}
