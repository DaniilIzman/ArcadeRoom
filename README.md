# Arcade Room — Bachelor's Degree Project

A Unity-based project developed as part of a Bachelor's degree in Mathematics and Computer Science. The game features arcade-style mini-games connected through an interactive 3D arcade room hub, a credit-based economy, an in-game shop, and a full-collection end goal.

---

## Overview

The player explores a first-person 3D arcade room and interacts with arcade cabinets to unlock and launch mini-games. Credits are earned based on in-game performance and can be spent to unlock new machines or to buy collectible trophies from the shop merchant. Once every machine is unlocked and every trophy collected, the player can leave through the exit door to complete the save slot. Galaxy Glide also keeps a full play history, letting the player review past attempts at any time.

---

## Features

- 🕹️ **Arcade mini-games** — Galaxy Glide (flappy-style) and Space Invaders, launched from cabinets in a shared 3D hub
- 🔓 **Unlockable machines** — spend credits to unlock a cabinet before it can be played
- 💰 **Credit economy** — earn credits through gameplay, then spend them on machine unlocks and shop items
- 🏪 **In-game shop** — buy collectible trophies from a merchant NPC; purchases persist per slot
- 📋 **Play history** — Galaxy Glide records the attempt number, date, and score for every run
- 🏁 **Completion goal** — unlock all machines and collect all trophies, then exit through the door to finish the slot (completed slots are badged on the main menu)
- 🔊 **Centralized audio** — every sound is routed through Music, SFX, and UI mixer groups, so the sliders control all audio with no source bypassing them
- ⚙️ **Settings menu** — music / SFX / UI volume, brightness, mouse sensitivity, and resolution, all saved across sessions
- 🌗 **Smooth transitions** — fade-to-black scene loading and a global brightness overlay
- ⏸️ **Pause menu** — accessible mid-game with settings and return-to-menu options
- 💾 **Save slots** — multiple independent slots; credits, unlocks, and completion stored in PlayerPrefs, shop purchases and play history in JSON

---

## Built With

- [Unity 6.3 LTS](https://unity.com/) (6000.3.10f1) — game engine
- C# — scripting
- Unity UI / TextMeshPro — user interface
- Unity Audio Mixer — audio routing and volume control

---

## Project Structure

```
Assets/
    ArcadeRoomGame/                        # main project content
        3rdPartyAssets/                    # third-party assets (referenced by prefabs)
            Dark UI/
            Floreswa/
            SimpleFX/
        ArcadeGames/                       # the three arcade mini-games
            CyberHeist/                    # endless runner
            Flappy/                        # Galaxy Glide (incl. Retro.renderTexture for CRT effect)
            SpaceInvaders/                 # Space Invaders shooter
        ArcadeRoom/                        # first-person hub: cabinets, shop, exit door
            3rdParty/
            materials/
            Prefabs/
            Scenes/
            Scripts/
            Sounds/
            ArcadeMachine_Icon.png         # progress icon (arcade machines)
            Trophy_Icon.png                # progress icon (trophies)
            ArcadeRoomInputs.inputactions  # Input System action map
        MainMenu/                          # title screen, save slot selection
            Audio/
            Materials/
            Resources/                     # contains SettingsManager prefab
            Scenes/
            Scripts/
            Textures/
            eas-vhs SDF.asset              # TMP font asset (VHS-style)
            eas-vhs.ttf                    # source font
        Shared/                            # scripts shared across all scenes
            SettingsManager.cs             # persistent settings + audio routing (single source of truth)
            SceneFader.cs                  # fade-to-black scene transitions
            AmbientAudio.cs                # ambient background audio
            SpatialAudioEmitter.cs         # 3D positional audio
        MainMixer.mixer                    # single Unity Audio Mixer (all sound routed here)
    Settings/                              # URP render pipeline + post-processing
        PC_RPAsset.asset / PC_Renderer.asset
        Mobile_RPAsset.asset / Mobile_Renderer.asset
        DefaultVolumeProfile.asset / SampleSceneProfile.asset
        UniversalRenderPipelineGlobalSettings.asset
    TextMesh Pro/                          # imported TMP package resources
```

---

## Academic Context

Developed as a Bachelor's degree project at the University of Łódź, Faculty of Mathematics and Computer Science. The project demonstrates practical applications of object-oriented programming, game architecture, persistent singletons, UI systems, and centralized audio management within a real-time interactive environment.
