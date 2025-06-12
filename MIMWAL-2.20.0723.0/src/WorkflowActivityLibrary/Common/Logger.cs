﻿//-----------------------------------------------------------------------------------------------------------------------
// <copyright file="Logger.cs" company="Microsoft">
//      Copyright (c) Microsoft. All Rights Reserved.
//      Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.
// </copyright>
// <summary>
// Logger class
// The WriteError message parameter is not marked as [Localizable(false)] to trigger CA1303: "Do not pass literals as localized parameters" if non-localized strings are passed.
// The message parameter of all other Write* methods is named as such so that they do not trigger CA1303 when literal strings are passed.
// </summary>
//-----------------------------------------------------------------------------------------------------------------------

namespace MicrosoftServices.IdentityManagement.WorkflowActivityLibrary.Common
{
    #region Namespaces Declarations

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Linq;
    using System.Security;
    using System.Text;
    using System.Workflow.ComponentModel;
    using Microsoft.ResourceManagement.Workflow.Activities;

    #endregion

    /// <summary>
    /// Logging Helper
    /// </summary>
    public class Logger
    {
        /// <summary>
        /// The prefix to be used in all log messages. Since the logger also listens on the FIM trace sources, we'll append this prefix to avoid any confusion.
        /// </summary>
        private const string LogPrefix = "WAL";

        /// <summary>
        /// The connectors log source name
        /// </summary>
        private const string WorkflowActivityLibraryLogSourceName = "MicrosoftServices.IdentityManagement.WorkflowActivityLibrary";

        /// <summary>
        /// The resource management log source name
        /// </summary>
        private const string ResourceManagementLogSourceName = "Microsoft.ResourceManagement";

        /// <summary>
        /// The event tracing for windows resource management log source name
        /// </summary>
        private const string EventTracingForWindowsResourceManagementLogSourceName = "Microsoft.ResourceManagement.EventTracingForWindowsTraceSource";

        /// <summary>
        /// The locker
        /// </summary>
        private static readonly object SyncLock = new object();

        /// <summary>
        /// The singleton log instance
        /// </summary>
        private static volatile Logger logInstance;

        /// <summary>
        /// The current source level
        /// </summary>
        private readonly SourceLevels currentSourceLevel;

        /// <summary>
        /// The trace sources
        /// </summary>
        private readonly List<TraceSource> traceSources = new List<TraceSource>();

        /// <summary>
        /// Prevents a default instance of the <see cref="Logger"/> class from being created.
        /// </summary>
        private Logger()
        {
            this.traceSources.Add(new TraceSource(WorkflowActivityLibraryLogSourceName));
            this.traceSources.Add(new TraceSource(ResourceManagementLogSourceName));
            this.traceSources.Add(new TraceSource(EventTracingForWindowsResourceManagementLogSourceName));

            this.currentSourceLevel = SourceLevels.Off;
            foreach (TraceSource ts in this.traceSources.Where(ts => this.currentSourceLevel < ts.Switch.Level))
            {
                this.currentSourceLevel = ts.Switch.Level;
            }
        }

        /// <summary>
        /// Gets the singleton instance of Logger class.
        /// </summary>
        public static Logger Instance
        {
            get
            {
                if (logInstance != null)
                {
                    return logInstance;
                }

                lock (SyncLock)
                {
                    if (logInstance == null)
                    {
                        logInstance = new Logger();
                    }
                }

                return logInstance;
            }
        }

        /// <summary>
        /// Gets the context item value.
        /// </summary>
        /// <param name="key">The context item key.</param>
        /// <returns>The context item value.</returns>
        [SecurityCritical]
        public static string GetContextItem(object key)
        {
            if (key == null)
            {
                throw new ArgumentNullException("key");
            }

            Hashtable contextItems = ContextItems.GetContextItems();
            if (contextItems == null || contextItems.Count == 0 || !contextItems.ContainsKey(key))
            {
                return null;
            }

            return ContextItems.GetContextItemValue(contextItems[key]);
        }

        /// <summary>
        /// Add important workflow attribute key/value pairs to the <see cref="System.Runtime.Remoting.Messaging.CallContext"/> dictionary.  
        /// Context items will be recorded with every log entry.
        /// </summary>
        /// <param name="activity">The activity.</param>
        /// <param name="workflowInstanceId">The workflow instance Id.</param>
        [SecurityCritical]
        public static void SetContextItem(Activity activity, Guid workflowInstanceId)
        {
            Logger.SetContextItem("WorkflowInstanceId", workflowInstanceId);

            SequentialWorkflow parentWorkflow;
            if (!SequentialWorkflow.TryGetContainingWorkflow(activity, out parentWorkflow))
            {
                return;
            }

            Logger.SetContextItem("RequestId", parentWorkflow.RequestId);
            Logger.SetContextItem("ActorId", parentWorkflow.ActorId);
            Logger.SetContextItem("TargetId", parentWorkflow.TargetId);
            Logger.SetContextItem("WorkflowDefinitionId", parentWorkflow.WorkflowDefinitionId);
        }

        /// <summary>
        /// Add a key/value pair to the <see cref="System.Runtime.Remoting.Messaging.CallContext"/> dictionary.  
        /// Context items will be recorded with every log entry.
        /// </summary>
        /// <param name="key">Hashtable key</param>
        /// <param name="value">Value.  Objects will be serialized.</param>
        /// <example>The following example demonstrates use of the AddContextItem method.
        /// <code>Logger.SetContextItem("SessionID", myComponent.SessionId);</code></example>
        [SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily", Justification = "Reviewed. Required.")]
        [SecurityCritical]
        public static void SetContextItem(object key, object value)
        {
            if (key == null)
            {
                throw new ArgumentNullException("key");
            }

            if (value is string && !string.IsNullOrEmpty(value as string))
            {
                // remove any curly braces as they cause FormatException in WriteEvent method when args is passed to it.
                var stringValue = ((string)value).Replace("{", string.Empty).Replace("}", string.Empty);
                ContextItems.SetContextItem(key, stringValue);
            }
            else
            {
                ContextItems.SetContextItem(key, value);
            }
        }

        /// <summary>
        /// Empty the context items dictionary.
        /// </summary>
        [SecurityCritical]
        public static void FlushContextItems()
        {
            ContextItems.FlushContextItems();
        }

        /// <summary>
        /// Determines if the event should be traced.
        /// </summary>
        /// <param name="traceEventType">Type of the trace event.</param>
        /// <returns>true if the event should be traced. Otherwise false.</returns>
        public bool ShouldTrace(TraceEventType traceEventType)
        {
            if (this.currentSourceLevel == SourceLevels.Off)
            {
                return false;
            }

            bool activityTracing = (this.currentSourceLevel & SourceLevels.ActivityTracing) == SourceLevels.ActivityTracing;
            bool critical = (this.currentSourceLevel & SourceLevels.Critical) == SourceLevels.Critical;
            bool error = critical || (this.currentSourceLevel & SourceLevels.Error) == SourceLevels.Error;
            bool warning = error || (this.currentSourceLevel & SourceLevels.Warning) == SourceLevels.Warning;
            bool info = warning || (this.currentSourceLevel & SourceLevels.Information) == SourceLevels.Information;
            bool verbose = info || (this.currentSourceLevel & SourceLevels.Verbose) == SourceLevels.Verbose;

            if (traceEventType <= TraceEventType.Start)
            {
                if (traceEventType > TraceEventType.Information)
                {
                    if (traceEventType == TraceEventType.Verbose)
                    {
                        return verbose;
                    }

                    if (traceEventType == TraceEventType.Start)
                    {
                        return activityTracing;
                    }
                }
                else
                {
                    switch (traceEventType)
                    {
                        case TraceEventType.Critical:
                        case TraceEventType.Error:
                            return error;
                        case TraceEventType.Warning:
                            return warning;
                        case TraceEventType.Information:
                            return info;
                    }
                }
            }
            else if (traceEventType <= TraceEventType.Suspend)
            {
                if (traceEventType == TraceEventType.Stop || traceEventType == TraceEventType.Suspend)
                {
                    return activityTracing;
                }
            }
            else if (traceEventType == TraceEventType.Resume || traceEventType == TraceEventType.Transfer)
            {
                return activityTracing;
            }

            return false;
        }

        /// <summary>
        /// Writes the method entry event.
        /// </summary>
        public void WriteMethodEntry()
        {
            this.WriteMethodEntry(EventIdentifier.Verbose);
        }

        /// <summary>
        /// Writes the method entry event.
        /// </summary>
        /// <param name="eventId">The event identifier.</param>
        public void WriteMethodEntry(int eventId)
        {
            this.WriteMethodEntry(eventId, string.Empty);
        }

        /// <summary>
        /// Writes the method entry.
        /// </summary>
        /// <param name="message">The message format string that describes the event.</param>
        /// <param name="args">The object array that contains zero or more objects to format.</param>
        public void WriteMethodEntry([Localizable(false)] string message, params object[] args)
        {
            this.WriteMethodEntry(EventIdentifier.Verbose, message, args);
        }

        /// <summary>
        /// Writes the specified method entry event.
        /// </summary>
        /// <param name="eventId">The event identifier.</param>
        /// <param name="message">The message format string that describes the event.</param>
        /// <param name="args">The object array that contains zero or more objects to format.</param>
        public void WriteMethodEntry(int eventId, [Localizable(false)] string message, params object[] args)
        {
            if (this.currentSourceLevel < SourceLevels.Verbose)
            {
                return;
            }

            this.WriteEvent(TraceEventType.Verbose, eventId, "Enter method. " + message, args);
        }

        /// <summary>
        /// Writes the specified error event.
        /// </summary>
        /// <param name="message">The message format string that describes the event.</param>
        /// <param name="args">The object array that contains zero or more objects to format.</param>
        public void WriteError(string message, params object[] args)
        {
            this.WriteError(EventIdentifier.Error, message, args);
        }

        /// <summary>
        /// Writes the specified error event.
        /// </summary>
        /// <param name="eventId">The event identifier.</param>
        /// <param name="message">The message format string that describes the event.</param>
        /// <param name="args">The object array that contains zero or more objects to format.</param>
        public void WriteError(int eventId, string message, params object[] args)
        {
            this.WriteEvent(TraceEventType.Error, eventId, message, args);
        }

        /// <summary>
        /// Writes the method exit event.
        /// </summary>
        public void WriteMethodExit()
        {
            this.WriteMethodExit(EventIdentifier.Verbose);
        }

        /// <summary>
        /// Writes the method exit event.
        /// </summary>
        /// <param name="eventId">The event identifier.</param>
        public void WriteMethodExit(int eventId)
        {
            this.WriteMethodExit(eventId, string.Empty);
        }

        /// <summary>
        /// Writes the specified method exit event.
        /// </summary>
        /// <param name="message">The message format string that describes the event.</param>
        /// <param name="args">The object array that contains zero or more objects to format.</param>
        public void WriteMethodExit([Localizable(false)] string message, params object[] args)
        {
            this.WriteMethodExit(EventIdentifier.Verbose, message, args);
        }

        /// <summary>
        /// Writes the specified method exit event.
        /// </summary>
        /// <param name="eventId">The event identifier.</param>
        /// <param name="message">The message format string that describes the event.</param>
        /// <param name="args">The object array that contains zero or more objects to format.</param>
        public void WriteMethodExit(int eventId, [Localizable(false)] string message, params object[] args)
        {
            if (this.currentSourceLevel < SourceLevels.Verbose)
            {
                return;
            }

            this.WriteEvent(TraceEventType.Verbose, eventId, "Exit method. " + message, args);
        }

        /// <summary>
        /// Writes the specified information event.
        /// </summary>
        /// <param name="message">The message format string that describes the event.</param>
        /// <param name="args">The object array that contains zero or more objects to format.</param>
        public void WriteInfo([Localizable(false)] string message, params object[] args)
        {
            this.WriteInfo(EventIdentifier.Info, message, args);
        }

        /// <summary>
        /// Writes the specified information event.
        /// </summary>
        /// <param name="eventId">The event identifier.</param>
        /// <param name="message">The message format string that describes the event.</param>
        /// <param name="args">The object array that contains zero or more objects to format.</param>
        public void WriteInfo(int eventId, [Localizable(false)] string message, params object[] args)
        {
            this.WriteEvent(TraceEventType.Information, eventId, message, args);
        }

        /// <summary>
        /// Writes the specified verbose event.
        /// </summary>
        /// <param name="message">The message format string that describes the event.</param>
        /// <param name="args">The object array that contains zero or more objects to format.</param>
        public void WriteVerbose([Localizable(false)] string message, params object[] args)
        {
            this.WriteVerbose(EventIdentifier.Verbose, message, args);
        }

        /// <summary>
        /// Writes the specified verbose event.
        /// </summary>
        /// <param name="eventId">The event identifier.</param>
        /// <param name="message">The message format string that describes the event.</param>
        /// <param name="args">The object array that contains zero or more objects to format.</param>
        public void WriteVerbose(int eventId, [Localizable(false)] string message, params object[] args)
        {
            this.WriteEvent(TraceEventType.Verbose, eventId, message, args);
        }

        /// <summary>
        /// Writes the specified warning event.
        /// </summary>
        /// <param name="message">The message format string that describes the event.</param>
        /// <param name="args">The object array that contains zero or more objects to format.</param>
        public void WriteWarning([Localizable(false)] string message, params object[] args)
        {
            this.WriteWarning(EventIdentifier.Warning, message, args);
        }

        /// <summary>
        /// Writes the specified warning event.
        /// </summary>
        /// <param name="eventId">The event identifier.</param>
        /// <param name="message">The message format string that describes the event.</param>
        /// <param name="args">The object array that contains zero or more objects to format.</param>
        public void WriteWarning(int eventId, [Localizable(false)] string message, params object[] args)
        {
            this.WriteEvent(TraceEventType.Warning, eventId, message, args);
        }

        /// <summary>
        /// Reports the error.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <returns>The input exception</returns>
        public Exception ReportError(Exception exception)
        {
            return this.ReportError(EventIdentifier.Error, exception);
        }

        /// <summary>
        /// Reports the error.
        /// </summary>
        /// <param name="eventId">The event identifier.</param>
        /// <param name="exception">The exception.</param>
        /// <returns>The input exception</returns>
        public Exception ReportError(int eventId, Exception exception)
        {
            if (exception == null)
            {
                exception = new ArgumentNullException("exception");
            }

            string methodName = MethodName();

            this.WriteEvent(TraceEventType.Error, eventId, "Exception in '{0}'. Details: {1}.", new object[] { methodName, exception });

            return exception;
        }

        /// <summary>
        /// Returns the name of the method that invoked this class.
        /// </summary>
        /// <returns>the name of the method that invoked this class</returns>
        private static string MethodName()
        {
            string methodName = "UnknownMethodName";
            try
            {
                StackFrame[] frames = (new StackTrace()).GetFrames();
                string loggerTypeFullName = Convert.ToString(logInstance, CultureInfo.InvariantCulture);
                for (int i = 1; frames != null && i < frames.Length; ++i)
                {
                    Type reflectedType = frames[i].GetMethod().ReflectedType;
                    if (reflectedType != null)
                    {
                        if (reflectedType.FullName.Equals(loggerTypeFullName, StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        string methodType = reflectedType.Name;
                        string name = frames[i].GetMethod().Name;
                        methodName = string.Format(CultureInfo.InvariantCulture, "{0} : {1}", new object[] { methodType, name });
                    }

                    break;
                }
            }
            catch (ArgumentNullException)
            {
            }
            catch (NullReferenceException)
            {
            }

            return methodName;
        }

        /// <summary>
        /// Gets the extended log properties.
        /// </summary>
        /// <returns>The extended log properties string.</returns>
        private static string GetExtendedLogProperties()
        {
            StringBuilder extendedProperties = new StringBuilder();

            try
            {
                Hashtable contextItems = ContextItems.GetContextItems();
                if (contextItems != null && contextItems.Count != 0)
                {
                    extendedProperties.AppendLine();
                    extendedProperties.Append("Extended Properties:");
                    foreach (DictionaryEntry entry in contextItems)
                    {
                        string itemValue = ContextItems.GetContextItemValue(entry.Value);
                        extendedProperties.AppendLine();
                        extendedProperties.AppendFormat(CultureInfo.CurrentUICulture, "{0}: {1}", entry.Key, itemValue);
                    }
                }
            }
            catch (SecurityException)
            {
                // ignore the security exception - no item could have been set if we get the exception here.
            }
            catch (MethodAccessException)
            {
                // ignore the security exception - no item could have been set if we get the exception here.
            }

            return extendedProperties.ToString();
        }

        /// <summary>
        /// Writes the event.
        /// </summary>
        /// <param name="eventType">Type of the event.</param>
        /// <param name="eventId">The event identifier.</param>
        /// <param name="message">The message format string that describes the event.</param>
        /// <param name="args">The object array that contains zero or more objects to format.</param>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Do not want to throw exception while logging.")]
        private void WriteEvent(TraceEventType eventType, int eventId, [Localizable(false)] string message, params object[] args)
        {
            string logMessagePrefix = string.Format(CultureInfo.InvariantCulture, "{0} ({1}): {2:d} {2:HH:mm:ss.ffff}: ", LogPrefix, VersionInfo.Version, DateTime.Now);
            const int LogMessageLength = 32766;

            if (message == null || string.IsNullOrEmpty(message))
            {
                return;
            }

            if (this.currentSourceLevel == SourceLevels.Verbose)
            {
                message = MethodName() + ": " + message;
            }

            message = logMessagePrefix + message + GetExtendedLogProperties();

            if (message.Length > LogMessageLength)
            {
                message = message.Substring(0, LogMessageLength);
            }

            foreach (TraceSource traceSource in this.traceSources)
            {
                try
                {
                    if (args != null && args.Length > 0)
                    {
                        traceSource.TraceEvent(eventType, eventId, message, args);
                    }
                    else
                    {
                        traceSource.TraceEvent(eventType, eventId, message);
                    }
                }
                catch (FormatException e)
                {
                    Debug.WriteLine(e);
                    traceSource.TraceEvent(eventType, eventId, message);
                }
                catch (Win32Exception e)
                {
                    // FIM Portal run in WSS_Minimal trust level by default
                    // the quickest way to fix this is to make FIM Portal Web Apppool Identity a local admin
                    Debug.WriteLine(e);
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                }
            }
        }
    }
}
