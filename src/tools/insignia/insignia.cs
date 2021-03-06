//-------------------------------------------------------------------------------------------------
// <copyright file="insignia.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// The Insignia inscriber application.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace WixToolset.Tools
{
    using System;
    using System.CodeDom.Compiler;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using WixToolset.Data;

    /// <summary>
    /// The main entry point for Insignia.
    /// </summary>
    public sealed class Insignia
    {
        private string bundlePath;
        private string bundleWithAttachedContainerPath;
        private StringCollection invalidArgs;
        private string msiPath;
        private string outputPath;
        private bool showHelp;
        private bool showLogo;
        private bool tidy;

        /// <summary>
        /// Instantiate a new Insignia class.
        /// </summary>
        private Insignia()
        {
            this.invalidArgs = new StringCollection();
            this.showLogo = true;
            this.tidy = true;
        }

        /// <summary>
        /// The main entry point for Insignia.
        /// </summary>
        /// <param name="args">Commandline arguments for the application.</param>
        /// <returns>Returns the application error code.</returns>
        [MTAThread]
        public static int Main(string[] args)
        {
            AppCommon.PrepareConsoleForLocalization();
            Messaging.Instance.InitializeAppName("INSG", "Insignia.exe").Display += Insignia.DisplayMessage;

            Insignia insignia = new Insignia();
            return insignia.Run(args);
        }

        /// <summary>
        /// Handler for display message events.
        /// </summary>
        /// <param name="sender">Sender of message.</param>
        /// <param name="e">Event arguments containing message to display.</param>
        private static void DisplayMessage(object sender, DisplayEventArgs e)
        {
            Console.WriteLine(e.Message);
        }

        /// <summary>
        /// Main running method for the application.
        /// </summary>
        /// <param name="args">Commandline arguments to the application.</param>
        /// <returns>Returns the application error code.</returns>
        private int Run(string[] args)
        {
            bool inscribed = false;
            try
            {
                Inscriber inscriber = null;

                // parse the command line
                this.ParseCommandLine(args);

                // exit if there was an error parsing the command line (otherwise the logo appears after error messages)
                if (Messaging.Instance.EncounteredError)
                {
                    return Messaging.Instance.LastErrorNumber;
                }

                if (String.IsNullOrEmpty(this.msiPath) && String.IsNullOrEmpty(this.bundlePath))
                {
                    this.showHelp = true;
                }

                if (this.showLogo)
                {
                    AppCommon.DisplayToolHeader();
                }

                if (this.showHelp)
                {
                    Console.WriteLine(InsigniaStrings.HelpMessage);
                    AppCommon.DisplayToolFooter();
                    return Messaging.Instance.LastErrorNumber;
                }

                foreach (string parameter in this.invalidArgs)
                {
                    Messaging.Instance.OnMessage(WixWarnings.UnsupportedCommandLineArgument(parameter));
                }
                this.invalidArgs = null;

                // Calculate the output path.
                string inputPath = String.IsNullOrEmpty(this.msiPath) ? Path.GetFullPath(this.bundlePath) : Path.GetFullPath(this.msiPath);
                if (String.IsNullOrEmpty(this.outputPath))
                {
                    this.outputPath = inputPath;
                }
                else if (this.outputPath.EndsWith("\\"))
                {
                    this.outputPath = Path.GetFullPath(Path.Combine(this.outputPath, Path.GetFileName(inputPath)));
                }

                inscriber = new Inscriber();

                // Set the temp directory - if it's null, we'll default appropriately
                inscriber.TempFilesLocation = Environment.GetEnvironmentVariable("WIX_TEMP");

                if (!String.IsNullOrEmpty(this.msiPath))
                {
                    try
                    {
                        inscribed = inscriber.InscribeDatabase(inputPath, this.outputPath, this.tidy);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        Messaging.Instance.OnMessage(WixErrors.UnauthorizedAccess(inputPath));
                    }
                }
                else if (!String.IsNullOrEmpty(this.bundleWithAttachedContainerPath))
                {
                    inscribed = inscriber.InscribeBundle(this.bundleWithAttachedContainerPath, this.bundlePath, this.outputPath);
                }
                else
                {
                    inscribed = inscriber.InscribeBundleEngine(this.bundlePath, this.outputPath);
                }

                if (this.tidy)
                {
                    inscriber.DeleteTempFiles();
                }
            }
            catch (WixException we)
            {
                Messaging.Instance.OnMessage(we.Error);
            }
            catch (Exception e)
            {
                Messaging.Instance.OnMessage(WixErrors.UnexpectedException(e.Message, e.GetType().ToString(), e.StackTrace));
                if (e is NullReferenceException || e is SEHException)
                {
                    throw;
                }
            }

            // On success but nothing inscribed then return -1. Otherwise, return whatever last error number
            // was (which could be zero if successfully inscribed).
            return (0 == Messaging.Instance.LastErrorNumber && !inscribed) ? -1 : Messaging.Instance.LastErrorNumber;
        }

        /// <summary>
        /// Parse the commandline arguments.
        /// </summary>
        /// <param name="args">Commandline arguments.</param>
        private void ParseCommandLine(string[] args)
        {
            for (int i = 0; i < args.Length; ++i)
            {
                string arg = args[i];

                // skip blank arguments
                if (null == arg || 0 == arg.Length)
                {
                    continue;
                }

                if ('-' == arg[0] || '/' == arg[0])
                {
                    string parameter = arg.Substring(1);

                    if ("?" == parameter || "help" == parameter)
                    {
                        this.showHelp = true;
                        return;
                    }
                    else if ("ab" == parameter) // attach container to bundle
                    {
                        this.bundlePath = CommandLine.GetFile(parameter, args, ++i);
                        this.bundleWithAttachedContainerPath = CommandLine.GetFile(parameter, args, ++i);
                    }
                    else if ("ib" == parameter) // inscribe bundle
                    {
                        this.bundlePath = CommandLine.GetFile(parameter, args, ++i);
                    }
                    else if ("im" == parameter) // inscribe msi
                    {
                        this.msiPath = CommandLine.GetFile(parameter, args, ++i);
                    }
                    else if ("nologo" == parameter)
                    {
                        this.showLogo = false;
                    }
                    else if ("notidy" == parameter)
                    {
                        this.tidy = false;
                    }
                    else if ("o" == parameter || "out" == parameter)
                    {
                        this.outputPath = CommandLine.GetFileOrDirectory(parameter, args, ++i);
                    }
                    else if ("swall" == parameter)
                    {
                        Messaging.Instance.OnMessage(WixWarnings.DeprecatedCommandLineSwitch("swall", "sw"));
                        Messaging.Instance.SuppressAllWarnings = true;
                    }
                    else if (parameter.StartsWith("sw", StringComparison.Ordinal))
                    {
                        string paramArg = parameter.Substring(2);
                        try
                        {
                            if (0 == paramArg.Length)
                            {
                                Messaging.Instance.SuppressAllWarnings = true;
                            }
                            else
                            {
                                int suppressWarning = Convert.ToInt32(paramArg, CultureInfo.InvariantCulture.NumberFormat);
                                if (0 >= suppressWarning)
                                {
                                    Messaging.Instance.OnMessage(WixErrors.IllegalSuppressWarningId(paramArg));
                                }

                                Messaging.Instance.SuppressWarningMessage(suppressWarning);
                            }
                        }
                        catch (FormatException)
                        {
                            Messaging.Instance.OnMessage(WixErrors.IllegalSuppressWarningId(paramArg));
                        }
                        catch (OverflowException)
                        {
                            Messaging.Instance.OnMessage(WixErrors.IllegalSuppressWarningId(paramArg));
                        }
                    }
                    else if ("v" == parameter)
                    {
                        Messaging.Instance.ShowVerboseMessages = true;
                    }
                    else if (parameter.StartsWith("wx", StringComparison.Ordinal))
                    {
                        string paramArg = parameter.Substring(2);
                        try
                        {
                            if (0 == paramArg.Length)
                            {
                                Messaging.Instance.WarningsAsError = true;
                            }
                            else
                            {
                                int elevateWarning = Convert.ToInt32(paramArg, CultureInfo.InvariantCulture.NumberFormat);
                                if (0 >= elevateWarning)
                                {
                                    Messaging.Instance.OnMessage(WixErrors.IllegalWarningIdAsError(paramArg));
                                }

                                Messaging.Instance.ElevateWarningMessage(elevateWarning);
                            }
                        }
                        catch (FormatException)
                        {
                            Messaging.Instance.OnMessage(WixErrors.IllegalWarningIdAsError(paramArg));
                        }
                        catch (OverflowException)
                        {
                            Messaging.Instance.OnMessage(WixErrors.IllegalWarningIdAsError(paramArg));
                        }
                    }
                    else
                    {
                        this.invalidArgs.Add(parameter);
                    }
                }
                else if ('@' == arg[0])
                {
                    this.ParseCommandLine(CommandLineResponseFile.Parse(arg.Substring(1)));
                }
                else
                {
                    this.invalidArgs.Add(arg);
                }
            }
        }
    }
}
