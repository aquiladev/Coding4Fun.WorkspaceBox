using System;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using EnvDTE;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Aquila.Coding4Fun_WorkspaceBox
{
	/// <summary>
	/// This is the class that implements the package exposed by this assembly.
	///
	/// The minimum requirement for a class to be considered a valid package for Visual Studio
	/// is to implement the IVsPackage interface and register itself with the shell.
	/// This package uses the helper classes defined inside the Managed Package Framework (MPF)
	/// to do it: it derives from the Package class that provides the implementation of the 
	/// IVsPackage interface and uses the registration attributes defined in the framework to 
	/// register itself and its components with the shell.
	/// </summary>
	// This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
	// a package.
	[PackageRegistration(UseManagedResourcesOnly = true)]
	// This attribute is used to register the information needed to show this package
	// in the Help/About dialog of Visual Studio.
	[InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
	// This attribute is needed to let the shell know that this package exposes some menus.
	[ProvideMenuResource("Menus.ctmenu", 1)]
	[Guid(GuidList.guidCoding4Fun_WorkspaceBoxPkgString)]
	[ProvideAutoLoad("F1536EF8-92EC-443C-9ED7-FDADF150DA82")]
	public sealed class Coding4Fun_WorkspaceBoxPackage : Package
	{
		private readonly int _baseWorkspaceId = (int)PkgCmdIDList.cmdidWorkspaceListCmd;
		private readonly ArrayList _workspaceList;
		private readonly WorkspaceInfo _workspaceInfo;

		/// <summary>
		/// Default constructor of the package.
		/// Inside this method you can place any initialization code that does not require 
		/// any Visual Studio service because at this point the package object is created but 
		/// not sited yet inside Visual Studio environment. The place to do all the other 
		/// initialization is the Initialize method.
		/// </summary>
		public Coding4Fun_WorkspaceBoxPackage()
		{
			Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this.ToString()));
			_baseWorkspaceId = (int)PkgCmdIDList.cmdidWorkspaceListCmd;
			_workspaceList = new ArrayList { "branch", "checkout" };
			_workspaceInfo = Workstation.Current.GetLocalWorkspaceInfo(Environment.CurrentDirectory);
		}

		/////////////////////////////////////////////////////////////////////////////
		// Overridden Package Implementation
		#region Package Members

		/// <summary>
		/// Initialization of the package; this method is called right after the package is sited, so this is the place
		/// where you can put all the initialization code that rely on services provided by VisualStudio.
		/// </summary>
		protected override void Initialize()
		{
			Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this.ToString()));
			base.Initialize();

			// Add our command handlers for menu (commands must exist in the .vsct file)
			var mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
			if (null != mcs)
			{
				for (int i = 0; i < _workspaceList.Count; i++)
				{
					var cmdId = new CommandID(GuidList.guidCoding4Fun_WorkspaceBoxCmdSet, _baseWorkspaceId + i);
					var mc = new OleMenuCommand(OnExec, cmdId);
					mc.BeforeQueryStatus += OnQueryStatus;
					mcs.AddCommand(mc);
				}
			}
		}
		#endregion

		private void OnExec(object sender, EventArgs e)
		{
			var menuCommand = sender as OleMenuCommand;
			if (null != menuCommand)
			{
				int itemIndex = menuCommand.CommandID.ID - _baseWorkspaceId;
				if (itemIndex >= 0 && itemIndex < _workspaceList.Count)
				{
					var value = _workspaceList[itemIndex] as string;
					switch (value)
					{
						case "branch":
							menuCommand.Text = GetCurrentWorkspace();
							break;
						case "checkout":
							CheckoutCurFile();
							break;
					}
				}
			}
		}

		private void OnQueryStatus(object sender, EventArgs e)
		{
			var menuCommand = sender as OleMenuCommand;
			if (null != menuCommand)
			{
				var itemIndex = menuCommand.CommandID.ID - _baseWorkspaceId;
				if (itemIndex >= 0 && itemIndex < _workspaceList.Count)
				{
					var value = _workspaceList[itemIndex] as string;
					switch (value)
					{
						case "branch":
							value = GetCurrentWorkspace();
							break;
						//case "checkout":
						//	value = "Checkout current file";
						//	break;
					}
					menuCommand.Text = value;
				}
			}
		}

		private string GetCurrentWorkspace()
		{
			return _workspaceInfo != null ? _workspaceInfo.DisplayName : "No Workspace";
		}

		private void CheckoutCurFile()
		{
			var app = (DTE)GetService(typeof(SDTE));
			if (app.ActiveDocument != null)
			{
				var text = (TextDocument)app.ActiveDocument.Object(String.Empty);
				string activeDocumentFullName = app.ActiveDocument.FullName;
				if (text != null && text.Type.Equals("Text") && _workspaceInfo != null)
				{
					var server = new TfsTeamProjectCollection(_workspaceInfo.ServerUri);
					var workspace = _workspaceInfo.GetWorkspace(server);
					workspace.PendEdit(activeDocumentFullName);
				}
			}
		}
	}
}
