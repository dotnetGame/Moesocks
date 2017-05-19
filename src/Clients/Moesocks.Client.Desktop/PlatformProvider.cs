using Moesocks.Client.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Moesocks.Client
{
    class PlatformProvider : IPlatformProvider
    {
        public void UnsetProxy()
        {
            SetProxy(null, null);
        }

        public void SetProxy(string strProxy)
        {
            SetProxy(strProxy, null);
        }

        public static unsafe void SetProxy(string strProxy, string exceptions)
        {
            int optionCount = string.IsNullOrEmpty(strProxy) ? 1 : (string.IsNullOrEmpty(exceptions) ? 2 : 3);
            var options = new InternetConnectionOption[optionCount];
            // USE a proxy server ...  
            options[0].dwOption = PerConnOption.INTERNET_PER_CONN_FLAGS;
            options[0].Value.dwValue = (uint)((optionCount < 2) ? PerConnFlags.PROXY_TYPE_DIRECT : (PerConnFlags.PROXY_TYPE_DIRECT | PerConnFlags.PROXY_TYPE_PROXY));
            fixed(char* strProxyPtr = strProxy)
                fixed(char* exceptionsPtr = exceptions)
            {
                // use THIS proxy server  
                if (optionCount > 1)
                {
                    options[1].dwOption = PerConnOption.INTERNET_PER_CONN_PROXY_SERVER;
                    options[1].Value.pszValue = strProxyPtr;
                    // except for these addresses ...  
                    if (optionCount > 2)
                    {
                        options[2].dwOption = PerConnOption.INTERNET_PER_CONN_PROXY_BYPASS;
                        options[2].Value.pszValue = exceptionsPtr;
                    }
                }
                NativeMethods.InternetSetOptionForAllConnections(IntPtr.Zero, InternetOption.INTERNET_OPTION_PER_CONNECTION_OPTION, options);
            }
        }
    }

    #region WinInet structures  

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct InternetConnectionOption
    {
        public PerConnOption dwOption;
        public InternetConnectionOptionValue Value;

        [StructLayout(LayoutKind.Explicit)]
        public struct InternetConnectionOptionValue
        {
            [FieldOffset(0)]
            public uint dwValue;
            [FieldOffset(0)]
            public unsafe char* pszValue;
            [FieldOffset(0)]
            public System.Runtime.InteropServices.ComTypes.FILETIME ftValue;
        }
    }
    #endregion

    #region WinInet enums  
    //  
    // options manifests for Internet{Query|Set}Option  
    //  
    public enum InternetOption : uint
    {
        INTERNET_OPTION_PER_CONNECTION_OPTION = 75
    }

    //  
    // Options used in INTERNET_PER_CONN_OPTON struct  
    //  
    public enum PerConnOption
    {
        INTERNET_PER_CONN_FLAGS = 1, // Sets or retrieves the connection type. The Value member will contain one or more of the values from PerConnFlags   
        INTERNET_PER_CONN_PROXY_SERVER = 2, // Sets or retrieves a string containing the proxy servers.    
        INTERNET_PER_CONN_PROXY_BYPASS = 3, // Sets or retrieves a string containing the URLs that do not use the proxy server.    
        INTERNET_PER_CONN_AUTOCONFIG_URL = 4//, // Sets or retrieves a string containing the URL to the automatic configuration script.    

    }

    //  
    // PER_CONN_FLAGS  
    //  
    [Flags]
    public enum PerConnFlags
    {
        PROXY_TYPE_DIRECT = 0x00000001,  // direct to net  
        PROXY_TYPE_PROXY = 0x00000002,  // via named proxy  
        PROXY_TYPE_AUTO_PROXY_URL = 0x00000004,  // autoproxy URL  
        PROXY_TYPE_AUTO_DETECT = 0x00000008   // use autoproxy detection  
    }
    #endregion

    internal static class NativeMethods
    {
        [DllImport("WinInet.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool InternetSetOption(IntPtr hInternet, InternetOption dwOption, IntPtr lpBuffer, uint dwBufferLength);

        [DllImport("rasapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern uint RasEnumEntries(IntPtr reserved, IntPtr lpszPhonebook, [In, Out] RASENTRYNAME[] lprasentryname, ref uint lpcb, ref uint lpcEntries);

        private unsafe static IReadOnlyList<string> EnumAllConnections()
        {
            uint dwCb = 0;
            uint dwEntries = 0;
            RASENTRYNAME[] lpRasEntryName = null;

            if(RasEnumEntries(IntPtr.Zero, IntPtr.Zero, lpRasEntryName, ref dwCb, ref dwEntries) == ERROR_BUFFER_TOO_SMALL)
            {
                lpRasEntryName = new RASENTRYNAME[dwEntries];
                lpRasEntryName[0].dwSize = Marshal.SizeOf<RASENTRYNAME>();
                var retVal = RasEnumEntries(IntPtr.Zero, IntPtr.Zero, lpRasEntryName, ref dwCb, ref dwEntries);
                if (retVal != ERROR_SUCCESS)
                    Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                return lpRasEntryName.Select(o => o.szEntryName).ToList();
            }
            return new string[0];
        }

        public static unsafe void InternetSetOptionForAllConnections(IntPtr hInternet, InternetOption dwOption, InternetConnectionOption[] options)
        {
            fixed(InternetConnectionOption* optionsPtr = options)
            {
                var optionList = new InternetPerConnOptionList
                {
                    pOptions = optionsPtr,
                    szConnection = null,
                    dwOptionCount = (uint)options.Length
                };
                var bufferSize = Marshal.SizeOf(optionList);
                optionList.dwSize = (uint)bufferSize;
                var bufferPtr = stackalloc byte[bufferSize];
                var bufferIntPtr = (IntPtr)bufferPtr;
                bool first = true;

                foreach (var connection in new string[] { null }.Concat(EnumAllConnections()))
                {
                    optionList.szConnection = connection;
                    Marshal.StructureToPtr(optionList, bufferIntPtr, !first);
                    first = false;
                    if (!InternetSetOption(hInternet, dwOption, bufferIntPtr, optionList.dwSize))
                        Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                }
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct InternetPerConnOptionList
        {
            public uint dwSize;               // size of the INTERNET_PER_CONN_OPTION_LIST struct  

            [MarshalAs(UnmanagedType.LPTStr)]
            public string szConnection;         // connection name to set/query options  
            public uint dwOptionCount;        // number of options to set/query  
            public uint dwOptionError;           // on error, which option failed  
            
            public unsafe InternetConnectionOption* pOptions;
        };

        const int MAX_PATH = 260;
        const int RAS_MaxEntryName = 256;
        const uint RSABASE = 600;
        const uint ERROR_BUFFER_TOO_SMALL = RSABASE + 3;
        const uint ERROR_SUCCESS = 0;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        struct RASENTRYNAME
        {
            public int dwSize;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = RAS_MaxEntryName + 1)]
            public string szEntryName;
            public int dwFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_PATH + 1)]
            public string szPhonebook;
        }
    }
}