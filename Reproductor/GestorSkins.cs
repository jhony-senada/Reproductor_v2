using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace Reproductor
{
    public class GestorSkins
    {
        private string rutaSkins;

        public GestorSkins()
        {
            rutaSkins = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Skins");
            InicializarSkins();
        }

        private void InicializarSkins()
        {
            if (!Directory.Exists(rutaSkins))
            {
                Directory.CreateDirectory(rutaSkins);
                CrearSkinsPorDefecto();
            }
        }

        public List<string> ObtenerListaSkins()
        {
            List<string> nombresSkins = new List<string>();
            string[] archivosSkin = Directory.GetFiles(rutaSkins, "*.skin");

            foreach (string archivo in archivosSkin)
            {
                nombresSkins.Add(Path.GetFileNameWithoutExtension(archivo));
            }
            return nombresSkins;
        }

        public void AplicarSkin(string nombreSkin)
        {
            string rutaSkin = Path.Combine(rutaSkins, nombreSkin + ".skin");

            if (File.Exists(rutaSkin))
            {
                string[] lineas = File.ReadAllLines(rutaSkin);
                var conversorColor = new BrushConverter();

                foreach (string linea in lineas)
                {
                    if (linea.Contains("="))
                    {
                        string[] partes = linea.Split('=');
                        string nombreVariable = partes[0].Trim();
                        string valor = partes[1].Trim();

                        try
                        {
                            if (nombreVariable.StartsWith("Fuente"))
                            {
                                Application.Current.Resources[nombreVariable] = new FontFamily(valor);
                            }
                            else
                            {
                                SolidColorBrush nuevoColor = (SolidColorBrush)conversorColor.ConvertFromString(valor);
                                Application.Current.Resources[nombreVariable] = nuevoColor;
                            }
                        }
                        catch { /* Ignorar errores de sintaxis en el archivo skin */ }
                    }
                }
            }
        }

        private void CrearSkinsPorDefecto()
        {
            string rutaYuki = Path.Combine(rutaSkins, "YukiGloom.skin");
            string contenidoYuki = @"ColorBarraSuperior=#2b2b2b
ColorFondoPrincipal=Black
ColorFondoSecundario=#111111
ColorControles=#660000
ColorAcentoTexto=Red
ColorBtnCerrar=DarkRed
ColorBtnMinimizar=#444444
ColorLetras=#FFFFFF
FuentePrincipal=Segoe UI
FuenteLetra=Chiller
Barras=#E65E39
ColorOscilo=Red";
            File.WriteAllText(rutaYuki, contenidoYuki);

            string rutaHacker = Path.Combine(rutaSkins, "DarkHacker.skin");
            string contenidoHacker = @"ColorBarraSuperior=#0f0f0f
ColorFondoPrincipal=#000000
ColorFondoSecundario=#050505
ColorControles=#002200
ColorAcentoTexto=#00ff00
ColorBtnCerrar=#005500
ColorBtnMinimizar=#113311
ColorLetras=#20C20E
FuentePrincipal=Lucida Console
FuenteLetra=Courier New
Barras=#A2E639
ColorOscilo=#39FF14";
            File.WriteAllText(rutaHacker, contenidoHacker);
        }
    }
}