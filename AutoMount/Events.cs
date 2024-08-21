using Kingmaker.PubSubSystem;

namespace AutoMount.Events
{
    public class OnAreaLoad : IAreaHandler
    {
        public void OnAreaDidLoad()
        {
            if (Settings.IsOnAreaMountEnabled())
            {
                Main.ForceMount();
            }
        }

        public void OnAreaBeginUnloading()
        { }
    }
}
