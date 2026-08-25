using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Listik
{
    public class TundraManager
    {

       
        // Импорт функций из DLL
        private const string DllName = "Hook.dll"; // Замените на имя вашей DLL

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int CheckVersion();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool isOpenProcess();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool OpenTankiProcess();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern void CloseTankiProcess();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern uint ReadTankiProcess();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool WriteTankiProcess(uint inputData, [MarshalAs(UnmanagedType.I1)] bool autoDisable);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern void SetAutoDisable([MarshalAs(UnmanagedType.I1)] bool autoDisable);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern int CheckGameCompatibility();


        // Публичные методы для использования
        public int GetVersion()
        {
            return CheckVersion();
        }

        public bool IsProcessOpen()
        {
            return isOpenProcess();
        }

        public bool OpenProcess()
        {
            return OpenTankiProcess();
        }

        public void CloseProcess()
        {
            CloseTankiProcess();
        }

        public void ReadData(TankiData data)
        {
            uint result = ReadTankiProcess();
            if (result == uint.MaxValue) // Проверка на -1
                return ;

            data.FullTundra = (byte)(result & 0xFF);
            data.Trees = (byte)((result >> 8) & 0xFF);
            data.Grass = (byte)((result >> 16) & 0xFF);
            data.IsBattle = (byte)((result >> 24) & 0xFF);
           
        }

        public bool WriteData(TankiData data, bool autoDisable)
        {
            uint packedData = 0;
            packedData |= (uint)(data.FullTundra & 0xFF);
            packedData |= (uint)((data.Trees & 0xFF) << 8);
            packedData |= (uint)((data.Grass & 0xFF) << 16);
            // IsBattle не пишется, только читается

            return WriteTankiProcess(packedData, autoDisable);
        }

        public void SetAutoDisableEnabled(bool autoDisable)
        {
            SetAutoDisable(autoDisable);
        }

        public int GetGameCompatibilityStatus()
        {
            return CheckGameCompatibility();
        }
    }
    // Класс для хранения данных
    public class TankiData
    {
        public byte FullTundra { get; set; } // 0, 2, 6
        public byte Trees { get; set; }      // 0, 2
        public byte Grass { get; set; }      // 0, 1
        public byte IsBattle { get; set; }   // 0 или 1


        public string FullTundraStatus() //=> FullTundra switch
        {
            switch (FullTundra)
            {
                case 0: return "Выкл";
                case 2: return "Видно стволы";
                case 6: return "Видно всю листву";
                default: return "Any";
            }
        }
        public string TreesStatus() //=> FullTundra switch
        {
            switch (Trees)
            {
                case 0: return "Выкл";
                case 2: return "Отображаются";
                default: return "Any";
            }
        }
        public string GrassStatus() //=> FullTundra switch
        {
            switch (Grass)
            {
                case 0: return "Выкл";
                case 2: return "Вкл";
                default: return "Any";
            }
        }
        public string IsBattleStatus() //=> FullTundra switch
        {
            switch (IsBattle)
            {
                case 0: return "В Бою";
                case 1: return "В Ангаре";
                default: return "Any";
            }
        }
    }
}
