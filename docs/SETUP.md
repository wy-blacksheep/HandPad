# Setup

## Web applications

Tested server environment: Python 3.12 on Windows. 

From the repository root, in PowerShell:

```powershell
python -m venv web/.venv
web/.venv/Scripts/python.exe -m pip install -r web/requirements.txt
web/.venv/Scripts/python.exe web/app.py
```

The server binds to `127.0.0.1:5000` with debug mode off. 
Open `/quiz`, `/canvas2`, `/map`, `/tutorial`, or `/page/tutorial2`. 
Clipboard and drag state are shared across windows in one server process, matching the single-participant study workflow. Do not use one instance for concurrent participants.

An optional `HANDPAD_SECRET_KEY` environment variable sets a stable session key; otherwise an ephemeral key is generated.

Run the server checks with:

```powershell
web/.venv/Scripts/python.exe -m unittest discover -s web/tests -v
```

External scripts (p5.js and jQuery) and images require internet access. 

## Unity

1. Install Unity **2022.3.22f1** through Unity Hub and add the `unity/` folder as a project.
2. Let Unity restore `Packages/manifest.json` and `packages-lock.json`. The manifest requests Meta XR All-in-One SDK **67.0.0** and Oculus XR **4.2.0**. Keep the lockfile.
3. Install a properly licensed **Vuplex 3D WebView for Windows and macOS** into `Assets/Vuplex`. 
4. Install the licensed Syncfusion PMML evaluator and preserve its dependencies. The expected assembly is `Syncfusion.PMML.Portable.dll` (namespace `Syncfusion.PMML`).
5. Connect a Meta Quest Pro using the PC VR setup used for the study and enable hand tracking. 
6. Start Flask on the same PC and open `Assets/Scenes/HandGestureScene.unity`.
