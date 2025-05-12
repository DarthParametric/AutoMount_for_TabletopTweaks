using Kingmaker.Localization;
using Kingmaker.Localization.Shared;
using Kingmaker.UI.Models.Log.CombatLog_ThreadSystem;
using Kingmaker.UI.Models.Log.CombatLog_ThreadSystem.LogThreads.Common;
using Kingmaker.UI.MVVM._VM.Tooltip.Templates;
using Kingmaker.Utility;
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

		public static void LogDebug(string message)
		{
#if DEBUG
			try
			{
				Main.Logger.Log($"DEBUG: {message}");
			}
			catch (Exception e)
			{
				Main.Logger.Log($"Caught exception in debug log:\n{e}");
			}
#endif
		}
	}

	internal class Utilities
	{
		private static readonly List<LocalString> Strings = [];
		internal static LocalizedString EmptyString = CreateString("", "");

		internal static LocalizedString CreateString(string key, string value)
		{
			var localizedString = new LocalizedString() { m_Key = key };
			LocalizationManager.CurrentPack.PutString(key, value);
			return localizedString;
		}

		internal static LocalizedString CreateStringAll(string key, string enGB, string deDE = null, string esES = null, string frFR = null, string itIT = null, string plPL = null, string ptBR = null, string ruRU = null, string zhCN = null)
		{
			var localString = new LocalString(key, enGB, deDE, esES, frFR, itIT, plPL, ptBR, ruRU, zhCN);
			Strings.Add(localString);
			if (LocalizationManager.Initialized)
			{
				localString.Register();
			}
			return localString.LocalizedString;
		}

		private class LocalString
		{
			public readonly LocalizedString LocalizedString;
			private readonly string enGB;
			private readonly string deDE;
			private readonly string esES;
			private readonly string frFR;
			private readonly string itIT;
			private readonly string plPL;
			private readonly string ptBR;
			private readonly string ruRU;
			private readonly string zhCN;
			const string NullString = "<null>";

			public LocalString(string key, string enGB, string deDE, string esES, string frFR, string itIT, string plPL, string ptBR, string ruRU, string zhCN)
			{
				LocalizedString = new LocalizedString() { m_Key = key };

				this.enGB = enGB;
				this.deDE = deDE;
				this.esES = esES;
				this.frFR = frFR;
				this.itIT = itIT;
				this.plPL = plPL;
				this.ptBR = ptBR;
				this.ruRU = ruRU;
				this.zhCN = zhCN;
			}

			public void Register()
			{
				string localized;

				if (LocalizationManager.CurrentPack.Locale == Locale.enGB)
				{
					localized = enGB;
					goto putString;
				}

				localized = (LocalizationManager.CurrentPack.Locale) switch
				{
					Locale.deDE => deDE,
					Locale.esES => esES,
					Locale.frFR => frFR,
					Locale.itIT => itIT,
					//Locale.plPL => plPL, // Owlcat removed Polish (temporarily?) in Update 2.7.0W (March 2025) - https://steamdb.info/patchnotes/17677240/
					Locale.ptBR => ptBR,
					Locale.ruRU => ruRU,
					Locale.zhCN => zhCN,
					_ => ""
				};

				if (localized.IsNullOrEmpty() || localized == NullString)
					localized = enGB;

				; putString:
				LocalizationManager.CurrentPack.PutString(LocalizedString.m_Key, localized);
			}
		}

		internal static Sprite CreateSprite(string embeddedImage)
		{
			var assembly = Assembly.GetExecutingAssembly();
			using var stream = assembly.GetManifestResourceStream(embeddedImage);
			byte[] bytes = new byte[stream.Length];
			stream.Read(bytes, 0, bytes.Length);
			var texture = new Texture2D(128, 128, TextureFormat.RGBA32, false);
			_ = texture.LoadImage(bytes);
			texture.name = embeddedImage + ".texture";
			// The default value for PixelsPerUnit is 1, meaning the sprite's preferred size becomes 100 times larger. So must be set to 100% manually.
			var sprite = Sprite.Create(texture, new(0, 0, texture.width, texture.height), Vector2.zero, 100);
			sprite.name = embeddedImage + ".sprite";
			return sprite;
		}
	}
}
