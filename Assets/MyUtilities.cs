using System;
using System.Runtime.InteropServices;


public class MyStringClassWrapper
{
    #if UNITY_STANDALONE_WIN
    const string dllName = "MyLibrary.dll";
    #elif UNITY_STANDALONE_LINUX
    const string dllName = "libMyLibrary.so";
    #else
    const string dllName = null; // Unsupported platform
    #endif

    [DllImport(dllName)]
    public static extern IntPtr MyStringClass_Create();

    [DllImport(dllName)]
    public static extern void MyStringClass_Delete(IntPtr instance);

    [DllImport(dllName)]
    public static extern int MyStringClass_GetStringSize(IntPtr instance);

    [DllImport(dllName)]
    public static extern void MyStringClass_GetString(IntPtr instance, IntPtr buffer, int bufferSize);


    private IntPtr MyStringClass;
    public MyStringClassWrapper(){
        if (dllName != null)
        {
            MyStringClass = MyStringClass_Create();
        }
        else
        {
            throw new Exception("Unsupported platform");
        }
    }

    public string GetString()
    {
        string result;
        // First, get the required buffer size
        int bufferSize = MyStringClass_GetStringSize(MyStringClass);

        // Allocate a buffer of the required size
        IntPtr buffer = Marshal.AllocHGlobal(bufferSize);

        try
        {
            // Call the function again to get the actual string
            MyStringClass_GetString(MyStringClass, buffer, bufferSize);

            // Convert the IntPtr to a string
            result = Marshal.PtrToStringAnsi(buffer);
        }
        finally
        {
            // Free the allocated buffer
            Marshal.FreeHGlobal(buffer);
        }
        return result;
    }
}
