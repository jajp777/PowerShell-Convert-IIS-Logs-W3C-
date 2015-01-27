using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;

namespace ConvertFromIISLogFile
{
    [Cmdlet(VerbsData.Export, "IISLogStats")]
    public class EportIISLogStats : Cmdlet
    {
        private bool stopRequest;

        [Parameter(
            Mandatory = false,
            ValueFromPipelineByPropertyName = true,
            ValueFromPipeline = false,
            Position = 0,
            HelpMessage = "IIS Log"
            )]
        [ValidateNotNull]
        public LogEntry[] LogEntries { get; set; }


        [Parameter(
            Mandatory = false,
            ValueFromPipelineByPropertyName = true,
            ValueFromPipeline = false,
            Position = 0,
            HelpMessage = "IIS Log"
            )]
        [ValidateNotNull]
        public FileInfo[] InputFiles { get; set; }

        #region Resolution


        #region NoProgress

        [Parameter(
            Mandatory = false,
            Position = 1,
            HelpMessage = "Don't display current line for more perfomance."
            )]
        [ValidateNotNull]
        public SwitchParameter NoProgress { get; set; }

        #endregion

        public const string ResolutionMinute = "Minute";
        public const string ResolutionHour = "Hour";
        public const string ResolutionDay = "Day";
        public const string ResolutionWeek = "Week";

        [Parameter(
            Mandatory = true,
            ValueFromPipelineByPropertyName = true,
            ValueFromPipeline = false,
            Position = 1,
            HelpMessage = "IIS Log"
            )]
        [ValidateNotNull]
        [ValidateSet(ResolutionMinute, ResolutionHour, ResolutionDay, ResolutionWeek)]
        public string Resolution { get; set; }

        #endregion

        protected override void ProcessRecord()
        {
            if (this.InputFiles == null && this.LogEntries == null)
            {
                this.WriteError(new ErrorRecord(new NullReferenceException("InputFiles and LogEntries are null."),"0002",ErrorCategory.InvalidData, this ));
                return;
            }

            List<LogEntry> logEntries = new List<LogEntry>();
            if (this.InputFiles != null)
            {
                this.WriteVerbose("Reading log files ...");
                LogReader.ProcessLogFiles(this.InputFiles, entry => logEntries.Add(entry), this.WriteProgress, this.ErrorHandling, this.NoProgress.IsPresent, () => { return this.stopRequest; });
            }
            else
            {
                logEntries = this.LogEntries.ToList();
            }
            
            StatsGenerator.Create(logEntries, this.Resolution, this.WriteOutputCallback, this.WriteVerboseCallback, () => this.stopRequest);
        }


        private void WriteProgress(int index, int total, string fullname)
        {
            ProgressRecord progressRecord;

            if (this.NoProgress.IsPresent)
            {
                progressRecord = new ProgressRecord(0, String.Format("Read file: {0}", fullname), String.Format("No line count because of switch NoProgress"));
            }
            else
            {
                progressRecord = new ProgressRecord(0, String.Format("Read file: {0}", fullname), String.Format("Read line {0} of {1}", index, total));
                if (index > 0)
                {
                    progressRecord.PercentComplete = (int)((double)index / total * 100);
                }
            }

            this.WriteProgress(progressRecord);
        }

        private void ErrorHandling(Exception obj)
        {
            if (this.stopRequest) return;
            this.WriteError(new ErrorRecord(obj, "0001", ErrorCategory.InvalidData, this));
        }

        private void WriteOutputCallback(string entry)
        {
            if (this.stopRequest) return;

            try
            {
                this.WriteObject(entry);
            }
            catch (PipelineStoppedException)
            {
                this.stopRequest = true;
            }
        }

        private void WriteVerboseCallback(string logEntry)
        {
            if (this.stopRequest) return;

            try
            {
                this.WriteVerbose(logEntry);
            }
            catch (PipelineStoppedException)
            {
                this.stopRequest = true;
            }
        }
    }
}