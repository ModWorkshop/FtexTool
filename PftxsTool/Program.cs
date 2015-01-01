﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using PftxsTool.Inf;
using PftxsTool.Pftxs;
using PftxsTool.Psub;

namespace PftxsTool
{
    internal static class Program
    {
        private const string UsageInfo = "PftxsTool.exe by Atvaark\n" +
                                         "Description:\n" +
                                         "  Unpacking and repacking Fox Engine texture pack (.pftxs) files.\n" +
                                         "Usage:\n" +
                                         "  PftxsTool.exe filename.pftxs     -Unpacks the files in the folder 'filename'\n" +
                                         "  PftxsTool.exe filename_pftxs.inf -Repacks the files in the folder 'filename'";

        private static void Main(string[] args)
        {
            if (args.Count() == 1)
            {
                string path = args[0];
                if (path.EndsWith(".pftxs"))
                {
                    UnpackPftxFile(path);
                    return;
                }
                if (path.EndsWith("_pftxs.inf"))
                {
                    PackPftxFile(path);
                    return;
                }
            }
            Console.WriteLine(UsageInfo);
        }

        private static void UnpackPftxFile(string path)
        {
            string archiveName = Path.GetFileNameWithoutExtension(path);
            string archiveDirectory = Path.GetDirectoryName(path);

            PftxsFile pftxsFile;
            using (FileStream input = new FileStream(path, FileMode.Open))
            {
                pftxsFile = PftxsFile.ReadPftxsFile(input);
            }

            string fileDirectory = "";
            StringBuilder logStringBuilder = new StringBuilder();
            foreach (var file in pftxsFile.FilesIndices)
            {
                var fileName = "";
                if (file.FileName.StartsWith("@"))
                {
                    fileName = file.FileName.Remove(0, 1);
                }
                else if (file.FileName.StartsWith("/"))
                {
                    fileDirectory = Path.GetDirectoryName(file.FileName.Remove(0, 1));
                    fileName = Path.GetFileName(file.FileName);
                }
                string relativeOutputDirectory = Path.Combine(archiveName, fileDirectory);
                string fullOutputDirectory = Path.Combine(archiveDirectory, relativeOutputDirectory);
                string fullPath = Path.Combine(fullOutputDirectory, fileName);
                string fullFilePath = string.Format("{0}.ftex", fullPath);
                Directory.CreateDirectory(fullOutputDirectory);
                using (FileStream fileOutputStream = new FileStream(fullFilePath, FileMode.Create))
                {
                    fileOutputStream.Write(file.Data, 0, file.Data.Length);
                }
                int subFileNumber = 1;
                foreach (var subFileIndex in file.PsubFile.Indices)
                {
                    string fullSubFilePath = String.Format("{0}.{1}.ftexs", fullPath, subFileNumber);

                    using (FileStream subFileOutputStream = new FileStream(fullSubFilePath, FileMode.Create))
                    {
                        subFileOutputStream.Write(subFileIndex.Data, 0, subFileIndex.Data.Length);
                    }
                    subFileNumber += 1;
                }
                string logLine = String.Format("{0};{1};{2};{3}", archiveName, fileDirectory, fileName,
                    file.PsubFile.Indices.Count());
                logStringBuilder.AppendLine(logLine);
            }
            string logPath = Path.Combine(archiveDirectory, String.Format("{0}_pftxs.inf", archiveName));
            File.WriteAllText(logPath, logStringBuilder.ToString());
        }

        private static void PackPftxFile(string path)
        {
            string archiveDirectory = Path.GetDirectoryName(path);
            string archiveName = GetArchiveName(path);
            string archiveFilePath = Path.Combine(archiveDirectory, String.Format("{0}.pftxs", archiveName));
            List<PftxsInfEntry> entries = ReadInfFile(path);
            PftxsFile pftxsFile = ConvertToPftxs(entries, archiveDirectory);
            using (FileStream output = new FileStream(archiveFilePath, FileMode.Create))
            {
                pftxsFile.Write(output);
            }
        }

        private static PftxsFile ConvertToPftxs(List<PftxsInfEntry> entries, string workingDirectoryPath)
        {
            PftxsFile pftxsFile = new PftxsFile();
            string lastDirectory = "";
            foreach (var entry in entries)
            {
                PftxsFileIndex index = new PftxsFileIndex();
                string filePath;
                if (lastDirectory.Equals(entry.FileDirectory))
                {
                    index.FileName = String.Format("@{0}", entry.FileName).Replace('\\', '/');
                    filePath = Path.Combine(workingDirectoryPath, entry.ArchiveName, lastDirectory, entry.FileName);
                }
                else
                {
                    string relativeFilePath = String.Format("{0}\\{1}", entry.FileDirectory, entry.FileName);
                    index.FileName = String.Format("\\{0}", relativeFilePath).Replace('\\', '/');
                    lastDirectory = entry.FileDirectory;
                    filePath = Path.Combine(workingDirectoryPath, entry.ArchiveName, relativeFilePath);
                }
                string fullFilePath = string.Format("{0}.ftex", filePath);
                index.Data = File.ReadAllBytes(fullFilePath);
                index.FileSize = index.Data.Length;
                PsubFile psubFile = new PsubFile();
                for (int i = 1; i <= entry.SubFileCount; i++)
                {
                    string fullSubFilePath = String.Format("{0}.{1}.ftexs", filePath, i);
                    var psubFileData = File.ReadAllBytes(fullSubFilePath);
                    PsubFileIndex psubFileIndex = new PsubFileIndex
                    {
                        Data = psubFileData,
                        Size = psubFileData.Length
                    };
                    psubFile.AddPsubFileIndex(psubFileIndex);
                }
                index.PsubFile = psubFile;
                pftxsFile.AddPftxsFileIndex(index);
            }
            pftxsFile.FileCount = pftxsFile.FilesIndices.Count();
            return pftxsFile;
        }

        private static string GetArchiveName(string path)
        {
            string infFileName = Path.GetFileNameWithoutExtension(path);
            int index = infFileName.LastIndexOf("_pftxs", StringComparison.Ordinal);
            string archiveName = infFileName.Substring(0, index);
            return archiveName;
        }

        private static List<PftxsInfEntry> ReadInfFile(string path)
        {
            List<PftxsInfEntry> entries;
            entries = new List<PftxsInfEntry>();
            var lines = File.ReadAllLines(path);
            foreach (var line in lines)
            {
                var splitLine = line.Split(';');
                string archiveName = splitLine[0];
                string fileDirectory = splitLine[1];
                String fileName = splitLine[2];
                int subFileCount = int.Parse(splitLine[3]);
                entries.Add(PftxsInfEntry.Create(archiveName, fileDirectory, fileName, subFileCount));
            }
            return entries;
        }
    }
}