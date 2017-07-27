using CommandLine;
using CommandLine.Text;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PCCExportTableCompare
{
    class Options
    {
        [Option("File1", HelpText = "File 1 to scan")]
        public string File1Path { get; set; }

        [Option("File2", HelpText = "File 2 to scan")]
        public string File2Path { get; set; }

        [Option("File1ExportIndex", HelpText = "Export index to pull class header from (File1)")]
        public string File1ExportIndex { get; set; }

        [Option("File2ExportIndex", HelpText = "Export index to pull class header from (File2)")]
        public string File2ExportIndex { get; set; }

        [Option("ReportFile", HelpText = "Filepath to write report to (optional)")]
        public string ReportFile { get; set; }

        [Option("Verbose", HelpText = "Outputs parsing info", DefaultValue = false)]
        public bool verbose { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this,
              (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }

    class Program
    {
        public static bool Verbose { get; private set; }

        static void Main(string[] args)
        {
            Console.WriteLine("PCC Class Function Import Comparison tool");
            Console.WriteLine("By ME3Tweaks");
            var options = new Options();
            if (CommandLine.Parser.Default.ParseArguments(args, options))
            {
                if (options.File1Path == null || options.File2Path == null || options.File1ExportIndex == null || options.File2ExportIndex == null)
                {
                    Console.WriteLine("All paramaters are mandatory.");
                    Console.WriteLine("File1 Present: " + options.File1Path);
                    Console.WriteLine("File1ExportIndex Present: " + options.File1ExportIndex);
                    Console.WriteLine("File2 Present: " + options.File2Path);
                    Console.WriteLine("File1ExportIndex Present: " + options.File2ExportIndex);
                    EndProgram(1);
                }

                Verbose = options.verbose;

                string inputfile = options.File1Path;
                string inputfile2 = options.File2Path;

                int export1 = Convert.ToInt32(options.File1ExportIndex);
                int export2 = Convert.ToInt32(options.File2ExportIndex);

                PCCObject file1 = new PCCObject(inputfile);
                PCCObject file2 = new PCCObject(inputfile2);


                List<String> file1TableNames = ReadTableBackwards(file1, export1);

                Console.WriteLine("======================");
                List<String> file2TableNames = ReadTableBackwards(file2, export2);

                file1TableNames.Reverse();
                file2TableNames.Reverse();
                List<string> ThirdList = file2TableNames.Except(file1TableNames).ToList();
                using (var cc = new ConsoleCopy(options.ReportFile))
                {
                    Console.WriteLine("======Difference report=====");
                    Console.WriteLine("File1 (Export " + export1 + "):\t" + inputfile);
                    Console.WriteLine("File2 (Export " + export2 + "):\t" + inputfile2);

                    Console.WriteLine("> indicates the function table index exists in both, but the package.objectname are not the same.");
                    Console.WriteLine("Table size difference: " + Math.Abs(file2TableNames.Count - file1TableNames.Count));
                    Console.WriteLine("Index\tFile\t| Full Object Name");
                    int existInBothDifferences = 0;
                    int biggerTableSize = Math.Max(file1TableNames.Count, file2TableNames.Count);
                    for (int i = 0; i < biggerTableSize; i++)
                    {
                        if (i < file1TableNames.Count && i < file2TableNames.Count && file1TableNames[i] == file2TableNames[i])
                        {
                            Console.WriteLine(i + "\tBOTH\t| " + file1TableNames[i]);
                            continue;
                        }
                        if (i < file1TableNames.Count && i < file2TableNames.Count && file1TableNames[i] != file2TableNames[i])
                        {
                            Console.WriteLine(">" + i + "\tFILE1 \t| " + file1TableNames[i]);
                            Console.WriteLine(">" + i + "\tFILE2 \t| " + file2TableNames[i]);
                            existInBothDifferences++;
                            continue;
                        }

                        if (i < file1TableNames.Count)
                        {
                            Console.WriteLine(i + "\tFILE1 ONLY\t| " + file1TableNames[i]);
                            continue;
                        }

                        if (i < file2TableNames.Count)
                        {
                            Console.WriteLine(i + "\tFILE2 ONLY\t| " + file2TableNames[i]);
                            continue;
                        }
                    }
                    Console.WriteLine(existInBothDifferences + " items exist in both as indexes but have different data.");
                    if (options.ReportFile!=null)
                    {
                        Console.WriteLine("Report written to " + options.ReportFile);
                    }
                }
                EndProgram(0);
            }
        }

        private static List<string> ReadTableBackwards(PCCObject pcc, int exportIndex)
        {
            Console.WriteLine("Parsing " + pcc.pccFileName);
            List<String> tableItems = new List<string>();

            byte[] data = pcc.Exports[exportIndex].Data;
            int endOffset = data.Length;
            int count = 0;
            endOffset -= 4; //int
            while (endOffset > 0)
            {
                int index = BitConverter.ToInt32(data, endOffset);
                if (index < 0 && -index - 1 < pcc.Imports.Count)
                {
                    //import
                    int localindex = Math.Abs(index) - 1;
                    if (Verbose)
                    {
                        Console.WriteLine("IMPORT\t"+localindex + "\t| " + pcc.Imports[localindex].PackageFullName + "." + pcc.Imports[localindex].ObjectName + "("+index.ToString("X8")+" at 0x" + endOffset.ToString("X8") + ")");
                    }
                    tableItems.Add(pcc.Imports[localindex].PackageFullName + "." + pcc.Imports[localindex].ObjectName);
               }
                else if (index > 0 && index != count)
                {
                    int localindex = index - 1;
                    Console.WriteLine("EXPORT\t" +  index+", "+pcc.Exports[localindex].PackageFullName+"."+pcc.Exports[localindex].ObjectName + "(" + index.ToString("X8") + " at 0x" + endOffset.ToString("X8") + ")");

                } else
                {
                    Console.WriteLine("UNPARSED INDEX: " + index);
                }
                //Console.WriteLine(index);
                if (index == count)
                {
                    Console.WriteLine("FOUND START OF LIST AT 0x" + endOffset.ToString("X8") + ", items: " + index);
                    break;
                }
                endOffset -= 4;
                count++;
            }

            Console.WriteLine("Number of items processed: " + count);
            return tableItems;
        }

        private static void EndProgram(int v)
        {
#if DEBUG
            Console.WriteLine("Press any key to close");
            Console.ReadKey();
#endif
            Environment.Exit(v);
        }
    }
}
