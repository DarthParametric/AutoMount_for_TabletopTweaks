using Kingmaker;
using Kingmaker.AreaLogic.Etudes;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Root;
using Kingmaker.Blueprints.Root.Strings.GameLog;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.Localization;
using Kingmaker.Localization.Shared;
using Kingmaker.UI.MVVM._VM.Tooltip.Templates;
using Kingmaker.UI.Models.Log.CombatLog_ThreadSystem;
using Kingmaker.UI.Models.Log.CombatLog_ThreadSystem.LogThreads.Common;
using Kingmaker.UnitLogic.Abilities.Components.TargetCheckers;
using Kingmaker.UnitLogic;
using Kingmaker.Utility;
using System.Text;
using UnityEngine;
using static AutoMount.Settings;
using static Kingmaker.QA.Clockwork.TaskExploreFlyingIsles;
using static Unity.Burst.Intrinsics.X86.Avx;

namespace AutoMount
{
    public class Utils
    {
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
			var KitsuneRace = (BlueprintRace)ResourcesLibrary.TryGetBlueprint(new BlueprintGuid(new Guid("fd188bb7bb0002e49863aec93bfb9d99")));
			var HumanRace = (BlueprintRace)ResourcesLibrary.TryGetBlueprint(new BlueprintGuid(new Guid("0a5d473ead98b0646b94495af250fdc4")));
			bool bMountValid = AbilityTargetIsSuitableMount.CanMount(Master, AnimalComp);
			bool bSizeValid = AbilityTargetIsSuitableMountSize.CanMount(Master, AnimalComp);
			bool bVermin = AnimalComp.HasFact(bpVermin);
			bool bElemental = AnimalComp.HasFact(bpElemental);
			bool bAnimalCompPoly = AnimalComp.GetActivePolymorph().Component != null;
			bool bMasterPoly = Master.GetActivePolymorph().Component != null;
			bool bCmbLog = IsCombatLoggingEnabled();
			bool bPopUp = IsCombatLogDebugEnabled();

			// Account for Nenio and other Kitsune counting as polymorphed when in Human form.
			if (Master.HasFact(KitsuneRace) && bMasterPoly)
			{
				// Check that they are polymorphed into a Human.
				var PolyType = Master.GetActivePolymorph().Component.Race;

				if (PolyType == HumanRace)
				{
					if (IsCombatLoggingEnabled())
					{
						// Don't pipe to in-game combat log to prevent Nenio spoilers.
						Main.Logger.Log($"{Master.CharacterName} is a Kitsune polymorphed into a Human. Disabling polymorph check.");
					}

					bMasterPoly = false;
				}
			}

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
			bool bAivu = IsEnabled(RideAivu);

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

		public static bool CheckEtudesForPetBlockers()
		{
			// Certain areas (for example, Arueshalae's dream, the Ten Thousand Delights in Alushinyrra) have problems when the player or other
			// party members have rideable mounts, due to Owlcat removing pets from the party altogether but not also blocking mounting like they
			// do in some other areas (like the Defender's Heart). While the pets are removed, the automount on area load and via hotkey functions
			// still work, which typically prevents the character from moving while riding their imaginary steed. Since the pet removal is handled
			// by etude, we need to check the running etudes for the current area for one of the three different methods they can use. There's also
			// the potential for edge cases here, since they make provision in one of the approaches to exclude Aivu from being removed. Fortunately,
			// it seems like there's only one specific case that needs to be addressed, which is handled in Main.Mount (Drezen basement Cam showdown).

			BlueprintArea currentArea = Game.Instance.CurrentlyLoadedArea;
			BlueprintAreaPart currentAreaPart = Game.Instance.CurrentlyLoadedAreaPart.Or<BlueprintAreaPart>((BlueprintAreaPart) currentArea);

			try
			{
				foreach (Etude rawFact in Game.Instance.Player.EtudesSystem.Etudes.RawFacts)
				{
					if (rawFact.IsPlaying && rawFact.Blueprint.IsLinkedAreaPart(currentAreaPart))
					{
						foreach (EntityFactComponent component in rawFact.Components)
						{
							var comp = component.SourceBlueprintComponent;

							if (comp is DisableMountRiding)
							{
								// If the DisableMountRiding component is present then we can ignore this area, since the game is blocking
								// mounting itself and the mod already has provision for that. Of course that assumes that DisableMountRiding
								// will be earlier in the component array in order for it to be caught first, which may not always be the case.
								LogDebug($"Utils.CheckEtudesForPetBlockers:\nComponent {comp.GetType()} detected in etude {rawFact.Blueprint.NameSafe()}, GUID {rawFact.Blueprint.AssetGuid}, skipping.");

								return false;
							}

							if (comp is EtudeBracketDetachPet || comp is EtudeBracketDetachPetsOnUnit || comp is HideAllPets)
							{
								string AreaName = currentAreaPart.AreaLocalName.ToString().AsNullIfEmpty() ?? currentArea.AreaName.ToString();

								LogDebug($"Utils.CheckEtudesForPetBlockers:\nCurrent area is {AreaName} ({currentAreaPart}).\nComponent {comp.GetType()} detected in etude {rawFact.Blueprint.NameSafe()}, GUID {rawFact.Blueprint.AssetGuid}");

								return true;
							}
						}
					}
				}
			}
			catch (Exception e)
			{
				Main.Logger.Log($"Caught exception in etude check:\n{e}");
			}

			return false;
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

			ConsoleLog(sErrorMsg, sPopUp, new Color(0.5f, 0f, 0f), bPopUp);
		}

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

	public static class EnumerationExtensions
	{
		// Stolen from: https://haacked.com/archive/2010/06/16/null-or-empty-coalescing.aspx
		public static string AsNullIfEmpty(this string item)
		{
			if (String.IsNullOrEmpty(item))
			{
				return null;
			}
			return item;
		}

		public static string AsNullIfWhiteSpace(this string item)
		{
			if (String.IsNullOrWhiteSpace(item))
			{
				return null;
			}
			return item;
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
