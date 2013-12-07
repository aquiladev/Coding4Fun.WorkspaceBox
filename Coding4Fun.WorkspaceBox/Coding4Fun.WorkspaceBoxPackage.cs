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
			var menuCommand = sender as OleMenuCommand;
			if (menuCommand != null)
			{
				menuCommand.Text = GetCurrentWorkspace();
			}
		}

		private void OnGoToSourceControl(object sender, EventArgs e)
		{
			var menuCommand = sender as OleMenuCommand;
			if (menuCommand != null)
			{
				var app = (DTE)GetService(typeof(SDTE));

				if (app.ActiveDocument != null)
				{
					var path = app.ActiveDocument.FullName;
					var vc = app.GetObject("Microsoft.VisualStudio.TeamFoundation.VersionControl.VersionControlExt") as VersionControlExt;
					if (vc != null && vc.Explorer != null)
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
			var menuCommand = sender as OleMenuCommand;
			if (menuCommand == null)
			{
				return;
			}

			var dte = (_DTE)GetService(typeof(_DTE));
			var doc = dte.ActiveDocument == null
				? null
				: (TextDocument)dte.ActiveDocument.Object();
			if (doc == null ||
				dte.ActiveWindow != ((EnvDTE.Document)(dte.ActiveDocument)).ActiveWindow)
			{
				_currentFile = string.Empty;
				menuCommand.Supported = false;
				return;
			}
			if (_currentFile == dte.ActiveDocument.FullName)
			{
				return;
			}

			menuCommand.Supported = false;
			_currentFile = dte.ActiveDocument.FullName;
			if (!HasWorkspace() || HasPandingChangesAlready())
			{
				return;
			}

			menuCommand.Supported = true;
			menuCommand.Text = Resources.CheckoutCurrentFile;
		}

		private void OnCheckoutExec(object sender, EventArgs e)
		{
			var menuCommand = sender as OleMenuCommand;
			if (menuCommand != null)
			{
				CheckoutCurFile();
			}
		}

		private bool HasWorkspace()
		{
			return _workspaceInfo != null;
		}

		private string GetCurrentWorkspace()
		{
			var dte = (_DTE)GetService(typeof(_DTE));
			if (string.IsNullOrEmpty(_currentDirectory) ||
				_currentDirectory != dte.Solution.FullName)
			{
				if (!string.IsNullOrEmpty(dte.Solution.FullName))
				{
					_currentDirectory = dte.Solution.FullName;
					_workspaceInfo = Workstation.Current.GetLocalWorkspaceInfo(_currentDirectory);
				}
				else
				{
					_workspaceInfo = null;
					_currentDirectory = string.Empty;
				}
			}
			return HasWorkspace() ? _workspaceInfo.DisplayName : Resources.NoWorkspace;
		}

		private bool HasPandingChangesAlready()
		{
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
			return doc != null && doc.Type.Equals("Text");
		}

		private static void RefreshPendingChanges(_DTE application)
		{
			try
			{
				application.ExecuteCommand("View.TfsPendingChanges");
				application.ExecuteCommand("View.Refresh");
			}
			catch (COMException)
			{
				//TODO: add notification
			}
		}
	}
}