using System.Runtime.InteropServices;

namespace SteamManager.Steam;

/// <summary>
/// P/Invoke declarations for kernel32.dll and user32.dll.
/// Used to load steamclient.dll and resolve its exports.
/// </summary>
internal static partial class NativeMethods
{
    [LibraryImport("kernel32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16, EntryPoint = "LoadLibraryExW")]
    internal static partial IntPtr LoadLibraryEx(string path, IntPtr file, uint flags);

    [LibraryImport("kernel32.dll", SetLastError = true, EntryPoint = "GetProcAddress")]
    internal static partial IntPtr GetProcAddress(IntPtr module, [MarshalAs(UnmanagedType.LPStr)] string name);

    [LibraryImport("kernel32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16, EntryPoint = "SetDllDirectoryW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool SetDllDirectory(string path);

    internal const uint LoadWithAlteredSearchPath = 8;
}