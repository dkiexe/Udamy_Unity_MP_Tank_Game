namespace BasicFleetServer.Utils
{
    public class AsyncEventSystem
    {
        public delegate Task AsyncEventHandler<TEventArgs>(object sender, TEventArgs e);

        public static async Task InvokeEventAsync<T>(AsyncEventHandler<T> handler, object sender, T args)
        {
            if (handler == null) return;

            // Retrieve all subscribers
            var delegates = handler.GetInvocationList();

            // Iterate and await each one individually
            foreach (var del in delegates)
            {
                var asyncMethod = (AsyncEventHandler<T>)del;
                await asyncMethod(sender, args).ContinueWith // Async Exception Swallowing Prevention.
                (
                    (t, _) =>
                    {
                        if (t.IsFaulted)
                        {
                            Console.WriteLine($"Error handling async Event: {t.Exception}");
                        }
                    },
                    TaskContinuationOptions.OnlyOnFaulted
                );
            }
        }
    }
}
