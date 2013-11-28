// Guids.cs
// MUST match guids.h
using System;

namespace Aquila.Coding4Fun_WorkspaceBox
{
    static class GuidList
    {
        public const string GuidCoding4Fun_WorkspaceBoxPkgString = "EC96722B-F4FB-432B-B4C3-5A3B33539698";
        public const string GuidCoding4FunCmdSetString = "1508C2F6-4C2C-4880-B5E2-64750036AADF";

        public static readonly Guid GuidCoding4FunCmdSet = new Guid(GuidCoding4FunCmdSetString);
    };
}