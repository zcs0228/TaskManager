using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            int id1 = TaskManager.TaskManager.CreateTask(() => {
                Console.WriteLine("任务1开始");
                Thread.Sleep(10000);
                if (TaskManager.TaskManager.WaitOneOrAbort())
                {
                    Console.WriteLine("任务1终止");
                    return;
                }
                Console.WriteLine("任务1结束");
            });
            Thread.Sleep(10000);
            TaskManager.TaskManager.Abort(id1);
            Console.WriteLine("任务1ID:" + id1.ToString());

            int id2 = TaskManager.TaskManager.CreateTask(() => {
                Console.WriteLine("任务2开始");
                Thread.Sleep(20000);
                //throw new Exception("test");
                int key = (int)Task.CurrentId;
                if (TaskManager.TaskManager.WaitOneOrAbort()) return;
                Console.WriteLine("任务2结束");
            });
            TaskManager.TaskManager.Suspend(id2);
            Console.WriteLine("任务2ID:" + id2.ToString());

            TaskManager.TaskManager.CreateTask(() => {
                Console.WriteLine("任务3开始");
                Thread.Sleep(10000);
                Console.WriteLine("任务3结束");
            });

            Console.ReadKey();
        }
    }
}