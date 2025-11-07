# 🎨 ACES 2.0 Tonemapper for Unity (URP)

[![Unity](https://img.shields.io/badge/Unity-6.0%2B-blue.svg)](https://unity.com/)
[![URP](https://img.shields.io/badge/URP-17%2B-green.svg)](https://docs.unity3d.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE.md)
[![OpenColorIO](https://img.shields.io/badge/ACES-2.0-orange.svg)](https://github.com/AcademySoftwareFoundation/OpenColorIO-Config-ACES)

A modern **ACES 2.0**-style tonemapper for Unity’s **Universal Render Pipeline (URP)**.  
This package adds an updated, physically-accurate color transform based on the latest **ACES 2.0 OpenColorIO** configuration.

---

## ✨ Features

- ✅ Physically-based **ACES 2.0 tonemapping**
- ⚙️ **Automatic setup** via **Tools → ACES2 Setup**
- 🎛️ Adjustable LUT contribution, size, and gamma options
- 🧩 Works as a **URP Render Feature**
- ⚡ Fully compatible with **Volume Profiles**
- 🧱 Supports baked `.spi3d`, `.cube`, and Unity `Texture3D` LUTs
- 💡 Tested with **Unity 6 / URP 17+**

> ⚠️ Requires a **Universal Render Pipeline** project.  
> Built-in and HDRP are not supported.

---

## 📦 Installation

### Option 1 — Unity Package Manager (Recommended)

1. Open **Unity → Window → Package Manager**
2. Click the **+** icon → “Add package from Git URL...”
3. Paste this URL: https://github.com/SNIELSEL/UnityURP-ACES2-Tonemapper.git
4. Click **Add** — Unity will download and import the package automatically.

---

## 🚀 Quick Setup (Recommended)

### 🧠 Automatic Setup via Menu

You no longer need to manually configure anything — just let the setup script do it for you:

1. In the Unity toolbar, go to:  
**Tools → ACES2 Setup**
2. The setup tool will:
- Automatically detect your **URP Renderer Asset**
- Add both required render features:
  - **Custom Tonemapper Renderer Feature**
  - **Full Screen Pass Renderer Feature**
- Assign the correct **Tonemapper Material** and default **ACES2 LUT**
3. Once complete, you’ll see a confirmation popup:  
*“Render Features added/updated successfully.”*

✅ Done! You now have a fully functional ACES2 Tonemapper integrated into your URP renderer.

> 💡 The setup tool configures your URP renderer automatically —  
> you only need to assign your **Volume** and **LUT** once.

---

### 🎚️ Final Step — Add the Tonemapper to Your Volume

1. Create or open a **Global Volume** in your scene (e.g. `PostProcessVolume`).
2. Add the **Custom Tonemapper (ACES2)** override from the *Post-processing* category.
3. The tonemapper is now active and fully driven by your **Volume Profile**.

> 💡 If this is your first time setting up, make sure the **ACES2 Texture3D LUT** and **Tonemapper Material** are correctly assigned in your Volume.  
> You can find these default assets in the **Samples → ACES2** folder.

Once assigned, you can adjust:
- **Contribution** → blend strength (0–1)
- **LUT Output Is SRGB** → toggle based on LUT type
- **LUT Size** → usually `33` or `65`

Disabling the override cleanly reverts rendering to un-tonemapped output with no gray-screen or buffer issues.

✅ **Verification Tip:**  
Try lowering the **Contribution** slider in your Volume — your scene’s contrast and brightness should change immediately.

---

## 🛠️ Manual Setup (if you prefer)

If you prefer to configure it yourself instead of using the auto-setup:

### 1️⃣ Add Render Features

In your **URP Renderer** asset:
1. Open your renderer (e.g., `ForwardRenderer.asset` or your custom one).
2. In the **Renderer Features** list, click **Add Renderer Feature** twice:
- Add **Custom Tonemapper Renderer Feature**
- Add **Full Screen Pass Renderer Feature**

It should look like this:

![URP Renderer Features](https://github.com/SNIELSEL/UnityURP-ACES2-Tonemapper/blob/main/Images/urp%20renderer%20features.jpg)

> Both features are required — the first applies the tonemapping, and the second binds it to the volume system.

---

### 2️⃣ Add a Volume

1. Create an empty GameObject named **PostProcessVolume**.
2. Add a **Volume** component and set it to *Global*.
3. Add **Custom Tonemapper (ACES2)** from the *Post-processing* category.

---

### 3️⃣ Assign the Material

1. In the **Custom Tonemapper** volume component:
- Drag in the **MAT_ACES2_Fullscreen** material (included in the samples).
- Optionally assign a custom **ACES 2.0 LUT (Texture3D)**.

2. Adjust settings:
- **Contribution** → blend strength (0–1)
- **LUT Output Is SRGB** → toggle based on LUT type
- **LUT Size** → typically `33` or `65`

---

## 🧰 Samples

You can import assets for reference or quick setup.

**Package Manager → ACES 2 Tonemapper (URP) → Samples → Import**

You’ll get:
- **Volume Profile**
- **Tonemapper Material**
- **LUT Texture3D**
- **Test Scene**

These can be used as-is or as templates for your own configuration.

---

## 🎨 Using Your Own LUTs

You can easily use your own color transform baked from **ACES**, **DaVinci Resolve**, **Nuke**, or **OpenColorIO**.

### Supported formats:
- `.spi3d` (Sony Imageworks 3D LUT)
- `.cube` (DaVinci Resolve / LUTCalc)
- `.asset` (Unity `Texture3D` asset)

Simply drag the LUT into the **Aces 2 LUT** slot in your Volume profile.

---

## 🧪 Baking a LUT from OpenColorIO

If you have an ACES `.ocio` config (e.g., from [OpenColorIO-Config-ACES](https://github.com/AcademySoftwareFoundation/OpenColorIO-Config-ACES)),  
you can bake your own LUT using the `ociobakelut` tool:

```powershell
& "C:\Tools\OpenColorIO\bin\ociobakelut.exe" `
--iconfig "[Path to .ocio]" `
--inputspace "ACEScg" `
--displayview "sRGB - Display" "ACES 2.0 - SDR 100 nits (Rec.709)" `
--format iridas_cube --cubesize 33 `
"YourOutputPath\aces2_sdr709_33.cube"
