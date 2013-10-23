using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Collections.Generic;

namespace RingtoneManager
{
    public class MediaAttach
    {

        public class EnumFile
        {
            public string FileName;
            public bool isFolder;
        }

        public static EnumFile[] EnumerateFiles(string folder)
        {
            InteropSvc.InteropLib.WIN32_FIND_DATA data;
            uint handle = InteropSvc.InteropLib.Instance.FindFirstFile7(folder + "\\*", out data);
            var list = new List<EnumFile>();

            if (handle != 0xFFFFFFFFU)
            {
                bool result = false;
                do
                {
                    if (data.cFileName != "." && data.cFileName != "..")
                    {
                        EnumFile ef = new EnumFile();
                        ef.FileName = data.cFileName;
                        bool t = ((data.dwFileAttributes & 0x10) == 0x10) ? true : false;
                        ef.isFolder = t ? true : false;
                        list.Add(ef);
                    }
                    result = InteropSvc.InteropLib.Instance.FindNextFile7(handle, out data);
                } while (result != false);
                InteropSvc.InteropLib.Instance.FindClose7(handle);
            }
            return list.ToArray();
        }

        private static void CopyDirectory(string src, string dest)
        {
            EnumFile[] files = EnumerateFiles(src);
            InteropSvc.InteropLib.Instance.CreateDirectory7(dest);
            if (files.Length > 0)
            {
                foreach (EnumFile ef in files)
                {
                    if (ef.isFolder == true)
                    {
                        CopyDirectory(src + "\\" + ef.FileName, dest + "\\" + ef.FileName);
                    }
                    else
                    {
                        InteropSvc.InteropLib.Instance.DeleteFile7(dest + "\\" + ef.FileName);
                        InteropSvc.InteropLib.Instance.CopyFile7(src + "\\" + ef.FileName, dest + "\\" + ef.FileName, false);
                    }
                }
            }
        }


        public static void AttachImagesToArtists()
        {
            var files = EnumerateFiles("\\MediaAttach");
            CopyDirectory("\\MediaAttach", "\\Applications\\Data\\9cefc0bf-7060-45b0-ba66-2d1dcad8dc3c\\Data\\IsolatedStore");
            CopyDirectory("\\Applications\\Install\\9CEFC0BF-7060-45B0-BA66-2D1DCAD8DC3C\\Install\\MediaAttach", "\\Applications\\Data\\9cefc0bf-7060-45b0-ba66-2d1dcad8dc3c\\Data\\IsolatedStore");
            InteropSvc.InteropLib.Instance.RemoveAllDummyMusicFiles();
            InteropSvc.InteropLib.Instance.FlushMediaDatabase();
            int trackNum = DateTime.Now.TimeOfDay.Seconds;
            foreach (var file in files)
            {
                string fnameLower = file.FileName.ToLower();
                if (fnameLower.EndsWith(".jpg"))
                {
                    string bandName = file.FileName.Substring(0, file.FileName.LastIndexOf("."));
                    InteropSvc.InteropLib.Instance.RegistrySetDWORD7(InteropSvc.InteropLib.HKEY_LOCAL_MACHINE, "Software\\Microsoft\\Zune\\Events", "ZNetSyncState", 0);
                    InteropSvc.InteropLib.Instance.AddMusicFile("dummy.mp3",
                        trackNum,
                        "...ultradummy_" + file.FileName.GetHashCode().ToString() + "_" + bandName.GetHashCode().ToString(),
                        1000,
                        bandName,
                        "Rock" + file.FileName.GetHashCode().ToString(),
                        "zzz_Dummy",
                        "zzz_Dummy_" + file.FileName.GetHashCode().ToString() + "_" + bandName.GetHashCode().ToString(),
                        "1900-01-01",
                        "cover.jpg",
                        file.FileName,
                        file.FileName);
                }
            }
            InteropSvc.InteropLib.Instance.HideAllDummyMusicFiles();
            InteropSvc.InteropLib.Instance.FlushMediaDatabase();
        }
    }
}
