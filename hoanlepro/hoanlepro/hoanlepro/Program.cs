using System;
using System.Windows.Forms;

namespace hoanlepro
{
    static class Program
    {
        private static Form2 existingForm2 = null;

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (existingForm2 == null || existingForm2.IsDisposed)
            {
                existingForm2 = new Form2();
                Application.Run(existingForm2);
            }
            else
            {
                // Nếu form đã mở rồi thì chỉ Activate nó
                existingForm2.BringToFront();
                existingForm2.Activate();
            }
        }
    }
}
