using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Builder
{

    public class WindowsExplorer
    {

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class OpenFileName
        {
            public int structSize = 0;
            public IntPtr dlgOwner = IntPtr.Zero;
            public IntPtr instance = IntPtr.Zero;
            public String filter = null;
            public String customFilter = null;
            public int maxCustFilter = 0;
            public int filterIndex = 0;
            public IntPtr file;
            public int maxFile = 0;
            public String fileTitle = null;
            public int maxFileTitle = 0;
            public String initialDir = null;
            public String title = null;
            public int flags = 0;
            public short fileOffset = 0;
            public short fileExtension = 0;
            public String defExt = null;
            public IntPtr custData = IntPtr.Zero;
            public IntPtr hook = IntPtr.Zero;
            public String templateName = null;
            public IntPtr reservedPtr = IntPtr.Zero;
            public int reservedInt = 0;
            public int flagsEx = 0;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class OpenDialogDir
        {
            public IntPtr hwndOwner = IntPtr.Zero;
            public IntPtr pidlRoot = IntPtr.Zero;
            public String pszDisplayName = null;
            public String lpszTitle = null;
            public UInt32 ulFlags = 0;
            public IntPtr lpfn = IntPtr.Zero;
            public IntPtr lParam = IntPtr.Zero;
            public int iImage = 0;
        }


        [DllImport("Comdlg32.dll", SetLastError = true, ThrowOnUnmappableChar = true, CharSet = CharSet.Auto)]
        public static extern bool GetOpenFileName([In, Out] OpenFileName ofn);

        [DllImport("Comdlg32.dll", SetLastError = true, ThrowOnUnmappableChar = true, CharSet = CharSet.Auto)]
        public static extern bool GetSaveFileName([In, Out] OpenFileName ofn);

        [DllImport("shell32.dll", SetLastError = true, ThrowOnUnmappableChar = true, CharSet = CharSet.Auto)]
        public static extern IntPtr SHBrowseForFolder([In, Out] OpenDialogDir ofn);

        [DllImport("shell32.dll", SetLastError = true, ThrowOnUnmappableChar = true, CharSet = CharSet.Auto)]
        public static extern bool SHGetPathFromIDList([In] IntPtr pidl, [In, Out] char[] fileName);

        /// <summary>
        /// 选择文件
        /// </summary>
        public static List<string> FileSelector(string defaultPath = null, string filter = "All Files\0*.*\0\0")
        {
            OpenFileName openFileName = new OpenFileName();
            openFileName.structSize = Marshal.SizeOf(openFileName);
            openFileName.filter = filter;// "All Files\0*.*\0\0"; //"Excel文件(*.xlsx)\0*.xlsx";
            openFileName.fileTitle = new string(new char[256]);
            openFileName.maxFileTitle = openFileName.fileTitle.Length;
            openFileName.initialDir = defaultPath.Replace('/', '\\');// Application.streamingAssetsPath.Replace('/', '\\');   //默认路径
            openFileName.title = "选择文件";
            openFileName.flags = 0x00080000 | 0x00001000 | 0x00000800 | 0x00000200 | 0x00000008; //OFN_EXPLORER|OFN_FILEMUSTEXIST|OFN_PATHMUSTEXIST| OFN_ALLOWMULTISELECT|OFN_NOCHANGEDIR

            string fileNames = new String(new char[2048]);
            openFileName.file = Marshal.StringToBSTR(fileNames);
            openFileName.maxFile = fileNames.Length;

            List<string> result = null;
            if (GetOpenFileName(openFileName))
            {
                result = new List<string>();

                List<string> selectedFilesList = new List<string>();

                long pointer = (long)openFileName.file;
                string file = Marshal.PtrToStringAuto(openFileName.file);

                while (file.Length > 0)
                {
                    selectedFilesList.Add(file);

                    pointer += file.Length * 2 + 2;
                    openFileName.file = (IntPtr)pointer;
                    file = Marshal.PtrToStringAuto(openFileName.file);
                }

                if (selectedFilesList.Count == 1)
                {
                    result = selectedFilesList;
                }
                else
                {
                    string[] selectedFiles = new string[selectedFilesList.Count - 1];

                    for (int i = 0; i < selectedFiles.Length; i++)
                    {
                        selectedFiles[i] = selectedFilesList[0] + "\\" + selectedFilesList[i + 1];
                    }
                    result = new List<string>(selectedFiles);
                }
            }
            return result;
        }

        ///// <summary>
        ///// 保存文件
        ///// </summary>
        //public static string SaveFile(string defaultPath = null)
        //{
        //    OpenFileName openFileName = new OpenFileName();
        //    openFileName.structSize = Marshal.SizeOf(openFileName);
        //    openFileName.filter = "All Files\0*.*\0\0"; //"Excel文件(*.xlsx)\0*.xlsx";
        //    openFileName.file = new string(new char[256]);
        //    openFileName.maxFile = openFileName.file.Length;
        //    openFileName.fileTitle = new string(new char[64]);
        //    openFileName.maxFileTitle = openFileName.fileTitle.Length;
        //    openFileName.initialDir = defaultPath;// Application.streamingAssetsPath.Replace('/', '\\');   //默认路径
        //    openFileName.title = "窗口标题";
        //    openFileName.flags = 0x00080000 | 0x00001000 | 0x00000800 | 0x00000200 | 0x00000008; //OFN_EXPLORER|OFN_FILEMUSTEXIST|OFN_PATHMUSTEXIST| OFN_ALLOWMULTISELECT|OFN_NOCHANGEDIR

        //    string ret = "";
        //    if (GetSaveFileName(openFileName))
        //    {
        //        ret = openFileName.file;
        //    }
        //    return ret;
        //}

        /// <summary>
        /// 选择文件夹
        /// </summary>
        /// <returns></returns>
        public static string FolderSelector()
        {
            OpenDialogDir ofn2 = new OpenDialogDir();
            ofn2.pszDisplayName = new string(new char[2000]);   // 存放目录路径缓冲区 
                                                                //ofn2.lpszTitle = dialogtitle;   //标题
                                                                //ofn2.ulFlags = 0x00000040; // 新的样式,带编辑框
            IntPtr pidlPtr = SHBrowseForFolder(ofn2);
            char[] charArray = new char[2000];
            for (int i = 0; i < charArray.Length; i++)
                charArray[i] = '\0';
            SHGetPathFromIDList(pidlPtr, charArray);
            string res = new String(charArray);
            res = res.Substring(0, res.IndexOf('\0'));
            return res;
        }

    }

}

