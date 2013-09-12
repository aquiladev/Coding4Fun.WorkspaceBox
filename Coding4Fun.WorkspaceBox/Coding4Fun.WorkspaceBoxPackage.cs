using System;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using EnvDTE;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Aquila.Coding4Fun_WorkspaceBox
{
	[PackageRegistration(UseManagedResourcesOnly = true)]
	[InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
	[ProvideMenuResource("Menus.ctmenu", 1)]
	[Guid(GuidList.guidCoding4Fun_WorkspaceBoxPkgString)]
	[ProvideAutoLoad("F1536EF8-92EC-443C-9ED7-FDADF150DA82")]
	public sealed class Coding4Fun_WorkspaceBoxPackage : Package
	{
		private readonly int _workspaceBoxId;
		private readonly int _checkoutId;
		
		private string _currentDirectory;
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
			if (null != mcs)
			{
				var cmdId = new CommandID(GuidList.guidCoding4Fun_WorkspaceBoxCmdSet, _workspaceBoxId);
				var workspaceItem = new OleMenuCommand(OnExec, cmdId);
				workspaceItem.BeforeQueryStatus += OnExec;
				mcs.AddCommand(workspaceItem);

				var checkoutBtnId = new CommandID(GuidList.guidCoding4Fun_CheckoutCmdSet, _checkoutId);
				var checkoutItem = new OleMenuCommand(OnCheckoutExec, checkoutBtnId);
				checkoutItem.BeforeQueryStatus += OnCheckoutQueryStatus;
				mcs.AddCommand(checkoutItem);
			}
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

		private void OnCheckoutExec(object sender, EventArgs e)
		{
			var menuCommand = sender as OleMenuCommand;
			if (menuCommand != null)
			{
				CheckoutCurFile();
			}
		}

		private void OnCheckoutQueryStatus(object sender, EventArgs e)
		{
			var menuCommand = sender as OleMenuCommand;
			if (menuCommand == null)
			{
				return;
			}

			menuCommand.Supported = false;
			if (HasWorkspace())
			{
				menuCommand.Supported = true;
				menuCommand.Visible = true;
				menuCommand.Text = Resources.CheckoutCurrentFile;
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
				_currentDirectory = dte.Solution.FullName;
				_workspaceInfo = Workstation.Current.GetLocalWorkspaceInfo(_currentDirectory);
			}
			return HasWorkspace() ? _workspaceInfo.DisplayName : Resources.NoWorkspace;
		}

		private void CheckoutCurFile()
		{
			var app = (DTE)GetService(typeof(SDTE));
			if (app.ActiveDocument == null)
			{
				return;
			}

			var text = (TextDocument)app.ActiveDocument.Object(String.Empty);
			var activeDocumentFullName = app.ActiveDocument.FullName;
			if (text == null || !text.Type.Equals("Text") || !HasWorkspace())
			{
				return;
			}

			var server = new TfsTeamProjectCollection(_workspaceInfo.ServerUri);
			var workspace = _workspaceInfo.GetWorkspace(server);
			workspace.PendEdit(activeDocumentFullName);
		}
	}
}
