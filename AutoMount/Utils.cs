using Kingmaker.UI.Models.Log.CombatLog_ThreadSystem.LogThreads.Common;
using Kingmaker.UI.Models.Log.CombatLog_ThreadSystem;
using Kingmaker.UI.MVVM._VM.Tooltip.Templates;
using UnityEngine;

namespace AutoMount
{
    public class Utils
    {
        public static void ConsoleLog(string sMsg1, string sMsg2, Color color, bool bTemplate)
        {
			CombatLogMessage message;

			if (bTemplate)
			{
				TooltipTemplateCombatLogMessage templateAutoMount = null;
				templateAutoMount = new TooltipTemplateCombatLogMessage(sMsg1, sMsg2);

				message = new CombatLogMessage(sMsg1, color, PrefixIcon.RightArrow, templateAutoMount, true);
			}
			else
			{
				message = new CombatLogMessage(sMsg1, color, PrefixIcon.RightArrow, null, false);
			}

			var messageLog = LogThreadService.Instance.m_Logs[LogChannelType.Common].First(x => x is MessageLogThread);

			messageLog.AddMessage(message);
        }
	}
}
