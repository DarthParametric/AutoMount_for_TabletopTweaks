using Kingmaker;
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
				// Arueshalae's "What They Dream Of" quest causes issues during the dream sequences if the main character has a rideable mount,
				// so ensure that automounting is disabled for that area.
				// \World\Areas\Act_3_DemonsHerecy\Arueshalae_Q1_WhatTheyDreamOf\Arueshalae_Q1_WhatTheyDreamOf.jbp
				// 6147558e4a97a724081ea120a9700be2
				
				// The Ten Thousand Delights brothel in Alushinyrra apparently has the same problem.
				// \World\Areas\Act_4_MidnightIsles\AlushinyrraMediumCity\TenThousandDelights.jbp
				// 27b0684aedfca0a4ca1eb437e77abb3f
				// \World\Areas\Act_4_MidnightIsles\AlushinyrraMediumCity\TenThousandDelights_CamelliaBasement.jbp
				// e3ff076f6fc11f74ab47e54a8bf3c32b
				// \World\Areas\Act_4_MidnightIsles\AlushinyrraMediumCity\TenThousandDelights_SeparateRoom.jbp
				// 98a80290f55ae08418fb6351a979b929
				
				var CurrentArea = Game.Instance.CurrentlyLoadedArea;
				var AreaGUID = CurrentArea.AssetGuid.ToString();
				var AreaAruDream = "6147558e4a97a724081ea120a9700be2";
				var Area10Delights = "27b0684aedfca0a4ca1eb437e77abb3f";
				var Area10DelCam = "e3ff076f6fc11f74ab47e54a8bf3c32b";
				var Area10DelSep = "98a80290f55ae08418fb6351a979b929";

				LogDebug($"\nCurrent chapter is {Game.Instance.Player.Chapter}\nCurrent area is {CurrentArea}\nAssetGuid is {AreaGUID}\nAreaName is {CurrentArea.AreaName}\n");

				if (AreaGUID == AreaAruDream || AreaGUID == Area10Delights || AreaGUID == Area10DelCam || AreaGUID == Area10DelSep)
				{
					LogDebug($"Skipping forced mount on area load for known trouble location");
					
					return;
				}
				
				ForceMount();
            }
        }

        public void OnAreaBeginUnloading()
        { }
    }
}
