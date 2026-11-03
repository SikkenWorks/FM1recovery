FM1 Recovery by SikkenWorks - Version 1.0k - Windows 10/11, 64-bit

At startup, choose Simple Mode or Advanced Mode.
No automatic elevation or mandatory administrator prompt on application launch.

SIMPLE MODE
1. Answer "Are you running this as Administrator?" The app checks its actual
   Windows permissions. If needed, close it, right-click the EXE, choose
   Run as administrator, accept the prompt and choose Simple Mode again.
2. Follow the OCT-minus startup and USB connection instructions. Click Next.
3. Wait while the app searches automatically. It retries every six seconds
   after the previous query finishes. Multiple devices are never guessed.
4. Once one FM1 is detected, type RESTORE and click Restore.
5. Keep the FM1 powered and USB connected. The backup, write and verification
   use the same recovery engine as Advanced Mode.
6. After verified success, follow the restart instructions shown on screen.
If recovery stops, keep power connected and open Advanced Mode to save a report.

ADVANCED MODE
The full existing interface: Restore FM1, Device help, Activity log and About.
Manual diagnostic fields do not bypass live device checks. Review the detected
FM1, acknowledge the firmware replacement, type RESTORE and confirm recovery.
Save diagnostic report exports the current log and device details.

The original V15 .fwsc, Windows Python and pinned WL82 tools are embedded.
No separate Python installation, firmware selection or internet setup is needed.
This restores stock V15. Install custom firmware separately afterwards.
Logs and mandatory full-flash backups are saved under:
%LOCALAPPDATA%\SikkenWorks\FM1-Recovery

All 1.0i detection fixes, About credits, licenses and backup/chip restrictions
are retained. Do not format the recovery disk or replace its driver.
An unsigned executable may produce a Windows unknown-publisher warning.
This GUI version has not been physically flashed on a Windows/FM1 setup here.

Version 1.0k: About / Credits is available from the startup chooser and every Simple Mode step. It opens a separate window using the same credits content as Advanced Mode, without resetting the recovery step.
