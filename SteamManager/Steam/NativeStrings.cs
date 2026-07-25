using System.Runtime.InteropServices;
using System.Text;

namespace SteamManager.Steam;

/// <summary>
/// Provides UTF-8 string marshaling between managed and native code.
/// Used for passing strings to/from steamclient.dll vtable functions.
/// </summary>
public static class NativeStrings
{
    /// <summary>
    /// Converts a managed string to a native UTF-8 string handle.
    /// The caller must dispose the returned SafeHandle to free memory.
    /// </summary>
    public static unsafe StringHandle StringToStringHandle(string str)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(str + '\0');
        IntPtr ptr = Marshal.AllocHGlobal(bytes.Length);
        Marshal.Copy(bytes, 0, ptr, bytes.Length);
        return new StringHandle(ptr);
    }

    /// <summary>
    /// Converts a native UTF-8 string pointer to a managed string.
    /// </summary>
    public static string PointerToString(IntPtr ptr)
    {
        if (ptr == IntPtr.Zero)
            return string.Empty;

        int length = 0;
        while (Marshal.ReadByte(ptr, length) != 0)
            length++;

        if (length == 0)
            return string.Empty;

        byte[] buffer = new byte[length];
        Marshal.Copy(ptr, buffer, 0, length);
        return Encoding.UTF8.GetString(buffer);
    }

    /// <summary>
    /// SafeHandle for native UTF-8 strings that frees memory on disposal.
    /// </summary>
    public class StringHandle : SafeHandle
    {
        public StringHandle(IntPtr preexistingHandle)
            : base(IntPtr.Zero, ownsHandle: true)
        {
            SetHandle(preexistingHandle);
        }

        public override bool IsInvalid => handle == IntPtr.Zero;

        protected override bool ReleaseHandle()
        {
            Marshal.FreeHGlobal(handle);
            return true;
        }
    }
}
