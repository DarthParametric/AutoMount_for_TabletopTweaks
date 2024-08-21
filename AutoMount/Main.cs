using AutoMount.Events;
using HarmonyLib;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Root;
using Kingmaker.Blueprints.Root.Strings.GameLog;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.PubSubSystem;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Components.TargetCheckers;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityModManagerNet;
using static UnityModManagerNet.UnityModManager.ModEntry;

namespace AutoMount
{
	public static class Main
	{
		public static bool Enabled;
		public static ModLogger Logger;
		private static Harmony m_harmony;
		private static OnAreaLoad m_area_load_handler;
		private static Guid m_mount_ability_guid = new Guid("d340d820867cf9741903c9be9aed1ccc");
		private static bool m_force_mount = false;

		public static bool Load(UnityModManager.ModEntry modEntry)
		{
			Logger = modEntry.Logger;

			Logger.Log("Loading");

			modEntry.OnToggle = OnToggle;
			modEntry.OnUpdate = OnUpdate;

			m_harmony = new Harmony(modEntry.Info.Id);
			m_harmony.PatchAll(Assembly.GetExecutingAssembly());

			m_area_load_handler = new OnAreaLoad();
			EventBus.Subscribe(m_area_load_handler);

			return true;
		}

		static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
		{
			Enabled = value;
			return true;
		}

		static void OnUpdate(UnityModManager.ModEntry modEntry, float delta)
		{
			if (Main.m_force_mount)
			{
				Mount(true);
				Main.m_force_mount = false;
			}
		}

		public static void ForceMount()
		{
			Main.m_force_mount = true;
		}

		static string DebugMessageHeader()
		{
			StringBuilder sDbgMsg = GameLogUtility.StringBuilder;
			BlueprintArea currentlyLoadedArea = Game.Instance.CurrentlyLoadedArea;
			TimeSpan gameTime = Game.Instance.Player.GameTime;
			string gameCurrentTime = string.Format("{0:D2}:{1:D2}", gameTime.Hours, gameTime.Minutes);

			sDbgMsg.Append($"Current Game Time: {gameCurrentTime}, {BlueprintRoot.Instance.Calendar.GetDateText(gameTime, GameDateFormat.Full, true)}");
			sDbgMsg.AppendLine();
			sDbgMsg.Append($"Current Real Time: {DateTime.Now:HH:mm, ddd dd MMM, yyyy}");
			sDbgMsg.AppendLine();
			sDbgMsg.Append($"Current Area: {currentlyLoadedArea.AreaDisplayName}");
			sDbgMsg.AppendLine();

			string sDbgMsg2 = sDbgMsg.ToString();
			sDbgMsg.Clear();

			return sDbgMsg2;
		}

		static string DebugCharacterState(UnitEntityData Char)
		{
			StringBuilder sCharMsg1 = GameLogUtility.StringBuilder;

			BlueprintUnit bpChar = Char.OriginalBlueprint;
			BlueprintUnit bpCharIns = Char.BlueprintForInspection;

			if (Char.IsMainCharacter)
			{
				sCharMsg1.Append($"Rider Name: {Char.CharacterName} (Player Character)");
				sCharMsg1.AppendLine();
			}
			else if (Char.IsStoryCompanion())
			{
				sCharMsg1.Append($"Rider Name: {Char.CharacterName} (Story Companion)");
				sCharMsg1.AppendLine();
			}
			else if (Char.IsPet)
			{
				string sPet = Char.Master.Pets.First(p => p.Entity == Char).EntityPart.Type.ToString();
				string sPetType;

				switch (sPet)
				{
					case ("AnimalCompanion"):
						sPetType = "Animal Companion";
						break;

					case ("MythicSkeletalChampion"):
						sPetType = "Mythic Skeletal Champion";
						break;

					case ("AzataHavocDragon"):
						sPetType = "Azata Havoc Dragon";
						break;

					case ("Clone"):
						sPetType = "Clone";
						break;

					case ("NightHag"):
						sPetType = "Nighthag";
						break;

					default:
						sPetType = "Unknown Pet Type";
						break;
				}

				sCharMsg1.Append($"Pet Name: {Char.CharacterName} ({sPetType})");
				sCharMsg1.AppendLine();
			}
			else
			{
				sCharMsg1.Append($"Rider Name: {Char.CharacterName} (Mercenary)");
				sCharMsg1.AppendLine();
				sCharMsg1.Append($"Is Custom Companion: {Char.IsCustomCompanion()}");
				sCharMsg1.AppendLine();
			}

			sCharMsg1.Append($"Gender: {Char.Gender}");
			sCharMsg1.AppendLine();

			if (Char.IsPet)
			{
				string sPetType = Char.Blueprint.LocalizedName.String;

				if (sPetType == "Aivu")
				{
					sCharMsg1.Append("Type: Havoc Dragon");
				}
				else
				{
					sCharMsg1.Append($"Type: {sPetType}");
				}
			}
			else
			{
				sCharMsg1.Append($"Race: {Char.Progression.Race.m_DisplayName}");
			}

			sCharMsg1.AppendLine();
			sCharMsg1.Append("Classes:");
			sCharMsg1.AppendLine();

			foreach (ClassData classData in Char.Progression.Classes)
			{
				if (!classData.Archetypes.Empty<BlueprintArchetype>())
				{
					foreach (var archetype in classData.Archetypes)
					{
						sCharMsg1.Append($"\t\t\t{classData.CharacterClass.LocalizedName}: {archetype.Name} ({classData.Level})");
						sCharMsg1.AppendLine();
					}
				}
				else
				{
					sCharMsg1.Append($"\t\t\t{classData.CharacterClass.LocalizedName} ({classData.Level})");
					sCharMsg1.AppendLine();
				}
			}

			CharacterStats nCharStat = Char.Stats;

			sCharMsg1.Append("Character Stats:");
			sCharMsg1.AppendLine();
			sCharMsg1.Append($"\t\t\tStr:\t{nCharStat.Strength.ModifiedValue}\t({nCharStat.Strength.BaseValue} + {nCharStat.Strength.ModifiedValue - nCharStat.Strength.BaseValue})");
			sCharMsg1.AppendLine();
			sCharMsg1.Append($"\t\t\tDex:\t{nCharStat.Dexterity.ModifiedValue}\t({nCharStat.Dexterity.BaseValue} + {nCharStat.Dexterity.ModifiedValue - nCharStat.Dexterity.BaseValue})");
			sCharMsg1.AppendLine();
			sCharMsg1.Append($"\t\t\tCon:\t{nCharStat.Constitution.ModifiedValue}\t({nCharStat.Constitution.BaseValue} + {nCharStat.Constitution.ModifiedValue - nCharStat.Constitution.BaseValue})");
			sCharMsg1.AppendLine();
			sCharMsg1.Append($"\t\t\tInt:\t{nCharStat.Intelligence.ModifiedValue}\t({nCharStat.Intelligence.BaseValue} + {nCharStat.Intelligence.ModifiedValue - nCharStat.Intelligence.BaseValue})");
			sCharMsg1.AppendLine();
			sCharMsg1.Append($"\t\t\tWis:\t{nCharStat.Wisdom.ModifiedValue}\t({nCharStat.Wisdom.BaseValue} + {nCharStat.Wisdom.ModifiedValue - nCharStat.Wisdom.BaseValue})");
			sCharMsg1.AppendLine();
			sCharMsg1.Append($"\t\t\tCha:\t{nCharStat.Charisma.ModifiedValue}\t({nCharStat.Charisma.BaseValue} + {nCharStat.Charisma.ModifiedValue - nCharStat.Charisma.BaseValue})");
			sCharMsg1.AppendLine();
			sCharMsg1.Append($"Original Size: {Char.OriginalSize}");
			sCharMsg1.AppendLine();
			sCharMsg1.Append($"Current Size: {Char.State.Size}");
			sCharMsg1.AppendLine();
			sCharMsg1.Append($"Position: {Char.Position.ToString().Trim(['(', ')'])}");
			sCharMsg1.AppendLine();
			sCharMsg1.Append($"Orientation: {Char.Orientation}°");
			sCharMsg1.AppendLine();
			sCharMsg1.Append($"HP: {Char.HPLeft} / {Char.MaxHP} (Temp HP: {Char.TemporaryHP})");
			sCharMsg1.AppendLine();
			sCharMsg1.Append($"Damage Sustained: {Char.Damage}");
			sCharMsg1.AppendLine();
			sCharMsg1.Append($"In Combat: {Char.IsInCombat}");
			sCharMsg1.AppendLine();
			sCharMsg1.Append($"Can't Attack: {Char.State.HasCondition(UnitCondition.CanNotAttack)}");
			sCharMsg1.AppendLine();
			sCharMsg1.Append($"Can't Act: {Char.State.HasCondition(UnitCondition.CantAct)}");
			sCharMsg1.AppendLine();
			sCharMsg1.Append($"Can't Use Standard Actions: {Char.State.HasCondition(UnitCondition.CantUseStandardActions)}");
			sCharMsg1.AppendLine();
			sCharMsg1.Append($"Can't Use Abilities: {Char.State.HasCondition(UnitCondition.UseAbilityForbidden)}");
			sCharMsg1.AppendLine();
			sCharMsg1.Append($"Can Move: {Char.State.CanMove}");
			sCharMsg1.AppendLine();
			sCharMsg1.Append($"Can't Move: {Char.State.HasCondition(UnitCondition.CantMove)}");
			sCharMsg1.AppendLine();
			sCharMsg1.Append($"Movement Banned: {Char.State.HasCondition(UnitCondition.MovementBan)}");
			sCharMsg1.AppendLine();

			if (Char.State.Owner.Encumbrance == Encumbrance.Overload)
			{
				sCharMsg1.Append("Is Encumbered: True");
			}
			else
			{
				sCharMsg1.Append("Is Encumbered: False");
			}

			sCharMsg1.AppendLine();
			sCharMsg1.Append($"Is Able: {Char.State.IsAble}");
			sCharMsg1.AppendLine();
			sCharMsg1.Append($"Is Asleep: {Char.IsSleeping}");
			sCharMsg1.AppendLine();
			sCharMsg1.Append($"Is Conscious: {Char.State.IsConscious}");
			sCharMsg1.AppendLine();
			sCharMsg1.Append($"Is Unconscious: {Char.State.HasCondition(UnitCondition.Unconscious)}");
			sCharMsg1.AppendLine();
			sCharMsg1.Append($"Is Confused: {Char.State.HasCondition(UnitCondition.Confusion)}");
			sCharMsg1.AppendLine();
			sCharMsg1.Append($"Is Dazed: {Char.State.HasCondition(UnitCondition.Dazed)}");
			sCharMsg1.AppendLine();
			sCharMsg1.Append($"Is Entangled: {Char.State.HasCondition(UnitCondition.Entangled)}");
			sCharMsg1.AppendLine();
			sCharMsg1.Append($"Is Frightened: {Char.State.HasCondition(UnitCondition.Frightened)}");
			sCharMsg1.AppendLine();
			sCharMsg1.Append($"Is Helpless: {Char.State.IsHelpless}");
			sCharMsg1.AppendLine();
			sCharMsg1.Append($"Is Prone: {Char.State.HasCondition(UnitCondition.Prone)}");
			sCharMsg1.AppendLine();
			sCharMsg1.Append($"Is Paralyzed: {Char.State.HasCondition(UnitCondition.Paralyzed)}");
			sCharMsg1.AppendLine();
			sCharMsg1.Append($"Is Petrified: {Char.State.HasCondition(UnitCondition.Petrified)}");
			sCharMsg1.AppendLine();
			sCharMsg1.Append($"Is Stunned: {Char.State.HasCondition(UnitCondition.Stunned)}");
			sCharMsg1.AppendLine();
			sCharMsg1.Append($"Is Polymorphed: {Char.GetActivePolymorph().Component != null}");
			sCharMsg1.AppendLine();
			sCharMsg1.Append($"Is Dead: {Char.State.IsDead}");
			sCharMsg1.AppendLine();
			sCharMsg1.Append($"Is Finally Dead: {Char.State.IsFinallyDead}");
			sCharMsg1.AppendLine();
			sCharMsg1.Append($"Lifestate: {Char.State.LifeState}");
			sCharMsg1.AppendLine();

			string sCharMsg2 = sCharMsg1.ToString();
			sCharMsg1.Clear();

			return sCharMsg2;
		}

	static void DebugErrorMessage(UnitEntityData uRider, UnitEntityData uMount, string sCondition, bool bDebugMode)
		{
			bool bPopUp = false;
			string sPopUp = string.Empty;
			string sErrorMsg;

			switch (sCondition)
			{
				case ("RiderDead"):
					sErrorMsg = $"AutoMount: {uRider.CharacterName} is dead, mount attempt failed.";
					break;

				case ("RiderUnconscious"):
					sErrorMsg = $"AutoMount: {uRider.CharacterName} is unconscious, mount attempt failed.";
					break;

				case ("RiderHelpless"):
					sErrorMsg = $"AutoMount: {uRider.CharacterName} is helpless, mount attempt failed.";
					break;

				case ("RiderPolymorphed"):
					sErrorMsg = $"AutoMount: {uRider.CharacterName} is polymorphed, mount attempt failed.";
					break;

				case ("PetDead"):
					sErrorMsg = $"AutoMount: {uRider.CharacterName}'s mount attempt failed. {uMount.CharacterName} is dead.";
					break;

				case ("PetUnconscious"):
					sErrorMsg = $"AutoMount: {uRider.CharacterName}'s mount attempt failed. {uMount.CharacterName} is unconscious.";
					break;

				case ("PetHelpless"):
					sErrorMsg = $"AutoMount: {uRider.CharacterName}'s mount attempt failed. {uMount.CharacterName} is helpless.";
					break;

				case ("PetPolymorphed"):
					sErrorMsg = $"AutoMount: {uRider.CharacterName}'s mount attempt failed. {uMount.CharacterName} is polymorphed.";
					break;

				case ("PetElemental"):
					sErrorMsg = $"AutoMount: {uRider.CharacterName}'s mount attempt failed. {uMount.CharacterName} is not a mountable type (Elemental).";
					break;

				case ("PetVermin"):
					sErrorMsg = $"AutoMount: {uRider.CharacterName}'s mount attempt failed. {uMount.CharacterName} is not a mountable type (Vermin).";
					break;

				case ("PetTooSmall"):
					sErrorMsg = $"AutoMount: {uRider.CharacterName}'s mount attempt failed. {uMount.CharacterName} is not large enough to ride.";
					break;

				case ("AivuTooSmall"):
					sErrorMsg = $"AutoMount: {uMount.CharacterName} is set to the preferred mount for {uRider.CharacterName} but is not large enough to ride, skipping.";
					break;

				default:
					sErrorMsg = "Error, failure condition not found. Please report!";
					break;
			}

			if (bDebugMode)
			{
				StringBuilder sMsg1 = GameLogUtility.StringBuilder;
				string sHeader = DebugMessageHeader();
				string sRider = DebugCharacterState(uRider);
				string sPet = DebugCharacterState(uMount);

				bPopUp = true;

				sMsg1.Append(sHeader);
				sMsg1.AppendLine();
				sMsg1.Append("============================================");
				sMsg1.AppendLine();
				sMsg1.AppendLine();
				sMsg1.Append(sRider);
				sMsg1.AppendLine();
				sMsg1.Append("============================================");
				sMsg1.AppendLine();
				sMsg1.AppendLine();
				sMsg1.Append(sPet);

				sPopUp = sMsg1.ToString();
				sMsg1.Clear();

				Main.Logger.Error($"Mounting failure due to condition check \"{sCondition}\":\n\n{sErrorMsg}\n\n{sPopUp}\n\n");
			}

			Utils.ConsoleLog(sErrorMsg, sPopUp, new Color(0.5f, 0f, 0f), bPopUp);
		}

		// Thanks to advice from AlterAsc on the Owlcat Discord server's #mod-dev-technical channel, specifically checking for the
		// presence of the UndersizedMount feat is no longer necessary, thus removing the hard dependency on TableTopTweaks. Now TTT
		// and possible alternative mods that may also alter pet size/rider ability to mount (like Toybox) are dynamically accounted
		// for. Pet size/rideability checking is now more thorough, and more detailed log feedback is provided for mount failures.
		// Additionally, an oversight from the original mod that allowed for the forced mounting of otherwise non-mountable pets
		// (e.g. Centipede) has been corrected, and mount attempts in mounting blacklisted areas/sequences (e.g. Defender's Heart)
		// are skipped.
		public static bool CheckCanMount(UnitEntityData Master, UnitEntityData AnimalComp, bool mounting)
		{
			var bpVermin = (BlueprintFeature)ResourcesLibrary.TryGetBlueprint(new BlueprintGuid(new Guid("09478937695300944a179530664e42ec")));
			var bpElemental = (BlueprintFeature)ResourcesLibrary.TryGetBlueprint(new BlueprintGuid(new Guid("daf893d14cc54e98a319fb121d7ac4d9")));
			bool bMountValid = AbilityTargetIsSuitableMount.CanMount(Master, AnimalComp);
			bool bSizeValid = AbilityTargetIsSuitableMountSize.CanMount(Master, AnimalComp);
			bool bVermin = AnimalComp.HasFact(bpVermin);
			bool bElemental = AnimalComp.HasFact(bpElemental);
			bool bAnimalCompPoly = AnimalComp.GetActivePolymorph().Component != null;
			bool bMasterPoly = Master.GetActivePolymorph().Component != null;
			bool bCmbLog = Settings.IsCombatLoggingEnabled();
			bool bPopUp = Settings.IsCombatLogDebugEnabled();

			if (!Master.State.IsDead && bMountValid && bSizeValid && !Master.State.IsHelpless && !AnimalComp.State.IsHelpless && !bMasterPoly && !bVermin && !bElemental)
			{
				return true;
			}

			if (bCmbLog && mounting)
			{
				if (Master.State.IsDead)
				{
					DebugErrorMessage(Master, AnimalComp, "RiderDead", bPopUp);
				}
				else if (AnimalComp.State.IsDead)
				{
					DebugErrorMessage(Master, AnimalComp, "PetDead", bPopUp);
				}
				else if (!Master.State.IsConscious)
				{
					DebugErrorMessage(Master, AnimalComp, "RiderUnconscious", bPopUp);
				}
				else if (Master.State.IsHelpless)
				{
					DebugErrorMessage(Master, AnimalComp, "RiderHelpless", bPopUp);
				}
				else if (!AnimalComp.State.IsConscious)
				{
					DebugErrorMessage(Master, AnimalComp, "PetUnconscious", bPopUp);
				}
				else if (AnimalComp.State.IsHelpless)
				{
					DebugErrorMessage(Master, AnimalComp, "PetHelpless", bPopUp);
				}
				else if (bMasterPoly)
				{
					DebugErrorMessage(Master, AnimalComp, "RiderPolymorphed", bPopUp);
				}
				else if (bAnimalCompPoly)
				{
					DebugErrorMessage(Master, AnimalComp, "PetPolymorphed", bPopUp);
				}
				else if (bVermin)
				{
					DebugErrorMessage(Master, AnimalComp, "PetVermin", bPopUp);
				}
				else if (bElemental)
				{
					DebugErrorMessage(Master, AnimalComp, "PetElemental", bPopUp);
				}
				else if (!bSizeValid && AnimalComp == Master.GetPet(PetType.AzataHavocDragon))
				{
					DebugErrorMessage(Master, AnimalComp, "AivuTooSmall", bPopUp);
				}
				else if (!bSizeValid)
				{
					DebugErrorMessage(Master, AnimalComp, "PetTooSmall", bPopUp);
				}
				else
				{
					DebugErrorMessage(Master, AnimalComp, "UnknownFailure", bPopUp);
				}
			}

			return false;
		}

		public static UnitEntityData GetRidersPet(UnitEntityData uRider, bool bMounting)
		{
			UnitEntityData uPet;
			UnitEntityData uAivu = uRider.GetPet(PetType.AzataHavocDragon);
			bool bAivu = Settings.IsEnabled(Settings.RideAivu);

			foreach (PetType pettype in Enum.GetValues(typeof(PetType)))
			{
				if (uRider.GetPet(pettype) != null)
				{
					uPet = uRider.GetPet(pettype);

					if (pettype == PetType.AnimalCompanion && uAivu != null)
					{
						if (bAivu)
						{
							if (!CheckCanMount(uRider, uAivu, bMounting))
							{
								return uPet;
							}

							continue;
						}
						else
						{
							return uPet;
						}
					}

					return uPet;
				}
			}

			return null;
		}

		public static void Mount(bool on)
		{
            foreach (var (rider, idx) in Game.Instance.Player.Party.Select((rider, idx) => (rider, idx)))
            {
				if (!Settings.IsSlotEnabled(idx))
				continue;

				var pet = GetRidersPet(rider, on);

				if (pet != null)
				{
					if (rider.State.HasCondition(UnitCondition.DisableMountRiding))
					{
						if (Settings.IsOnAreaMountEnabled())
						{
							Utils.ConsoleLog("AutoMount: Mounting is currently disabled for this area.", "", new Color(0.5f, 0f, 0f), false);
						}
						break;
					}
					else
					{
						if (CheckCanMount(rider, pet, on))
						{
							var mount = rider.ActivatableAbilities.Enumerable.Find(a => a.Blueprint.AssetGuid.CompareTo(m_mount_ability_guid) == 0);

							if (mount != null)
							{
								if (mount.IsOn && !on)
								{
									rider.RiderPart?.Dismount();

									if (Settings.IsCombatLoggingEnabled())
									{
										Utils.ConsoleLog($"AutoMount: {rider.CharacterName} dismounted {pet.CharacterName}.", "", new Color(0f, 0.27f, 0.54f), false);
									}
								}
								else if (!mount.IsOn && on)
								{
									rider.Ensure<UnitPartRider>().Mount(pet);

									if (Settings.IsCombatLoggingEnabled())
									{
										Utils.ConsoleLog($"AutoMount: {rider.CharacterName} mounted {pet.CharacterName}.", "", new Color(0f, 0.39f, 0f), false);
									}
								}
							}
						}
					}
				}
            }
        }
    }
}
