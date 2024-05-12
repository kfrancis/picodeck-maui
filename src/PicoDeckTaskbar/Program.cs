namespace PicoDeckTaskbar
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Application.Run(new Form1());
        }
    }

    public static class UsefulExtensions
    {
        public static void SafeInvoke(this Control uiElement, Action updater, bool forceSynchronous)
        {
            if (uiElement == null)
            {
                throw new ArgumentNullException(nameof(uiElement));
            }

            // Action to perform the update
            void performUpdate()
            {
                if (uiElement.IsDisposed)
                    throw new ObjectDisposedException(uiElement.Name, "Control is already disposed.");

                updater();
            }

            if (uiElement.InvokeRequired)
            {
                // Choose between synchronous and asynchronous calls
                if (forceSynchronous)
                {
                    uiElement.Invoke(performUpdate);
                }
                else
                {
                    uiElement.BeginInvoke(performUpdate);
                }
            }
            else
            {
                performUpdate();
            }
        }
    }
}
