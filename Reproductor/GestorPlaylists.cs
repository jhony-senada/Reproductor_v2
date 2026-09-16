using System;
using System.Collections.Generic;
using System.IO;

namespace Reproductor
{
    public class GestorPlaylists
    {
        public List<string> RutasInternas { get; private set; }
        public int ModoLoop { get; set; } = 0; // 0=Off, 1=All, 2=One
        public bool ModoShuffle { get; set; } = false;

        private string rutaCarpetaPlaylists;
        private Random rndRandomizer;

        public GestorPlaylists()
        {
            RutasInternas = new List<string>();
            rndRandomizer = new Random();
            rutaCarpetaPlaylists = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Playlists");

            if (!Directory.Exists(rutaCarpetaPlaylists))
            {
                Directory.CreateDirectory(rutaCarpetaPlaylists);
                File.WriteAllText(Path.Combine(rutaCarpetaPlaylists, "Default.txt"), "");
            }
        }

        public List<string> ObtenerNombresPlaylists()
        {
            List<string> nombres = new List<string>();
            string[] archivos = Directory.GetFiles(rutaCarpetaPlaylists, "*.txt");
            foreach (string archivo in archivos)
            {
                nombres.Add(Path.GetFileNameWithoutExtension(archivo));
            }
            return nombres;
        }

        public void CargarPlaylist(string nombre)
        {
            RutasInternas.Clear();
            string rutaArchivo = Path.Combine(rutaCarpetaPlaylists, nombre + ".txt");

            if (File.Exists(rutaArchivo))
            {
                string[] lineas = File.ReadAllLines(rutaArchivo);
                foreach (string ruta in lineas)
                {
                    if (File.Exists(ruta)) RutasInternas.Add(ruta);
                }
            }
        }

        public void GuardarPlaylist(string nombre)
        {
            string rutaArchivo = Path.Combine(rutaCarpetaPlaylists, nombre + ".txt");
            File.WriteAllLines(rutaArchivo, RutasInternas);
        }

        public int CalcularSiguienteIndice(int indiceActual, bool automatico)
        {
            if (RutasInternas.Count == 0) return -1;
            if (automatico && ModoLoop == 2) return indiceActual;

            int nuevoIndice = indiceActual;

            if (ModoShuffle)
            {
                if (RutasInternas.Count > 1)
                {
                    while (nuevoIndice == indiceActual)
                        nuevoIndice = rndRandomizer.Next(RutasInternas.Count);
                }
            }
            else
            {
                nuevoIndice++;
                if (nuevoIndice >= RutasInternas.Count)
                {
                    if (ModoLoop == 1 || !automatico) nuevoIndice = 0;
                    else return -1;
                }
            }
            return nuevoIndice;
        }

        public int CalcularIndiceAnterior(int indiceActual)
        {
            if (RutasInternas.Count == 0) return -1;

            int nuevoIndice = indiceActual;

            if (ModoShuffle)
            {
                if (RutasInternas.Count > 1)
                {
                    while (nuevoIndice == indiceActual)
                        nuevoIndice = rndRandomizer.Next(RutasInternas.Count);
                }
            }
            else
            {
                nuevoIndice--;
                if (nuevoIndice < 0) nuevoIndice = RutasInternas.Count - 1;
            }
            return nuevoIndice;
        }
    }
}