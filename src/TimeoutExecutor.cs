using System.Threading;

namespace ProcBridge_CSharp
{
    public class TimeoutExecutor
    {
        private readonly long _timeout;

        public TimeoutExecutor(long timeout)
        {
            _timeout = timeout;
        }

        public long GetTimeout()
        {
            return _timeout;
        }

        public void Execute(ThreadStart task)
        {
            Semaphore semaphore = new Semaphore(0, 0);
            bool[] isTimeout = {false};

            void TimerCallBack(object stateInfo)
            {
                isTimeout[0] = true;
                semaphore.Release();
            }

            Timer timer = new Timer(TimerCallBack, null, _timeout, Timeout.Infinite);

            void RunTask()
            {
                try {
                    task.Invoke();
                }
                finally {
                    semaphore.Release();
                }
            }

            Thread thr = new Thread(RunTask);
            thr.Start();

            try {
                semaphore.WaitOne();
                if (isTimeout[0]) {
                    throw new TimeoutException();
                }
            }
            catch (ThreadInterruptedException) {
            }
            finally {
                timer.Dispose();
            }
        }
    }
}