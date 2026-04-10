using BasicFleetServer.Utils;

namespace BasicFleetServer.Operation
{
    public class LooseMatchMakingTimer : IntervalEventAsync
    {
        protected override int Interval => 1000 * 60; // 1 Minute

        private static readonly Lazy<LooseMatchMakingTimer> _instance =
        new(() => new LooseMatchMakingTimer());

        public static LooseMatchMakingTimer Instance => _instance.Value;

        private LooseMatchMakingTimer() : base()
        {
            
        }
    }
}
