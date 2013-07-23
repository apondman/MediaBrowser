using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace MediaPortal.GUI.Library
{
    public sealed class GUITask
    {
        public static readonly GUITask None = new GUITask();
        
        GUITask()
        {

        }

        bool _cancelRequested = false;

        /// <summary>
        /// Cancels this task.
        /// </summary>
        public void Cancel()
        {
            _cancelRequested = true;
        }

        /// <summary>
        /// Gets the IAsyncResult.
        /// </summary>
        /// <value>
        /// The result.
        /// </value>
        public IAsyncResult Result { get; internal set; }

        /// <summary>
        /// Gets a value indicating whether this task is cancelled.
        /// </summary>
        /// <value>
        /// <c>true</c> if this task is cancelled; otherwise, <c>false</c>.
        /// </value>
        public bool IsCancelled
        {
            get
            {
                return _cancelRequested;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this task has completed.
        /// </summary>
        /// <value>
        /// <c>true</c> if this task has completed; otherwise, <c>false</c>.
        /// </value>
        public bool IsCompleted { get; internal set; }

        public static GUITask Run<TResult>(Func<GUITask, TResult> process, Action<Exception> onError, bool mainThread = false)
        {
            return Run(process, (a) => { }, onError, mainThread);
        }

        public static GUITask Run<TResult>(Func<GUITask, TResult> process, Action<TResult> onSuccess, Action<Exception> onError, bool mainThread = false)
        {
            Guard.NotNull(() => process, process);
            Guard.NotNull(() => onSuccess, onSuccess);
            Guard.NotNull(() => onError, onError);

            // init and show the wait cursor in MediaPortal
            GUIWaitCursor.Init();
            GUIWaitCursor.Show();

            GUITask task = new GUITask();
            task.Result = process.BeginInvoke(task,
                iar =>
                {
                    try
                    {
                        TResult result = process.EndInvoke(iar);
                        if (!task.IsCancelled)
                        {
                            if (!mainThread)
                            {
                                onSuccess(result);
                            }
                            else
                            {
                                MainThreadCallback(() => onSuccess(result));
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        MainThreadCallback(() => onError(e));
                    }
                    finally
                    {
                        // hide the wait cursor
                        GUIWaitCursor.Hide();
                        task.IsCompleted = true;
                    }
                }
            , task);

            return task;
        }

        /// <summary>
        /// Simple wrapper for GUIWindowManager.SendThreadCallback
        /// </summary>
        /// <param name="callback">The callback.</param>
        public static void MainThreadCallback(System.Action callback) 
        {
            // execute the ResultHandler on the Main Thread
            GUIWindowManager.SendThreadCallback((p1, p2, o) =>
            {
                callback();
                return 0;
            }, 0, 0, null);
        }

        /// <summary>
        /// Simple wrapper for GUIWindowManager.SendThreadCallback
        /// </summary>
        /// <param name="callback">The callback.</param>
        public static void MainThreadCallbackAndWait(System.Action callback)
        {
            // execute the ResultHandler on the Main Thread
            GUIWindowManager.SendThreadCallbackAndWait((p1, p2, o) =>
            {
                callback();
                return 0;
            }, 0, 0, null);
        }

    }
}
