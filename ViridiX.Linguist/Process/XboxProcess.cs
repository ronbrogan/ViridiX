﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ViridiX.Mason.Extensions;
using ViridiX.Mason.Logging;

namespace ViridiX.Linguist.Process
{
    /// <summary>
    /// TODO: description
    /// </summary>
    public class XboxProcess
    {
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly Xbox _xbox;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private readonly ILogger _logger;

        /// <summary>
        /// Retrieves a list of loaded modules.
        /// </summary>
        public List<XboxModule> Modules
        {
            get
            {
                List<XboxModule> modules = new List<XboxModule>();

                _xbox.CommandSession.SendCommandStrict("modules");
                foreach (var moduleResponse in _xbox.CommandSession.ReceiveLines())
                {
                    var moduleInfo = moduleResponse.ParseXboxResponseLine();
                    XboxModule module = new XboxModule
                    {
                        Name = (string) moduleInfo["name"],
                        BaseAddress = (long) moduleInfo["base"],
                        Size = (int) (long) moduleInfo["size"],
                        Checksum = (long) moduleInfo["check"],
                        TimeStamp = ((long) moduleInfo["timestamp"]).ToDateTimeFromEpochSeconds(),
                        Sections = new List<XboxModuleSection>()
                    };
 
                    _xbox.CommandSession.SendCommandStrict("modsections name=\"{0}\"", module.Name);
                    foreach (var sectionResponse in _xbox.CommandSession.ReceiveLines())
                    {
                        var sectionInfo = sectionResponse.ParseXboxResponseLine();
                        XboxModuleSection section = new XboxModuleSection
                        {
                            Name = (string) sectionInfo["name"],
                            Base = (long) sectionInfo["base"],
                            Size = (int) (long) sectionInfo["size"],
                            Index = (int) (long) sectionInfo["index"],
                            Flags = (long) sectionInfo["flags"]
                        };
                        module.Sections.Add(section);
                    }

                    modules.Add(module);
                }

                return modules;
            }
        }

        /// <summary>
        /// Retrieves a list of Xbox threads.
        /// </summary>
        public List<XboxThread> Threads
        {
            get
            {
                List<XboxThread> threads = new List<XboxThread>();

                // retrieves a list of thread ids and some additional information
                _xbox.CommandSession.SendCommandStrict("threads");
                foreach (string thread in _xbox.CommandSession.ReceiveLines())
                {
                    int threadId = Convert.ToInt32(thread);

                    // NOTE: assumes a single line returned in the multi-line response, unsure if Microsoft ever got around to including additional info
                    _xbox.CommandSession.SendCommandStrict("threadinfo thread={0}", threadId);
                    var info = _xbox.CommandSession.ReceiveLines()[0].ParseXboxResponseLine();

                    XboxThread threadInfo = new XboxThread
                    {
                        Id = threadId,
                        Suspend = (int) (long) info["suspend"],
                        Priority = (int) (long) info["priority"],
                        TlsBase = (long) info["tlsbase"],
                        Start = (long) info["start"],
                        Base = (long) info["base"],
                        Limit = (long) info["limit"],
                        CreationTime = DateTime.FromFileTime((long) info["createlo"] | ((long) info["createhi"] << 32))
                    };
                    threads.Add(threadInfo);
                }

                return threads;
            }
        }

        /// <summary>
        /// Initializes the Xbox process subsystem.
        /// </summary>
        /// <param name="xbox"></param>
        public XboxProcess(Xbox xbox)
        {
            if (xbox == null)
                throw new ArgumentNullException(nameof(xbox));

            _xbox = xbox;
            _logger = xbox.Logger;

            _logger?.Info("XboxProcess subsystem initialized");
        }

        /// <summary>
        /// Gets an Xbox module by name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public XboxModule GetModule(string name)
        {
            return _xbox.Process.Modules.FirstOrDefault(module => module.Name.Equals(name));
        }
    }
}
