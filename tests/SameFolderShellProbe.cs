using System;
using System.IO;
using System.Runtime.InteropServices;

class Probe
{
    [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
    struct SHFILEOPSTRUCT
    {
        public IntPtr hwnd; public uint wFunc;
        [MarshalAs(UnmanagedType.LPWStr)] public string pFrom;
        [MarshalAs(UnmanagedType.LPWStr)] public string pTo;
        public ushort fFlags;
        [MarshalAs(UnmanagedType.Bool)] public bool fAnyOperationsAborted;
        public IntPtr hNameMappings;
        [MarshalAs(UnmanagedType.LPWStr)] public string lpszProgressTitle;
    }
    [DllImport("shell32.dll", CharSet=CharSet.Unicode)]
    static extern int SHFileOperation(ref SHFILEOPSTRUCT op);
    static void Copy(string from,string to,ushort flags)
    {
        SHFILEOPSTRUCT op = new SHFILEOPSTRUCT();
        op.wFunc=2; op.pFrom=from+"\0\0"; op.pTo=to+"\0\0"; op.fFlags=flags;
        int result=SHFileOperation(ref op);
        Console.WriteLine("result="+result+" aborted="+op.fAnyOperationsAborted);
        if(result!=0 || op.fAnyOperationsAborted) throw new Exception("SHFileOperation unsuccessful");
    }
    static void Main()
    {
        string dir=Path.Combine(Path.GetTempPath(),"Ferry-Duplicate-Probe-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try {
            string file=Path.Combine(dir,"sample.txt"); File.WriteAllText(file,"hello");
            Copy(file,dir,0x0008|0x0010|0x0400|0x0200);
            foreach(string p in Directory.GetFiles(dir)) Console.WriteLine("FILE "+Path.GetFileName(p));
            if(Directory.GetFiles(dir).Length!=2) throw new Exception("same-folder file duplicate not made");
            string folder=Path.Combine(dir,"sample-dir"); Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder,"child.txt"),"child");
            Copy(folder,dir,0x0008|0x0010|0x0400|0x0200);
            foreach(string p in Directory.GetDirectories(dir)) Console.WriteLine("DIR "+Path.GetFileName(p));
            if(Directory.GetDirectories(dir).Length!=2) throw new Exception("same-folder dir duplicate not made");
            Console.WriteLine("PASS");
        } finally { try { Directory.Delete(dir,true); } catch {} }
    }
}