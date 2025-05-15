using Kingmaker.PubSubSystem;
using static AutoMount.Main;
using static AutoMount.Settings;
using static AutoMount.Utils;

namespace AutoMount.Events
{
    public class OnAreaLoad : IAreaHandler
    {
        public void OnAreaDidLoad()
        {
            if (IsOnAreaMountEnabled())
            {
				if (CheckEtudesForPetBlockers())
				{
					LogDebug("Events.OnAreaDidLoad: Skipping forced mount on area load due to etude check.");
					return;
				}
				
				ForceMount();
            }
        }

        public void OnAreaBeginUnloading()
        { }
    }
}
