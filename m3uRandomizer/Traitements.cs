using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace m3uRandomizer
{
    internal static class Traitements
    {
        #region constantes
        private const int nbMinutesMax = 8;
        #endregion

        /// <summary>
        /// méthode principale d'execution
        /// </summary>
        /// <param name="args">arguments passés en paramètre de l'exe</param>
        internal static void Executer(string[] args)
        {
            Console.WriteLine("*---------------------------------------------------------*");
            Console.WriteLine("*                                                         *");
            Console.WriteLine("*                     .M3U RANDOMIZER                     *");
            Console.WriteLine("*                                                         *");
            Console.WriteLine("*---------------------------------------------------------*");
            Console.WriteLine("");

            string pathOrigine = "";
            string pathFinal = "";
            if (args != null && args.Count() > 0 && File.Exists(args[0]) && Path.GetExtension(args[0]) == ".m3u")
            {
                pathOrigine = args[0];
                try
                {
                    pathFinal = RecreerM3u(pathOrigine);
                }
                catch (Exception e)
                {

                    Console.WriteLine("Erreur lors du traitement : " + e.Message);
                }
                
                Console.WriteLine("Randomize effectué :");
                Console.WriteLine("origine : " + pathOrigine);
                Console.WriteLine("nouveau : " + pathFinal);
            }
            else
            {
                Console.WriteLine("Aucun fichier .m3u existant fourni en paramètre.");
            }
            Console.WriteLine("Appuyez sur une touche pour fermer.");
            Console.ReadKey();
        }

        #region méthodes utilitaires

        /// <summary>
        /// recréer un fichier .m3u
        /// </summary>
        /// <param name="pathOrigine">chemin du fichier de base</param>
        /// <returns>chemin du nouveau fichier</returns>
        private static string RecreerM3u(string pathOrigine)
        {
            List<string> contenu = new List<string>();
            string cheminFinal = PreparerCheminFinal(pathOrigine);

            // préparation de la liste aléatoire : 
            //  * on lit le contenu du fichier
            //  * on le transforme en liste de morceaux pour associer les #EXTINF au chemin de fichier associé
            //  * on randomize
            List<Morceau> listeRandom = Randomizer(TransformerLignes(pathOrigine));


            // on commence par mettre l'entête obligatoire d'un fichier .m3u
            contenu.Add("#EXTM3U");

            // ajout des différentes lignes
            foreach (Morceau item in listeRandom)
            {
                contenu.Add(item.Extinf);
                contenu.Add(item.Path);
            }
            File.AppendAllLines(cheminFinal, contenu);
            return cheminFinal;
        }

        /// <summary>
        /// transformer la liste de lignes récupérées du fichier en liste de morceaux
        /// </summary>
        /// <param name="pathOrigine">chemin du fichier d'origine</param>
        /// <returns>liste de morceaux</returns>
        private static List<Morceau> TransformerLignes(string pathOrigine)
        {
            string[] lignes = File.ReadAllLines(pathOrigine);
            List<Morceau> retour = new List<Morceau>();
            List<string> listeChemins = new List<string>();
            Morceau morceauTemp = new Morceau();
            bool estMorceauAOublier = false;
            foreach (string ligne in lignes)
            {
                // #EXTM3U correspond à la première ligne d'une liste m3u étendue. On exclut.
                // #EXTREM correspond à une ligne de commentaire, on exclut.
                if (!ligne.Equals("#EXTM3U") && !ligne.Equals("#EXTREM"))
                {
                    // #EXTINF correspond à une ligne d'infos de morceau
                    if (ligne.Contains("#EXTINF"))
                    {
                        // récupération de la durée du morceau entre ":" et le premier ","
                        int nbSecondes = int.Parse(ligne.Split(':')[1].Split(',')[0]);
                        // si le morceau fait moins que le nombre de minutes max, on le garde
                        if (nbSecondes < (nbMinutesMax * 60))
                        {
                            morceauTemp.Extinf = ligne;
                        }
                        else
                        {
                            estMorceauAOublier = true;
                        }
                    }
                    else
                    {
                        // si on arrive ici, c'est qu'on est sur une ligne de chemin. 
                        // On ajoute le morceauTemp à la liste et on réinitialise
                        if (!estMorceauAOublier)
                        {
                            morceauTemp.Path = ligne;
                            retour.Add(morceauTemp);
                            morceauTemp = new Morceau();

                            if (listeChemins == null || listeChemins.Count == 0 || 
                                listeChemins.All(path => !path.Equals(Path.GetDirectoryName(ligne))))
                            {
                                listeChemins.Add(Path.GetDirectoryName(ligne));
                            }
                        }
                        else
                        {
                            estMorceauAOublier = false;
                        }
                    }
                }
            }
            File.AppendAllLines(Path.GetDirectoryName(pathOrigine) + "\\" + "_liste_chemins_" + DateTime.Now.ToShortDateString().Replace("/", "") + "_" + DateTime.Now.ToLongTimeString().Replace(":", "") + ".txt",listeChemins);
            return retour;
        }

        /// <summary>
        /// création d'une liste aléatoire en fonction d'une autre liste.
        /// </summary>
        /// <param name="listeMorceaux">liste des morceaux à utiliser et à mettre en désordre</param>
        /// <returns>liste aléatoire</returns>
        private static List<Morceau> Randomizer(List<Morceau> listeMorceaux)
        {
            List<Morceau> retour = new List<Morceau>();
            Random rnd = new Random();
            // en fonction du nombre de morceaux restants (non traités), on en prend un au hasard, 
            // on l'ajoute dans la liste finale et on le retire de la liste de base.
            while (listeMorceaux.Count > 0)
            {
                int index = rnd.Next(0, listeMorceaux.Count - 1);
                Morceau mrc = listeMorceaux[index];
                retour.Add(mrc);
                listeMorceaux.Remove(mrc);

            }
            return retour;
        }

        /// <summary>
        /// méthode de préparation du chemin vers le fichier final
        /// </summary>
        /// <param name="pathOrigine">chemin d'origine</param>
        /// <returns>chemin final</returns>
        private static string PreparerCheminFinal(string pathOrigine)
        {
            string cheminFinal = "";
            string fichierSansExtansion = Path.GetFileNameWithoutExtension(pathOrigine);
            string chemin = Path.GetDirectoryName(pathOrigine);
            cheminFinal = chemin + "\\" + fichierSansExtansion + "_random_" + DateTime.Now.ToShortDateString().Replace("/", "") + "_" + DateTime.Now.ToLongTimeString().Replace(":", "") + ".m3u";
            return cheminFinal;
        }
        #endregion
    }
}
