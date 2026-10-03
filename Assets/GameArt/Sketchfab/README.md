# Sketchfab Third-Party Assets & Attribution Pipeline

This directory holds external 3D models and textures imported from **Sketchfab** for production use.

---

## 📂 Folder Organization

Each model should live in its own sub-folder with its raw files and a `TXT_Credit.json` file:

```text
Assets/GameArt/Sketchfab/
├── _Template/
│   └── TXT_Credit.json                 # Copy this template to new asset folders
├── Old Couch/
│   ├── TXT_Credit.json                 # Attribution metadata for this asset
│   ├── FBX_OldCouch.fbx
│   └── TEX_OldCouch.png
├── Desert_Shack_Radio/
│   ├── TXT_Credit.json
│   ├── SM_Radio_Ham.fbx
│   └── Textures/
└── CREDITS_ALL.md                      # Auto-generated master credits list!
```

---

## 📝 How to Add a New Asset

1. Create a folder under `Assets/GameArt/Sketchfab/` named after the asset (e.g., `Old Couch`).
2. Copy `_Template/TXT_Credit.json` into that folder (or right-click the folder in Unity's Project view -> **Create > Sketchfab Credit File**).
3. Fill in the fields:
   * **assetName**: The title on Sketchfab.
   * **author**: The creator's username.
   * **authorUrl**: Link to their profile.
   * **sourceUrl**: Direct URL of the model.
   * **license**: `CC-BY-4.0`, `Creative Commons Attribution`, etc.
   * **modifications**: Brief note on changes made (e.g. "Optimized mesh, converted textures to URP Lit.").
   * **category**: e.g., `Environment / Props`, `Vehicle`, `Weapons`.
   * **dateAdded**: Current date (`YYYY-MM-DD`).

---

## 🚀 One-Click Export to Final Game Credits

You never have to hunt down or manually copy-paste credits across dozens of folders!

In the Unity Editor menu bar:
* Click **Tools > Sketchfab Credits > Export All Credits to File**
  *(Compiles every `TXT_Credit.json` into `Assets/GameArt/Sketchfab/CREDITS_ALL.md`)*
* Click **Tools > Sketchfab Credits > Copy All Credits to Clipboard**
  *(Instantly copies formatted credits directly to your clipboard for Steam, itch.io, or in-game credits UI!)*
