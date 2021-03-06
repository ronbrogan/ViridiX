﻿using System;
using System.Diagnostics;
using System.IO;
using ViridiX.Linguist.Memory;
using ViridiX.Mason.Extensions;
using ViridiX.Mason.Logging;

namespace ViridiX.Linguist.Kernel
{
    /// <summary>
    /// An interface to the Xbox kernel.
    /// </summary>
    public class XboxKernel
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Xbox _xbox;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly ILogger _logger;

        /// <summary>
        /// The kernel image size. NOTE: May not be contiguous memory.
        /// </summary>
        public int Size { get; }

        /// <summary>
        /// The kernel export table.
        /// </summary>
        public long[] ExportTable { get; }

        /// <summary>
        /// The kernel base address in memory.
        /// TODO: I vaguely remember a certain debug bios changing this so we may need to do some auto-detection instead.
        /// </summary>
        public const long Address = 0x80010000;

        /// <summary>
        /// The kernel build time in UTC.
        /// </summary>
        public DateTime Date { get; }

        /// <summary>
        /// The Xbox kernel exports.
        /// </summary>
        public XboxKernelExports Exports { get; }

        /// <summary>
        /// The Xbox kernel build version.
        /// </summary>
        public Version Version
        {
            get
            {
                var version = _xbox.Memory.ReadUInt32(Exports.XboxKrnlVersion);
                return new Version((byte) (version & 0xFF), (byte) ((version >> 8) & 0xFF),
                    (byte) ((version >> 16) & 0xFF), (byte) ((version >> 24) & 0xFF));
            }
        }

        /// <summary>
        /// Initializes communication with the Xbox kernel.
        /// </summary>
        /// <param name="xbox"></param>
        public XboxKernel(Xbox xbox)
        {
            if (xbox == null)
                throw new ArgumentNullException(nameof(xbox));

            _xbox = xbox;
            _logger = xbox.Logger;
            ExportTable = GetExportTable();

            Date = _xbox.Memory.ReadUInt32(Address + 0xF0).ToDateTimeFromEpochSeconds();
            Size = _xbox.Memory.ReadInt32(Address + 0x138);

            Exports = new XboxKernelExports(ExportTable);

            _logger?.Info("XboxKernel subsystem initialized");
        }

        /// <summary>
        /// Retrieves kernel export table.
        /// </summary>
        /// <returns>Kernel export addresses.</returns>
        private long[] GetExportTable()
        {
            _logger?.Info("Reading kernel export table");

            // gets export table with function offsets
            long peBase = _xbox.Memory.ReadUInt32(Address + 0x3C);
            long dataDirectory = _xbox.Memory.ReadUInt32(Address + peBase + 0x78);
            int exportCount = _xbox.Memory.ReadInt32(Address + dataDirectory + 0x14);
            long exportAddress = Address + _xbox.Memory.ReadUInt32(Address + dataDirectory + 0x1C);
            byte[] exportBytes = _xbox.Memory.ReadBytes(exportAddress, exportCount * sizeof(uint));

            // converts them to absolute addresses
            long[] exportTable = new long[exportCount + 1];
            for (int i = 0; i < exportCount; i++)
            {
                long offset = BitConverter.ToUInt32(exportBytes, i * 4);
                if (offset != 0)
                {
                    exportTable[i + 1] = Address + offset;
                }
            }
                
            return exportTable;
        }

        /// <summary>
        /// Looks up the kernel export address from an ordinal.
        /// </summary>
        /// <param name="ordinal"></param>
        /// <returns></returns>
        public long GetExportAddress(int ordinal)
        {
            _logger?.Trace("Looking up kernel export address for ordinal {0}", ordinal);

            if (ordinal >= ExportTable.Length)
            {
                throw new IndexOutOfRangeException();
            }

            return ExportTable[ordinal];
        }

        /// <summary>
        /// Dumps the kernel from memory and saves to the specified file.
        /// </summary>
        /// <param name="filename"></param>
        public void MemoryDump(string filename)
        {
            _logger?.Info("Dumping the kernel from memory to local file {0}", filename);

            using (FileStream fs = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.None))
            using (BinaryWriter bw = new BinaryWriter(fs))
            {
                // skip any invalid pages since the memory may not be contiguous
                for (long position = Address;
                    position < Address + Size && _xbox.Memory.IsValidAddress(position);
                    position += XboxMemory.PageSize)
                {
                    bw.Write(_xbox.Memory.ReadBytes(position, XboxMemory.PageSize));
                }
            }

            _logger?.Info("Completed kernel dump");
        }
    }
}
