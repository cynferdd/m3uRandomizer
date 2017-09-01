using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace m3uRandomizer
{
    /// <summary>
    /// Entité représentant un morceau
    /// </summary>
    public class Morceau
    {
        /// <summary>
        /// informations EXTINF du morceau
        /// </summary>
        public string Extinf { get; set; }

        /// <summary>
        /// chemin du morceau
        /// </summary>
        public string Path { get; set; }
    }
}
