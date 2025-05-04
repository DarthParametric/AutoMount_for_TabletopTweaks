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
				var CurrentArea = Game.Instance.CurrentlyLoadedArea;
				var AreaGUID = CurrentArea.AssetGuid.ToString();
				var AruDream = "6147558e4a97a724081ea120a9700be2";

				LogDebug($"\nCurrent chapter is {Game.Instance.Player.Chapter}\nCurrent area is {CurrentArea}\nAssetGuid is {AreaGUID}\nAreaName is {CurrentArea.AreaName}\n");

				if (AreaGUID != AruDream)
				{
					ForceMount();
				}
            }
        }

        public void OnAreaBeginUnloading()
        { }
    }
}
