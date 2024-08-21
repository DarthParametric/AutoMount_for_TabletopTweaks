using HarmonyLib;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Localization;
using Kingmaker.UI;
using ModMenu.Settings;
using KeyBinding = ModMenu.Settings.KeyBinding;
using UnityEngine;
using UnityModManagerNet;

namespace AutoMount
{
    public static class Settings
    {
        private static bool Initialized = false;

        // Keys
        private static readonly string RootKey = "automount";
        private static readonly string Hotkeys = "hotkeys";
        private static readonly string Whitelist = "whitelist";
        public static readonly string MountOnAreaEnter = "areaentermount";
        public static readonly string ConsoleOutput = "consoleoutput";
		public static readonly string ConsoleDebug = "consoledebug";
		public static readonly string RideAivu = "rideaivu";
		public static readonly string MountHotKey = $"{Hotkeys}.mounthotkey";
        public static readonly string DismountHotKey = $"{Hotkeys}.dismounthotkey";

		private static SettingsBuilder settings = SettingsBuilder.New(RootKey, GetString(GetKey("title"), "Auto Mount"));

        public static void Init()
        {
            if (Initialized)
            {
                Main.Logger.Log("Settings already initialized");
                return;
            }

            Main.Logger.Log("Initializing Settings");

            // Main settings
            settings.AddToggle(
                Toggle.New(
                    GetKey(MountOnAreaEnter),
                    true,
                    GetString($"{MountOnAreaEnter}-desc", "Mount On Entering Area"))
                .WithLongDescription(GetString($"{MountOnAreaEnter}-desc-long", "Automatically mounts all whitelisted party members when entering a new area.")));

			settings.AddToggle(
				Toggle.New(
					GetKey(RideAivu),
					true,
					GetString($"{RideAivu}-desc", "Prefer Aivu As Primary Mount"))
				.WithLongDescription(GetString($"{RideAivu}-desc-long", "When enabled, an Azata character will mount Aivu instead of their class pet (if they have one) when using the AutoMount hotkey and the automatic Mount On Entering Area.\n\n<b>N.B.</b> This will only take effect for characters that have Aivu and another rideable pet simultaneously. It should also work for companions taking Azata via ToyBox, etc. if they have a second pet.\n\nNote that if Aivu is currently too small or otherwise unsuitable to mount (incapacitated, dead, etc.), the character will attempt to mount their class pet instead.")));

			settings.AddToggle(
                Toggle.New(
                    GetKey(ConsoleOutput),
                    false,
                    GetString($"{ConsoleOutput}-desc", "Enable Combat Log Output"))
                .WithLongDescription(GetString($"{ConsoleOutput}-desc-long", "Outputs simple notifications of mod actions to the combat log. \n\n<b>N.B.</b> A notification will always be displayed when entering an area that prohibits mounting if the Mount On Entering Area setting is also enabled, regardless of this setting.")));

			settings.AddToggle(
				Toggle.New(
					GetKey(ConsoleDebug),
					false,
					GetString($"{ConsoleDebug}-desc", "Enable Additional Debug Output"))
				.ShowVisualConnection()
				.IsModificationAllowed(IsCombatLoggingEnabled)
				.WithLongDescription(GetString($"{ConsoleDebug}-desc-long", "Adds additional information to failure messages in the combat log as a pop-up/tooltip, and also saves it to the Player.log file. Only intended for troubleshooting purposes, generally not recommended to leave on.\n\n<b>N.B.</b>: Requires the above Combat Log Output setting to be enabled. Due to Mod Menu only reading setting values on the inital opening of the screen, switch tabs or close and reopen the menu after enabling Combat Log Output and saving the settings.")));

            // Hotkeys
            var hotkeys = settings.AddSubHeader(GetString(Hotkeys, "Hotkeys"), true);
            hotkeys.AddKeyBinding(
                KeyBinding.New(
                    GetKey(MountHotKey),
                    KeyboardAccess.GameModesGroup.All,
                    GetString($"{MountHotKey}-desc", "Mount"))
                .SetPrimaryBinding(KeyCode.A, withCtrl: true, withShift: true)
                .WithLongDescription(GetString($"{MountHotKey}-desc-long", "Sets the hotkey for mounting all whitelisted party members.")),
                () => Main.Mount(true));

            hotkeys.AddKeyBinding(
                KeyBinding.New(
                    GetKey(DismountHotKey),
                    KeyboardAccess.GameModesGroup.All,
                    GetString($"{DismountHotKey}-desc", "Dismount"))
                .SetPrimaryBinding(KeyCode.D, withCtrl: true, withShift: true)
                .WithLongDescription(GetString($"{DismountHotKey}-desc-long", "Sets the hotkey for dismounting all whitelisted party members.")),
                () => Main.Mount(false));

            // Whitelist
            var whitelist = settings.AddSubHeader(GetString(Whitelist, "Character Whitelist"), true);
            int slotCount = 6;
            
			if (GetMPSSlots() != null)
			{
				slotCount = Convert.ToInt32(GetMPSSlots());
			}
			
			for (int i = 0; i < slotCount; i++)
            {
                whitelist.AddToggle(
                    Toggle.New(
                        GetSlotKey(i),
                        true,
                        GetString($"{GetSlotPartialKey(i)}-desc", $"Slot {i + 1}"))
                    .WithLongDescription(
                        GetString($"{GetSlotPartialKey(i)}-desc-long",
                        $"Enables hotkeyed mount/dismount for the party member in slot {i + 1}. (You can change party order by dragging character portraits)")));
            }


            ModMenu.ModMenu.AddSettings(settings);

            Initialized = true;

            Main.Logger.Log("Settings Initialized");
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
            return Helpers.CreateString(GetKey(partialKey), text);
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
