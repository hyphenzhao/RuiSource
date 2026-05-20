# RuiSource Installation and Deployment Guide

## Local development prerequisites

- .NET 10 SDK / runtime
- Windows with desktop runtime support
- Python virtual environment located at `./venv`
- Required Python packages installed into that venv:
  - `mne`
  - `numpy`
  - `matplotlib`

Install Python packages into the project venv:

```powershell
.\venv\Scripts\python.exe -m pip install mne numpy matplotlib
```

## Running the app locally

1. Keep the repository folder structure intact.
2. Ensure `venv` exists in the repository root.
3. Run the WinForms app from Visual Studio or publish output.
4. TFR uses:
   - `./venv/Scripts/python.exe`
   - `RuiSource/Python/compute_tfr.py`

## Publishing / distributing to other users

### Option 1: distribute app + embedded local python venv

Recommended folder layout after distribution:

```text
RuiSource/
  RuiSource.exe
  ...other published .NET files...
  venv/
    Scripts/python.exe
    Lib/site-packages/mne/...
  Python/
    compute_tfr.py
```

Notes:
- The app expects the Python environment in a sibling `venv` folder relative to the project root logic used in development.
- If publishing to a standalone output folder, keep a matching `venv` folder beside the deployed content and preserve the `Python/compute_tfr.py` script.
- Virtual environments can be machine-specific. For reliable redistribution, recreate the venv on the target machine or package a portable Python runtime.

### Option 2: install Python separately on target machine

On each target machine:

1. Install Python
2. Create venv beside the app
3. Install packages:

```powershell
python -m venv venv
.\venv\Scripts\python.exe -m pip install mne numpy matplotlib
```

4. Copy `compute_tfr.py` into the app's `Python` folder.

## Recommended future improvement for deployment

For easier shipping, consider:

- adding a first-run dependency check
- configurable Python path in app settings
- bundling Python with an installer
- generating a `requirements.txt`
- publishing with MSIX or another Windows installer

## Troubleshooting

If TFR fails:

1. Verify Python exists:

```powershell
.\venv\Scripts\python.exe --version
```

2. Verify packages:

```powershell
.\venv\Scripts\python.exe -m pip show mne numpy matplotlib
```

3. Try running the script manually:

```powershell
.\venv\Scripts\python.exe .\RuiSource\Python\compute_tfr.py .\RuiSource\Python\tfr_input.json
```

4. If a package is missing, install it into the same `venv`.
