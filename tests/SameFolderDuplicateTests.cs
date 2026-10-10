using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

class DuplicateTests
{
    static object Call(Type t, string method, params object[] args)
    {
        try { return t.GetMethod(method, BindingFlags.Public | BindingFlags.Static).Invoke(null,args); }
        catch(TargetInvocationException e) { throw e.InnerException; }
    }
    static void Expect(bool condition,string description)
    {
        if(!condition) throw new Exception("FAIL: "+description);
        Console.WriteLine("PASS: "+description);
    }
    [STAThread]
    static void Main(string[] args)
    {
        string exe=Path.GetFullPath(args[0]);
        Assembly ferry=Assembly.LoadFrom(exe);
        // Exercise the real Ferry UI-dispatcher transfer route, not merely its
        // fallback for non-WPF callers: CopyWithDestinations must pump the frame
        // until its dedicated Shell STA worker posts completion to this dispatcher.
        string frameworkDirectory=Path.GetDirectoryName(typeof(object).Assembly.Location);
        Assembly wpf=Assembly.LoadFrom(Path.Combine(frameworkDirectory,"WPF","PresentationFramework.dll"));
        Type appType=wpf.GetType("System.Windows.Application",true);
        object app=Activator.CreateInstance(appType);
        Expect(app!=null,"WPF dispatcher initialized for transfer regression");
        Type planner=ferry.GetType("Ferry.SameFolderDuplicatePlan",true);
        Type shell=ferry.GetType("Ferry.ShellFileOperations",true);
        string root=Path.Combine(Path.GetTempPath(),"Ferry-Duplicate-Regression-"+Guid.NewGuid().ToString("N"));
        string dir=Path.Combine(root,"Current"), other=Path.Combine(root,"Other");
        Directory.CreateDirectory(dir);
        Directory.CreateDirectory(other);
        try
        {
            string file=Path.Combine(dir,"report.txt");
            string existing=Path.Combine(dir,"report (1).txt");
            File.WriteAllText(file,"original");
            File.WriteAllText(existing,"preexisting");
            string folder=Path.Combine(dir,"Photos");
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder,"inside.txt"),"child");

            var sources=new List<string>{file,folder};
            var targets=(List<string>)Call(planner,"CreateTargets",dir,sources);
            Expect(targets.Count==2 &&
                Path.GetFileName(targets[0])=="report (2).txt" &&
                Path.GetFileName(targets[1])=="Photos (1)","reserved names and file/folder suffixes");
            bool result=(bool)Call(shell,"CopyWithDestinations",sources,targets);
            Expect(result && File.ReadAllText(targets[0])=="original" &&
                File.ReadAllText(Path.Combine(targets[1],"inside.txt"))=="child","mixed file/directory batch Shell copy");
            Expect(File.ReadAllText(file)=="original" &&
                File.ReadAllText(existing)=="preexisting" &&
                File.Exists(Path.Combine(folder,"inside.txt")),"all originals and pre-existing copy unchanged");

            var repeated=new List<string>{file,file};
            var next=(List<string>)Call(planner,"CreateTargets",dir,repeated);
            Expect(Path.GetFileName(next[0])=="report (3).txt" &&
                Path.GetFileName(next[1])=="report (4).txt","multiple planned copies of one source get distinct names");
            Expect((bool)Call(shell,"CopyWithDestinations",repeated,next) &&
                File.Exists(next[0]) && File.Exists(next[1]),"repeated source multi-destination Shell copy");

            Expect(!(bool)Call(planner,"IsSameFolderSource",Path.Combine(other,"report.txt"),dir),
                "external source not classified as same-folder");
            bool rejected=false;
            try { Call(shell,"CopyWithDestinations",new List<string>{file},new List<string>{file}); }
            catch(IOException) { rejected=true; }
            Expect(rejected && File.ReadAllText(file)=="original","mapped copy refuses source overwrite");

            Console.WriteLine("ALL TESTS PASS");
        }
        finally { try { Directory.Delete(root,true); } catch {} }
    }
}
