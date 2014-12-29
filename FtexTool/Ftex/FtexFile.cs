﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using FtexTool.Ftexs;

namespace FtexTool.Ftex
{
    public class FtexFile
    {
        private const long MagicNumber = 4612226451348214854; // FTEX 85 EB 01 40

        private readonly List<FtexFileMipMapInfo> _mipMapInfos;
        private readonly Dictionary<int, FtexsFile> _ftexsFiles;

        public short DtxType { get; set; }
        public short Width { get; set; }
        public short Height { get; set; }
        public byte MipMapCount { get; set; }
        public byte FtexsFileCount { get; set; }

        // FtexsFileCount - 1
        public byte UnknownCount { get; set; }

        public IEnumerable<FtexFileMipMapInfo> MipMapInfos
        {
            get { return _mipMapInfos; }
        }

        public IEnumerable<FtexsFile> FtexsFiles
        {
            get { return _ftexsFiles.Values; }
        }

        public byte[] Data
        {
            get
            {
                MemoryStream stream = new MemoryStream();
                foreach (var ftexsFile in FtexsFiles.Reverse())
                {
                    byte[] ftexsFileData = ftexsFile.Data;
                    stream.Write(ftexsFileData, 0, ftexsFileData.Length);
                }
                return stream.ToArray();
            }
        }

        public FtexFile()
        {
            _ftexsFiles = new Dictionary<int, FtexsFile>();
            _mipMapInfos = new List<FtexFileMipMapInfo>();
        }

        public void AddFtexsFile(FtexsFile ftexsFile)
        {
            _ftexsFiles.Add(ftexsFile.FileNumber, ftexsFile);
        }

        public void AddFtexsFiles(IEnumerable<FtexsFile> ftexsFiles)
        {
            foreach (var ftexsFile in ftexsFiles)
            {
                AddFtexsFile(ftexsFile);
            }
        }

        public bool TryGetFtexsFile(int fileNumber, out FtexsFile ftexsFile)
        {
            return  _ftexsFiles.TryGetValue(fileNumber, out ftexsFile);
        }

        public static FtexFile ReadFtexFile(Stream inputStream)
        {
            FtexFile result = new FtexFile();
            result.Read(inputStream);
            return result;
        }

        private void Read(Stream inputStream)
        {
            BinaryReader reader = new BinaryReader(inputStream, Encoding.Default, true);
            reader.Assert(MagicNumber);
            DtxType = reader.ReadInt16();
            Width = reader.ReadInt16();
            Height = reader.ReadInt16();
            reader.Skip(2);
            MipMapCount = reader.ReadByte();
            reader.Skip(15);
            FtexsFileCount = reader.ReadByte();
            UnknownCount = reader.ReadByte();
            reader.Skip(30);

            for (int i = 0; i < MipMapCount; i++)
            {
                FtexFileMipMapInfo fileMipMapInfo = FtexFileMipMapInfo.ReadFtexFileMipMapInfo(inputStream);
                AddMipMapInfo(fileMipMapInfo);
            }
        }

        private void AddMipMapInfo(FtexFileMipMapInfo fileMipMapInfo)
        {
            _mipMapInfos.Add(fileMipMapInfo);
        }

        public void AddMipMapInfos(IEnumerable<FtexFileMipMapInfo> mipMapInfos)
        {
            foreach (var mipMapInfo in mipMapInfos)
            {
                AddMipMapInfo(mipMapInfo);
            }
        }

        public void Write(Stream outputStream)
        {
            BinaryWriter writer = new BinaryWriter(outputStream, Encoding.Default, true);
            writer.Write(MagicNumber);
            writer.Write(DtxType);
            writer.Write(Width);
            writer.Write(Height);
            writer.WriteZeros(2);
            writer.Write(MipMapCount);
            writer.WriteZeros(15);
            writer.Write(FtexsFileCount);
            writer.Write(UnknownCount);
            writer.WriteZeros(30);
            foreach (var mipMap in MipMapInfos)
            {
                mipMap.Write(outputStream);
            }
        }

        public void UpdateOffsets()
        {
            int mipMapIndex = 0;
            foreach (var ftexsFile in FtexsFiles)
            {
                foreach (var ftexsFileMipMap in ftexsFile.MipMaps)
                {
                    FtexFileMipMapInfo ftexMipMapInfo = MipMapInfos.ElementAt(mipMapIndex);
                    ftexMipMapInfo.CompressedFileSize = ftexsFileMipMap.CompressedDataSize;
                    ftexMipMapInfo.ChunkCount = Convert.ToByte(ftexsFileMipMap.Chunks.Count());
                    ftexMipMapInfo.Offset = ftexsFileMipMap.Offset;
                    ++mipMapIndex;
                }
            }
        }

    }
}