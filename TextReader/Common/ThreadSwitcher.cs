using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using Windows.System.Threading;
using Windows.UI.Core;

namespace TextReader.Common
{
    /// <summary>
    /// A helper type for switch thread by <see cref="CoreDispatcher"/>. This type is not intended to be used directly from your code.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public readonly struct DispatcherThreadSwitcher : INotifyCompletion
    {
        private readonly CoreDispatcher dispatcher;
        private readonly CoreDispatcherPriority priority;

        /// <summary>
        /// Initializes a new instance of the <see cref="DispatcherThreadSwitcher"/> struct.
        /// </summary>
        /// <param name="dispatcher">A <see cref="CoreDispatcher"/> whose foreground thread to switch execution to.</param>
        /// <param name="priority">Specifies the priority for event dispatch.</param>
        public DispatcherThreadSwitcher(CoreDispatcher dispatcher, CoreDispatcherPriority priority = CoreDispatcherPriority.Normal)
        {
            this.dispatcher = dispatcher;
            this.priority = priority;
        }

        /// <summary>
        /// Gets a value that indicates whether the asynchronous operation has completed.
        /// </summary>
        public bool IsCompleted => dispatcher?.HasThreadAccess != false;

        /// <summary>
        /// Ends the await on the completed task.
        /// </summary>
        public void GetResult() { }

        /// <summary>
        /// Gets an awaiter used to await this <see cref="DispatcherThreadSwitcher"/>.
        /// </summary>
        /// <returns>An awaiter instance.</returns>
        public DispatcherThreadSwitcher GetAwaiter() => this;

        /// <inheritdoc/>
        public void OnCompleted(Action continuation) => _ = dispatcher.RunAsync(priority, () => continuation());
    }

    /// <summary>
    /// A helper type for switch thread by <see cref="ThreadPool"/>. This type is not intended to be used directly from your code.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public readonly struct ThreadPoolThreadSwitcher : INotifyCompletion
    {
        private readonly WorkItemPriority priority;

        /// <summary>
        /// Initializes a new instance of the <see cref="ThreadPoolThreadSwitcher"/> struct.
        /// </summary>
        /// <param name="priority">Specifies the priority for event dispatch.</param>
        public ThreadPoolThreadSwitcher(WorkItemPriority priority = WorkItemPriority.Normal) => this.priority = priority;

        /// <summary>
        /// Gets a value that indicates whether the asynchronous operation has completed.
        /// </summary>
        public bool IsCompleted => SynchronizationContext.Current == null;

        /// <summary>
        /// Ends the await on the completed task.
        /// </summary>
        public void GetResult() { }

        /// <summary>
        /// Gets an awaiter used to await this <see cref="ThreadPoolThreadSwitcher"/>.
        /// </summary>
        /// <returns>An awaiter instance.</returns>
        public ThreadPoolThreadSwitcher GetAwaiter() => this;

        /// <inheritdoc/>
        public void OnCompleted(Action continuation) => _ = ThreadPool.RunAsync(_ => continuation(), priority);
    }

    /// <summary>
    /// The extensions for switching threads.
    /// </summary>
    public static class ThreadSwitcher
    {
        /// <summary>
        /// A helper function—for use within a coroutine—that you can <see langword="await"/> to switch execution to a specific foreground thread. 
        /// </summary>
        /// <param name="dispatcher">A <see cref="CoreDispatcher"/> whose foreground thread to switch execution to.</param>
        /// <param name="priority">Specifies the priority for event dispatch.</param>
        /// <returns>An object that you can <see langword="await"/>.</returns>
        public static DispatcherThreadSwitcher ResumeForegroundAsync(this CoreDispatcher dispatcher, CoreDispatcherPriority priority = CoreDispatcherPriority.Normal) => new DispatcherThreadSwitcher(dispatcher, priority);

        /// <summary>
        /// A helper function—for use within a coroutine—that returns control to the caller, and then immediately resumes execution on a thread pool thread.
        /// </summary>
        /// <param name="priority">Specifies the priority for event dispatch.</param>
        /// <returns>An object that you can <see langword="await"/>.</returns>
        public static ThreadPoolThreadSwitcher ResumeBackgroundAsync(WorkItemPriority priority = WorkItemPriority.Normal) => new ThreadPoolThreadSwitcher(priority);
    }
}