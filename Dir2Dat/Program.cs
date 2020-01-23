using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Compress;
using Compress.SevenZip;
using Compress.ZipFile;
using DATReader.DatClean;
using DATReader.DatStore;
using DATReader.DatWriter;
using FileHeaderReader;
using DirectoryInfo = RVIO.DirectoryInfo;
using FileInfo = RVIO.FileInfo;
using Path = RVIO.Path;

namespace Dir2Dat
{
    class Program
    {
        private static int testCount = 0;
        private static int parallel = 0;
        private static int threadedScan = 2;
        private static bool fileLock = false;

        static void Main(string[] args)
        {
            DatHeader ThisDat = new DatHeader()
            {
                BaseDir = new DatDir(DatFileType.Dir)
            };

            bool style = false;
            string dirSource = null;
            string outfile = null;
            string logOut = null;

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                bool isflag = arg.Substring(0, 1) == "-";
                if (isflag)
                {
                    string flag = arg.Substring(1);
                    switch (flag.ToLower())
                    {
                        case "name":
                        case "n":
                            ThisDat.Name = args[++i];
                            break;
                        case "description":
                        case "d":
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
                        
                        case "test":
                        case "t":
                            testCount = Convert.ToInt32(args[++i]);
                            break;

                        case "log":
                        case "l":
                            logOut = args[++i];
                            break;

                        // number of parallel ZIPs to process at the same time
                        case "parallel":
                        case "p":
                            parallel = Convert.ToInt32(args[++i]);
                            break;
                        // use multi threaded hashing
                        // 0 no threading
                        // 1 decomp thread & hashing thread
                        // 2 fully threaded
                        case "threaded":
                        case "th":
                            threadedScan = Convert.ToInt32(args[++i]);
                            break;
                        // lock file reading to one thread
                        case "filelock":
                        case "fl":
                            fileLock = true;
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

            if (Reader.lockObj == null)
                Reader.lockObj = new Object();


            Stopwatch sw = new Stopwatch();
            sw.Start();

            DirectoryInfo di = new DirectoryInfo(dirSource);
            ProcessDir(di, ThisDat.BaseDir, false);

            Console.WriteLine($"Time Taken for {ThisDat.Name}  {sw.ElapsedMilliseconds}");
            if (!string.IsNullOrWhiteSpace(logOut))
            {
                using (StreamWriter strWr = File.AppendText(logOut))
                {
                    strWr.WriteLine($"{outfile + ".dat"} Style: {parallel} Scan Time: {sw.ElapsedMilliseconds} ms");
                }
            }

            DatXMLWriter dWriter = new DatXMLWriter();
            dWriter.WriteDat(outfile + ".dat", ThisDat, style);
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

            if (testCount > 0 && fia.Length > testCount)
            {
                FileInfo[] fit = new FileInfo[testCount];
                for (int i = 0; i < testCount; i++)
                    fit[i] = fia[i];
                fia = fit;
            }

            if (parallel > 1)
            {
                Parallel.ForEach(fia, new ParallelOptions { MaxDegreeOfParallelism = parallel }, f =>
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
                });
            }
            else
            {

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
            Stream inStr = null;
            if (fileLock)
            {
                inStr = new Reader(f.FullName);
                zf1.ZipFileOpen(inStr);
            }
            else
            {
                zf1.ZipFileOpen(f.FullName, -1, true);
            }

            zf1.ZipStatus = ZipStatus.TrrntZip;

            DatDir ZipDir = new DatDir(zf1.ZipStatus == ZipStatus.TrrntZip ? DatFileType.DirTorrentZip : DatFileType.DirRVZip)
            {
                Name = Path.GetFileNameWithoutExtension(f.Name),
                DGame = new DatGame()
            };
            ZipDir.DGame.Description = ZipDir.Name;
            lock (thisDir)
            {
                thisDir.ChildAdd(ZipDir);
            }


            FileScan fs = new FileScan();
            List<FileScan.FileResults> fr = fs.Scan(zf1, true, true, threadedScan);
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

            inStr?.Close();
            inStr?.Dispose();
        }

        private static void Add7Zip(FileInfo f, DatDir thisDir)
        {
            DatDir ZipDir = new DatDir(DatFileType.Dir7Zip)
            {
                Name = Path.GetFileNameWithoutExtension(f.Name),
                DGame = new DatGame()
            };
            ZipDir.DGame.Description = ZipDir.Name;
            lock (thisDir)
            {
                thisDir.ChildAdd(ZipDir);
            }

            SevenZ zf1 = new SevenZ();
            zf1.ZipFileOpen(f.FullName, -1, true);
            FileScan fs = new FileScan();
            List<FileScan.FileResults> fr = fs.Scan(zf1, true, true, threadedScan);
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

            lock (thisDir)
            {
                thisDir.ChildAdd(fDir);
            }

            FileInfo[] fia = di.GetFiles();

            foreach (FileInfo f in fia)
            {
                Console.WriteLine(f.FullName);
                AddFile(f, fDir);
            }
        }

        private static void AddFile(FileInfo f, DatDir thisDir)
        {
            Compress.File.File zf1 = new Compress.File.File();
            zf1.ZipFileOpen(f.FullName, -1, true);
            FileScan fs = new FileScan();
            List<FileScan.FileResults> fr = fs.Scan(zf1, true, true, threadedScan);

            DatFile df = new DatFile(DatFileType.File)
            {
                Name = f.Name,
                Size = fr[0].Size,
                CRC = fr[0].CRC,
                SHA1 = fr[0].SHA1
            };

            lock (thisDir)
            {
                thisDir.ChildAdd(df);
            }

            zf1.ZipFileClose();
        }
    }
}
