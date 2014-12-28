﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FtexTool.Ftexs
{
    public class FtexsFileMipMap
    {
        private readonly List<FtexsFileChunk> _chunks;

        public List<FtexsFileChunk> Chunks
        {
            get { return _chunks; }
        }

        public byte[] Data
        {
            get
            {
                MemoryStream stream = new MemoryStream();
                foreach (var chunk in Chunks)
                {
                    stream.Write(chunk.ChunkData, 0, chunk.ChunkData.Length);
                }
                return stream.ToArray();
            }
        }

        internal int IndexBlockSize
        {
            get { return FtexsFileChunk.IndexSize*Chunks.Count; }
        }

        public FtexsFileMipMap()
        {
            _chunks = new List<FtexsFileChunk>();
        }

        public static FtexsFileMipMap Read(Stream inputStream, short chunkCount, bool seekOffset)
        {
            FtexsFileMipMap result = new FtexsFileMipMap();
            for (int i = 0; i < chunkCount; i++)
            {
                FtexsFileChunk chunk = FtexsFileChunk.Read(inputStream, seekOffset);
                result.Chunks.Add(chunk);
            }
            return result;
        }

        public void Write(Stream outputStream)
        {
            BinaryWriter writer = new BinaryWriter(outputStream, Encoding.Default, true);
            long startPosition = writer.BaseStream.Position;
            writer.BaseStream.Position += IndexBlockSize;
            foreach (var chunk in Chunks)
            {
                chunk.Offset = Convert.ToInt32(writer.BaseStream.Position);
                chunk.WriteData(outputStream);
            }
            long endPosition = writer.BaseStream.Position;
            writer.BaseStream.Position = startPosition;
            foreach (var chunk in Chunks)
            {
                chunk.Write(outputStream);
            }
            writer.BaseStream.Position = endPosition;
        }
    }
}