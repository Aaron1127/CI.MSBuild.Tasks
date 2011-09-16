﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Resources;
using CI.MSBuild.Tasks.Properties;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.VisualStudio.Coverage.Analysis;

namespace CI.MSBuild.Tasks
{
    /// <summary>
    /// Converts the coverage file generated by
    /// the Visual Studio Testing Tools (MSTest) into XML.
    /// </summary>
    public class ConvertVSCoverageToXml : Task
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConvertVSCoverageToXml"/> class.
        /// </summary>
        public ConvertVSCoverageToXml()
            : base(new ResourceManager(typeof(Resources)))
        {
        }

        /// <summary>
        /// Gets or sets the files containing
        /// Visual Studio code coverage data.
        /// </summary>
        [Required]
        public ITaskItem[] CoverageFiles { get; set; }

        /// <summary>
        /// Gets or sets the path to the directory
        /// containing the symbols of the intrumented binaries being covered.
        /// </summary>
        /// <remarks>
        /// By default the symbols are expected to be in the
        /// same directory as the coverage file.
        /// </remarks>
        public ITaskItem SymbolsDirectory { get; set; }

        /// <summary>
        /// Gets or sets the path to the directory
        /// where to store the converted files.
        /// </summary>
        /// <remarks>
        /// By default the converted files will be saved
        /// in the directory where the build is run from.
        /// </remarks>
        public ITaskItem OutputDirectory { get; set; }

        /// <summary>
        /// Gets the files generated from the conversion.
        /// </summary>
        [Output]
        public ITaskItem[] ConvertedFiles { get; private set; }

        /// <summary>
        /// Executes the task.
        /// </summary>
        /// <returns>
        /// true if the task successfully executed; otherwise, false.
        /// </returns>
        public override bool Execute()
        {
            List<ITaskItem> results = new List<ITaskItem>();

            foreach (ITaskItem file in CoverageFiles)
            {
                try
                {
                    string sourceFile = file.ItemSpec;

                    if (File.Exists(sourceFile))
                    {
                        Log.LogMessageFromResources(MessageImportance.Normal, "ConvertingVSCoverageFile", sourceFile);

                        string symbolsDir = Path.GetDirectoryName(sourceFile);

                        if (SymbolsDirectory != null)
                        {
                            symbolsDir = SymbolsDirectory.ItemSpec;
                        }


                        using (CoverageInfo info = CoverageInfo.CreateFromFile(
                            sourceFile, executablePaths: new [] { symbolsDir }, symbolPaths: new [] {symbolsDir}))
                        {
                            CoverageDS dataSet = info.BuildDataSet(null);
                            string outputFile = Path.ChangeExtension(sourceFile, "xml");

                            // Unless an output dir is specified
                            // the converted files will be stored in the same dir
                            // as the source files, with the .XML extension
                            if (OutputDirectory != null)
                            {
                                outputFile = Path.Combine(OutputDirectory.ItemSpec, Path.GetFileName(outputFile));
                            }

                            dataSet.WriteXml(outputFile);

                            Log.LogMessageFromResources(MessageImportance.Normal, "WrittenXmlCoverageFile", outputFile);
                        }

                        ITaskItem item = new TaskItem(file);
                        results.Add(item);
                    }
                    else
                    {
                        Log.LogMessageFromResources(MessageImportance.Normal, "SkippingNonExistentFile", sourceFile);
                    }
                }
                catch (Exception e)
                {
                    Log.LogErrorFromException(e, true);
                }
            }

            ConvertedFiles = results.ToArray();

            return !Log.HasLoggedErrors;
        }
    }
}
