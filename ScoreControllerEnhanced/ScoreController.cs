using GTA;
using GTA.Native;
using LemonUI;
using LemonUI.Menus;
using LemonUI.Scaleform;
using Score_Controller;
using System;
using System.Windows.Forms;

namespace ScoreControllerEnhanced
{
    public class ScoreController : Script
    {
        private readonly ObjectPool pool;
        private static NativeMenu mainMenu;

        private static NativeListItem<string> mainScoreCollection;
        private static NativeListItem<string> mainScoreTrack;
        private static NativeListItem<string> mainScoreIntensity;
        private static NativeCheckboxItem mainMuteSound;
        private static NativeCheckboxItem mainMuteRadio;
        private static NativeCheckboxItem mainDisableWanted;
        private static NativeCheckboxItem mainDisableFlight;
        private static NativeCheckboxItem mainDisableOnDeath;

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

        private static ScoreTrack currentScoreTrack = null;
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
            Tracks.AddTracks();
            Collections.AddCollections();
            Intensities.AddIntensities();

            pool = new ObjectPool();

            mainMenu = new NativeMenu(Text.controllerTitle, Text.controllerSubtitle);

            mainScoreCollection = new NativeListItem<string>(
                Text.mainScoreCollectionTitle,
                Text.mainScoreCollectionDescr,
                Collections.CollectionsList.ConvertAll(x => x.Title).ToArray());

            mainScoreTrack = new NativeListItem<string>(
                Text.mainScoreTrackTitle,
                Text.mainScoreTrackDescr,
                Tracks.scoreLists[0].ToArray());

            mainScoreIntensity = new NativeListItem<string>(
                Text.mainScoreIntensityTitle,
                Text.mainScoreIntensityDescr,
                Intensities.listIntensities.ToArray());

            mainMuteSound = new NativeCheckboxItem(Text.mainMuteSoundTitle, Text.mainMuteSoundDescr, false);
            mainMuteRadio = new NativeCheckboxItem(Text.mainMuteRadioTitle, Text.mainMuteRadioDescr, false);
            mainDisableWanted = new NativeCheckboxItem(Text.mainDisableWantedTitle, Text.mainDisableWantedDescr, false);
            mainDisableFlight = new NativeCheckboxItem(Text.mainDisableFlightTitle, Text.mainDisableFlightDescr, false);
            mainDisableOnDeath = new NativeCheckboxItem(Text.mainDisableOnDeathTitle, Text.mainDisableOnDeathDescr, true);

            mainMenu.Add(mainScoreCollection);
            mainMenu.Add(mainScoreTrack);
            mainMenu.Add(mainScoreIntensity);
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
            mainScoreIntensity.ItemChanged += OnSelectedScoreIntensity;

            mainMuteSound.CheckboxChanged += OnCheckboxChangedMuteSound;
            mainMuteRadio.CheckboxChanged += OnCheckboxChangedMuteRadio;
            mainDisableWanted.CheckboxChanged += OnCheckboxChangedDisableWanted;
            mainDisableFlight.CheckboxChanged += OnCheckboxChangedDisableFlight;
            mainDisableOnDeath.CheckboxChanged += OnCheckboxChangedDisableOnDeath;

            Tick += OnTick;
            KeyDown += OnKeyDown;

            SetControlFlag(ControlFlags.IsDisableOnDeath);

            if (Tracks.scoreLists.Count > 0 && Tracks.scoreLists[0].Count > 0)
            {
                currentScoreTrack = Tracks.FindTrack(Tracks.scoreLists[0][0].ToString());
            }
        }

        private void OnSelectedScoreCollection(object sender, ItemChangedEventArgs<string> e)
        {
            int collectionIndex = mainScoreCollection.SelectedIndex;

            if (collectionIndex < 0 || collectionIndex >= Tracks.scoreLists.Count)
                return;

            mainScoreTrack = new NativeListItem<string>(
                Text.mainScoreTrackTitle,
                Text.mainScoreTrackDescr,
                Tracks.scoreLists[collectionIndex].ToArray());

            mainMenu.Items[1] = mainScoreTrack;
            mainScoreTrack.ItemChanged += OnSelectedScoreTrack;

            if (Tracks.scoreLists[collectionIndex].Count > 0)
            {
                currentScoreTrack = Tracks.FindTrack(Tracks.scoreLists[collectionIndex][0]);
            }
            else
            {
                currentScoreTrack = null;
            }
        }

        private void OnSelectedScoreTrack(object sender, ItemChangedEventArgs<string> e)
        {
            int collectionIndex = mainScoreCollection.SelectedIndex;

            if (collectionIndex < 0 || collectionIndex >= Tracks.scoreLists.Count)
                return;

            if (e.Index < 0 || e.Index >= Tracks.scoreLists[collectionIndex].Count)
                return;

            currentScoreTrack = Tracks.FindTrack(Tracks.scoreLists[collectionIndex][e.Index]);
        }

        private void OnSelectedScoreIntensity(object sender, ItemChangedEventArgs<string> e)
        {
            SetIntensity();
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

            if (Game.IsControlJustPressed(Controls.PrimaryAction) && IsMenuAvailable())
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
                mainScoreIntensity.Enabled = true;
            }
            else
            {
                mainScoreCollection.Enabled = true;
                mainScoreTrack.Enabled = true;
                mainScoreIntensity.Enabled = false;
            }

            if (HasControlFlag(ControlFlags.IsRadioMuted))
            {
                Game.DisableControlThisFrame(GTA.Control.VehicleRadioWheel);
                Game.DisableControlThisFrame(GTA.Control.VehicleNextRadio);
                Game.DisableControlThisFrame(GTA.Control.VehiclePrevRadio);
                Game.DisableControlThisFrame(GTA.Control.RadioWheelLeftRight);
                Game.DisableControlThisFrame(GTA.Control.RadioWheelUpDown);
            }

#if DEBUG
            if (currentScoreTrack != null)
            {
                GTA.UI.Notification.PostTicker("The current track is: " + currentScoreTrack.Title, false);
            }
#endif

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

        private static void PlayScore()
        {
            if (currentScoreTrack == null)
                return;

            SetControlFlag(ControlFlags.IsScorePlaying);

#if DEBUG
            GTA.UI.Notification.PostTicker("Current track is: " + currentScoreTrack.Title, false);
#endif

            TriggerEvent(currentScoreTrack.Event);

            string supportingEvent = "BTL_IDLE";

            switch (currentScoreTrack.Stems)
            {
                case 3:
                    supportingEvent = Intensities.EventsList3[0];
                    break;
                case 4:
                    supportingEvent = Intensities.EventsList4[0];
                    break;
                case 5:
                    supportingEvent = Intensities.EventsList5[0];
                    break;
                case 6:
                    supportingEvent = Intensities.EventsList6[0];
                    break;
                case 7:
                    supportingEvent = Intensities.EventsList7[0];
                    break;
                case 8:
                    supportingEvent = Intensities.EventsList8[0];
                    break;
            }

            TriggerEvent(supportingEvent);
            mainScoreIntensity.SelectedIndex = 0;
        }

        private static void StopScore()
        {
            ClearControlFlag(ControlFlags.IsScorePlaying);
            TriggerEvent("GTA_ONLINE_STOP_SCORE");
        }

        private static void SetIntensity()
        {
            if (currentScoreTrack == null)
                return;

            if (currentScoreTrack.Stems == 1 || currentScoreTrack.Stems == 2)
                return;

            if (mainScoreIntensity.SelectedIndex < 0 || mainScoreIntensity.SelectedIndex >= mainScoreIntensity.Items.Count)
                return;

            ScoreIntensity intensity = Intensities.FindIntensity(
                mainScoreIntensity.Items[mainScoreIntensity.SelectedIndex].ToString());

            if (intensity == null)
                return;

            int index = Intensities.IntensitiesList.IndexOf(intensity);
            if (index < 0)
                return;

            if (currentScoreTrack.Stems == 5)
                TriggerEvent(Intensities.EventsList5[index]);

            if (currentScoreTrack.Stems == 6)
                TriggerEvent(Intensities.EventsList6[index]);

            if (currentScoreTrack.Stems == 7)
                TriggerEvent(Intensities.EventsList7[index]);

            if (currentScoreTrack.Stems == 8)
                TriggerEvent(Intensities.EventsList8[index]);
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