using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Localization;
using Kingmaker.UI;
using ModMenu.Settings;
using KeyBinding = ModMenu.Settings.KeyBinding;
using UnityEngine;
using UnityModManagerNet;
using static AutoMount.Strings.ModStrings;
using static AutoMount.Utils;

namespace AutoMount
{
	public static class Settings
	{
		private static bool Initialized = false;
		
		private static readonly string RootKey = "automount";
		public static readonly string MountOnAreaEnter = "areaentermount";
		public static readonly string RideAivu = "rideaivu";
		public static readonly string ConsoleOutput = "consoleoutput";
		public static readonly string ConsoleDebug = "consoledebug";
		public static readonly string MountHotKey = "hotkeys.mounthotkey";
		public static readonly string DismountHotKey = "hotkeys.dismounthotkey";

		private static SettingsBuilder MMSettings = SettingsBuilder.New(RootKey, GetString(GetKey("automount-title"), "AutoMount for TabletopTweaks"));
		
		public static void Init()
		{
			if (Initialized)
			{
				LogDebug("ModMenu settings already initialised");
				return;
			}

			Main.Logger.Log("Initialising ModMenu settings");

			MMSettings.SetMod(Main.modEntry);
			MMSettings.SetModDescription(ModDesc);
			MMSettings.SetModIllustration(Utilities.CreateSprite("AutoMount.Img.Racing_Snail.png"));

			MMSettings.AddToggle(
				Toggle.New(
					GetKey(MountOnAreaEnter),
					true,
					ToggleMountOnEnterDesc)
				.WithLongDescription(ToggleMountOnEnterDescLong)
			);

			MMSettings.AddToggle(
				Toggle.New(
					GetKey(RideAivu),
					true,
					ToggleRideAivuDesc)
				.WithLongDescription(ToggleRideAivuDescLong)
			);

			MMSettings.AddToggle(
				Toggle.New(
					GetKey(ConsoleOutput),
					false,
					ToggleConsoleOutputDesc)
				.WithLongDescription(ToggleConsoleOutputDescLong)
			);

			MMSettings.AddToggle(
				Toggle.New(
					GetKey(ConsoleDebug),
					false,
					ToggleConsoleDebugDesc)
				.WithLongDescription(ToggleConsoleDebugDescLong)
			);

			var Hotkeys = MMSettings.AddSubHeader(HeaderHotkeysDesc, true);

			Hotkeys.AddKeyBinding(
				KeyBinding.New(
					GetKey(MountHotKey),
					KeyboardAccess.GameModesGroup.All,
					ToggleMountHotKeyDesc)
				.SetPrimaryBinding(KeyCode.A, withCtrl: true, withShift: true)
				.WithLongDescription(ToggleMountHotKeyDescLong),
				() => Main.Mount(true)
			);

			Hotkeys.AddKeyBinding(
				KeyBinding.New(
					GetKey(DismountHotKey),
					KeyboardAccess.GameModesGroup.All,
					ToggleDismountHotKeyDesc)
				.SetPrimaryBinding(KeyCode.D, withCtrl: true, withShift: true)
				.WithLongDescription(ToggleDismountHotKeyDescLong),
				() => Main.Mount(false)
			);

			var Whitelist = MMSettings.AddSubHeader(HeaderWhitelistDesc, true);

			int slotCount = GetMPSSlots() ?? 6; // Former monkey code approach amended after harsh criticism from Kuru and ADDB.

			for (int i = 0; i < slotCount; i++)
			{
				Whitelist.AddToggle(
					Toggle.New(
						GetSlotKey(i),
						true,
						GetString($"{GetSlotPartialKey(i)}-desc", String.Format(WLSlotDesc(), i + 1)))
					.WithLongDescription(GetString($"{GetSlotPartialKey(i)}-desc-long", String.Format(WLSlotDescLong(), i + 1)))
				);
			}

			ModMenu.ModMenu.AddSettings(MMSettings);
			Initialized = true;
			Main.Logger.Log("ModMenu settings initialisation complete");
		}

		public static string GetSlotKey(int slot)
		{
			return GetKey($"slot-{slot}");
		}

		private static string GetSlotPartialKey(int slot)
		{
			return $"slot-{slot}";
		}

		internal static bool IsEnabled(string key)
		{
			return ModMenu.ModMenu.GetSettingValue<bool>(GetKey(key));
		}

		internal static bool IsSlotEnabled(int slot)
		{
			return ModMenu.ModMenu.GetSettingValue<bool>(GetSlotKey(slot));
		}

		internal static bool IsOnAreaMountEnabled()
		{
			return ModMenu.ModMenu.GetSettingValue<bool>(GetKey("areaentermount"));
		}

		internal static bool IsCombatLoggingEnabled()
		{
			return ModMenu.ModMenu.GetSettingValue<bool>(GetKey("consoleoutput"));
		}

		internal static bool IsCombatLogDebugEnabled()
		{
			return ModMenu.ModMenu.GetSettingValue<bool>(GetKey("consoledebug"));
		}

		private static string GetKey(string partialKey)
		{
			return $"{RootKey}.{partialKey}";
		}

		private static LocalizedString GetString(string partialKey, string text)
		{
			return Utilities.CreateString(GetKey(partialKey), text);
		}

		// Checks for the presence of xADDBx's "More Party Slots" mod and returns its config value if installed.
		// Code was kindly supplied by microsoftenator2022 on the Owlcat Discord server's #mod-dev-technical channel.
		public static int? GetMPSSlots()
		{
			int? mpsSlots = null;

			if (UnityModManager.FindMod("MorePartySlots") is { } morePartySlots && morePartySlots.Active)
			{
				Main.Logger.Log($"Found {morePartySlots.Info.DisplayName} v{morePartySlots.Info.Version}");

				var mpsSettingsType = morePartySlots.Assembly.GetType("MorePartySlots.Settings");

				if (mpsSettingsType is null)
				{
					Main.Logger.Error("Could not get MorePartySlots settings type");

					return null;
				}

				Main.Logger.Log($"Settings type: {mpsSettingsType.FullName}");

				var mpsSlotsField = mpsSettingsType.GetField("Slots");

				if (mpsSlotsField is null)
				{
					Main.Logger.Error("Could not get MorePartySlots slots count field");

					return null;
				}

				var mpsSettings =
					typeof(UnityModManager.ModSettings)
						.GetMethods()
						.First(mi => mi.Name == nameof(UnityModManager.ModSettings.Load) && mi.GetParameters().Length == 1)
						.MakeGenericMethod([mpsSettingsType])
						.Invoke(null, [morePartySlots]);

				mpsSlots = (int)mpsSlotsField.GetValue(mpsSettings);
			}

			return mpsSlots;
		}
	}

	[HarmonyPatch(typeof(BlueprintsCache))]
	static class BlueprintsCache_Postfix
	{
		[HarmonyPatch(nameof(BlueprintsCache.Init)), HarmonyPostfix]
		static void Postfix()
		{
			Settings.Init();
		}
	}
}
