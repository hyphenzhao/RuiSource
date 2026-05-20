# RuiSource

RuiSource is a Windows desktop application for BESA-like epilepsy ictal/interictal source localisation.

## Goals
- Support workflows for epilepsy source localisation
- Keep the project Python-compatible where practical
- Reuse or align with selected functionality from MNE-Python when appropriate
- Provide an interactive EEG review interface with analysis functions for selected time ranges

## Current status and features

RuiSource is currently a .NET 10 WinForms application with early EEG viewing and analysis tools.

Implemented features:

- Load EDF files
- Display EEG channels in a 10-second time window
- Scroll through channels from the left label area
- Scroll through time from the signal plotting area
- Adjust signal voltage scale with `Ctrl + mouse wheel`
- Select a time range by dragging in the EEG plotting area
- Configure visible channels and rename channels through `Channels...`
- Apply display filters:
  - low cut
  - high cut
  - notch
- Compute PSD on selected EEG segments using Welch PSD
- Compute TFR on selected EEG segments using Python/MNE Morlet TFR
- Native C# rendering for PSD and TFR results
- Multi-channel TFR viewer with per-channel magnifying view
- Right-side source-localisation preparation previews:
  - matched standard EEG electrode 3D positions
	- Python/MNE-rendered source-space brain/electrode preview image
  - native fallback standard brain/head preview when Python rendering is unavailable
- Automatic case-insensitive matching of loaded EEG channel names to standard electrode names
- Configurable Python executable and Python scripts folder through `Configure...`

## Technology stack

- .NET 10
- Windows Forms
- C# for UI, EDF loading, plotting, PSD display, and native TFR rendering
- Python for MNE-backed TFR computation
- MNE-Python, NumPy, and Matplotlib in a local Python environment

## Python integration

Python is currently used for Morlet TFR computation and MNE-backed source preview image rendering. C# passes selected EEG data or channel names to Python, Python computes numeric/preview outputs, and C# renders the result in the desktop UI.

Expected Python environment:

```powershell
.\venv\Scripts\python.exe
```

Install required Python packages:

```powershell
.\venv\Scripts\python.exe -m pip install mne numpy matplotlib
```

The Python TFR script is located at:

```text
RuiSource/Python/compute_tfr.py
```

The Python source preview script is located at:

```text
RuiSource/Python/render_source_preview.py
```

After an EDF file is loaded, RuiSource calls this script to render a static MNE `standard_1020` source-space preview PNG containing matched EEG electrodes and a brain/head reference. If Python, MNE, or matplotlib is unavailable, EDF loading still succeeds and the app keeps a native fallback preview with an explanatory message.

If Python cannot be found, use the app menu:

```text
Configure...
```

and set:

- Python executable path
- Python scripts folder containing `compute_tfr.py`

## Basic usage

1. Start the app.
2. Open an EDF file from:

```text
File -> Open EDF
```

3. Use the mouse wheel:
   - over the left channel-label area to scroll channels
   - over the EEG plot area to move through time
   - with `Ctrl` over the EEG plot area to change voltage scale
4. Drag across the EEG plot area to select a time segment.
5. Use analysis buttons that appear after selecting a range:
   - `Compute PSD`
   - `Compute TFR`

After an EDF file is loaded, the right-side source-localisation preview area is enabled. RuiSource attempts to match loaded EEG channel names to standard electrode names while ignoring case and common prefixes/suffixes such as `EEG`, `REF`, `LE`, and `RE`.

## Menus

- `File`
  - Open EDF
- `Configure...`
  - Set Python executable and Python scripts folder
- `Channels...`
  - Choose visible channels
  - Rename channels
  - Reverse channel selection
- `Filters...`
  - Configure low cut, high cut, and notch filter settings

## Development setup

Recommended local setup:

```powershell
git clone https://github.com/hyphenzhao/RuiSource.git
cd RuiSource
python -m venv venv
.\venv\Scripts\python.exe -m pip install mne numpy matplotlib
```

Open the solution/project in Visual Studio and build/run the WinForms project.

## Repository hygiene

The repository intentionally ignores:

- Visual Studio local files
- .NET build outputs
- Python virtual environments
- local test folders
- EDF data files
- generated TFR input/output files
- local app configuration

See `.gitignore` for details.

## Deployment notes

For deployment, the app needs both:

- published .NET application files
- a Python environment with MNE/NumPy/Matplotlib available

See `DEPLOYMENT.md` for packaging guidance.

Recommended deployed layout:

```text
RuiSource/
  RuiSource.exe
  ...published .NET files...
  venv/
	Scripts/python.exe
	Lib/site-packages/...
  Python/
	compute_tfr.py
```

If the deployed folder layout differs, configure paths inside the app using `Configure...`.

## Development notes
- Python compatibility is a project requirement
- Python/MNE should be used where it improves correctness or compatibility with established EEG/MEG workflows
- C# should own interactive UI rendering where possible for a consistent desktop experience
- Future source-localisation features should remain interoperable with Python-based workflows
