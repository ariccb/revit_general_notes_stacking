using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace rjc.ManifestFilePatch
{
    class Program
    {
        static void Main(string[] args)
        {
            //find user directory
            string commongApplictionDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            int revitVersion = 2017;

            List<string> manifestFileDirectoryList = new List<string>();
            string manifestFileDirectory;

            manifestFileDirectoryList.Add(commongApplictionDataPath);
            manifestFileDirectoryList.Add("Autodesk");
            manifestFileDirectoryList.Add("Revit");
            manifestFileDirectoryList.Add("Addins");
            manifestFileDirectoryList.Add(revitVersion.ToString());

            manifestFileDirectory = Path.Combine(manifestFileDirectoryList.ToArray());

            while (Directory.Exists(manifestFileDirectory))
            {
                string autopdFilePath = Path.Combine(manifestFileDirectory, "RJC AutoPDF.addin");
                string beamScheduleToolsPath = Path.Combine(manifestFileDirectory, "BeamScheduleTools" + revitVersion.ToString() + ".addin");

                revitVersion++;
                manifestFileDirectoryList.Clear();
                manifestFileDirectoryList.Add(commongApplictionDataPath);
                manifestFileDirectoryList.Add("Autodesk");
                manifestFileDirectoryList.Add("Revit");
                manifestFileDirectoryList.Add("Addins");
                manifestFileDirectoryList.Add(revitVersion.ToString());

                manifestFileDirectory = Path.Combine(manifestFileDirectoryList.ToArray());

                if(File.Exists(autopdFilePath))
                {
                    //File.Delete(Path.Combine(manifestFileDirectory, autopdFilePath));
                }

                if(File.Exists(beamScheduleToolsPath))
                {
                    //File.Delete(Path.Combine(manifestFileDirectory, beamScheduleToolsPath));
                }

                Console.WriteLine(autopdFilePath + " deleted");
                Console.WriteLine();
                Console.WriteLine(beamScheduleToolsPath + " deleted");
                Console.WriteLine();

            }

            Console.WriteLine();
            Console.WriteLine("Press Enter To Continue");
            Console.ReadKey();

        }
    }
}
