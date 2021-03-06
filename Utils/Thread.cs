﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StackTracer.Utils
{
    /// <summary>
    /// Thread object contains the information related to the current thread for which stack
    /// trace is being generated
    /// </summary>
    public class Thread
    {
        public Thread()
        {
        }
        public Thread(DateTime stackCaptureTime, List<StackFrame> stackTrace)
        {
            this.sampleCaptureTime = stackCaptureTime;
            this.stackTrace = stackTrace;
        }
        public DateTime sampleCaptureTime { get; set; }
        public int managedThreadId { get; set; }
        public uint oSID { get; set; }
        public List<StackFrame> stackTrace { get; set; }
    }
}
