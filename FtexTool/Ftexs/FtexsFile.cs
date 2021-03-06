﻿using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FtexTool.Ftexs
{
    public class FtexsFile
    {
        private readonly List<FtexsFileMipMap> _mipMaps;

        public FtexsFile()
        {
            _mipMaps = new List<FtexsFileMipMap>();
        }

        public IEnumerable<FtexsFileMipMap> MipMaps
        {
            get { return _mipMaps; }
        }

        public byte FileNumber { get; set; }

        public byte[] Data
        {
            get
            {
                MemoryStream stream = new MemoryStream();
                foreach (var mipMap in MipMaps)
                {
                    stream.Write(mipMap.Data, 0, mipMap.Data.Length);
                }
                return stream.ToArray();
            }
        }

        public int CompressedDataSize
        {
            get { return MipMaps.Sum(mipMap => mipMap.CompressedDataSize); }
        }

        public void Read(Stream inputStream, short chunkCount)
        {
            FtexsFileMipMap mipMap = FtexsFileMipMap.ReadFtexsFileMipMap(inputStream, chunkCount);
            AddMipMap(mipMap);
        }

        public void AddMipMap(FtexsFileMipMap mipMap)
        {
            _mipMaps.Add(mipMap);
        }

        public void Write(Stream outputStream)
        {
            foreach (var mipMap in MipMaps.Reverse())
            {
                mipMap.Write(outputStream);
            }
        }
    }
}
