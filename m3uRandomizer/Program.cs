using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace m3uRandomizer
{
    /// <summary>
    /// classe principale
    /// </summary>
    class Program
    {
        /// <summary>
        /// méthode principale.
        /// On précise STAThread pour pouvoir lancer un composant issu de System.Windows.Form
        /// </summary>
        /// <param name="args"></param>
        [STAThread]
        static void Main(string[] args)
        {
            Traitements.Executer(args);
        }
    }
}
