using AutoMount;
using Epic.OnlineServices;
using HarmonyLib;
using Kingmaker.Blueprints.JsonSystem;
using Kingmaker.Localization;
using Kingmaker.Settings;
using Kingmaker.UI;
using ModMenu.Settings;
using UnityEngine;
using UnityModManagerNet;
using KeyBinding = ModMenu.Settings.KeyBinding;

namespace AutoMount
{
    /*
	public class Settings : UnityModManager.ModSettings
    {
       public UnityModManagerNet.KeyBinding MountHotKey = new() { keyCode = KeyCode.A, modifiers = 1 | 2 }; // ctrl+shift+A
       public UnityModManagerNet.KeyBinding DismountHotKey = new() { keyCode = KeyCode.D, modifiers = 1 | 2 }; // ctrl+shift+D
       public bool MountOnAreaEnter = true;
       public bool ConsoleOutput = false;

       public override void Save(UnityModManager.ModEntry modEntry)
       {
           Save(this, modEntry);
       }

       public static void Draw()
       {
           GUILayout.Space(10);

           GUILayout.BeginHorizontal(GUILayout.Width(300));
           GUILayout.Label("Mount hotkey");
           UnityModManager.UI.DrawKeybinding(ref Main.Settings.MountHotKey, "Mount hotkey");
           GUILayout.EndHorizontal();

           GUILayout.BeginHorizontal(GUILayout.Width(300));
           GUILayout.Label("Dismount hotkey");
           UnityModManager.UI.DrawKeybinding(ref Main.Settings.DismountHotKey, "Dismount hotkey");
           GUILayout.EndHorizontal();

           GUILayout.BeginHorizontal();
           Main.Settings.MountOnAreaEnter = GUILayout.Toggle(Main.Settings.MountOnAreaEnter, " Always mount when entering area");
           GUILayout.EndHorizontal();

           GUILayout.BeginHorizontal();
           Main.Settings.ConsoleOutput = GUILayout.Toggle(Main.Settings.ConsoleOutput, " Enable console output");
           GUILayout.EndHorizontal();


           GUILayout.Space(10);
       }
    }
	*/

    public static class Settings
    {
        private static bool Initialized = false;

        /* Keys */
        private static readonly string RootKey = "automount";
        private static readonly string Hotkeys = "hotkeys";
        private static readonly string Whitelist = "whitelist";
        public static readonly string MountOnAreaEnter = "areaentermount";
        public static readonly string ConsoleOutput = "consoleoutput";
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

            /* Main settings */
            settings.AddToggle(
                Toggle.New(
                    GetKey(MountOnAreaEnter),
                    true,
                    GetString($"{MountOnAreaEnter}-desc", "Always mount when entering area"))
                .WithLongDescription(GetString($"{MountOnAreaEnter}-desc-long", "Automatically mounts all whitelisted party members when entering a new area.")));

            settings.AddToggle(
                Toggle.New(
                    GetKey(ConsoleOutput),
                    false,
                    GetString($"{ConsoleOutput}-desc", "Enable console output"))
                .WithLongDescription(GetString($"{ConsoleOutput}-desc-long", "Enables output of mod functional information in combat console log. Mainly for debugging purposes.")));

            /* Hotkeys */
            var hotkeys = settings.AddSubHeader(GetString(Hotkeys, "Hotkeys"), true);
            hotkeys.AddKeyBinding(
                KeyBinding.New(
                    GetKey(MountHotKey),
                    KeyboardAccess.GameModesGroup.All,
                    GetString($"{MountHotKey}-desc", "Mount"))
                .SetPrimaryBinding(KeyCode.A, withCtrl: true, withShift: true)
                .WithLongDescription(GetString($"{MountHotKey}-desc-long", "Sets hotkey for automatically mounting all whitelisted party members.")),
                () => Main.Mount(true));

            hotkeys.AddKeyBinding(
                KeyBinding.New(
                    GetKey(DismountHotKey),
                    KeyboardAccess.GameModesGroup.All,
                    GetString($"{DismountHotKey}-desc", "Dismount"))
                .SetPrimaryBinding(KeyCode.D, withCtrl: true, withShift: true)
                .WithLongDescription(GetString($"{DismountHotKey}-desc-long", "Sets hotkey for automatically dismounting all whitelisted party members.")),
                () => Main.Mount(false));

            /* Whitelist */
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
                        $"Enables automatic mount for party member in slot {i + 1}. (You can change party order by dragging character portraits)")));
            }


            ModMenu.ModMenu.AddSettings(settings);

            Initialized = true;

            Main.Logger.Log("Settings Initialized");
        }

        //public static void Update()
        //{
        //}

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

        private static string GetKey(string partialKey)
        {
            return $"{RootKey}.{partialKey}";
        }

        private static LocalizedString GetString(string partialKey, string text)
        {
            return Helpers.CreateString(GetKey(partialKey), text);
        }
		
        // Checks for the presence of xADDBx's "More Party Slots" mod and returns its config value, kindly supplied by microsoftenator2022
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
                    Main.Logger.Error("Could not get slots count field");
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
