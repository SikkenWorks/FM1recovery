# FM1 Recovery by SikkenWorks

**Version 1.0k · Windows 10/11, 64-bit**

A Windows recovery application for the **M-VAVE FM1**, with a guided Simple Mode and a complete Advanced Mode. It restores the original **V15 firmware** through the FM1's WL82 USB recovery interface.

Application interface, Windows integration and guided workflow by **Joppe Sikken / SikkenWorks**: [SikkenWorks on GitHub](https://github.com/SikkenWorks).

The recovery engine and low-level communication tools were created by the developers credited below. This application builds on their work.

## Features

- **Simple Mode:** administrator check, OCT-minus connection instructions, automatic device discovery, typed confirmation and restart instructions.
- **Advanced Mode:** device selection, Windows device details, connection diagnostics, activity log and diagnostic report export.
- **About / Credits** available in both modes.
- Embedded original V15 firmware, Windows Python runtime and recovery tools.
- Firmware and loader integrity checks, mandatory verified full-flash backup and verification after writing.
- No separate Python installation or firmware file selection required.
- No automatic privilege elevation on startup. Simple Mode checks administrator access and explains how to reopen the app manually if needed.

## Before starting

Use a USB **data** cable and a direct USB connection to your PC. Close other firmware tools.

Recovery replaces the application firmware and may affect saved sounds and settings. Keep the FM1 powered and USB connected until recovery and verification have finished.

**If Windows offers to format the recovery disk, click Cancel. Do not copy files onto it or replace its driver.**

## Simple Mode

1. Open the application and choose **Simple Mode**.
2. At **Are you running this as Administrator?**, follow the instructions. If needed, close the app, right-click its `.exe`, choose **Run as administrator**, accept the Windows prompt and select Simple Mode again.
3. Switch the FM1 off. Hold **OCT-minus (OCT−)** while switching it on, connect USB and release the button. The FM1 screen may remain black; this is expected in recovery mode.
4. Click **Next**. Wait while the application searches for your FM1. It retries automatically after each completed search. If several FM1s are found, disconnect the others.
5. Once your FM1 is found, type **RESTORE** and click **Restore**.
6. Wait for the backup, firmware write and verification to complete.
7. After verified success, unplug USB, switch the FM1 off, wait five seconds and switch it back on.

This restores stock V15. Install your preferred custom firmware separately afterwards.

## Advanced Mode

Choose **Advanced Mode** at startup to use the full interface:

- **Restore FM1:** find and select the recovery device, acknowledge the firmware replacement, type `RESTORE` and confirm the operation.
- **Device help:** read Windows device information automatically or enter Device Manager details for diagnosis.
- **Activity log:** inspect progress, export a diagnostic report or open the backup/log folder.
- **About:** view credits and access bundled license notices.

Manual hardware IDs and disk numbers do not bypass live device checks or authorize writing to an arbitrary disk.

## Troubleshooting

**FM1 not found:** repeat the OCT-minus startup, try another USB data cable and use another direct USB port. Allow Windows time to register the device. In recovery mode it commonly appears under Disk drives as `WL82 UBOOT1.00 USB Device`; it is not expected to appear as a MIDI port.

**Access denied:** close the app and reopen it using **Run as administrator**.

**Still not found:** open Advanced Mode, use **Device help**, then select **Save diagnostic report**. Include the exact Device Manager name and any Windows device error.

**Recovery stops or verification fails:** keep the FM1 powered and connected, preserve the log and request help. Do not assume recovery succeeded.

Backups and logs are stored in:

```text
%LOCALAPPDATA%\SikkenWorks\FM1-Recovery
```

Keep full-flash backups private unless you intentionally choose to share them.

## Credits and acknowledgements

### Recovery engine

**Michael P Jones / Quixotic7 — MvaveFM1Unbricker**

The foundation of the recovery workflow: firmware extraction, chip and flash checks, full-flash backup, restoration and verification. SikkenWorks adds the Windows interface, embedded packaging and adapted Windows discovery/integration.

- [Repository](https://github.com/Quixotic7/MvaveFM1Unbricker)
- [Upstream credits and documentation](https://github.com/Quixotic7/MvaveFM1Unbricker/blob/main/README.md)
- [MIT license](https://github.com/Quixotic7/MvaveFM1Unbricker/blob/main/LICENSE)
- Copyright © 2026 Michael P Jones.

### Low-level WL82 communication

**Andrey Grigoryev / kagaimiq — jl-uboot-tool**

Provides the Jieli WL82 communication and loader tooling used by the recovery engine.

- [Repository](https://github.com/kagaimiq/jl-uboot-tool)
- [MIT license](https://github.com/kagaimiq/jl-uboot-tool/blob/adb3f18889e88ac512ce0a3c4d8cc3d3cb30696a/LICENSE)
- Bundled revision: [`adb3f18889e88ac512ce0a3c4d8cc3d3cb30696a`](https://github.com/kagaimiq/jl-uboot-tool/tree/adb3f18889e88ac512ce0a3c4d8cc3d3cb30696a).
- Copyright © 2023 Andrey Grigoryev (kagaimiq).

### Original recovery research and community guides

| Contributor | Contribution | Source |
| --- | --- | --- |
| **u/acrawf1** | Original plain-USB soft-brick recovery guide and firmware extraction script. | [Recovery guide](https://www.reddit.com/r/MVaveFM1/comments/1wz48t7/) · [Extraction script](https://gist.github.com/Acrawf1/ae2b9930b49231db397568cfc2abc2a8) |
| **u/International-Soup24** | Early demonstration of detecting and dumping a bricked FM1 with jl-uboot-tool. | [Contributor profile](https://www.reddit.com/user/International-Soup24/) · [Upstream acknowledgement](https://github.com/Quixotic7/MvaveFM1Unbricker#credits) |
| **Leo Kuroshita / kurogedelic and Hügelton Instruments** | Felucca documentation of the firmware package and flash layout; FM-1 Transporter firmware and recovery protocol. The Transporter is a separate recovery route, not a requirement of this application's standard USB workflow. | [Felucca](https://github.com/hugelton/Felucca) · [FM-1 Transporter](https://github.com/kurogedelic/FM-1-transporter) |
| **GoodPerson42** | Accessible community recovery instructions. | [FM1-Unbricking-For-Dummies](https://github.com/GoodPerson42/FM1-Unbricking-For-Dummies) |
| **The r/MVaveFM1 community** | Shared recovery findings, testing and documentation. | [Community](https://www.reddit.com/r/MVaveFM1/) |

Where a contributor's work is a post or gist rather than a repository, the original source is linked instead.

### Hardware and original firmware

**M-VAVE** created the FM1 hardware and original firmware. The embedded V15 `.fwsc` remains M-VAVE's work; it is not SikkenWorks firmware and is not covered by the MIT licenses of the recovery tools.

- [Official FM1 product and downloads page](https://www.m-vave.com/product?id=fm-1)

### Runtime and bundled Python dependencies

| Component | Bundled version | Credits | License / source |
| --- | --- | --- | --- |
| **CPython** | 3.14.0, Windows x64 embeddable | Python Software Foundation, Guido van Rossum and Python contributors; incorporated components retain their own notices. | PSF License v2 and incorporated notices · [Repository](https://github.com/python/cpython) · [License](https://docs.python.org/3/license.html) |
| **colorama** | 0.4.6 | Jonathan Hartley and contributors. | BSD-3-Clause · [Repository](https://github.com/tartley/colorama) |
| **crcmod** | 1.7 | Raymond L. Buvel and Craig McQueen. | MIT · [Project and source](https://crcmod.sourceforge.net/) |
| **PyCryptodomex / PyCryptodome** | 3.24.1 | Helder Eijs and contributors; original component notices retained. | BSD-2-Clause and public-domain components · [Repository](https://github.com/Legrandin/pycryptodome) |
| **PyYAML** | 6.0.3 | Kirill Simonov and contributors. | MIT · [Repository](https://github.com/yaml/pyyaml) |
| **tqdm** | 4.70.1 | Noam Yorav-Raphael, the tqdm developers and contributors; individual authors retain their copyrights. | MPL-2.0 and MIT portions · [Repository](https://github.com/tqdm/tqdm) |

The application uses Microsoft .NET Framework / Windows Forms supplied by Windows; that runtime is not bundled in this source package. [Windows Forms source](https://github.com/microsoft/referencesource/tree/master/System.Windows.Forms).

## Licensing

Using MIT-licensed recovery tools **does not require SikkenWorks' original interface or integration code to use the MIT license**. MIT allows redistribution and modification, provided the required original copyright and permission notices are retained with copies or substantial portions of the covered software.

This README does not assign a new license to SikkenWorks' original code. Its licensing must be specified separately by the maintainer. The upstream tools, runtime and dependencies retain their respective licenses; they are not all MIT-licensed.

The complete original notices are supplied in **[THIRD_PARTY_NOTICES.txt](THIRD_PARTY_NOTICES.txt)**. Individual notices also remain inside `payload.zip`, including:

- `vendor/unbricker/LICENSE`
- `vendor/unbricker/jl-uboot-tool/LICENSE`
- `python/LICENSE.txt`
- Dependency license files under `python/site-packages/*.dist-info/`.

Keep these notices when redistributing the application or source. Repository links and this acknowledgements section do not replace the required license texts. M-VAVE's firmware is separate from the open-source recovery code; this README does not grant rights to redistribute or relicense it.

## Building from source

1. Extract the complete source ZIP.
2. Keep `RecoveryApp.cs`, `payload.zip`, `app.ico`, `app.manifest` and `BUILD.bat` together.
3. Run **BUILD.bat** on 64-bit Windows with the .NET Framework C# compiler available.
4. The build creates `FM1-Recovery-by-SikkenWorks-1.0k.exe`.

`payload.zip` contains the Python adapter, recovery-engine source, Windows discovery scripts, pinned loader tools, embedded Python runtime and V15 firmware. If its contents change, rebuild it and update the `PayloadHash` constant in `RecoveryApp.cs` before compiling.

## Project status

The application has automated recovery-engine and interface checks. The earlier command-line recovery workflow was confirmed working on the developer's FM1. A physical Windows/FM1 recovery test of this particular GUI release has not been confirmed here.

FM1 Recovery by SikkenWorks is an independent project. It is not affiliated with or endorsed by M-VAVE or the credited projects and contributors. Credit acknowledges their work; it does not imply their endorsement of this application.

## Building and publishing with GitHub Actions

The workflow in `.github/workflows/build.yml` builds the Windows application on pushes, pull requests, manual runs and newly published releases. The application icon, recovery payload and full third-party license notices are embedded in the executable. The Licenses button works in both modes without waiting for recovery tools to be extracted.

To publish a version:

1. Commit and push the complete updated source, including `.github/workflows/build.yml`.
2. Open **Releases → Draft a new release** on GitHub.
3. Create a new version tag pointing to the commit you want to release. The tagged commit must contain this workflow and the updated source.
4. Enter a title and release notes, then click **Publish release**.
5. Wait for **Build FM1 Recovery** to finish successfully in Actions. The publish job attaches the compiled executable, README and third-party notices to that release's **Assets**.

Ordinary pushes and manual runs produce an Actions artifact, not a public release. Previously published releases are not rebuilt automatically. A failed build prevents the publish job from running. Existing assets with the same filenames are replaced; other assets and release notes are preserved. The publishing job alone receives `contents: write` permission.

For local builds, run `BUILD.bat` on Windows. Keep `THIRD_PARTY_NOTICES.txt` beside the source when compiling. It is embedded by both build routes.

When uploading this repository from the revised ZIP, copy the contents of `FM1recovery`, including the `.github` folder, into the repository root. The ZIP excludes local Git history and configuration (`.git`); all supplied project files are retained.

## Uploading a build to an existing release

1. Commit this updated `.github/workflows/build.yml` to the default branch.
2. Open Actions > Build FM1 Recovery > Run workflow. Select the default branch.
3. Enter the exact existing release tag in `release_tag`, including capitalization (for example `V1.0k`).
4. Click Run workflow. Both build and Attach compiled files to release must succeed.
5. Refresh that release. The compiled executable, README and notices appear under Assets.

The manual release build checks out the selected tag, so the binary matches that release's source. Updating main does not change old tags. Choose a new tag on updated code if you need the latest fixes. Leave release_tag empty to build only. A rerun of an old push run retains its original event and does not upload; use Run workflow for this new option. New published releases still trigger automatic builds. Same-name assets are replaced. No release is created or tag moved by the manual upload.
