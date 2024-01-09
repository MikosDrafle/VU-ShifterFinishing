﻿using System;
using System.Collections;
using Microsoft.Xna.Framework;
using MonoMod.ModInterop;

namespace Celeste.Mod.Verillia.Utils {
    public class VerilliaUtilsModule : EverestModule {
        public static VerilliaUtilsModule Instance { get; private set; }

        public override Type SettingsType => typeof(VerilliaUtilsModuleSettings);
        public static VerilliaUtilsModuleSettings Settings => (VerilliaUtilsModuleSettings) Instance._Settings;

        public override Type SessionType => typeof(VerilliaUtilsModuleSession);
        public static VerilliaUtilsModuleSession Session => (VerilliaUtilsModuleSession) Instance._Session;

        public VerilliaUtilsModule() {
            Instance = this;
#if DEBUG
            // debug builds use verbose logging
            Logger.SetLogLevel(nameof(VerilliaUtilsModule), LogLevel.Verbose);
#else
            // release builds use info logging to reduce spam in log files
            Logger.SetLogLevel(nameof(VerilliaUtilsModule), LogLevel.Info);
#endif
        }

        public override void Load() {
            typeof(VerilliaUtilsExports).ModInterop(); // TODO: delete this line if you do not need to export any functions

            On.Celeste.LevelLoader.ctor += LevelLoader_ctor;
            On.Celeste.OverworldLoader.ctor += OverworldLoader_ctor;
            On.Celeste.Player.ctor += Player_ctor;
            Everest.Events.Player.OnRegisterStates += Player_addStates;
        }

        public override void Unload() {
            On.Celeste.LevelLoader.ctor -= LevelLoader_ctor;
            On.Celeste.OverworldLoader.ctor -= OverworldLoader_ctor;
            On.Celeste.Player.ctor -= Player_ctor;
            Everest.Events.Player.OnRegisterStates -= Player_addStates;

            // TODO: unapply any hooks applied in Load()
        }

        public void LoadBeforeLevel() {
            

            // TODO: apply any hooks that should only be active while a level is loaded
        }

        public void UnloadAfterLevel() {
            

            // TODO: unapply any hooks applied in LoadBeforeLevel()
        }

        private void OverworldLoader_ctor(On.Celeste.OverworldLoader.orig_ctor orig, OverworldLoader self, Overworld.StartMode startmode, HiresSnow snow) {
            orig(self, startmode, snow);
            if (startmode != (Overworld.StartMode) (-1)) {
                UnloadAfterLevel();
            }
        }

        private void LevelLoader_ctor(On.Celeste.LevelLoader.orig_ctor orig, LevelLoader self, Session session, Vector2? startposition) {
            orig(self, session, startposition);
            LoadBeforeLevel();
        }

        private void Player_ctor(On.Celeste.Player.orig_ctor orig, Player self, Vector2 pos, PlayerSpriteMode spriteMode)
        {
            self.Add(new VerilliaUtilsPlayerExt());
            orig(self, pos, spriteMode);
        }

        private void Player_addStates(Player player)
        {
            VerilliaUtilsPlayerExt extension = player.Components.Get<VerilliaUtilsPlayerExt>();
            player.Components.Get<VerilliaUtilsPlayerExt>().RailBoostState = player.AddState(
                "VUK-RailBoost",
                extension.RailBoostUpdate,
                extension.RailBoostCoroutine,
                extension.GenericStartState);
        }
    }
}