using FleetServerUtils;
using BasicFleetServer.Operation;

namespace BasicFleetServer
{
    public class FleetServer : IAsyncDisposable
    {
        private FleetApplicationData fleetAppdata;

        private GS_Manager gsManager;

        private FleetServerSocket UserListnerSocket;

        private FleetServerSocket GameServerListnerSocket;

        public FleetServer(FleetApplicationData fleetAppdata)
        {
            this.fleetAppdata = fleetAppdata;

            gsManager = new GS_Manager(fleetAppdata);

            UserListnerSocket = new FleetServerSocket(fleetAppdata.UserCommPort, SocketType.ForUsers);

            GameServerListnerSocket = new FleetServerSocket(FleetApplicationData.GameServerCommPort, SocketType.ForGameServers);
        }

        public async Task StartOperationLoop()
        {
            _ = UserListnerSocket.StartListening(); // start listening fire & forget.
            _ = GameServerListnerSocket.StartListening(); // start listening fire & forget.

            while (true)
            {
                Console.WriteLine("[*] Type a command below : ");
                string? input = await Task.Run(() => Console.ReadLine());

                if (input != null && input.ToLower() == "exit")
                {
                    Console.WriteLine("Shutting down all game servers...");
                    await DisposeAsync();
                    Console.WriteLine("All game servers have been shut down. Exiting application.");
                    break;
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            Task exitTask1 = gsManager.DisposeAsync().AsTask();
            Task exitTask2 = UserListnerSocket.DisposeAsync().AsTask();
            Task exitTask3 = GameServerListnerSocket.DisposeAsync().AsTask();
            await Task.WhenAll([exitTask1, exitTask2, exitTask3]);
        }
    }

    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.Clear();

            FleetApplicationData fleetAppdata = new FleetApplicationData();
            FleetServer fleetServer = new FleetServer(fleetAppdata);

            await fleetServer.StartOperationLoop();
        }
    }
}
