module Fenrir.Ui.Framework.Native.Windows

open System
open System.ComponentModel

open ConsoleFramework.Native

type DWORD = uint32

module Constants =
    let INVALID_HANDLE = -1
    let STD_INPUT_HANDLE: DWORD = uint32 -10
    let ENABLE_EXTENDED_FLAGS: DWORD = 0x0080u
    let ENABLE_QUICK_EDIT_MODE: DWORD = 0x0040u

let getStdInHandle(): IntPtr =
    let result = Win32.GetStdHandle StdHandleType.STD_INPUT_HANDLE
    if int32 result = Constants.INVALID_HANDLE then raise <| Win32Exception()
    result

let getConsoleMode(handle: IntPtr): DWORD =
    let mutable mode = 0u
    if not(Win32.GetConsoleMode(handle, &mode)) then raise <| Win32Exception()
    mode

let setConsoleMode (handle: IntPtr) (mode: DWORD): unit =
    if not(Win32.SetConsoleMode(handle, mode)) then raise <| Win32Exception()
