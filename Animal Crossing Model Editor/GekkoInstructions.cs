using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Animal_Crossing_Model_Editor
{
    public static class GekkoInstructions
    {
        private static uint GetMask32(int MB, int ME)
        {
            uint MaskMB = (uint)(0xFFFFFFFF >> MB);
            uint MaskME = (uint)(0xFFFFFFFF << (31 - ME));
            return (MB <= ME) ? (uint)(MaskMB & MaskME) : (uint)(MaskMB | MaskME);
        }

        private static uint RotateLeft32(uint Value, int RotateCount)
        {
            return (uint)((Value << RotateCount) | (Value >> (32 - RotateCount)));
        }

        /// <summary>
        /// Shift Left Word
        /// </summary>
        /// <param name="Value"></param>
        /// <param name="Shift_Count"></param>
        /// <returns></returns>
        public static uint slw(uint Value, int Shift_Count)
        {
            return RotateLeft32(Value, Shift_Count);
        }

        /// <summary>
        /// Shift Right Word
        /// </summary>
        /// <param name="Value"></param>
        /// <param name="Shift_Count"></param>
        /// <returns></returns>
        public static uint srw(uint Value, int Shift_Count)
        {
            return RotateLeft32(Value, 32 - Shift_Count);
        }

        /// <summary>
        /// Rotate Left Word Immediate and Mask
        ///     rlwinm rotates the dword left by X bits, then and's the value with a generated mask from MB & ME
        /// </summary>
        /// <param name="Value"></param>
        /// <param name="Shift_Count"></param>
        /// <param name="MB"></param>
        /// <param name="ME"></param>
        /// <returns></returns>
        public static uint rlwinm(uint Value, int Shift_Count, int MB, int ME)
        {
            return (uint)(RotateLeft32(Value, Shift_Count) & GetMask32(MB, ME));
        }

        public static uint rlwimi(uint PrevValue, uint Value, int Shift_Count, int MB, int ME)
        {
            uint Mask = GetMask32(MB, ME);
            return (uint)((PrevValue & ~Mask) | (RotateLeft32(Value, Shift_Count) & Mask));
        }

        public static uint extlwi(uint Value, int MB, int ME)
        {
            return rlwinm(Value, ME, 0, MB - 1);
        }

        public static uint extrwi(uint Value, int MB, int ME)
        {
            return rlwinm(Value, ME + MB, 32 - MB, 31);
        }

        public static uint inslwi(uint Value, int MB, int ME)
        {
            return rlwinm(Value, 32 - ME, ME, (ME + MB) - 1);
        }

        public static uint insrwi(uint Value, int MB, int ME)
        {
            return rlwinm(Value, 32 - (ME + MB), ME, (ME + MB) - 1);
        }

        public static uint clrlwi(uint Value, int MB)
        {
            return rlwinm(Value, 0, MB, 31);
        }

        public static uint clrrwi(uint Value, int MB)
        {
            return rlwinm(Value, 0, 0, 31 - MB);
        }

        /// <summary>
        /// Shift Left Word Immediate
        /// </summary>
        /// <param name="Value"></param>
        /// <param name="ShiftCount"></param>
        /// <returns></returns>
        public static uint slwi(uint Value, int Shift_Count)
        {
            if (Shift_Count < 32)
            {
                return rlwinm(Value, Shift_Count, 0, 31 - Shift_Count);
            }
            return 0;
        }

        /// <summary>
        /// Shift Right Word Immediate
        /// </summary>
        /// <param name="Value"></param>
        /// <param name="ShiftCount"></param>
        /// <returns></returns>
        public static uint srwi(uint Value, int Shift_Count)
        {
            return rlwinm(Value, 32 - Shift_Count, Shift_Count, 31);
        }
    }
}
