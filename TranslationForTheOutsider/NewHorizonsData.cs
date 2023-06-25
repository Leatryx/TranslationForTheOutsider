﻿// Downloaded from https://github.com/Outer-Wilds-New-Horizons/new-horizons/blob/cb08f4710d184b778bb4d92e995ba264186744bb/NewHorizons/External/NewHorizonsData.cs and edited.

using System;
using System.Collections.Generic;

namespace NewHorizons.External {
    public static class NewHorizonsData {
        private static NewHorizonsSaveFile _saveFile;
        private static NewHorizonsProfile _activeProfile;
        private static string _activeProfileName;
        private static readonly string FileName = "save.json";

        private static object _lock = new();

        // This is its own method so it can be patched by NH-QSB compat
        public static string GetProfileName() => StandaloneProfileManager.SharedInstance?.currentProfile?.profileName;

        public static void Load() {
            lock (_lock) {
                _activeProfileName = GetProfileName();
                if (_activeProfileName == null) {
                    TranslationForTheOutsider.TranslationForTheOutsider.Instance.Log("Couldn't find active profile, are you on Gamepass?", OWML.Common.MessageType.Warning);
                    _activeProfileName = "XboxGamepassDefaultProfile";
                }

                try {
                    _saveFile = TranslationForTheOutsider.TranslationForTheOutsider.Instance.ModHelper.Storage.Load<NewHorizonsSaveFile>(FileName, false);
                    if (!_saveFile.Profiles.ContainsKey(_activeProfileName))
                        _saveFile.Profiles.Add(_activeProfileName, new NewHorizonsProfile());
                    _activeProfile = _saveFile.Profiles[_activeProfileName];
                    TranslationForTheOutsider.TranslationForTheOutsider.Instance.Log($"Loaded save data for {_activeProfileName}");
                }
                catch (Exception) {
                    try {
                        TranslationForTheOutsider.TranslationForTheOutsider.Instance.Log($"Couldn't load save data from {FileName}, creating a new file");
                        _saveFile = new NewHorizonsSaveFile();
                        _saveFile.Profiles.Add(_activeProfileName, new NewHorizonsProfile());
                        _activeProfile = _saveFile.Profiles[_activeProfileName];
                        TranslationForTheOutsider.TranslationForTheOutsider.Instance.ModHelper.Storage.Save(_saveFile, FileName);
                        TranslationForTheOutsider.TranslationForTheOutsider.Instance.Log($"Loaded save data for {_activeProfileName}");
                    }
                    catch (Exception e) {
                        TranslationForTheOutsider.TranslationForTheOutsider.Instance.Log($"Couldn't create save data:\n{e}", OWML.Common.MessageType.Error);
                    }
                }
            }
        }

        public static void Save() {
            if (_saveFile == null) return;

            // Threads exist
            lock (_lock) {
                try {
                    TranslationForTheOutsider.TranslationForTheOutsider.Instance.ModHelper.Storage.Save(_saveFile, FileName);
                }
                catch (Exception ex) {
                    TranslationForTheOutsider.TranslationForTheOutsider.Instance.Log($"Couldn't save data:\n{ex}", OWML.Common.MessageType.Error);
                }
            }
        }

        public static void Reset() {
            if (_saveFile == null || _activeProfile == null) Load();
            TranslationForTheOutsider.TranslationForTheOutsider.Instance.Log($"Resetting save data for {_activeProfileName}");
            _activeProfile = new NewHorizonsProfile();
            _saveFile.Profiles[_activeProfileName] = _activeProfile;

            Save();
        }

        private class NewHorizonsSaveFile {
            public NewHorizonsSaveFile() {
                Profiles = new Dictionary<string, NewHorizonsProfile>();
            }

            public Dictionary<string, NewHorizonsProfile> Profiles { get; }
        }

        private class NewHorizonsProfile {
            public NewHorizonsProfile() {
                KnownFrequencies = new List<string>();
                KnownSignals = new List<string>();
                NewlyRevealedFactIDs = new List<string>();
                PopupsRead = new List<string>();
                CharactersTalkedTo = new List<string>();
            }

            public List<string> KnownFrequencies { get; }
            public List<string> KnownSignals { get; }
            public List<string> NewlyRevealedFactIDs { get; }
            public List<string> PopupsRead { get; }
            public List<string> CharactersTalkedTo { get; }
        }

        #region Frequencies

        public static bool KnowsFrequency(string frequency) {
            if (_activeProfile == null) return true;
            return _activeProfile.KnownFrequencies.Contains(frequency);
        }

        public static void LearnFrequency(string frequency) {
            if (_activeProfile == null) return;
            if (!KnowsFrequency(frequency)) {
                _activeProfile.KnownFrequencies.Add(frequency);
                Save();
            }
        }

        public static void ForgetFrequency(string frequency) {
            if (_activeProfile == null) return;
            if (KnowsFrequency(frequency)) {
                _activeProfile.KnownFrequencies.Remove(frequency);
                Save();
            }
        }

        public static bool KnowsMultipleFrequencies() {
            return _activeProfile != null && _activeProfile.KnownFrequencies.Count > 0;
        }

        #endregion

        #region Signals

        public static bool KnowsSignal(string signal) {
            if (_activeProfile == null) return true;
            return _activeProfile.KnownSignals.Contains(signal);
        }

        public static void LearnSignal(string signal) {
            if (_activeProfile == null) return;
            if (!KnowsSignal(signal)) {
                _activeProfile.KnownSignals.Add(signal);
                Save();
            }
        }

        #endregion

        #region Newly Revealed Facts

        public static void AddNewlyRevealedFactID(string id) {
            _activeProfile?.NewlyRevealedFactIDs.Add(id);
            Save();
        }

        public static List<string> GetNewlyRevealedFactIDs() {
            return _activeProfile?.NewlyRevealedFactIDs;
        }

        public static void ClearNewlyRevealedFactIDs() {
            _activeProfile?.NewlyRevealedFactIDs.Clear();
            Save();
        }

        #endregion

        #region Read popups

        public static void ReadOneTimePopup(string id) {
            _activeProfile?.PopupsRead.Add(id);
            Save();
        }

        public static bool HasReadOneTimePopup(string id) {
            // To avoid spam, we'll just say the popup has been read if we can't load the profile
            return _activeProfile?.PopupsRead.Contains(id) ?? true;
        }

        #endregion

        #region Characters talked to

        public static void OnTalkedToCharacter(string name) {
            if (name == CharacterDialogueTree.RECORDING_NAME || name == CharacterDialogueTree.SIGN_NAME) return;
            _activeProfile?.CharactersTalkedTo.SafeAdd(name);
            Save();
        }

        public static bool HasTalkedToFiveCharacters() {
            if (_activeProfile == null) return false;
            return _activeProfile.CharactersTalkedTo.Count >= 5;
        }

        public static int GetCharactersTalkedTo() {
            if (_activeProfile == null) return 0;
            return _activeProfile.CharactersTalkedTo.Count;
        }

        #endregion
    }
}
