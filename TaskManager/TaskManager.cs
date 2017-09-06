using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TaskManager
{
    public class TaskManager
    {
        //任务容器字典
        private static Dictionary<int, Task> taskDictionary = new Dictionary<int, Task>();
        //线程控制类容器字典
        private static Dictionary<int, ManualResetEvent> _eventWorkDic = new Dictionary<int, ManualResetEvent>();
        //任务取消类容器字典
        private static Dictionary<int, CancellationTokenSource> _tokenSource = new Dictionary<int, CancellationTokenSource>();
        private static System.Threading.Timer timer;

        static TaskManager()
        {
            timer = new System.Threading.Timer(new System.Threading.TimerCallback(TaskClear));
            //timer.Change(300000, 300000);
            timer.Change(5000, 5000);//设置任务清理线程的执行时间
        }

        private static void TaskClear(object state)
        {
            //清理taskDictionary已完成、已终止和已报错的任务
            List<int> checkBody = taskDictionary.Keys.ToList();
            foreach (int item in checkBody)
            {
                if (!taskDictionary.ContainsKey(item)) continue;
                if (taskDictionary[item].Exception != null)
                {
                    string msg = taskDictionary[item].Exception.InnerException.Message;
                    taskDictionary.Remove(item);
                    _eventWorkDic.Remove(item);
                    _tokenSource.Remove(item);
                }
                if(taskDictionary[item].IsCanceled)
                {
                    taskDictionary.Remove(item);
                    _eventWorkDic.Remove(item);
                    _tokenSource.Remove(item);
                }
                if (taskDictionary[item].IsCompleted)
                {
                    taskDictionary.Remove(item);
                    _eventWorkDic.Remove(item);
                    _tokenSource.Remove(item);
                }
            }
        }
        public static int CreateTask(Action action)
        {
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            CancellationToken token = tokenSource.Token;
            Task task = new Task(action, token);
            task.Start();
            taskDictionary.Add(task.Id, task);
            SetTaskState(task.Id);
            SetTaskTokenSource(task.Id, tokenSource);
            return task.Id;
        }
        /// <summary>
        /// 设置线程任务终止控制类
        /// </summary>
        /// <param name="taskId"></param>
        /// <param name="tokenSource"></param>
        public static void SetTaskTokenSource(int taskId, CancellationTokenSource tokenSource)
        {
            string result = String.Empty;
            if (!_tokenSource.ContainsKey(taskId))
            {
                _tokenSource.Add(taskId, tokenSource);
            }
            else
            {
                result = "线程状态类字典中存在值为【" + taskId + "】的键值！";
                throw new Exception(result);
            }
        }
        /// <summary>
        /// 线程是否终止
        /// </summary>
        /// <param name="taskId"></param>
        /// <returns></returns>
        public static bool IsAbort(int taskId)
        {
            if (_tokenSource.ContainsKey(taskId))
            {
                if (_tokenSource[taskId].IsCancellationRequested)//判断是否设置为终止状态
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return true;
            }
        }
        /// <summary>
        /// 线程终止
        /// </summary>
        /// <param name="taskId"></param>
        public static void Abort(int taskId)
        {
            if(_tokenSource.ContainsKey(taskId))
            {
                _tokenSource[taskId].Cancel();
            }
        }

        /// <summary>
        /// 设置与指定线程相关的状态,其中包括ManualResetEvent类和停止标识
        /// </summary>
        /// <param name="taskId"></param>
        public static void SetTaskState(int taskId)
        {
            string result = String.Empty;
            //当初始化为true时，为终止状态；当初始化为false时，为非终止状态
            //终止状态时WaitOne()允许线程访问下边的语句； 非终止状态时WaitOne()阻塞线程，不允许线程访问下边的语句
            ManualResetEvent newEventWork = new ManualResetEvent(true);
            if (!_eventWorkDic.ContainsKey(taskId))
            {
                _eventWorkDic.Add(taskId, newEventWork);
            }
            else
            {
                result = "线程状态类字典中存在值为【" + taskId + "】的键值！";
                throw new Exception(result);
            }
        }
        /// <summary>
        /// 清除指定线程的控制状态
        /// </summary>
        /// <param name="taskId"></param>
        public static void ClearTaskState(int taskId)
        {
            if (_eventWorkDic.ContainsKey(taskId))
            {
                _eventWorkDic.Remove(taskId);
            }
        }
        /// <summary>
        /// 清除全部线程的控制状态
        /// </summary>
        public static void ClearAllTaskState()
        {
            _eventWorkDic.Clear();
        }
        /// <summary>
        /// 获取与指定线程相关的ManualResetEvent类（状态类）
        /// </summary>
        /// <param name="taskId"></param>
        /// <returns></returns>
        public static ManualResetEvent GetManualResetEvent(int taskId)
        {
            return _eventWorkDic.ContainsKey(taskId) ? _eventWorkDic[taskId] : null;
        }

        /// <summary>
        /// 暂停线程
        /// </summary>
        /// <param name="taskId"></param>
        public static void Suspend(int taskId)
        {
            ManualResetEvent eventWork = GetManualResetEvent(taskId);
            if (eventWork != null)
            {
                //把终止状态改为非终止状态用Reset()方法
                eventWork.Reset();
            }
        }
        /// <summary>
        /// 恢复线程
        /// </summary>
        /// <param name="taskId"></param>
        public static void Resume(int taskId)
        {
            ManualResetEvent eventWork = GetManualResetEvent(taskId);
            if (eventWork != null)
            {
                //把非终止状态改为终止状态用Set()方法
                eventWork.Set();
            }
        }
        /// <summary>
        /// 线程检查
        /// </summary>
        public static void WaitOne()
        {
            int taskId = (int)Task.CurrentId;
            ManualResetEvent eventWork = GetManualResetEvent(taskId);
            if (eventWork != null)
            {
                //终止状态时WaitOne()允许线程访问下边的语句；非终止状态时WaitOne()阻塞线程，不允许线程访问下边的语句
                eventWork.WaitOne();
            }
        }
        /// <summary>
        /// 线程检查
        /// </summary>
        public static bool WaitOneOrAbort()
        {
            int taskId = (int)Task.CurrentId;
            if (IsAbort(taskId)) return true;//检查线程任务是否已被取消
            ManualResetEvent eventWork = GetManualResetEvent(taskId);
            if (eventWork != null)
            {
                //终止状态时WaitOne()允许线程访问下边的语句；非终止状态时WaitOne()阻塞线程，不允许线程访问下边的语句
                eventWork.WaitOne();
            }
            return false;
        }
    }
}