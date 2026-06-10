# Arcade Room — Bachelor's Degree Project

A Unity-based project developed as part of a Bachelor's degree in Mathematics and Computer Science. The game features two arcade-style mini-games connected through an interactive 3D arcade room hub, a credit-based economy, and a small in-game shop.

---

## Overview

The player explores a virtual arcade room and interacts with arcade machines to launch mini-games. Credits are earned based on in-game performance and can be spent in the shop. The Galaxy Glide machine tracks full play history, letting the player review past attempts at any time.

---

## Features

- 🕹️ **Two arcade mini-games** — Galaxy Glide and Space Invaders, accessible from a shared 3D hub
- 💰 **Credit economy** — earn credits through gameplay, spend them in the shop
- 🏪 **In-game shop** — purchase items or upgrades using earned credits
- 📋 **Play history** — Flappy Bird tracks attempt number, date, and score for each run
- 🔊 **Full audio system** — separate music, SFX, and UI mixer channels with persistent volume settings
- ⚙️ **Settings menu** — audio sliders and resolution dropdown with saved preferences
- ⏸️ **Pause menu** — accessible mid-game with settings and return-to-menu options
- 💾 **Save slots** — per-slot credit and history persistence via PlayerPrefs

---

## Built With

- [Unity](https://unity.com/) — game engine
- C# — scripting
- Unity UI / TextMeshPro — user interface
- Unity Audio Mixer — audio routing and volume control
- Universal Render Pipeline (URP) — rendering

---

## Project Structure

```
Assets/
├── Scenes/
│   ├── ArcadeRoom          # main hub scene
│   ├── FlappyMenu          # flappy bird menu
│   ├── FlappyLevel         # flappy bird gameplay
│   ├── SpaceInvadersMenu   # space invaders menu
│   ├── SpaceInvadersLevel  # space invaders gameplay
│   └── ...
├── Scripts/
│   ├── FlappyGameManager.cs
│   ├── FlappyMenuController.cs
│   └── ...
├── Audio/
├── Prefabs/
└── UI/
```

---

## Academic Context

Developed as a Bachelor's degree capstone project in Mathematics and Computer Science. The project demonstrates practical applications of object-oriented programming, game architecture, UI systems, and audio management within a real-time interactive environment.
