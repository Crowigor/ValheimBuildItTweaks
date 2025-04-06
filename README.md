# BuildIt Tweaks

**BuildIt Tweaks** is a lightweight configuration companion for the [BuildIt](https://valheim.thunderstore.io/package/RockerKitten/BuildIt) mod by **RockerKitten**.

It allows server administrators to customize and control which pieces from BuildIt (prefabs starting with `rk_`) are available in-game — with full server-side configuration sync and support for runtime tweaks.

---

## ✨ Features

- 🔧 Enable or disable individual BuildIt pieces
- 🧱 Set custom piece categories (e.g. Misc, Furniture, Crafting, etc.)
- 🛠️ Assign required crafting stations per piece
- 📦 Customize build costs with item:amount format
- 🌐 Fully synced via **ServerSync**
- 🔁 Support for **dynamic config reloads** — no relog required
- ⚙️ Integrates with **BepInEx ConfigurationManager** (Azumatt’s fork recommended)

---

## 📦 Credits

- **Original mod**: [BuildIt by RockerKitten](https://valheim.thunderstore.io/package/RockerKitten/BuildIt)
- **Tweaks, logic, and configuration**: [Crowigor](https://github.com/Crowigor)

---

## 📂 Config Location

The config file is auto-generated and located at:
`BepInEx/config/com.crowigor.buildittweaks.cfg`

All piece settings are grouped by prefab name for clarity.

---

## ⚠️ Known Issues

- After enabling or disabling a piece, **Valheim needs to be restarted** to update the hammer menu properly.
  - 🔄 Categories and station filters are applied immediately.
  - 🪚 However, the in-game UI cache only refreshes **on relog or game restart**.
  - 💡 Tip: Configure all required pieces first, then restart the game for changes to take full effect.

We’re investigating a way to trigger the hammer menu refresh at runtime. Stay tuned!