﻿
using System;
using DATReader.DatStore;

namespace DATReader.DatClean
{
    public static partial class DatClean
    {
        public static void MakeDatSingleLevel(DatHeader tDatHeader,bool useDescription)
        {
            DatBase[] db = tDatHeader.BaseDir.ToArray();
            tDatHeader.Dir = "noautodir";


            // if we are auto adding extra directories then create a new directory.
            string extraDirName="";
            if (string.IsNullOrEmpty(extraDirName) && useDescription && !string.IsNullOrWhiteSpace(tDatHeader.Description))
                extraDirName = tDatHeader.Description;
            if (string.IsNullOrEmpty(extraDirName))
                extraDirName = tDatHeader.Name;

            tDatHeader.BaseDir.ChildrenClear();
            DatDir root = new DatDir(DatFileType.UnSet)
            {
                Name = extraDirName,
                DGame = new DatGame { Description = tDatHeader.Description }
            };
            tDatHeader.BaseDir.ChildAdd(root);

            foreach (DatBase set in db)
            {
                string dirName = set.Name;
                if (!(set is DatDir romSet))
                    continue;
                DatBase[] dbr = romSet.ToArray();
                foreach (DatBase rom in dbr)
                {
                    rom.Name = dirName + "\\" + rom.Name;
                    root.ChildAdd(rom);
                }
            }
        }

        public static void DirectoryExpand(DatDir dDir)
        {
            DatBase[] arrDir = dDir.ToArray();
            bool foundSubDir = false;
            foreach (DatBase db in arrDir)
            {
                if (CheckDir(db))
                {
                    if (db.Name.Contains("\\"))
                    {
                        foundSubDir = true;
                        break;
                    }
                }
            }

            if (foundSubDir)
            {
                dDir.ChildrenClear();
                foreach (DatBase db in arrDir)
                {
                    if (CheckDir(db))
                    {
                        if (db.Name.Contains("\\"))
                        {
                            string dirName = db.Name;
                            int split = dirName.IndexOf("\\", StringComparison.Ordinal);
                            string part0 = dirName.Substring(0, split);
                            string part1 = dirName.Substring(split + 1);

                            db.Name = part1;
                            DatDir dirFind = new DatDir(DatFileType.Dir) { Name = part0 };
                            if (dDir.ChildNameSearch(dirFind, out int index) != 0)
                            {
                                dDir.ChildAdd(dirFind);
                            }
                            else
                            {
                                dirFind = (DatDir)dDir.Child(index);
                            }

                            if (part1.Length > 0)
                                dirFind.ChildAdd(db);
                            continue;
                        }
                    }
                    dDir.ChildAdd(db);
                }

                arrDir = dDir.ToArray();
            }

            foreach (DatBase db in arrDir)
            {
                if (db is DatDir dbDir)
                    DirectoryExpand(dbDir);
            }
        }

        private static bool CheckDir(DatBase db)
        {
            DatFileType dft = db.DatFileType;

            switch (dft)
            {
                // files inside of zips/7zips do not need to be expanded
                case DatFileType.File7Zip:
                case DatFileType.FileTorrentZip:
                    return false;
                // everything else should be fully expanded
                default:
                    return true;
            }
        }
    }
}
