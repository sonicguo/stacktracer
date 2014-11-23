﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
 using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace StackTracer.Utils
{
  
   

public class Injector
{
    [DllImport("kernel32.dll")]
public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

 [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
public static extern IntPtr GetModuleHandle(string lpModuleName);

[DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

[DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress,uint dwSize, uint flAllocationType, uint flProtect);

    [DllImport("kernel32.dll", SetLastError = true)]
    static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out UIntPtr lpNumberOfBytesWritten);
 
    [DllImport("kernel32.dll")]
    static extern IntPtr CreateRemoteThread(IntPtr hProcess,
        IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    static extern bool CreateProcess(string lpApplicationName,
       string lpCommandLine, ref SECURITY_ATTRIBUTES lpProcessAttributes,
       ref SECURITY_ATTRIBUTES lpThreadAttributes, bool bInheritHandles,
       uint dwCreationFlags, IntPtr lpEnvironment, string lpCurrentDirectory,
       [In] ref STARTUPINFO lpStartupInfo,
       out PROCESS_INFORMATION lpProcessInformation);

 [StructLayout(LayoutKind.Sequential, Pack=1)]
public struct RemoteThreadParams
{
     [MarshalAs(UnmanagedType.LPWStr)]
     public string lpApplicationName;

     [MarshalAs(UnmanagedType.LPWStr)]
     public string lpCommandLine;
          
     public IntPtr lpProcessAttributes;

     public IntPtr lpThreadAttributes;

     [MarshalAs(UnmanagedType.Bool)]
     public bool bInheritHandles;

     [MarshalAs(UnmanagedType.U4)]
     public uint dwCreationFlags;

     public IntPtr lpEnvironment;
     
     [MarshalAs(UnmanagedType.LPWStr)]
     public string lpCurrentDirectory;

     public IntPtr lpStartupInfo;

     public IntPtr lpProcessInformation;
}

    [StructLayout(LayoutKind.Sequential)]
    public struct SECURITY_ATTRIBUTES
    {
        public int nLength;
        public unsafe byte* lpSecurityDescriptor;
        public int bInheritHandle;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    struct STARTUPINFO
    {
        public Int32 cb;
        public string lpReserved;
        public string lpDesktop;
        public string lpTitle;
        public Int32 dwX;
        public Int32 dwY;
        public Int32 dwXSize;
        public Int32 dwYSize;
        public Int32 dwXCountChars;
        public Int32 dwYCountChars;
        public Int32 dwFillAttribute;
        public Int32 dwFlags;
        public Int16 wShowWindow;
        public Int16 cbReserved2;
        public IntPtr lpReserved2;
        public IntPtr hStdInput;
        public IntPtr hStdOutput;
        public IntPtr hStdError;
    }
    [StructLayout(LayoutKind.Sequential)]
    internal struct PROCESS_INFORMATION
    {
        public IntPtr hProcess;
        public IntPtr hThread;
        public int dwProcessId;
        public int dwThreadId;
    }
    // privileges
    const int PROCESS_CREATE_THREAD = 0x0002;
    const int PROCESS_QUERY_INFORMATION = 0x0400;
    const int PROCESS_VM_OPERATION = 0x0008;
    const int PROCESS_VM_WRITE = 0x0020;
    const int PROCESS_VM_READ = 0x0010;
 
    // used for memory allocation
    const uint MEM_COMMIT = 0x00001000;
    const uint MEM_RESERVE = 0x00002000;
    const uint PAGE_READWRITE = 4;
 
    public static int InjectAndRun(int processid,string applicationpath, string stacktracercmd)
    {
        // the target process - I'm using a dummy process for this
        // if you don't have one, open Task Manager and choose wisely
        //Process targetProcess = Process.GetProcessesByName("testApp")[0];
 
        // geting the handle of the process - with required privileges
        IntPtr procHandle = OpenProcess(PROCESS_CREATE_THREAD | PROCESS_QUERY_INFORMATION | PROCESS_VM_OPERATION | PROCESS_VM_WRITE | PROCESS_VM_READ, false, processid);
        
        // searching for the address of LoadLibraryA and storing it in a pointer
        IntPtr loadLibraryAddr = GetProcAddress(GetModuleHandle("kernel32.dll"), "CreateProcessW");
 
        // name of the dll we want to inject
        //string dllName = "test.dll";

        RemoteThreadParams myparams=new RemoteThreadParams();

        STARTUPINFO si= new STARTUPINFO();
        PROCESS_INFORMATION pi= new PROCESS_INFORMATION();

        myparams.lpApplicationName = applicationpath;
        myparams.lpCommandLine = stacktracercmd;
        myparams.lpCurrentDirectory = "";
        myparams.lpEnvironment = IntPtr.Zero;
        myparams.lpProcessAttributes = IntPtr.Zero;
        //myparams.lpProcessInformation = IntPtr.Zero;
        //myparams.lpStartupInfo = IntPtr.Zero;
        myparams.lpThreadAttributes = IntPtr.Zero;
        myparams.bInheritHandles = true;

        // Allocate some native heap memory in your process big enough to store the
        // parameter data
        IntPtr iptrtoparams = Marshal.AllocHGlobal(Marshal.SizeOf(myparams));

        // Use to alloc "committed" memory that is addressable by other process
        //IntPtr iptrremoteallocatedmemory =

        IntPtr remoteallocMemAddress = VirtualAllocEx(procHandle, IntPtr.Zero, (uint)Marshal.SizeOf(myparams), MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
        IntPtr remoteallocMemAddresssi = VirtualAllocEx(procHandle, IntPtr.Zero, (uint)Marshal.SizeOf(si), MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
        IntPtr remoteallocMemAddresspi = VirtualAllocEx(procHandle, IntPtr.Zero, (uint)Marshal.SizeOf(pi), MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);


        //Marshal.StructureToPtr(si, myparams.lpProcessInformation, false);
        //Marshal.StructureToPtr(pi, myparams.lpStartupInfo, false);

        myparams.lpProcessInformation = remoteallocMemAddresspi;
        myparams.lpStartupInfo = remoteallocMemAddresssi;

        // Copies the data in your structure into the native heap memory just allocated
        Marshal.StructureToPtr(myparams, iptrtoparams, false);

        byte[] paramsbytes=new byte[Marshal.SizeOf(myparams)];
        
        for (int i = 0; i < paramsbytes.Length; i++)
        {
            paramsbytes[i] = Marshal.ReadByte(iptrtoparams, i);
        }

        // alocating some memory on the target process - enough to store the name of the dll
        // and storing its address in a pointer
        //IntPtr allocMemAddress = VirtualAllocEx(procHandle, IntPtr.Zero, (uint)((dllName.Length + 1) * Marshal.SizeOf(typeof(char))), MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);

        
 
        // writing the name of the dll there
        UIntPtr bytesWritten;
//        WriteProcessMemory(procHandle, allocMemAddress, Encoding.Default.GetBytes(dllName), (uint)((dllName.Length + 1) * Marshal.SizeOf(typeof(char))), out bytesWritten);

        WriteProcessMemory(procHandle, remoteallocMemAddress,paramsbytes, (uint)Marshal.SizeOf(myparams), out bytesWritten);
 
        // creating a thread that will call LoadLibraryA with allocMemAddress as argument
        CreateRemoteThread(procHandle, IntPtr.Zero, 0, loadLibraryAddr, remoteallocMemAddress, 0, IntPtr.Zero);
 
       return 0;
    }
}

}
