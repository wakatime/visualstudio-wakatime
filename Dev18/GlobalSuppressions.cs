// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Usage", "VSTHRD010:Invoke single-threaded types on Main thread", Justification = "", Scope = "member", Target = "~M:WakaTime.ExtensionUtils.Logger.GetWakatimeOutputWindowPane~Microsoft.VisualStudio.Shell.Interop.IVsOutputWindowPane")]
[assembly: SuppressMessage("Usage", "VSTHRD010:Invoke single-threaded types on Main thread", Justification = "", Scope = "member", Target = "~M:WakaTime.WakaTimePackage.DocEventsOnDocumentOpened(EnvDTE.Document)")]
[assembly: SuppressMessage("Usage", "VSTHRD010:Invoke single-threaded types on Main thread", Justification = "", Scope = "member", Target = "~M:WakaTime.WakaTimePackage.GetProjectName~System.String")]
[assembly: SuppressMessage("Usage", "VSTHRD010:Invoke single-threaded types on Main thread", Justification = "", Scope = "member", Target = "~M:WakaTime.WakaTimePackage.DocEventsOnDocumentSaved(EnvDTE.Document)")]
[assembly: SuppressMessage("Usage", "VSTHRD010:Invoke single-threaded types on Main thread", Justification = "", Scope = "member", Target = "~M:WakaTime.WakaTimePackage.WindowEventsOnWindowActivated(EnvDTE.Window,EnvDTE.Window)")]
[assembly: SuppressMessage("Usage", "VSTHRD010:Invoke single-threaded types on Main thread", Justification = "", Scope = "member", Target = "~M:WakaTime.WakaTimePackage.SolutionEventsOnOpened")]
[assembly: SuppressMessage("Usage", "VSTHRD010:Invoke single-threaded types on Main thread", Justification = "", Scope = "member", Target = "~M:WakaTime.WakaTimePackage.GetCurrentProjectOutputForCurrentConfiguration~System.String")]
[assembly: SuppressMessage("Usage", "VSTHRD010:Invoke single-threaded types on Main thread", Justification = "", Scope = "member", Target = "~M:WakaTime.WakaTimePackage.GetProjectOutputForConfiguration(System.String,System.String,System.String)~System.String")]
[assembly: SuppressMessage("Usage", "VSTHRD010:Invoke single-threaded types on Main thread", Justification = "", Scope = "member", Target = "~M:WakaTime.WakaTimePackage.TextEditorEventsLineChanged(EnvDTE.TextPoint,EnvDTE.TextPoint,System.Int32)")]
[assembly: SuppressMessage("Usage", "VSTHRD010:Invoke single-threaded types on Main thread", Justification = "", Scope = "member", Target = "~M:WakaTime.ExtensionUtils.Logger.Log(WakaTime.Shared.ExtensionUtils.LogLevel,System.String)")]
[assembly: SuppressMessage("Usage", "VSTHRD010:Invoke single-threaded types on Main thread", Justification = "", Scope = "member", Target = "~M:WakaTime.WakaTimePackage.InitializeAsync(System.Threading.CancellationToken,System.IProgress{Microsoft.VisualStudio.Shell.ServiceProgressData})~System.Threading.Tasks.Task")]
[assembly: SuppressMessage("ApiDesign", "RS0030:Do not used banned APIs", Justification = "", Scope = "member", Target = "~M:WakaTime.ExtensionUtils.Logger.Log(WakaTime.Shared.ExtensionUtils.LogLevel,System.String)")]
