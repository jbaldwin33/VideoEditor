using System;
using Microsoft.VisualBasic;
using Microsoft.VisualBasic.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace WPFVideoPlayer
{
    public class Keyboard
    {
        [DllImport("user32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern int SendInput(int cInputs, ref INPUT pInputs, int cbSize);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern uint MapVirtualKey(uint uCode, MapVirtualKeyMapTypes uMapType);

        private static ScanKey GetScanKey(uint VKey) => new ScanKey(MapVirtualKey(VKey, MapVirtualKeyMapTypes.MAPVK_VK_TO_VSC), VKey == 165U | VKey == 163U | VKey == 37U | VKey == 39U | VKey == 38U | VKey == 40U | VKey == 36U | VKey == 46U | VKey == 33U | VKey == 34U | VKey == 35U | VKey == 45U | VKey == 144U | VKey == 44U | VKey == 111U);

        public static void KeyDown(Keys kCode)
        {
            var scanKey = GetScanKey(checked((uint)kCode));
            var pInputs = new INPUT()
            {
                dwType = 1,
                mkhi = { ki = new KEYBDINPUT() }
            };
            pInputs.mkhi.ki.wScan = checked((short)scanKey.ScanCode);
            pInputs.mkhi.ki.dwExtraInfo = IntPtr.Zero;
            pInputs.mkhi.ki.dwFlags = Conversions.ToInteger(Operators.OrObject(8U, Interaction.IIf(scanKey.Extended, 1U, null)));
            var cbSize = Marshal.SizeOf(typeof(INPUT));
            SendInput(1, ref pInputs, cbSize);
        }

        public static void KeyUp(Keys kCode)
        {
            var scanKey = GetScanKey(checked((uint)kCode));
            var pInputs = new INPUT()
            {
                dwType = 1,
                mkhi = { ki = new KEYBDINPUT() }
            };
            pInputs.mkhi.ki.wScan = checked((short)scanKey.ScanCode);
            pInputs.mkhi.ki.dwExtraInfo = IntPtr.Zero;
            pInputs.mkhi.ki.dwFlags = Conversions.ToInteger(Operators.OrObject(10U, Interaction.IIf(scanKey.Extended, 1U, null)));
            var cbSize = Marshal.SizeOf(typeof(INPUT));
            SendInput(1, ref pInputs, cbSize);
        }

        public static void KeyPress(Keys kCode, int Delay = 0)
        {
            new Thread(a0 =>
            {
                var obj = a0;
                var keyPressStruct = new KeyPressStruct();
                KeyPressThread(obj != null ? (KeyPressStruct)obj : keyPressStruct);
            }).Start(new KeyPressStruct(new Keys[1] { kCode }, Delay));
        }

        private static void KeyPressThread(KeyPressStruct KeysP)
        {
            var keys1 = KeysP.Keys;
            var index1 = 0;
            while (index1 < keys1.Length)
            {
                KeyDown(keys1[index1]);
                checked { ++index1; }
            }
            if (KeysP.Delay > 0)
                Thread.Sleep(KeysP.Delay);
            var keys2 = KeysP.Keys;
            var index2 = 0;
            while (index2 < keys2.Length)
            {
                KeyUp(keys2[index2]);
                checked { ++index2; }
            }
        }

        private struct INPUT
        {
            public int dwType;
            public Keyboard.MOUSEKEYBDHARDWAREINPUT mkhi;
        }

        private struct KEYBDINPUT
        {
            public short wVk;
            public short wScan;
            public int dwFlags;
            public int time;
            public IntPtr dwExtraInfo;
        }

        private struct HARDWAREINPUT
        {
            public int uMsg;
            public short wParamL;
            public short wParamH;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct MOUSEKEYBDHARDWAREINPUT
        {
            [FieldOffset(0)]
            public MOUSEINPUT mi;
            [FieldOffset(0)]
            public KEYBDINPUT ki;
            [FieldOffset(0)]
            public HARDWAREINPUT hi;
        }

        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public int mouseData;
            public int dwFlags;
            public int time;
            public IntPtr dwExtraInfo;
        }

        public enum MapVirtualKeyMapTypes : uint
        {
            MAPVK_VK_TO_VSC,
            MAPVK_VSC_TO_VK,
            MAPVK_VK_TO_CHAR,
            MAPVK_VSC_TO_VK_EX,
            MAPVK_VK_TO_VSC_EX,
        }

        private struct ScanKey
        {
            public uint ScanCode;
            public bool Extended;

            public ScanKey(uint sCode, bool ex = false) : this()
            {
                ScanCode = sCode;
                Extended = ex;
            }
        }

        private struct KeyPressStruct
        {
            public Keys[] Keys;
            public int Delay;

            public KeyPressStruct(Keys[] KeysToPress, int DelayTime = 0) : this()
            {
                Keys = KeysToPress;
                Delay = DelayTime;
            }
        }
    }
}