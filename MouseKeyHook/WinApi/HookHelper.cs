// This code is distributed under MIT license. 
// Copyright (c) 2015 George Mamaladze
// See license.txt or https://mit-license.org/

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Gma.System.MouseKeyHook.Implementation;

namespace Gma.System.MouseKeyHook.WinApi
{
    internal static class HookHelper
    {
        private static HookProcedure _appHookProc;
        private static HookProcedure _globalHookProc;
        private static HookResult _hookResult;

        public static HookResult HookAppMouse(Callback callback)
        {
            return HookApp(HookIds.WH_MOUSE, callback);
        }

        public static HookResult HookAppKeyboard(Callback callback)
        {
            return HookApp(HookIds.WH_KEYBOARD, callback);
        }

        public static HookResult HookGlobalMouse(Callback callback)
        {
            return HookGlobal(HookIds.WH_MOUSE_LL, callback);
        }

        public static HookResult HookGlobalKeyboard(Callback callback)
        {
            return HookGlobal(HookIds.WH_KEYBOARD_LL, callback);
        }

        private static HookResult HookApp(int hookId, Callback callback)
        {
            _appHookProc = (code, param, lParam) => HookProcedure(code, param, lParam, callback);

            var hookHandle = HookNativeMethods.SetWindowsHookEx(
                hookId,
                _appHookProc,
                IntPtr.Zero,
                ThreadNativeMethods.GetCurrentThreadId());

            if (hookHandle.IsInvalid)
                ThrowLastUnmanagedErrorAsException();

            return new HookResult(hookHandle, _appHookProc);
        }

        private static HookResult HookGlobal(int hookId, Callback callback)
        {
            _globalHookProc = (code, param, lParam) => HookProcedure(code, param, lParam, callback);

            var hookHandle = HookNativeMethods.SetWindowsHookEx(
                hookId,
                _globalHookProc,
                IntPtr.Zero /*Process.GetCurrentProcess().MainModule.BaseAddress*/, // For WH_KEYBOARD_LL, it's safe to pass IntPtr.Zero
                0);

            if (hookHandle.IsInvalid)
                ThrowLastUnmanagedErrorAsException();

            _hookResult = new HookResult(hookHandle, _globalHookProc);
            return _hookResult;
        }

        // private static IntPtr HookProcedure(int nCode, IntPtr wParam, IntPtr lParam, Callback callback)
        // {
        //     var passThrough = nCode != 0;
        //     if (passThrough)
        //         return CallNextHookEx(nCode, wParam, lParam);
        //
        //     var callbackData = new CallbackData(wParam, lParam);
        //     var continueProcessing = callback(callbackData);
        //
        //     if (!continueProcessing)
        //         return new IntPtr(-1);
        //
        //     return CallNextHookEx(nCode, wParam, lParam);
        // }

        /*private static IntPtr HookProcedure(int nCode, IntPtr wParam, IntPtr lParam, Callback callback)
        {
            //When shifting to get diacritics / special characters, we don't want to process this key, but simple pass it on
            if (nCode < 0)
            {
                // Pass the event to the next hook without processing
                return CallNextHookEx(nCode, wParam, lParam);
            }

            // Create callback data from wParam and lParam
            var callbackData = new CallbackData(wParam, lParam);

            // Execute the callback to determine if processing should continue
            var continueProcessing = callback(callbackData);

            if (!continueProcessing)
            {
                // Block the event by returning a non-zero value
                return new IntPtr(1);
            }

            // Always call the next hook to allow proper processing
            return CallNextHookEx(nCode, wParam, lParam);
        }*/
        
        private static IntPtr HookProcedure(int nCode, IntPtr wParam, IntPtr lParam, Callback callback)
        {
            if (nCode < 0)
            {
                // Pass the event to the next hook without processing
                return HookNativeMethods.CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
            }

            var callbackData = new CallbackData(wParam, lParam);
            
            // Always allow processing to continue
            var continueProcessing = callback(callbackData);
            //Console.WriteLine($"HookProcedure: nCode={nCode}, wParam={wParam}, lParam={lParam}, continueProcessing={continueProcessing}");
            
            if (!continueProcessing)
            {
                //Console.WriteLine("HookProcedure: Blocking the event.");
                return new IntPtr(1); // Blocking the event
            }

            // Always allow the event to be processed further
            return HookNativeMethods.CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
        }

        /*private static IntPtr CallNextHookEx(int nCode, IntPtr wParam, IntPtr lParam)
        {
            return HookNativeMethods.CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
        }*/

        private static void ThrowLastUnmanagedErrorAsException()
        {
            var errorCode = Marshal.GetLastWin32Error();
            throw new Win32Exception(errorCode);
        }
    }
}