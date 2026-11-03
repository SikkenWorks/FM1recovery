using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Drawing;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

[assembly: AssemblyTitle("FM1 Recovery by SikkenWorks")]
[assembly: AssemblyDescription("Guided WL82 USB recovery with verified embedded M-VAVE V15 firmware")]
[assembly: AssemblyCompany("SikkenWorks")]
[assembly: AssemblyProduct("FM1 Recovery by SikkenWorks")]
[assembly: AssemblyVersion("1.0.0.10")]
[assembly: AssemblyFileVersion("1.0.0.10")]
[assembly: AssemblyInformationalVersion("1.0k")]

static class StartupLog {
 static readonly object Sync=new object();
 public static readonly string Folder=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"SikkenWorks","FM1-Recovery");
 public static string FilePath {get{return Path.Combine(Folder,"startup.log");}}
 public static void Write(string message){try{lock(Sync){Directory.CreateDirectory(Folder);File.AppendAllText(FilePath,DateTime.Now.ToString("s")+"  "+message+Environment.NewLine,Encoding.UTF8);}}catch{}}
 public static void Error(Exception ex){Write(ex==null?"Unknown exception":ex.ToString());}
}
class Device { public string Path,Name;public override string ToString(){return Name;} }
class NavigationPanel:TableLayoutPanel {
 readonly FlowLayoutPanel navigation=new FlowLayoutPanel{Dock=DockStyle.Top,AutoSize=true,WrapContents=true,Margin=new Padding(0,0,0,12)};
 readonly Panel content=new Panel{Dock=DockStyle.Fill,Margin=Padding.Empty};
 readonly List<Panel> pages=new List<Panel>();readonly List<Button> buttons=new List<Button>();int selected=-1;
 public NavigationPanel(){ColumnCount=1;RowCount=2;ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));RowStyles.Add(new RowStyle(SizeType.AutoSize));RowStyles.Add(new RowStyle(SizeType.Percent,100));Controls.Add(navigation,0,0);Controls.Add(content,0,1);}
 public int SelectedIndex{get{return selected;}set{if(value<0||value>=pages.Count)return;selected=value;for(int i=0;i<pages.Count;i++){pages[i].Visible=i==value;buttons[i].BackColor=i==value?Color.FromArgb(25,94,86):Color.FromArgb(28,34,44);buttons[i].ForeColor=i==value?Color.White:Color.FromArgb(185,197,209);buttons[i].FlatAppearance.BorderColor=i==value?Color.FromArgb(78,216,194):Color.FromArgb(65,78,91);}pages[value].BringToFront();}}
 public Panel AddPage(string title,Color bg,Color ink,Color accent,Color panel){int index=pages.Count;var page=new Panel{Dock=DockStyle.Fill,AutoScroll=true,BackColor=bg,ForeColor=ink,Padding=new Padding(18),Visible=false,BorderStyle=BorderStyle.FixedSingle};var button=new Button{Text=title,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,MinimumSize=new Size(160,44),FlatStyle=FlatStyle.Flat,Font=new Font("Segoe UI",10,FontStyle.Bold),Padding=new Padding(16,9,16,9),Margin=new Padding(0,0,10,8),UseVisualStyleBackColor=false};button.Click+=(s,e)=>SelectedIndex=index;buttons.Add(button);navigation.Controls.Add(button);pages.Add(page);content.Controls.Add(page);if(selected<0)SelectedIndex=0;else SelectedIndex=selected;return page;}
}
class RecoveryForm:Form {
 const string PayloadHash="5e23471a28386fe754531d259c04b0de66e66eba4318e095d366004d319127c5";
 readonly string storage=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"SikkenWorks","FM1-Recovery");
 string payload;
 readonly ConcurrentQueue<string> pendingOutput=new ConcurrentQueue<string>();
 readonly System.Windows.Forms.Timer outputTimer=new System.Windows.Forms.Timer();
 Form aboutWindow;
 Panel modeHost,wizard;TableLayoutPanel wizardBody;Label wizardTitle,wizardText,wizardStatus;Button wizardNext,wizardBack,wizardAdvanced;TextBox wizardConfirmation;ProgressBar wizardProgress;
 readonly System.Windows.Forms.Timer discoveryTimer=new System.Windows.Forms.Timer();
 bool simpleMode=false,toolsReady=false,preparing=false,simpleStarted=false;int wizardStep=0;
 Func<bool> administratorCheck=IsAdministrator;
 NavigationPanel tabs;ComboBox devices;Button scan,restore;CheckBox consent;TextBox confirmation,deviceName,hardware,parentId,diskNumber;RichTextBox log;Label state,phase,diagnosis,windowsState;ProgressBar progress;
 bool busy=false,writing=false,writeFailed=false;List<Device> found=new List<Device>();
 Color bg=Color.FromArgb(18,22,29),panel=Color.FromArgb(28,34,44),ink=Color.FromArgb(232,238,245),accent=Color.FromArgb(78,216,194);
 public RecoveryForm(){
  Text="FM1 Recovery by SikkenWorks  |  Version 1.0k";Size=new Size(980,900);MinimumSize=new Size(800,680);StartPosition=FormStartPosition.CenterScreen;BackColor=bg;ForeColor=ink;Font=new Font("Segoe UI",10);AutoScaleMode=AutoScaleMode.Dpi;
  using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("branding.ico")){
   if(stream!=null)using(var source=new Icon(stream)){Icon=(Icon)source.Clone();}
  }
  var shell=new TableLayoutPanel{Dock=DockStyle.Fill,RowCount=3,ColumnCount=1,Padding=new Padding(24)};
  shell.RowStyles.Add(new RowStyle(SizeType.AutoSize));shell.RowStyles.Add(new RowStyle(SizeType.Percent,100));shell.RowStyles.Add(new RowStyle(SizeType.AutoSize));Controls.Add(shell);
  var header=new TableLayoutPanel{Dock=DockStyle.Top,AutoSize=true,RowCount=2,ColumnCount=2,Padding=new Padding(0,0,0,20)};header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,82));header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));header.RowStyles.Add(new RowStyle(SizeType.AutoSize));header.RowStyles.Add(new RowStyle(SizeType.AutoSize));
  var portrait=new PictureBox{Dock=DockStyle.Fill,SizeMode=PictureBoxSizeMode.CenterImage,Margin=new Padding(0,0,14,10)};
  if(Icon!=null)portrait.Image=new Icon(Icon,64,64).ToBitmap();header.Controls.Add(portrait,0,0);header.SetRowSpan(portrait,2);
  var title=Label("FM1 Recovery by SikkenWorks",21,accent);var subtitle=Label("Version 1.0k   /   Windows USB recovery",10,Color.Silver);header.Controls.Add(title,1,0);header.Controls.Add(subtitle,1,1);header.SizeChanged+=(s,e)=>{int width=Math.Max(120,header.ClientSize.Width-header.GetColumnWidths()[0]-8);title.MaximumSize=new Size(width,0);subtitle.MaximumSize=new Size(width,0);};shell.Controls.Add(header,0,0);
  modeHost=new Panel{Dock=DockStyle.Fill};shell.Controls.Add(modeHost,0,1);tabs=new NavigationPanel{Dock=DockStyle.Fill,BackColor=bg,Visible=false};modeHost.Controls.Add(tabs);
  var footer=Label("Embedded official V15  •  Mandatory backup  •  No MIDI or Python installation required",9,Color.Silver);footer.TextAlign=ContentAlignment.MiddleLeft;shell.Controls.Add(footer,0,2);shell.SizeChanged+=(s,e)=>footer.MaximumSize=new Size(Math.Max(120,shell.ClientSize.Width-shell.Padding.Horizontal-8),0);
  BuildRestore();BuildHelp();BuildLog();BuildAbout();BuildModeChoice();
  outputTimer.Interval=60;outputTimer.Tick+=(s,e)=>{DrainOutput();SyncWizard();};outputTimer.Start();
  FormClosed+=(s,e)=>{outputTimer.Stop();outputTimer.Dispose();discoveryTimer.Stop();discoveryTimer.Dispose();StartupLog.Write("Main window closed.");};
  StartupLog.Write("Main window constructed.");
  FormClosing+=(s,e)=>{if(writing){MessageBox.Show("Recovery is running. Keep the FM1 powered and connected until verification completes.","Recovery in progress");e.Cancel=true;}else if(writeFailed && MessageBox.Show("The last write was not verified. Keep the FM1 powered and save the log. Close the app?","Unverified write",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)!=DialogResult.Yes)e.Cancel=true;};
  discoveryTimer.Interval=6000;discoveryTimer.Tick+=async(s,e)=>{if(simpleMode&&wizardStep==3&&toolsReady&&!busy&&!writing&&!simpleStarted)await SimpleScan();};
  Shown+=(s,e)=>StartupLog.Write("Mode selection shown.");
 }
 static bool IsAdministrator(){
  if(Environment.OSVersion.Platform!=PlatformID.Win32NT)return false;
  try{using(var identity=System.Security.Principal.WindowsIdentity.GetCurrent())return new System.Security.Principal.WindowsPrincipal(identity).IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);}catch{return false;}
 }
 void BuildModeChoice(){
  wizard=new Panel{Dock=DockStyle.Fill,AutoScroll=true,BackColor=bg,Padding=new Padding(20),BorderStyle=BorderStyle.FixedSingle};modeHost.Controls.Add(wizard);wizard.BringToFront();
  wizardBody=new TableLayoutPanel{Dock=DockStyle.Top,AutoSize=true,ColumnCount=1,RowCount=0};wizard.Controls.Add(wizardBody);
  wizardTitle=Label("How would you like to recover your FM1?",19,ink);Add(wizardBody,wizardTitle,50);
  wizardText=Label("Simple Mode guides you through each step.\nAdvanced Mode gives you device details, diagnostics and the activity log.",12,ink);Add(wizardBody,wizardText,85);
  wizardStatus=Label("The original V15 firmware is included. No file to download or select.",11,accent);Add(wizardBody,wizardStatus,60);
  wizardConfirmation=Input();wizardConfirmation.Visible=false;wizardConfirmation.TextChanged+=(s,e)=>SyncWizard();Add(wizardBody,wizardConfirmation,40);
  wizardProgress=new ProgressBar{Dock=DockStyle.Fill,Visible=false};Add(wizardBody,wizardProgress,20);
  var actions=new FlowLayoutPanel{Dock=DockStyle.Top,AutoSize=true,WrapContents=true};
  wizardNext=Button("Simple Mode");wizardNext.BackColor=Color.FromArgb(25,94,86);wizardNext.Click+=async(s,e)=>await WizardNext();actions.Controls.Add(wizardNext);
  wizardBack=Button("Back");wizardBack.Visible=false;wizardBack.Click+=(s,e)=>{if(!busy&&!simpleStarted){discoveryTimer.Stop();SetWizardStep(Math.Max(1,wizardStep-1));}};actions.Controls.Add(wizardBack);
  wizardAdvanced=Button("Advanced Mode");wizardAdvanced.Click+=async(s,e)=>await OpenAdvanced();actions.Controls.Add(wizardAdvanced);var about=Button("About / Credits");about.Click+=(s,e)=>ShowSimpleAbout();actions.Controls.Add(about);Add(wizardBody,actions,58);wizardBody.RowStyles[3].Height=0;wizardBody.RowStyles[4].Height=0;
 }
 async Task InitializeTools(){
  if(toolsReady||preparing)return;preparing=true;SetBusy(true);state.Text="Preparing the built-in recovery tools...";
  try{await Task.Run((Action)Prepare);if(IsDisposed)return;int rc=await RunBackend("verify","");if(rc!=0)throw new Exception("Embedded package verification failed.");toolsReady=true;state.Text="Ready.";}
  catch(Exception ex){StartupLog.Error(ex);state.Text="Preparation failed: "+ex.Message;Append(ex.ToString());if(simpleMode)wizardStatus.Text="The recovery tools could not be prepared. Open Advanced Mode and save a diagnostic report.";}
  finally{preparing=false;if(!IsDisposed){SetBusy(false);SyncWizard();}}
 }
 async Task OpenAdvanced(){
  if(writing||busy)return;simpleMode=false;discoveryTimer.Stop();wizard.Visible=false;tabs.Visible=true;tabs.BringToFront();Text="FM1 Recovery by SikkenWorks | Version 1.0k | Advanced Mode";
  await InitializeTools();if(toolsReady&&!IsDisposed)await Scan();
 }
 void SetWizardStep(int step){
  wizardStep=step;wizardBody.RowStyles[3].Height=step==4?50:0;wizardBody.RowStyles[4].Height=(step==3||step==5)?30:0;wizardConfirmation.Visible=step==4;wizardProgress.Visible=step==3||step==5;wizardBack.Visible=step==2||step==3||step==4;wizardAdvanced.Text="Open Advanced Mode";
  wizardNext.Visible=step!=5;wizardNext.Enabled=true;
  if(step==1){wizardTitle.Text="1. Are you running this as Administrator?";wizardText.Text="Administrator access is needed to restore the FM1.\n\nIf not:\n1. Close this app.\n2. Right-click FM1 Recovery by SikkenWorks.\n3. Choose Run as administrator.\n4. Click Yes in the Windows prompt.\n5. Choose Simple Mode again.";wizardStatus.Text=administratorCheck()?"Yes: this app is running as administrator.":"This app is not running as administrator. Follow the steps above.";wizardNext.Text="Yes, continue";}
  if(step==2){wizardTitle.Text="2. Connect your FM1";wizardText.Text="1. Switch your FM1 off.\n2. Hold down the OCT-minus (OCT-) button.\n3. Keep holding it while switching your FM1 on.\n4. Connect it directly to your PC with a USB data cable.\n5. Release OCT-minus.\n\nThe FM1 screen may stay black. That is expected.\nIf Windows asks you to format a disk, click Cancel.";wizardStatus.Text="Once your FM1 is connected, click Next.";wizardNext.Text="Next";}
  if(step==3){wizardTitle.Text="3. Waiting for your FM1";wizardText.Text="Please wait while we look for your FM1.\nKeep it switched on and connected.\n\nIf it is not found, try another USB data cable or a different USB port.\nYou can also go Back and repeat the OCT-minus steps.";wizardStatus.Text="Looking for your FM1...";wizardNext.Text="Check again";if(toolsReady)discoveryTimer.Start();}
  if(step==4){discoveryTimer.Stop();wizardTitle.Text="4. Your FM1 is ready to restore";wizardText.Text="Type RESTORE below, then click the Restore button.\n\nThis installs the original V15 firmware and may affect saved sounds and settings. A full backup is made automatically first.\n\nKeep your FM1 powered and USB connected until recovery has finished.";wizardStatus.Text="FM1 found. Nothing has been written yet.";wizardNext.Text="Restore";wizardConfirmation.Clear();wizardConfirmation.Focus();}
  if(step==5){discoveryTimer.Stop();wizardTitle.Text="Restoring your FM1";wizardText.Text="Please wait.\n\nDo not switch your FM1 off.\nDo not unplug the USB cable.\n\nA full backup is made before the firmware is written. The result is then verified.";wizardStatus.Text="Starting recovery...";}
  if(step==6){wizardTitle.Text="Your FM1 has been restored";wizardText.Text="1. Unplug the USB cable.\n2. Switch your FM1 off.\n3. Wait 5 seconds.\n4. Switch it back on.\n\nYour FM1 should now start with the original V15 firmware.";wizardStatus.Text="Recovery and verification completed successfully.";wizardNext.Text="Close";wizardBack.Visible=false;}
  if(step==7){wizardTitle.Text="Recovery could not be completed";wizardText.Text="Keep your FM1 powered and connected.\n\nOpen Advanced Mode, then Activity log.\nClick Save diagnostic report and send the report for help.\n\nDo not assume that recovery succeeded.";wizardStatus.Text=phase.Text;wizardNext.Text="Open Advanced Mode";wizardBack.Visible=false;}
  SyncWizard();
 }
 async Task WizardNext(){
  if(writing)return;
  if(wizardStep==0){simpleMode=true;Text="FM1 Recovery by SikkenWorks | Version 1.0k | Simple Mode";SetWizardStep(1);return;}
  if(wizardStep==1){if(!administratorCheck()){wizardStatus.Text="Please close the app and reopen it using Run as administrator. Then return here.";return;}SetWizardStep(2);return;}
  if(wizardStep==2){SetWizardStep(3);await InitializeTools();if(toolsReady){discoveryTimer.Start();await SimpleScan();}return;}
  if(wizardStep==3){if(!busy&&toolsReady)await SimpleScan();return;}
  if(wizardStep==4){if(busy||wizardConfirmation.Text.Trim()!="RESTORE"||found.Count!=1||devices.SelectedItem==null)return;confirmation.Text="RESTORE";consent.Checked=true;Gate();if(!restore.Enabled)return;simpleStarted=true;SetWizardStep(5);await Restore();if(IsDisposed)return;SetWizardStep(simpleSuccess?6:7);return;}
  if(wizardStep==6){Close();return;}if(wizardStep==7)await OpenAdvanced();
 }
 bool simpleSuccess=false;
 async Task SimpleScan(){
  if(busy||!toolsReady||wizardStep!=3)return;wizardStatus.Text="Looking for your FM1...";await Scan();if(IsDisposed||!simpleMode||wizardStep!=3)return;
  if(found.Count==1&&devices.SelectedItem is Device)SetWizardStep(4);
  else if(found.Count>1)wizardStatus.Text="More than one FM1 was found. Disconnect the others. We will check again automatically.";
  else wizardStatus.Text="Your FM1 has not been found yet. Check the cable and OCT-minus steps. We will keep checking.";
 }
 void SyncWizard(){
  if(wizardNext==null||!simpleMode)return;wizardAdvanced.Enabled=!busy&&!writing;wizardBack.Enabled=!busy&&!writing;
  if(wizardStep==3){wizardNext.Enabled=!busy&&toolsReady;wizardProgress.Style=ProgressBarStyle.Marquee;if(preparing)wizardStatus.Text="Preparing the built-in recovery tools. Please wait...";}
  if(wizardStep==4)wizardNext.Enabled=!busy&&!writing&&found.Count==1&&devices.SelectedItem is Device&&wizardConfirmation.Text.Trim()=="RESTORE";
  if(wizardStep==5){wizardStatus.Text=phase.Text;wizardProgress.Style=ProgressBarStyle.Marquee;}
 }
 Label Label(string text,float size,Color color){return new Label{Text=text,Font=new Font("Segoe UI",size),ForeColor=color,AutoSize=true,Dock=DockStyle.Top,Margin=new Padding(0,0,0,8),Padding=new Padding(0,4,0,4)};}
 Panel Page(string title){return tabs.AddPage(title,bg,ink,accent,panel);}
 Button Button(string title){var b=new Button{Text=title,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,MinimumSize=new Size(150,40),BackColor=panel,ForeColor=ink,FlatStyle=FlatStyle.Flat,Padding=new Padding(14,7,14,7),Margin=new Padding(0,0,10,8),UseVisualStyleBackColor=false};b.FlatAppearance.BorderColor=Color.FromArgb(65,78,91);b.FlatAppearance.MouseOverBackColor=Color.FromArgb(40,53,64);return b;}
 TextBox Input(bool multi=false){return new TextBox{BackColor=panel,ForeColor=ink,BorderStyle=BorderStyle.FixedSingle,Dock=DockStyle.Fill,Multiline=multi,ScrollBars=multi?ScrollBars.Vertical:ScrollBars.None};}
 void BuildRestore(){
  var p=Page("1  Restore FM1");var t=new TableLayoutPanel{Dock=DockStyle.Top,AutoSize=true,ColumnCount=1,RowCount=0};p.Controls.Add(t);
  Add(t,Label("Restore the original M-VAVE V15 firmware",17,ink),48);
  Add(t,Label("1. Switch the FM1 off.\n2. Hold OCT-minus while switching it on, connect USB, then release the button.\n3. Windows should show WL82 UBOOT1.00 USB Device. A MIDI port is not expected.",11,ink),91);
  Add(t,Label("Do not format the USB disk or copy files onto it. Close other firmware tools.",10,accent),34);
  var row=new FlowLayoutPanel{Dock=DockStyle.Fill};scan=Button("Find FM1");scan.Click+=async(s,e)=>await Scan();row.Controls.Add(scan);
  var help=Button("FM1 not found?");help.Click+=(s,e)=>tabs.SelectedIndex=1;row.Controls.Add(help);var manager=Button("Device Manager");manager.Click+=(s,e)=>OpenManager();row.Controls.Add(manager);Add(t,row,48);
  devices=new ComboBox{Dock=DockStyle.Fill,DropDownStyle=ComboBoxStyle.DropDownList,BackColor=panel,ForeColor=ink};devices.SelectedIndexChanged+=async(s,e)=>{Gate();if(!busy && devices.SelectedItem is Device)await ReadWindowsDetails();};Add(t,devices,36);
  state=Label("Preparing tools...",11,ink);Add(t,state,65);
  Add(t,Label("Official V15 is built in and checked by SHA-256. No firmware file to select.\nA full flash backup is read twice before the application region is written.\nBackups are saved in your user account, outside this executable.",10,Color.Silver),77);
  consent=new CheckBox{Text="I understand that this replaces the firmware and may affect saved sounds/settings.",AutoSize=false,Dock=DockStyle.Fill,ForeColor=ink};consent.CheckedChanged+=(s,e)=>Gate();Add(t,consent,41);
  Add(t,Label("Type RESTORE to enable writing:",10,ink),27);confirmation=Input();confirmation.TextChanged+=(s,e)=>Gate();Add(t,confirmation,36);
  var actions=new FlowLayoutPanel{Dock=DockStyle.Fill};restore=Button("Restore original V15");restore.Enabled=false;restore.BackColor=Color.FromArgb(25,94,86);restore.Click+=async(s,e)=>await Restore();actions.Controls.Add(restore);var backups=Button("Open backups / logs");backups.Click+=(s,e)=>OpenStorage();actions.Controls.Add(backups);Add(t,actions,49);
  phase=Label("Nothing has been written.",10,accent);Add(t,phase,43);progress=new ProgressBar{Dock=DockStyle.Fill,Style=ProgressBarStyle.Blocks};Add(t,progress,18);
 }
 void Add(TableLayoutPanel t,Control control,int height){
  if(t.ColumnStyles.Count==0)t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
  int row=t.RowCount++;bool flexible=control is Label || control is FlowLayoutPanel || control is CheckBox;
  control.Margin=new Padding(0,0,0,10);
  if(flexible){control.AutoSize=true;control.Dock=DockStyle.Top;t.RowStyles.Add(new RowStyle(SizeType.AutoSize));
   var flow=control as FlowLayoutPanel;if(flow!=null){flow.AutoSizeMode=AutoSizeMode.GrowAndShrink;flow.WrapContents=true;}
   EventHandler fit=(sender,args)=>{int width=Math.Max(100,t.ClientSize.Width-8);if(control.MaximumSize.Width!=width)control.MaximumSize=new Size(width,0);};t.SizeChanged+=fit;fit(t,EventArgs.Empty);
  }else t.RowStyles.Add(new RowStyle(SizeType.Absolute,height+10));
  t.Controls.Add(control,0,row);
 }
 void BuildHelp(){
  var p=Page("2  Device help");var t=new TableLayoutPanel{Dock=DockStyle.Top,AutoSize=true,ColumnCount=1,RowCount=0};p.Controls.Add(t);
  Add(t,Label("Find the device in Windows",17,ink),44);
  Add(t,Label("Device details are read automatically when the app starts or you click Find FM1.\nIf fields are missing: 1. Click Open Device Manager below.\n2. Expand Disk drives and look for WL82 UBOOT1.00 USB Device.\n3. Right-click it > Properties > Details > Hardware Ids. Copy the entries.\n4. For USB VID/PID, choose Parent in the same Details list.\n5. Paste the information below. It helps diagnose the connection; it does not bypass device checks.",10,ink),150);
  var row=new FlowLayoutPanel{Dock=DockStyle.Fill};var open=Button("Open Device Manager");open.Click+=(s,e)=>OpenManager();row.Controls.Add(open);var refresh=Button("Find FM1 again");refresh.Click+=async(s,e)=>await Scan();row.Controls.Add(refresh);var automatic=Button("Read Windows details");automatic.Click+=async(s,e)=>await ReadWindowsDetails();row.Controls.Add(automatic);Add(t,row,48);
  windowsState=Label("Windows properties have not been read yet.",10,accent);Add(t,windowsState,48);
  Add(t,Label("Device name (as shown in Device Manager)",10,ink),27);deviceName=Input();Add(t,deviceName,32);
  Add(t,Label("Hardware Ids (paste all lines)",10,ink),27);hardware=Input(true);Add(t,hardware,70);
  Add(t,Label("Parent / USB hardware ID (for example USB\\VID_4C4A&PID_8057\\...)",10,ink),27);parentId=Input();Add(t,parentId,32);
  Add(t,Label("Optional disk number: Properties > Volumes > Populate > Disk (for example 2)",10,ink),27);diskNumber=Input();Add(t,diskNumber,32);
  var buttons=new FlowLayoutPanel{Dock=DockStyle.Fill};var validate=Button("Check my information");validate.Click+=(s,e)=>Diagnose();buttons.Controls.Add(validate);var export=Button("Save diagnostic report");export.Click+=(s,e)=>Export();buttons.Controls.Add(export);Add(t,buttons,46);
  diagnosis=Label("The app reads Windows device properties automatically. Manual entry remains available if some fields are missing.",10,accent);Add(t,diagnosis,115);
  Add(t,Label("Nothing appears? Try another USB data cable and a direct USB port.\nNormal B1 or a MIDI device? Switch off and repeat the OCT-minus startup.\nWL82 has a warning icon? Include the General tab's exact error in your report.\nDo not format the disk, replace drivers, or guess a PhysicalDrive number.",10,Color.Silver),104);
 }
 void BuildLog(){var p=Page("3  Activity log");p.AutoScroll=false;var layout=new TableLayoutPanel{Dock=DockStyle.Fill,RowCount=2};layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));log=new RichTextBox{Dock=DockStyle.Fill,ReadOnly=true,BackColor=Color.FromArgb(12,15,20),ForeColor=ink,Font=new Font("Consolas",9),WordWrap=true};layout.Controls.Add(log,0,0);var row=new FlowLayoutPanel{Dock=DockStyle.Top,AutoSize=true,WrapContents=true,Padding=new Padding(0,12,0,0)};var save=Button("Save diagnostic report");save.Click+=(s,e)=>Export();row.Controls.Add(save);var folder=Button("Open backups / logs");folder.Click+=(s,e)=>OpenStorage();row.Controls.Add(folder);layout.Controls.Add(row,0,1);p.Controls.Add(layout);}
 void BuildAbout(){BuildAboutContent(Page("4  About"));}
 void ShowSimpleAbout(){
  if(aboutWindow!=null&&!aboutWindow.IsDisposed){aboutWindow.Activate();return;}
  aboutWindow=new Form{Text="About / Credits | FM1 Recovery by SikkenWorks",Icon=Icon,Size=new Size(800,740),MinimumSize=new Size(640,480),StartPosition=FormStartPosition.CenterParent,BackColor=bg,ForeColor=ink,Font=Font,AutoScaleMode=AutoScaleMode.Dpi};
  var content=new Panel{Dock=DockStyle.Fill,AutoScroll=true,Padding=new Padding(24)};aboutWindow.Controls.Add(content);BuildAboutContent(content);aboutWindow.Show(this);
 }
 void BuildAboutContent(Panel p){
  var t=new TableLayoutPanel{Dock=DockStyle.Top,AutoSize=true,ColumnCount=1,RowCount=0};p.Controls.Add(t);
  Add(t,Label("FM1 Recovery by SikkenWorks",17,ink),44);
  Add(t,Label("Version 1.0k  /  Windows USB recovery\nApplication, interface and integration by Joppe Sikken / SikkenWorks.",11,accent),60);
  Add(t,Label("Recovery tools",14,ink),36);
  Add(t,Label("MvaveFM1Unbricker - Michael P Jones (2026). MIT License.\nAutomates the FM1 recovery, backup and verification procedure.\n\njl-uboot-tool - Andrey Grigoryev (kagaimiq). MIT License.\nProvides communication with the Jieli WL82 chip.\nhttps://github.com/kagaimiq/jl-uboot-tool",10,ink),140);
  Add(t,Label("Community credits",14,ink),36);
  Add(t,Label("u/acrawf1 - original USB unbricking guide and firmware extraction script.\nu/International-Soup24 - early detection and flash dumping with jl-uboot-tool.\nLeo Kuroshita (kurogedelic) / Hugelton Instruments - Felucca package/flash documentation and FM-1 Transporter.\nGoodPerson42 - FM1-Unbricking-For-Dummies guide.",10,ink),115);
  Add(t,Label("Included components",14,ink),36);
  Add(t,Label("Official V15 firmware - M-VAVE.\nCPython 3.14 - Python Software Foundation and contributors.\nThird-party dependencies retain their original licenses and notices.\nThis is an independent recovery application; it is not affiliated with or endorsed by M-VAVE or the credited projects.",10,Color.Silver),100);
  var licenses=Button("Open licenses / credits");licenses.Click+=(s,e)=>{if(String.IsNullOrEmpty(payload)){MessageBox.Show("The built-in tools are still being prepared. Please try again shortly.","Licenses and credits");return;}try{Process.Start(new ProcessStartInfo{FileName=Path.Combine(payload,"CREDITS.txt"),UseShellExecute=true});}catch(Exception ex){Append(ex.Message);}};Add(t,licenses,48);
 }
 void Prepare(){
  StartupLog.Write("Unpacking embedded payload.");Directory.CreateDirectory(storage);payload=Path.Combine(storage,"tools-1.0k-"+PayloadHash.Substring(0,12));
  // Clean, per-version extraction from an authenticated-in-process resource.
  string temp=payload+"-"+Guid.NewGuid().ToString("N");Directory.CreateDirectory(temp);
  using(var input=Assembly.GetExecutingAssembly().GetManifestResourceStream("payload.zip")){
   if(input==null)throw new Exception("Missing embedded tools");using(var ms=new MemoryStream()){input.CopyTo(ms);byte[] bytes=ms.ToArray();using(var sha=SHA256.Create()){if(BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant()!=PayloadHash)throw new Exception("Tools hash mismatch");}
    using(var data=new MemoryStream(bytes))using(var zip=new ZipArchive(data,ZipArchiveMode.Read)){foreach(var item in zip.Entries){string target=Path.GetFullPath(Path.Combine(temp,item.FullName));if(!target.StartsWith(Path.GetFullPath(temp)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new Exception("Invalid embedded path");if(item.FullName.EndsWith("/")){Directory.CreateDirectory(target);continue;}Directory.CreateDirectory(Path.GetDirectoryName(target));item.ExtractToFile(target,true);}}
   }
  }
  // Per-process payload avoids reusing modified executables from a previous run.
  payload=temp;StartupLog.Write("Payload extracted to "+payload);
 }
 void SetBusy(bool b){if(IsDisposed||Disposing)return;busy=b;scan.Enabled=!b;devices.Enabled=!b;confirmation.Enabled=!b;consent.Enabled=!b;progress.Style=b?ProgressBarStyle.Marquee:ProgressBarStyle.Blocks;if(!b)progress.Value=0;Gate();}
 void Gate(){if(IsDisposed||Disposing)return;restore.Enabled=!busy && devices.SelectedItem is Device && consent.Checked && confirmation.Text.Trim()=="RESTORE";}
 void Append(string line){StartupLog.Write(line);if(IsDisposed||Disposing)return;if(InvokeRequired){pendingOutput.Enqueue(line);return;}log.AppendText(line+Environment.NewLine);log.SelectionStart=log.TextLength;log.ScrollToCaret();}
 public static int MockCount=0,MockRestoreResult=0,MockRestoreCalls=0;
 async Task<int> RunBackend(string action,string path){await Task.FromResult(0);if(action=="scan"){for(int i=0;i<MockCount;i++)OnOutput("@@DEVICE\t"+@"\\.\PhysicalDrive"+(i+2)+"\tWL82 UBOOT1.00 USB Device");}if(action=="restore"){MockRestoreCalls++;return MockRestoreResult;}return 0;}
 static bool WaitForRecovery(Process process){process.WaitForExit();return true;}
 void DrainOutput(){if(IsDisposed||Disposing)return;string line;int budget=500;while(budget-->0&&pendingOutput.TryDequeue(out line)){try{OnOutput(line);}catch(Exception ex){StartupLog.Error(ex);if(log!=null&&!log.IsDisposed)log.AppendText("Output processing error: "+ex.Message+Environment.NewLine);}}}

 void OnOutput(string line){if(IsDisposed||Disposing)return;if(line.StartsWith("@@DEVICE\t")){var v=line.Split(new char[]{'\t'},StringSplitOptions.None);if(v.Length==3){var d=new Device{Path=v[1],Name=v[2]};found.Add(d);devices.Items.Add(d);}return;}if(line.StartsWith("@@COUNT\t"))return;if(line.StartsWith("@@DETAIL\t")){var fields=line.Split(new char[]{'\t'},StringSplitOptions.None);if(fields.Length==3){try{string value=Encoding.UTF8.GetString(Convert.FromBase64String(fields[2]));switch(fields[1]){case "name":deviceName.Text=value;break;case "hardware":hardware.Text=value;break;case "parent":parentId.Text=value;break;case "disk":diskNumber.Text=value;break;case "status":windowsState.Text=value;break;}}catch(FormatException){Append("Invalid device-property response; use manual entry.");}}return;}Append(line);if(writing){if(line.Contains("Step 1/6"))phase.Text="Checking the built-in firmware...";if(line.Contains("Step 2/6"))phase.Text="Checking the FM1 chip and flash...";if(line.Contains("Step 3/6"))phase.Text="Reading and verifying your full flash backup...";if(line.Contains("Step 5/6"))phase.Text="Writing and verifying V15. Keep USB connected.";}}
 async Task Scan(){if(busy||payload==null)return;SetBusy(true);found.Clear();devices.Items.Clear();deviceName.Clear();hardware.Clear();parentId.Clear();diskNumber.Clear();windowsState.Text="Reading Windows properties...";state.Text="Looking for WL82 UBOOT1.00 over USB...";try{int rc=await RunBackend("scan","");if(rc!=0){state.Text="Device scan failed. Checking Windows device properties for diagnostic information...";tabs.SelectedIndex=1;await RunBackend("details","");Diagnose();}else if(found.Count==1){devices.SelectedIndex=0;state.Text="FM1 found. Windows device details have been requested automatically.";Diagnose();}else if(found.Count==0){state.Text="No FM1 found. Follow Device help, then scan again.";tabs.SelectedIndex=1;Diagnose();}else{state.Text="Several FM1 devices found. Select the correct device or disconnect the others.";tabs.SelectedIndex=0;}}catch(Exception ex){state.Text="Scan failed: "+ex.Message;Append(ex.ToString());tabs.SelectedIndex=1;}finally{if(windowsState.Text=="Reading Windows properties...")windowsState.Text="Automatic properties are unavailable. Retry Read Windows details or use manual entry.";SetBusy(false);}}
 async Task ReadWindowsDetails(){if(busy||payload==null)return;SetBusy(true);windowsState.Text="Reading properties directly from Windows...";deviceName.Clear();hardware.Clear();parentId.Clear();diskNumber.Clear();try{var selected=devices.SelectedItem as Device;int rc=await RunBackend("details",selected==null?"":selected.Path);if(rc!=0)windowsState.Text="Windows could not return the details. Use the manual steps or save a report.";else if(deviceName.Text.Length==0)windowsState.Text="No unique matching device found. Reconnect in OCT-minus mode or fill in the fields manually.";Diagnose();}catch(Exception ex){windowsState.Text="Automatic lookup failed. Manual entry is still available.";Append(ex.ToString());}finally{SetBusy(false);}}
 async Task Restore(){if(!restore.Enabled)return;Device selected=devices.SelectedItem as Device;if(selected==null)return;
  if(!simpleMode && MessageBox.Show("Restore official V15 on:\n"+selected.Name+"\n\nA verified full backup is mandatory. This replaces the application firmware. Keep the FM1 powered and connected until verification finishes.\n\nStart recovery?","Confirm V15 recovery",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)!=DialogResult.Yes)return;
  simpleSuccess=false;writing=true;writeFailed=false;phase.Text="Starting recovery...";SetBusy(true);state.Text="Recovery running. Do not disconnect USB.";try{int rc=await RunBackend("restore",selected.Path);if(rc==0){simpleSuccess=true;phase.Text="Restore and byte-for-byte verification completed.";state.Text="Success: unplug USB, switch the FM1 off, wait 5 seconds, then switch it on.";if(!simpleMode)MessageBox.Show(state.Text,"Recovery verified",MessageBoxButtons.OK,MessageBoxIcon.Information);}else{writeFailed=rc==3;phase.Text=writeFailed?"WRITE NOT VERIFIED. Keep the FM1 powered and save the log.":"Recovery stopped. No successful recovery confirmed. See Activity log.";state.Text=phase.Text;tabs.SelectedIndex=2;}}catch(Exception ex){writeFailed=true;phase.Text="Recovery interrupted. Keep the FM1 powered and save the log.";Append(ex.ToString());tabs.SelectedIndex=2;}finally{writing=false;confirmation.Clear();SetBusy(false);}}
 void Diagnose(){string name=deviceName.Text.Trim(),hw=hardware.Text.Trim(),parent=parentId.Text.Trim();string all=(name+"\n"+hw+"\n"+parent).ToUpperInvariant();
  if(Regex.IsMatch(parent,@"VID_[0-9A-F]{4}",RegexOptions.IgnoreCase)&&!parent.ToUpperInvariant().Contains("VID_4C4A&PID_8057")){diagnosis.Text="This USB ID differs from the known 4C4A:8057 recovery interface. Copy the FM1's Parent property in OCT-minus mode and save a diagnostic report. The ID alone cannot confirm a recovery device.";return;}
  bool known=all.Contains("WL82")&&all.Contains("UBOOT")||all.Contains("VID_4C4A&PID_8057");
  if(known){int index;string number=diskNumber.Text.Trim();if(number.Length>0&&!int.TryParse(number,out index)){diagnosis.Text="Enter only the disk number, such as 2. This is not a drive letter or a USB hardware ID.";return;}
   if(found.Count==0)diagnosis.Text="Your details match WL82 recovery mode. Click Find FM1 again. If it still fails, save the diagnostic report. Pasted IDs cannot establish a live USB connection.";
   else{Device match=null;if(number.Length>0){foreach(var d in found)if(d.Path.EndsWith("PhysicalDrive"+number,StringComparison.OrdinalIgnoreCase))match=d;if(match==null){diagnosis.Text="That disk number does not match a currently detected FM1. Recheck Volumes > Populate, then scan again. No device selected from this number.";return;}}else if(found.Count==1)match=found[0];
    if(match!=null){devices.SelectedItem=match;diagnosis.Text="Details match a currently detected WL82 FM1. Return to Restore FM1 to review and start recovery.";}else diagnosis.Text="Several FM1s are detected. Select the intended device on the Restore tab or disconnect the others.";}
  }else if(all.Contains("FM-1")||all.Contains("MIDI")||all.Contains("B1"))diagnosis.Text="This looks like normal firmware/MIDI mode. Switch off, hold OCT-minus while switching on, connect USB, then scan again.";
  else diagnosis.Text="Copy the device name, Hardware Ids and Parent fields from Device Manager. If nothing appears, try a different data cable and USB port. Save a report if you are still stuck.";
 }
 void Export(){using(var s=new SaveFileDialog{Filter="Text report (*.txt)|*.txt",FileName="FM1-Recovery-Diagnostics.txt"}){if(s.ShowDialog()!=DialogResult.OK)return;File.WriteAllText(s.FileName,"FM1 Recovery by SikkenWorks 1.0k\r\nDate: "+DateTime.Now.ToString("s")+"\r\nDevice name: "+deviceName.Text+"\r\nHardware IDs:\r\n"+hardware.Text+"\r\nParent: "+parentId.Text+"\r\nDisk number: "+diskNumber.Text+"\r\nWindows status: "+windowsState.Text+"\r\nDiagnosis: "+diagnosis.Text+"\r\n\r\n"+log.Text,Encoding.UTF8);}}
 void OpenManager(){try{Process.Start(new ProcessStartInfo("mmc.exe","devmgmt.msc"){UseShellExecute=true});}catch(Exception e){MessageBox.Show("Right-click Start > Device Manager.\n"+e.Message);}}
 void OpenStorage(){Directory.CreateDirectory(storage);Process.Start(new ProcessStartInfo("explorer.exe",Quote(storage)){UseShellExecute=true});}
 static string Quote(string s){return "\""+Regex.Replace(s,@"(\\*)"+"\"", "$1$1\\\"")+"\"";}
}
static class WizardTest {
 const BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
 static object Get(RecoveryForm f,string n){return typeof(RecoveryForm).GetField(n,Flags).GetValue(f);}
 static void Set(RecoveryForm f,string n,object v){typeof(RecoveryForm).GetField(n,Flags).SetValue(f,v);}
 static void Call(RecoveryForm f,string n,params object[] args){var value=typeof(RecoveryForm).GetMethod(n,Flags).Invoke(f,args);if(value is Task)((Task)value).GetAwaiter().GetResult();Application.DoEvents();}
 static void Assert(bool value,string why){if(!value)throw new Exception(why);}
 static int Step(RecoveryForm f){return (int)Get(f,"wizardStep");}
 static void Check(Control c){if(c is TableLayoutPanel||c is FlowLayoutPanel)for(int i=0;i<c.Controls.Count;i++)for(int j=i+1;j<c.Controls.Count;j++){var a=c.Controls[i];var b=c.Controls[j];if(a.Visible&&b.Visible&&a.Bounds.IntersectsWith(b.Bounds))throw new Exception("Overlap: "+a.Text+" / "+b.Text);}foreach(Control child in c.Controls)Check(child);}
 [STAThread]static void Main(){Application.EnableVisualStyles();
 using(var f=new RecoveryForm()){f.Show();Application.DoEvents();Assert(Step(f)==0,"chooser missing");Assert(!((Control)Get(f,"tabs")).Visible,"advanced visible before choice");Call(f,"WizardNext");Assert(Step(f)==1,"admin question missing");Set(f,"administratorCheck",new Func<bool>(()=>false));Call(f,"WizardNext");Assert(Step(f)==1,"non-admin passed");Set(f,"administratorCheck",new Func<bool>(()=>true));Call(f,"WizardNext");Assert(Step(f)==2,"connect missing");Set(f,"toolsReady",true);Set(f,"payload","mock");RecoveryForm.MockCount=0;Call(f,"WizardNext");Assert(Step(f)==3,"must wait without disk");Assert(RecoveryForm.MockRestoreCalls==0,"unexpected restore");RecoveryForm.MockCount=2;Call(f,"SimpleScan");Assert(Step(f)==3,"multiple devices guessed");RecoveryForm.MockCount=1;Call(f,"SimpleScan");Assert(Step(f)==4,"unique device missing");var input=(TextBox)Get(f,"wizardConfirmation");var next=(Button)Get(f,"wizardNext");Assert(!next.Enabled,"empty confirmation enabled restore");input.Text="restore";Call(f,"SyncWizard");Assert(!next.Enabled,"lowercase confirmation accepted");input.Text="RESTORE";Call(f,"SyncWizard");Assert(next.Enabled,"valid confirmation disabled");RecoveryForm.MockRestoreResult=0;Call(f,"WizardNext");Assert(Step(f)==6&&RecoveryForm.MockRestoreCalls==1,"verified success missing");Set(f,"simpleStarted",false);RecoveryForm.MockRestoreResult=1;Call(f,"SetWizardStep",4);input.Text="RESTORE";Call(f,"WizardNext");Assert(Step(f)==7,"failure presented as success");
  Set(f,"simpleStarted",false);
  foreach(int scale in new[]{100,125,150,200}){if(scale!=100){float factor=(float)scale/(scale==125?100:scale==150?125:150);f.Scale(new SizeF(factor,factor));}foreach(var size in new[]{new Size(980,900),new Size(800,680)}){f.MinimumSize=Size.Empty;f.Size=size;for(int step=1;step<=7;step++){Call(f,"SetWizardStep",step);for(int i=0;i<4;i++){f.PerformLayout();Application.DoEvents();}Check(f);if(scale==100&&size.Width==980){using(var image=new Bitmap(f.Width,f.Height)){using(var g=Graphics.FromImage(image))g.CopyFromScreen(f.Location,Point.Empty,f.Size);image.Save("fm1-recovery-1.0k/simple-step-"+step+".png");}}}}}
  int before=Step(f);Call(f,"ShowSimpleAbout");var about=(Form)Get(f,"aboutWindow");Assert(about.Visible&&Step(f)==before,"About changed recovery step");Check(about);about.Close();Call(f,"OpenAdvanced");Assert(!((Control)Get(f,"wizard")).Visible&&((Control)Get(f,"tabs")).Visible,"advanced switch failed");
 }
 Console.WriteLine("PASS: mode choice, admin gate, connect, no/multiple/unique device, RESTORE gate, success/failure, Advanced switch; seven screens at two sizes and four scales.");
 }
}
