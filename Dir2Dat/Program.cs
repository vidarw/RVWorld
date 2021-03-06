﻿using System;
using System.Collections.Generic;
using Compress;
using Compress.SevenZip;
using Compress.ZipFile;
using DATReader.DatClean;
using DATReader.DatStore;
using DATReader.DatWriter;
using FileHeaderReader;
using RVIO;

namespace Dir2Dat
{
    class Program
    {
        private static bool testMode = false;

        static void Main(string[] args)
        {
            DatHeader ThisDat = new DatHeader()
            {
                BaseDir = new DatDir(DatFileType.Dir)
            };

            bool style = false;
            string dirSource = null;
            string outfile = null;

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                bool isflag = arg.Substring(0, 1) == "-";
                if (isflag)
                {
                    string flag = arg.Substring(1);
                    switch (flag.ToLower())
                    {
                        case "help":
                        case "h":
                        case "?":
                            ShowHelp();
                            return;
                        case "name": case "n":
                            ThisDat.Name = args[++i];
                            break;
                        case "description": case "d":
                            ThisDat.Description = args[++i];
                            break;
                        case "category":
                        case "ca":
                            ThisDat.Category = args[++i];
                            break;
                        case "version":
                        case "v":
                            ThisDat.Version = args[++i];
                            break;
                        case "date":
                        case "dt":
                            ThisDat.Date = args[++i];
                            break;
                        case "autodate":
                        case "ad":
                            ThisDat.Date = DateTime.Now.ToString("MM/dd/yyyy");
                            break;
                        case "author":
                        case "a":
                            ThisDat.Author = args[++i];
                            break;
                        case "email":
                        case "e":
                            ThisDat.Email = args[++i];
                            break;
                        case "homepage":
                        case "hp":
                            ThisDat.Homepage = args[++i];
                            break;
                        case "url":
                            ThisDat.URL = args[++i];
                            break;
                        case "comment":
                        case "co":
                            ThisDat.Comment = args[++i];
                            break;
                        case "newstyle":
                        case "ns":
                            style = true;
                            break;
                        case "test": case "t":
                            testMode = true;
                            break;
                    }
                }
                else if (dirSource == null)
                {
                    dirSource = arg;
                }
                else if (outfile == null)
                {
                    outfile = arg;
                }
                else
                {
                    Console.WriteLine("Unknown arg: " + arg);
                    return;
                }
            }

            if (dirSource == null || outfile == null)
            {
                Console.WriteLine("Must supply source DIR and destination filename.");
                return;
            }

            DirectoryInfo di = new DirectoryInfo(dirSource);
            ProcessDir(di, ThisDat.BaseDir, style);

            DatXMLWriter dWriter = new DatXMLWriter();
            if (Path.GetExtension(outfile).ToLower() != ".dat")
                outfile += ".dat";
            dWriter.WriteDat(outfile, ThisDat, style);
        }

        private static void ShowHelp()
        {
            Console.WriteLine("Dir2Dat Commandline");
            Console.WriteLine("");
            Console.WriteLine("Copyright (C) 2020 GordonJ");
            Console.WriteLine("Homepage : https://www.romvault.com/");
            Console.WriteLine("");
            Console.WriteLine("Usage: Dir2Dat [SOURCE DIR] [OUTPUT DAT FILENAME] [OPTIONS|VALUE]");
            Console.WriteLine("");
            Console.WriteLine("Options:");
            Console.WriteLine("");
            Console.WriteLine("-help        -?  : Show this help");
            Console.WriteLine("-name        -n  : Name for header");
            Console.WriteLine("-description -d  : Description for header");
            Console.WriteLine("-category    -c  : Category for header");
            Console.WriteLine("-version     -v  : Version for header");
            Console.WriteLine("-date        -dt : Date for header");
            Console.WriteLine("-autodate    -ad : Auto Set the Date (No parameter needed)");
            Console.WriteLine("-author      -a  : Author for header");
            Console.WriteLine("-email       -e  : Email for header");
            Console.WriteLine("-homepage    -hp : Homepage for header");
            Console.WriteLine("-url             : URL for header");
            Console.WriteLine("-comment     -co : Comment for header");
            Console.WriteLine("");
            Console.WriteLine("Example:");
            Console.WriteLine("Dir2Dat C:\\Mame mameOut -n Mame -d \"Mame Dat\" -ad");
        }

        private static void ProcessDir(DirectoryInfo di, DatDir thisDir, bool newStyle)
        {
            DirectoryInfo[] dia = di.GetDirectories();
            foreach (DirectoryInfo d in dia)
            {
                bool procAsGame = CheckAddDir(d);
                if (procAsGame)
                {
                    Console.WriteLine(d.FullName + "\\ need to add as game");
                    AddDirAsGame(d, thisDir);
                }
                else
                {
                    DatDir nextDir = new DatDir(DatFileType.Dir) { Name = d.Name };
                    thisDir.ChildAdd(nextDir);
                    ProcessDir(d, nextDir, newStyle);
                }
            }
            FileInfo[] fia = di.GetFiles();

            int fCount = 0;
            foreach (FileInfo f in fia)
            {
                Console.WriteLine(f.FullName);
                string ext = Path.GetExtension(f.Name).ToLower();

                switch (ext)
                {
                    case ".zip":
                        AddZip(f, thisDir);
                        break;
                    case ".7z":
                        Add7Zip(f, thisDir);
                        break;
                    default:
                        if (newStyle)
                            AddFile(f, thisDir);
                        break;
                }

                if (testMode)
                {
                    fCount++;
                    if (fCount > 10)
                        break;
                }
            }
        }

        private static bool CheckAddDir(DirectoryInfo di)
        {
            DirectoryInfo[] dia = di.GetDirectories();
            if (dia.Length > 0)
                return false;
            FileInfo[] fia = di.GetFiles();

            foreach (FileInfo f in fia)
            {
                string ext = Path.GetExtension(f.Name).ToLower();

                switch (ext)
                {
                    case ".zip":
                    case ".7z":
                        return false;
                }
            }
            return true;
        }

        private static void AddZip(FileInfo f, DatDir thisDir)
        {

            ZipFile zf1 = new ZipFile();
            ZipReturn result =zf1.ZipFileOpen(f.FullName, -1, true);
            if (result != ZipReturn.ZipGood)
                return;

            //zf1.ZipStatus = ZipStatus.TrrntZip;

            DatDir ZipDir = new DatDir(zf1.ZipStatus == ZipStatus.TrrntZip ? DatFileType.DirTorrentZip : DatFileType.DirRVZip)
            {
                Name = Path.GetFileNameWithoutExtension(f.Name),
                DGame = new DatGame()
            };
            ZipDir.DGame.Description = ZipDir.Name;
            thisDir.ChildAdd(ZipDir);



            FileScan fs = new FileScan();
            List<FileScan.FileResults> fr = fs.Scan(zf1, true, true);
            bool isTorrentZipDate = true;
            for (int i = 0; i < fr.Count; i++)
            {
                if (fr[i].FileStatus != ZipReturn.ZipGood)
                {
                    Console.WriteLine("File Error :" + zf1.Filename(i) + " : " + fr[i].FileStatus);
                    continue;
                }

                DatFile df = new DatFile(DatFileType.FileTorrentZip)
                {
                    Name = zf1.Filename(i),
                    Size = fr[i].Size,
                    CRC = fr[i].CRC,
                    SHA1 = fr[i].SHA1,
                    Date = zf1.LastModified(i).ToString("yyyy/MM/dd HH:mm:ss")
                    //df.MD5 = zf.MD5(i)
                };
                if (zf1.LastModified(i).Ticks != 629870671200000000)
                    isTorrentZipDate = false;

                ZipDir.ChildAdd(df);
            }
            zf1.ZipFileClose();
            if (isTorrentZipDate && ZipDir.DatFileType == DatFileType.DirRVZip)
                ZipDir.DatFileType = DatFileType.DirTorrentZip;

            if (ZipDir.DatFileType == DatFileType.DirTorrentZip)
            {
                DatSetCompressionType.SetZip(ZipDir);
                DatClean.RemoveUnNeededDirectoriesFromZip(ZipDir);
            }
        }

        private static void Add7Zip(FileInfo f, DatDir thisDir)
        {
            DatDir ZipDir = new DatDir(DatFileType.Dir7Zip)
            {
                Name = Path.GetFileNameWithoutExtension(f.Name),
                DGame = new DatGame()
            };
            ZipDir.DGame.Description = ZipDir.Name;
            thisDir.ChildAdd(ZipDir);

            SevenZ zf1 = new SevenZ();
            zf1.ZipFileOpen(f.FullName, -1, true);
            FileScan fs = new FileScan();
            List<FileScan.FileResults> fr = fs.Scan(zf1, true, true);
            for (int i = 0; i < fr.Count; i++)
            {
                if (zf1.IsDirectory(i))
                    continue;
                DatFile df = new DatFile(DatFileType.File7Zip)
                {
                    Name = zf1.Filename(i),
                    Size = fr[i].Size,
                    CRC = fr[i].CRC,
                    SHA1 = fr[i].SHA1
                    //df.MD5 = zf.MD5(i)
                };
                ZipDir.ChildAdd(df);
            }
            zf1.ZipFileClose();
        }

        private static void AddDirAsGame(DirectoryInfo di, DatDir thisDir)
        {
            DatDir fDir = new DatDir(DatFileType.Dir)
            {
                Name = Path.GetFileNameWithoutExtension(di.Name),
                DGame = new DatGame()
            };
            fDir.DGame.Description = fDir.Name;
            thisDir.ChildAdd(fDir);

            FileInfo[] fia = di.GetFiles();

            int fCount = 0;
            foreach (FileInfo f in fia)
            {
                Console.WriteLine(f.FullName);
                AddFile(f, fDir);

                if (testMode)
                {
                    fCount++;
                    if (fCount > 10)
                        break;
                }
            }

        }

        private static void AddFile(FileInfo f, DatDir thisDir)
        {
            Compress.File.File zf1 = new Compress.File.File();
            zf1.ZipFileOpen(f.FullName, -1, true);
            FileScan fs = new FileScan();
            List<FileScan.FileResults> fr = fs.Scan(zf1, true, true);

            DatFile df = new DatFile(DatFileType.File)
            {
                Name = f.Name,
                Size = fr[0].Size,
                CRC = fr[0].CRC,
                SHA1 = fr[0].SHA1
            };

            thisDir.ChildAdd(df);
            zf1.ZipFileClose();
        }
    }
}
