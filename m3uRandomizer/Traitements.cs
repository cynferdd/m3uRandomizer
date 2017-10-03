using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace m3uRandomizer
{
    internal static class Traitements
    {
        #region constantes
        private const int nbMinutesMax = 8;
        #endregion

        private static List<string> ListeChemins = new List<string>();

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

            string pathFinal = "";
            string pathOrigine = ObtenirCheminFichier(args);

            if (!pathOrigine.Equals("") && File.Exists(pathOrigine))
            {
                if (Path.GetExtension(pathOrigine) == ".m3u")
                {

                    try
                    {
                        // le coeur du traitement
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
                    Console.WriteLine("mauvaise extension : " + Path.GetExtension(pathOrigine) + " - attendu : .m3u");
                }
            }
            else
            {
                Console.WriteLine("chemin de fichier incorrect : " + pathOrigine);
            }
            Console.WriteLine("Appuyez sur une touche pour fermer.");
            Console.ReadKey();
        }


        #region méthodes utilitaires

        /// <summary>
        /// méthode d'obtention du chemin du fichier
        /// </summary>
        /// <param name="args">arguments passés en paramètres de l'application</param>
        /// <returns>chemin du fichier à traiter</returns>
        private static string ObtenirCheminFichier(string[] args)
        {
            string pathOrigine = "";
            if (args != null && args.Count() > 0)
            {
                if (File.Exists(args[0]))
                {
                    pathOrigine = args[0];
                }
                else
                {
                    // si on ne trouve pas le fichier fourni en paramètre, on tente de se baser sur le répertoire en cours
                    if (File.Exists(".//" + args[0]))
                    {
                        pathOrigine = ".//" + args[0];
                    }
                }
            }
            if (pathOrigine.Equals(""))
            {
                // si on a un chemin vide, on lance un prompt
                OpenFileDialog fdlg = new OpenFileDialog();
                if (fdlg.ShowDialog() == DialogResult.OK)
                {
                    // récupération du fichier
                    pathOrigine = fdlg.FileName;
                }
            }
            return pathOrigine;
        }

        /// <summary>
        /// recréer un fichier .m3u
        /// </summary>
        /// <param name="pathOrigine">chemin du fichier de base</param>
        /// <returns>chemin du nouveau fichier</returns>
        private static string RecreerM3u(string pathOrigine)
        {
            string cheminFinal = PreparerCheminFinal(pathOrigine);

            // préparation de la liste aléatoire : 
            //  * on lit le contenu du fichier
            //  * on le transforme en liste de morceaux pour associer les #EXTINF au chemin de fichier associé
            //  * on randomize
            List<string> listeChemins;
            List<Morceau> listeRandom = Randomizer(TransformerLignes(pathOrigine, out listeChemins));

            List<string> contenu = CreationFichierM3u(cheminFinal, listeRandom);
            DeplacerFichiers(listeChemins, contenu);
            return cheminFinal;
        }

        

        /// <summary>
        /// méthode de création en tant que tel du fichier m3u
        /// </summary>
        /// <param name="cheminFinal">chemin du fichier</param>
        /// <param name="listeMorceaux">liste de morceaux à intégrer</param>
        private static List<string> CreationFichierM3u(string cheminFinal, List<Morceau> listeMorceaux)
        {
            List<string> contenu = new List<string>();
            // on commence par mettre l'entête obligatoire d'un fichier .m3u
            contenu.Add("#EXTM3U");

            // ajout des différentes lignes
            foreach (Morceau item in listeMorceaux)
            {
                contenu.Add(item.Extinf);
                contenu.Add(item.Path);
            }
            File.AppendAllLines(cheminFinal, contenu);
            Console.WriteLine("fichier créé : " + cheminFinal);
            return contenu;
        }

        /// <summary>
        /// transformer la liste de lignes récupérées du fichier en liste de morceaux
        /// </summary>
        /// <param name="pathOrigine">chemin du fichier d'origine</param>
        /// <returns>liste de morceaux</returns>
        private static List<Morceau> TransformerLignes(string pathOrigine, out List<string> ListeChemins)
        {
            ListeChemins = new List<string>();
            string[] lignes = File.ReadAllLines(pathOrigine);
            List<Morceau> retour = new List<Morceau>();
            Morceau morceauTemp = new Morceau();
            bool estMorceauAOublier = false;
            foreach (string ligne in lignes)
            {
                // #EXTM3U correspond à la première ligne d'une liste m3u étendue. On exclut.
                // #EXTREM correspond à une ligne de commentaire, on exclut.
                // #EXTVLCOPT correspond à une infos spécifique à vlc
                if (!ligne.Equals("#EXTM3U") && !ligne.Contains("#EXTREM") && !ligne.Contains("#EXTVLCOPT"))
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

                            if (ListeChemins.Count == 0 ||
                                ListeChemins.All(path => !path.Equals(Path.GetDirectoryName(ligne))))
                            {
                                ListeChemins.Add(Path.GetDirectoryName(ligne));
                            }
                        }
                        else
                        {
                            estMorceauAOublier = false;
                        }
                    }
                }
            }
            string cheminListe = Path.GetDirectoryName(pathOrigine) + "\\" + "_liste_chemins_" + DateTime.Now.ToShortDateString().Replace("/", "") + "_" + DateTime.Now.ToLongTimeString().Replace(":", "") + ".txt";
            File.AppendAllLines(cheminListe, ListeChemins);
            Console.WriteLine("fichier créé : " + cheminListe);
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

        /// <summary>
        /// méthode finale de préparation de la liste
        /// </summary>
        /// <param name="listeChemins">liste des chemins rencontrés</param>
        /// <param name="contenu">contenu de la liste m3u</param>
        private static void DeplacerFichiers(List<string> listeChemins, List<string> contenu)
        {
            //Est ce que l'utilisateur veut copier les morceaux quelque part ailleurs ?
            if (MessageBox.Show("Voulez-vous copier les fichiers ailleurs ?", "copie fichiers", MessageBoxButtons.YesNo) == DialogResult.Yes) { 
                // recherche de l'endroit où tout copier
                // si on a un chemin vide, on lance un prompt

                FolderBrowserDialog fdlg = new FolderBrowserDialog();
                if (fdlg.ShowDialog() == DialogResult.OK)
                {
                    // récupération du fichier
                    string cheminCopie = fdlg.SelectedPath;
                    
                    // correspondances de chemins
                    foreach (string chemin in listeChemins)
                    {
                        string nouveauChemin = cheminCopie + "\\" + chemin.Split('\\').LastOrDefault();
                        List<string> nouveauContenu = new List<string>();
                        foreach (string ligne in contenu)
                        {
                            // remplacement en bourrin des lignes par le nouveau chemin
                            nouveauContenu.Add(ligne.Replace(chemin, nouveauChemin));
                        }
                        // écrasement du contenu pour avoir les nouveaux chemins
                        contenu = nouveauContenu;


                        // création des chemins si inexistants
                        if (!Directory.Exists(nouveauChemin))
                        {
                            Directory.CreateDirectory(nouveauChemin);
                        }
                        try
                        {
                            // copie des fichiers
                            foreach (string newPath in Directory.GetFiles(chemin, "*.*",
                                SearchOption.AllDirectories))
                                File.Copy(newPath, newPath.Replace(chemin, nouveauChemin), true);
                        }
                        catch (Exception e)
                        {

                            Console.WriteLine(e.Message);
                        }
                        
                    }

                    // création du m3u
                    string cheminListe = cheminCopie + "\\liste_" + DateTime.Now.ToShortDateString().Replace("/", "") + "_" + DateTime.Now.ToLongTimeString().Replace(":", "") + "_copie.m3u";
                    File.AppendAllLines(cheminListe , contenu);
                    Console.WriteLine("fichier créé : " + cheminListe);
                }
            }
        }
        #endregion
    }
}
