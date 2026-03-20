using GTA;
using GTA.Native;
using LemonUI;
using LemonUI.Menus;
using LemonUI.Scaleform;
using Score_Controller;
using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace ScoreControllerEnhanced
{
    public class ScoreController : Script
    {
        private readonly ObjectPool pool;
        private static NativeMenu mainMenu;

        private static NativeListItem<string> mainScoreCollection;
        private static NativeListItem<string> mainScoreTrack;
        private static NativeListItem<string> mainScoreProfiles;
        private static NativeListItem<string> mainScoreProfileEvents;
        private static NativeCheckboxItem mainMuteSound;
        private static NativeCheckboxItem mainMuteRadio;
        private static NativeCheckboxItem mainDisableWanted;
        private static NativeCheckboxItem mainDisableFlight;
        private static NativeCheckboxItem mainDisableOnDeath;

        private static JsonRoot root;

        [Flags]
        private enum ControlFlags
        {
            None = 0,
            IsScorePlaying = 1 << 0,
            IsSoundMuted = 1 << 1,
            IsRadioMuted = 1 << 2,
            IsWantedDisabled = 1 << 3,
            IsFlightDisabled = 1 << 4,
            IsDisableOnDeath = 1 << 5,
        }

        private static int controlFlags = 0;

        private static JsonTrack currentScoreTrack = null;
        private static string currentMusicEvent = null;
        private static string currentAudioScene = null;

        private static InstructionalButton buttonPlayScore =
            new InstructionalButton(Text.buttonPlayScore, Controls.PrimaryAction);

        private static readonly InstructionalButton buttonStopScore =
            new InstructionalButton(Text.buttonStopScore, Controls.SecondaryAction);

        private static readonly InstructionalButton buttonStopScene =
            new InstructionalButton(Text.buttonStopScene, Controls.TertiaryAction);

        private static readonly InstructionalButton buttonCancelEvent =
            new InstructionalButton(Text.buttonCancelEvent, Controls.TertiaryAction);

        public ScoreController()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string jsonPath = Path.Combine(baseDir, "Score Controller Enhanced", "score_controls.json");

            try
            {
                root = JsonLoader.Load(jsonPath);
            }
            catch (Exception ex)
            {
                GTA.UI.Notification.PostTicker("JSON load failed: " + ex.Message, false);
                GTA.UI.Notification.PostTicker("JSON path: " + jsonPath, false);
                return;
            }

            pool = new ObjectPool();

            mainMenu = new NativeMenu(Text.controllerTitle, Text.controllerSubtitle);

            mainScoreCollection = new NativeListItem<string>(
                Text.mainScoreCollectionTitle,
                Text.mainScoreCollectionDescr,
                JsonRoot.GetCollectionTitles(root));

            mainScoreTrack = new NativeListItem<string>(
                Text.mainScoreTrackTitle,
                Text.mainScoreTrackDescr,
                JsonRoot.GetTrackTitles(root, 0));

            mainScoreProfiles = new NativeListItem<string>(
                "Profile",
                "Select a profile.",
                JsonRoot.GetProfileNames(root, 0, 0));

            mainScoreProfileEvents = new NativeListItem<string>(
                "Profile Event",
                "Select a profile event.",
                JsonRoot.GetProfileEventNames(root, 0, 0, 0));

            mainMuteSound = new NativeCheckboxItem(Text.mainMuteSoundTitle, Text.mainMuteSoundDescr, false);
            mainMuteRadio = new NativeCheckboxItem(Text.mainMuteRadioTitle, Text.mainMuteRadioDescr, false);
            mainDisableWanted = new NativeCheckboxItem(Text.mainDisableWantedTitle, Text.mainDisableWantedDescr, false);
            mainDisableFlight = new NativeCheckboxItem(Text.mainDisableFlightTitle, Text.mainDisableFlightDescr, false);
            mainDisableOnDeath = new NativeCheckboxItem(Text.mainDisableOnDeathTitle, Text.mainDisableOnDeathDescr, true);

            mainMenu.Add(mainScoreCollection);
            mainMenu.Add(mainScoreTrack);
            mainMenu.Add(mainScoreProfiles);
            mainMenu.Add(mainScoreProfileEvents);
            mainMenu.Add(mainMuteSound);
            mainMenu.Add(mainMuteRadio);
            mainMenu.Add(mainDisableWanted);
            mainMenu.Add(mainDisableFlight);
            mainMenu.Add(mainDisableOnDeath);

            mainMenu.Buttons.Add(buttonPlayScore);
            mainMenu.Buttons.Add(buttonStopScore);

            pool.Add(mainMenu);

            mainScoreCollection.ItemChanged += OnSelectedScoreCollection;
            mainScoreTrack.ItemChanged += OnSelectedScoreTrack;
            mainScoreProfiles.ItemChanged += OnSelectedScoreProfiles;
            mainScoreProfileEvents.ItemChanged += OnSelectedScoreProfileEvents;

            mainMuteSound.CheckboxChanged += OnCheckboxChangedMuteSound;
            mainMuteRadio.CheckboxChanged += OnCheckboxChangedMuteRadio;
            mainDisableWanted.CheckboxChanged += OnCheckboxChangedDisableWanted;
            mainDisableFlight.CheckboxChanged += OnCheckboxChangedDisableFlight;
            mainDisableOnDeath.CheckboxChanged += OnCheckboxChangedDisableOnDeath;

            Tick += OnTick;
            KeyDown += OnKeyDown;

            SetControlFlag(ControlFlags.IsDisableOnDeath);

            if (root.Collections.Count > 0 && root.Collections[0].Tracks.Count > 0)
            {
                currentScoreTrack = root.Collections[0].Tracks[0];
            }
        }

        private void OnSelectedScoreCollection(object sender, ItemChangedEventArgs<string> e)
        {
            int collectionIndex = mainScoreCollection.SelectedIndex;

            if (root?.Collections == null)
                return;

            if (collectionIndex < 0 || collectionIndex >= root.Collections.Count)
                return;

            mainScoreTrack = new NativeListItem<string>(
                Text.mainScoreTrackTitle,
                Text.mainScoreTrackDescr,
                JsonRoot.GetTrackTitles(root, collectionIndex));

            mainMenu.Items[1] = mainScoreTrack;
            mainScoreTrack.ItemChanged += OnSelectedScoreTrack;

            if (root.Collections[collectionIndex].Tracks != null && root.Collections[collectionIndex].Tracks.Count > 0)
            {
                currentScoreTrack = root.Collections[collectionIndex].Tracks[0];
            }
            else
            {
                currentScoreTrack = null;
            }

            mainScoreProfiles = new NativeListItem<string>(
                "Profile",
                "Select a profile.",
                JsonRoot.GetProfileNames(root, collectionIndex, 0));

            mainMenu.Items[2] = mainScoreProfiles;
            mainScoreProfiles.ItemChanged += OnSelectedScoreProfiles;

            mainScoreProfileEvents = new NativeListItem<string>(
                "Profile Event",
                "Select a profile event.",
                JsonRoot.GetProfileEventNames(root, collectionIndex, 0, 0));

            mainMenu.Items[3] = mainScoreProfileEvents;
            mainScoreProfileEvents.ItemChanged += OnSelectedScoreProfileEvents;
        }

        private void OnSelectedScoreTrack(object sender, ItemChangedEventArgs<string> e)
        {
            int collectionIndex = mainScoreCollection.SelectedIndex;

            if (root?.Collections == null)
                return;

            if (collectionIndex < 0 || collectionIndex >= root.Collections.Count)
                return;

            var tracks = root.Collections[collectionIndex].Tracks;
            if (tracks == null || e.Index < 0 || e.Index >= tracks.Count)
                return;

            currentScoreTrack = tracks[e.Index];

            mainScoreProfiles = new NativeListItem<string>(
                "Profile",
                "Select a profile.",
                JsonRoot.GetProfileNames(root, collectionIndex, e.Index));

            mainMenu.Items[2] = mainScoreProfiles;
            mainScoreProfiles.ItemChanged += OnSelectedScoreProfiles;

            mainScoreProfileEvents = new NativeListItem<string>(
                "Profile Event",
                "Select a profile event.",
                JsonRoot.GetProfileEventNames(root, collectionIndex, e.Index, 0));

            mainMenu.Items[3] = mainScoreProfileEvents;
            mainScoreProfileEvents.ItemChanged += OnSelectedScoreProfileEvents;
        }
        private void OnSelectedScoreProfiles(object sender, ItemChangedEventArgs<string> e)
        {
            if (currentScoreTrack == null)
                return;

            int collectionIndex = mainScoreCollection.SelectedIndex;
            int trackIndex = mainScoreTrack.SelectedIndex;

            if (root?.Collections == null)
                return;

            if (collectionIndex < 0 || collectionIndex >= root.Collections.Count)
                return;

            var tracks = root.Collections[collectionIndex].Tracks;
            if (tracks == null || trackIndex < 0 || trackIndex >= tracks.Count)
                return;

            mainScoreProfileEvents = new NativeListItem<string>(
                "Profile Event",
                "Select a profile event.",
                JsonRoot.GetProfileEventNames(root, collectionIndex, trackIndex, e.Index));

            mainMenu.Items[3] = mainScoreProfileEvents;
            mainScoreProfileEvents.ItemChanged += OnSelectedScoreProfileEvents;
        }

        private void OnSelectedScoreProfileEvents(object sender, ItemChangedEventArgs<string> e)
        {
            if (mainScoreProfileEvents == null)
                return;

            if (e.Index < 0 || e.Index >= mainScoreProfileEvents.Items.Count)
                return;

            int collectionIndex = mainScoreCollection.SelectedIndex;
            int trackIndex = mainScoreTrack.SelectedIndex;
            int profileIndex = mainScoreProfiles.SelectedIndex;

            var profileNames = JsonRoot.GetProfileNames(root, collectionIndex, trackIndex);
            if (profileIndex < 0 || profileIndex >= profileNames.Length)
                return;

            var profile = JsonRoot.FindProfileByName(root, profileNames[profileIndex]);
            if (profile?.Events == null || e.Index >= profile.Events.Count)
                return;

            TriggerEvent(profile.Events[e.Index].Event);
        }

        private void OnCheckboxChangedDisableOnDeath(object sender, EventArgs e)
        {
            if (mainDisableOnDeath.Checked)
                SetControlFlag(ControlFlags.IsDisableOnDeath);
            else
                ClearControlFlag(ControlFlags.IsDisableOnDeath);
        }

        private void OnCheckboxChangedDisableFlight(object sender, EventArgs e)
        {
            if (mainDisableFlight.Checked)
                DisableFlight();
            else
                EnableFlight();
        }

        private void OnCheckboxChangedDisableWanted(object sender, EventArgs e)
        {
            if (mainDisableWanted.Checked)
                DisableWanted();
            else
                EnableWanted();
        }

        private void OnCheckboxChangedMuteRadio(object sender, EventArgs e)
        {
            if (mainMuteRadio.Checked)
                MuteRadio();
            else
                UnmuteRadio();
        }

        private void OnCheckboxChangedMuteSound(object sender, EventArgs e)
        {
            if (mainMuteSound.Checked)
                MuteSound();
            else
                UnmuteSound();
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (Game.IsControlPressed(Controls.ScoreController) && IsMenuAvailable())
            {
                DisplayMenu();
            }

            if (Game.IsControlJustPressed(Controls.PrimaryAction) && mainMenu.Visible)
            {
                PlayScore();
            }

            if (Game.IsControlPressed(Controls.SecondaryAction) && IsStopScoreAvailable())
            {
                StopScore();
            }
        }

        private void OnTick(object sender, EventArgs e)
        {
            pool.Process();

            if (mainMenu.Visible)
            {
                Game.DisableControlThisFrame(GTA.Control.Jump);
                Game.DisableControlThisFrame(GTA.Control.VehicleHandbrake);
                Function.Call(Hash.HIDE_HELP_TEXT_THIS_FRAME);
            }

            if (!mainMenu.Visible)
            {
                mainMenu.Buttons.Remove(buttonStopScene);
                mainMenu.Buttons.Remove(buttonCancelEvent);
            }

            if (HasControlFlag(ControlFlags.IsScorePlaying))
            {
                mainScoreCollection.Enabled = false;
                mainScoreTrack.Enabled = false;
                mainScoreProfiles.Enabled = false;
                mainScoreProfileEvents.Enabled = true;
            }
            else
            {
                mainScoreCollection.Enabled = true;
                mainScoreTrack.Enabled = true;
                mainScoreProfiles.Enabled = true;
                mainScoreProfileEvents.Enabled = false;
            }

            if (HasControlFlag(ControlFlags.IsRadioMuted))
            {
                Game.DisableControlThisFrame(GTA.Control.VehicleRadioWheel);
                Game.DisableControlThisFrame(GTA.Control.VehicleNextRadio);
                Game.DisableControlThisFrame(GTA.Control.VehiclePrevRadio);
                Game.DisableControlThisFrame(GTA.Control.RadioWheelLeftRight);
                Game.DisableControlThisFrame(GTA.Control.RadioWheelUpDown);
            }

            if (HasControlFlag(ControlFlags.IsDisableOnDeath))
            {
                Player player = Function.Call<Player>(Hash.PLAYER_ID);

                if (Function.Call<bool>(Hash.IS_PLAYER_DEAD, player))
                {
                    StopScore();
                }
            }
        }

        private static void TriggerEvent(string name)
        {
            if (string.IsNullOrEmpty(name))
                return;

            Function.Call(Hash.TRIGGER_MUSIC_EVENT, name);
            currentMusicEvent = name;
        }

        private static void StartScene(string name)
        {
            if (string.IsNullOrEmpty(name))
                return;

            Function.Call(Hash.START_AUDIO_SCENE, name);
            currentAudioScene = name;
        }

        private static void StopScene(string name)
        {
            if (string.IsNullOrEmpty(name))
                return;

            Function.Call(Hash.STOP_AUDIO_SCENE, name);
            currentAudioScene = null;
        }

        private static void SetControlFlag(ControlFlags flag)
        {
            controlFlags |= (int)flag;
        }

        private static void ClearControlFlag(ControlFlags flag)
        {
            controlFlags &= ~(int)flag;
        }

        private static bool HasControlFlag(ControlFlags flag)
        {
            return (controlFlags & (int)flag) != 0;
        }

        private static void SetFlag(string name)
        {
            if (string.IsNullOrEmpty(name))
                return;

            Function.Call(Hash.SET_AUDIO_FLAG, name, true);
        }

        private static void DisableFlag(string name)
        {
            if (string.IsNullOrEmpty(name))
                return;

            Function.Call(Hash.SET_AUDIO_FLAG, name, false);
        }

        private static string GetSelectedProfileEvent()
        {
            int collectionIndex = mainScoreCollection.SelectedIndex;
            int trackIndex = mainScoreTrack.SelectedIndex;
            int profileIndex = mainScoreProfiles.SelectedIndex;
            int profileEventIndex = mainScoreProfileEvents.SelectedIndex;

            var profileNames = JsonRoot.GetProfileNames(root, collectionIndex, trackIndex);
            if (profileIndex < 0 || profileIndex >= profileNames.Length)
                return null;

            var profile = JsonRoot.FindProfileByName(root, profileNames[profileIndex]);
            if (profile?.Events == null)
                return null;

            if (profileEventIndex < 0 || profileEventIndex >= profile.Events.Count)
                return null;

            return profile.Events[profileEventIndex].Event;
        }

        private static void PlayScore()
        {
            if (currentScoreTrack == null)
                return;

            SetControlFlag(ControlFlags.IsScorePlaying);

#if DEBUG
            GTA.UI.Notification.PostTicker("Current track is: " + currentScoreTrack.Title, false);
#endif

            TriggerEvent(currentScoreTrack.Event);

            string profileEvent = GetSelectedProfileEvent();
            if (!string.IsNullOrWhiteSpace(profileEvent))
            {
                TriggerEvent(profileEvent);
            }
        }

        private static void StopScore()
        {
            ClearControlFlag(ControlFlags.IsScorePlaying);
            TriggerEvent("GTA_ONLINE_STOP_SCORE");
        }

        private static void MuteSound()
        {
            SetControlFlag(ControlFlags.IsSoundMuted);
            StartScene("END_CREDITS_SCENE");
        }

        private static void UnmuteSound()
        {
            ClearControlFlag(ControlFlags.IsSoundMuted);
            StopScene("END_CREDITS_SCENE");
        }

        private static void MuteRadio()
        {
            SetControlFlag(ControlFlags.IsRadioMuted);
            Function.Call(Hash.SET_RADIO_TO_STATION_NAME, "OFF");
            StartScene("MIC1_RADIO_DISABLE");
        }

        private static void UnmuteRadio()
        {
            ClearControlFlag(ControlFlags.IsRadioMuted);
            StopScene("MIC1_RADIO_DISABLE");
        }

        private static void DisableWanted()
        {
            SetControlFlag(ControlFlags.IsWantedDisabled);
            SetFlag("WantedMusicDisabled");
        }

        private static void EnableWanted()
        {
            ClearControlFlag(ControlFlags.IsWantedDisabled);
            DisableFlag("WantedMusicDisabled");
        }

        private static void DisableFlight()
        {
            SetControlFlag(ControlFlags.IsFlightDisabled);
            SetFlag("DisableFlightMusic");
        }

        private static void EnableFlight()
        {
            ClearControlFlag(ControlFlags.IsFlightDisabled);
            DisableFlag("DisableFlightMusic");
        }

        private static void DisplayMenu()
        {
            if (!mainMenu.Visible)
            {
                mainMenu.SelectedIndex = 0;
            }

            mainMenu.Visible = !mainMenu.Visible;

            if (mainMenu.Visible)
            {
                Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "SELECT", "HUD_FRONTEND_DEFAULT_SOUNDSET", true);
            }
            else
            {
                Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "SELECT", "HUD_FRONTEND_DEFAULT_SOUNDSET", true);
                Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, "BACK", "HUD_FRONTEND_DEFAULT_SOUNDSET", true);
            }
        }

        private static bool IsPhoneActive()
        {
            int pedHandle = Function.Call<int>(Hash.PLAYER_PED_ID);
            return Function.Call<bool>(Hash.IS_PED_RUNNING_MOBILE_PHONE_TASK, pedHandle);
        }

        private static bool IsMinigameInProgress()
        {
            return Function.Call<bool>(Hash.IS_MINIGAME_IN_PROGRESS);
        }

        private static bool IsMissionInProgress()
        {
            return Function.Call<bool>(Hash.GET_MISSION_FLAG);
        }

        private static bool IsMenuAvailable()
        {
            bool isPlayerControllable = Game.Player.CanControlCharacter;
            bool isPhoneNotActive = !IsPhoneActive();
            bool isMinigameNotInProgress = !IsMinigameInProgress();
            bool isMenuDisplayed = mainMenu.Visible;

            return isPlayerControllable && isPhoneNotActive && isMinigameNotInProgress && !isMenuDisplayed;
        }

        private static bool IsStopScoreAvailable()
        {
            return mainMenu.Visible && HasControlFlag(ControlFlags.IsScorePlaying);
        }
    }
}