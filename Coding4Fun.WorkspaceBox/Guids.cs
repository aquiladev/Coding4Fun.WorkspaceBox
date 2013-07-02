﻿// Guids.cs
// MUST match guids.h
using System;

namespace Aquila.Coding4Fun_WorkspaceBox
{
	static class GuidList
	{
		public const string guidCoding4Fun_WorkspaceBoxPkgString = "593fa676-e509-443f-b1de-2237d5f7e545";
		public const string guidCoding4Fun_WorkspaceBoxCmdSetString = "458389fd-2202-4e28-9113-d4cb39de5a11";
		public const string guidCoding4Fun_CheckoutCmdSetString = "548389fd-2202-4e28-9113-d4cb39de5a22";

		public static readonly Guid guidCoding4Fun_WorkspaceBoxCmdSet = new Guid(guidCoding4Fun_WorkspaceBoxCmdSetString);
		public static readonly Guid guidCoding4Fun_CheckoutCmdSet = new Guid(guidCoding4Fun_CheckoutCmdSetString);
	};
}