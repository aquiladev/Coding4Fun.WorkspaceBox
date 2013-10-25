using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using EnvDTE;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TeamFoundation.VersionControl;

namespace Aquila.Coding4Fun_WorkspaceBox
{
	[PackageRegistration(UseManagedResourcesOnly = true)]
	[InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
	[ProvideMenuResource("Menus.ctmenu", 1)]
	[Guid(GuidList.GuidCoding4Fun_WorkspaceBoxPkgString)]
	[ProvideAutoLoad("F1536EF8-92EC-443C-9ED7-FDADF150DA82")]
	public sealed class Coding4Fun_WorkspaceBoxPackage : Package
	{
		private readonly int _workspaceBoxId;
		private readonly int _checkoutId;

		private string _currentDirectory;
		private string _currentFile;
		private WorkspaceInfo _workspaceInfo;

		public Coding4Fun_WorkspaceBoxPackage()
		{
			_workspaceBoxId = (int)PkgCmdIDList.cmdidWorkspaceBoxCmd;
			_checkoutId = (int)PkgCmdIDList.cmdidCheckoutCmd;
		}

		#region Package Members
		protected override void Initialize()
		{
			Debug.WriteLine("================Initialize {0}====================", _i1++);
			base.Initialize();

			var mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
			if (null == mcs)
			{
				return;
			}

			var cmdId = new CommandID(GuidList.GuidCoding4FunCmdSet, _workspaceBoxId);
			var workspaceItem = new OleMenuCommand(OnGoToSourceControl, cmdId);
			workspaceItem.BeforeQueryStatus += OnExec;
			mcs.AddCommand(workspaceItem);

			var checkoutBtnId = new CommandID(GuidList.GuidCoding4FunCmdSet, _checkoutId);
			var checkoutItem = new OleMenuCommand(OnCheckoutExec, checkoutBtnId);
			checkoutItem.BeforeQueryStatus += OnCheckoutQueryStatus;
			mcs.AddCommand(checkoutItem);
		}
		#endregion

		private void OnExec(object sender, EventArgs e)
		{
			//~300 t
			Debug.WriteLine("================OnExec {0}====================", _oe1++);
			var menuCommand = sender as OleMenuCommand;
			if (menuCommand != null)
			{
				menuCommand.Text = GetCurrentWorkspace();
			}
		}

		private void OnGoToSourceControl(object sender, EventArgs e)
		{
			Debug.WriteLine("================OnGoToSourceControl {0}====================", _ogs1++);
			var menuCommand = sender as OleMenuCommand;
			if (menuCommand != null)
			{
				var app = (DTE)GetService(typeof(SDTE));

				if (app.ActiveDocument != null)
				{
					var path = app.ActiveDocument.FullName;
					var vc = app.GetObject("Microsoft.VisualStudio.TeamFoundation.VersionControl.VersionControlExt") as VersionControlExt;
					if (vc != null)
					{
						_workspaceInfo = Workstation.Current.GetLocalWorkspaceInfo(path);
						var server = new TfsTeamProjectCollection(_workspaceInfo.ServerUri);
						var workspace = _workspaceInfo.GetWorkspace(server);
						var serverPath = workspace.GetServerItemForLocalItem(path);
						vc.Explorer.Navigate(serverPath);
						app.ExecuteCommand("View.TfsSourceControlExplorer");
					}
				}
			}
		}

		private void OnCheckoutQueryStatus(object sender, EventArgs e)
		{
			//~280 t
			Debug.WriteLine("================OnCheckoutQueryStatus {0}====================", _ocq1++);
			var menuCommand = sender as OleMenuCommand;
			if (menuCommand == null)
			{
				return;
			}

			var dte = (_DTE)GetService(typeof(_DTE));
			var doc = dte.ActiveDocument == null
				? null
				: (TextDocument)dte.ActiveDocument.Object();
			if (doc != null && _currentFile == dte.ActiveDocument.FullName)
			{
				return;
			}
			_currentFile = dte.ActiveDocument.FullName;
			menuCommand.Supported = false;
			if (!HasWorkspace() || HasPandingChangesAlready())
			{
				return;
			}

			menuCommand.Supported = true;
			menuCommand.Visible = true;
			menuCommand.Text = Resources.CheckoutCurrentFile;
		}

		private void OnCheckoutExec(object sender, EventArgs e)
		{
			Debug.WriteLine("================OnCheckoutExec {0}====================", _oce1++);
			var menuCommand = sender as OleMenuCommand;
			if (menuCommand != null)
			{
				CheckoutCurFile();
			}
		}

		private bool HasWorkspace()
		{
			//~ 320 t
			Debug.WriteLine("================HasWorkspace {0}====================", _hw1++);
			return _workspaceInfo != null;
		}

		private string GetCurrentWorkspace()
		{
			//~ 320 t
			Debug.WriteLine("================GetCurrentWorkspace {0}====================", _gcw1++);
			var dte = (_DTE)GetService(typeof(_DTE));
			if (string.IsNullOrEmpty(_currentDirectory) ||
				_currentDirectory != dte.Solution.FullName)
			{
				_currentDirectory = dte.Solution.FullName;
				_workspaceInfo = Workstation.Current.GetLocalWorkspaceInfo(_currentDirectory);
			}
			return HasWorkspace() ? _workspaceInfo.DisplayName : Resources.NoWorkspace;
		}

		private bool HasPandingChangesAlready()
		{
			// 1 t
			Debug.WriteLine("================HasPandingChangesAlready {0}====================", _hpca++);
			var app = (DTE)GetService(typeof(SDTE));
			var doc = app.ActiveDocument == null
				? null
				: (TextDocument)app.ActiveDocument.Object();
			if (!IsCanPendEdit(doc) || app.ActiveDocument == null)
			{
				return false;
			}

			var activeDocumentFullName = app.ActiveDocument.FullName;
			var server = new TfsTeamProjectCollection(_workspaceInfo.ServerUri);
			var workspace = _workspaceInfo.GetWorkspace(server);
			return workspace.GetPendingChanges(activeDocumentFullName).Any();
		}

		private void CheckoutCurFile()
		{
			Debug.WriteLine("================CheckoutCurFile {0}====================", _ccf++);
			var app = (DTE)GetService(typeof(SDTE));
			var doc = app.ActiveDocument == null
				? null
				: (TextDocument)app.ActiveDocument.Object();
			if (HasWorkspace() && IsCanPendEdit(doc) && app.ActiveDocument != null)
			{
				var activeDocumentFullName = app.ActiveDocument.FullName;
				var server = new TfsTeamProjectCollection(_workspaceInfo.ServerUri);
				var workspace = _workspaceInfo.GetWorkspace(server);
				workspace.PendEdit(activeDocumentFullName);

				RefreshPendingChanges(app);
			}
		}

		private static bool IsCanPendEdit(TextDocument doc)
		{
			// 1 t
			Debug.WriteLine("================IsCanPendEdit {0}====================", _icpe++);
			return doc != null && doc.Type.Equals("Text");
		}

		private static void RefreshPendingChanges(_DTE application)
		{
			Debug.WriteLine("================RefreshPendingChanges {0}====================", _rpc++);
			application.ExecuteCommand("View.TfsPendingChanges");
			application.ExecuteCommand("View.Refresh");
		}

		private int _i1;
		private int _oe1;
		private int _ogs1;
		private int _ocq1;
		private int _oce1;
		private int _hw1;
		private int _gcw1;
		private int _hpca;
		private int _ccf;
		private static int _icpe;
		private static int _rpc;
	}
}
