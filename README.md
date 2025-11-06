# 🎨 ACES 2.0 Tonemapper for Unity (URP)

A modern **ACES 2.0**-style tonemapper for Unity's Universal Render Pipeline (URP).  
This package adds an updated, physically-accurate color transform based on the latest ACES 2.0 OpenColorIO configuration.

---

## ✨ Features

- ✅ Physically-based **ACES 2.0 tonemapping**
- 🎛️ Adjustable LUT contribution, size, and gamma options
- 🧩 Works as a **URP Render Feature**
- ⚡ Lightweight and compatible with **Volume Profiles**
- 🧱 Supports baked `.spi3d`, `.cube`, or Unity `Texture3D` LUTs

---

## 📦 Installation

### Option 1 — Unity Package Manager (Recommended)

1. Open **Unity → Window → Package Manager**  
2. Click the **+** icon → “Add package from Git URL...”
3. Paste this URL: https://github.com/SNIELSEL/UnityURP-ACES2-Tonemapper.git
4. Click **Add**  
Unity will fetch and import the package automatically.

---

## ⚙️ Setup

### 1️⃣ Add Render Features

In your **URP Renderer** asset:
1. Open your URP renderer (e.g., `ForwardRenderer.asset` or your custom renderer).
2. In the **Renderer Features** list, click **Add Renderer Feature** twice and add:
- `Custom Tonemapper`
- `Fullscreen Pass`

It should look like this:

![URP Renderer Features](https://github.com/SNIELSEL/UnityURP-ACES2-Tonemapper/blob/main/Images/urp%20renderer%20features.jpg)

> Both features are required — the first applies tonemapping, the second binds it to the Volume system.

---

### 2️⃣ Add a Volume

1. Create an empty GameObject and name it **PostProcessVolume**.
2. Add a **Volume** component and set it to *Global*.
3. Add **Custom Tonemapper (ACES2)** from the “Post-processing” category.

---

### 3️⃣ Assign the Material

1. In the **Custom Tonemapper** volume component:
- Drag in your **Tonemapper Material** (from the package or your own).
- Optionally, assign a custom **ACES 2.0 LUT (Texture3D)**.

2. Adjust:
- **Contribution** → blend strength (0–1)
- **LUT Output Is SRGB** → toggle based on your LUT output
- **LUT Size** → usually `33` or `65`

---

## 🧰 Optional: Using Samples

If you imported the package samples via  
**Package Manager → Samples → Import**,  
you’ll get:
- A ready-made **Renderer Asset** setup with both features
- Example **Volume Profile**
- Example **Tonemapper Material**

Perfect for quick testing or reference!

---

## 🎨 Using Your Own LUTs

You can use your own color transform baked from **ACES**, **DaVinci Resolve**, **Nuke**, or **OCIO**.

### Supported formats:
- `.spi3d` (Sony Imageworks 3D LUT)
- `.cube` (DaVinci Resolve / LUTCalc)
- `.asset` (Unity `Texture3D` asset)

---

### 🧪 Baking a LUT from OpenColorIO (optional)

If you have an ACES `.ocio` config (e.g. from [OpenColorIO-Config-ACES](https://github.com/AcademySoftwareFoundation/OpenColorIO-Config-ACES)),  
you can bake a LUT using the `ociobakelut` tool:

```powershell
& "C:\Tools\OpenColorIO\bin\ociobakelut.exe" `
  --iconfig "C:\Users\offic\Downloads\zanderlabs\Unity\Ocio\aces2.ocio" `
  --inputspace "ACEScg" `
  --displayview "sRGB - Display" "ACES 2.0 - SDR 100 nits (Rec.709)" `
  --format iridas_cube --cubesize 33 `
  "C:\Users\offic\Downloads\zanderlabs\Unity\Ocio\aces2_sdr709_33.cube"

