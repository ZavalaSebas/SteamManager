using System.Runtime.InteropServices;

namespace SteamManager.Steam;

/// <summary>
/// Represents the vtable pointer of a COM-style C++ object.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct NativeClass
{
    public IntPtr VirtualTable;
}

/// <summary>
/// Generic base class for calling Steam interface methods via vtable.
/// Extracts function pointers from the native vtable and wraps them as .NET delegates.
/// </summary>
public abstract class NativeWrapper<TNativeFunctions> where TNativeFunctions : struct
{
    protected IntPtr ObjectAddress;
    protected TNativeFunctions Functions;

    private readonly Dictionary<IntPtr, Delegate> _delegateCache = new();

    /// <summary>
    /// Initializes the wrapper with a native object pointer.
    /// Reads the vtable and populates the Functions struct.
    /// </summary>
    public void SetupFunctions(IntPtr objectAddress)
    {
        ObjectAddress = objectAddress;
        NativeClass nativeClass = Marshal.PtrToStructure<NativeClass>(objectAddress);
        Functions = Marshal.PtrToStructure<TNativeFunctions>(nativeClass.VirtualTable);
    }

    /// <summary>
    /// Gets a callable delegate from a vtable function pointer.
    /// </summary>
    protected TDelegate GetFunction<TDelegate>(IntPtr functionPointer) where TDelegate : Delegate
    {
        if (_delegateCache.TryGetValue(functionPointer, out Delegate? cached))
            return (TDelegate)cached;

        TDelegate del = Marshal.GetDelegateForFunctionPointer<TDelegate>(functionPointer);
        _delegateCache[functionPointer] = del;
        return del;
    }

    /// <summary>
    /// Calls a native function via vtable with ThisCall convention.
    /// </summary>
    protected TReturn Call<TReturn, TDelegate>(IntPtr functionPointer, params object[] args)
        where TDelegate : Delegate
    {
        TDelegate del = GetFunction<TDelegate>(functionPointer);
        var allArgs = new object[] { ObjectAddress }.Concat(args).ToArray();
        return (TReturn)del.DynamicInvoke(allArgs)!;
    }

    /// <summary>
    /// Calls a native function via vtable with ThisCall convention (void return).
    /// </summary>
    protected void Call<TDelegate>(IntPtr functionPointer, params object[] args)
        where TDelegate : Delegate
    {
        TDelegate del = GetFunction<TDelegate>(functionPointer);
        var allArgs = new object[] { ObjectAddress }.Concat(args).ToArray();
        del.DynamicInvoke(allArgs);
    }
}
