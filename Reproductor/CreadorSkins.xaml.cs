using System;
using System.IO;
using System.Windows;
using Microsoft.Win32;
using System.Windows.Media;

namespace Reproductor
{
    public partial class CreadorSkins : Window
    {
        public CreadorSkins()
        {
            InitializeComponent();
            TxtNombreSkin.Text = "Custom_skin"; // Sugerencia de nombre por defecto
        }

        private void ImportarFuente(System.Windows.Controls.TextBox cajaTextoDestino)
        {
            OpenFileDialog openFile = new OpenFileDialog();
            openFile.Filter = "Archivos de fuente (*.ttf;*.otf)|*.ttf;*.otf";

            if (openFile.ShowDialog() == true)
            {
                try
                {
                    string rutaFuentesExternas = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Skins", "Fuentes");
                    if (!Directory.Exists(rutaFuentesExternas)) Directory.CreateDirectory(rutaFuentesExternas);

                    string nombreArchivo = Path.GetFileName(openFile.FileName);
                    string rutaDestino = Path.Combine(rutaFuentesExternas, nombreArchivo);

                    if (!File.Exists(rutaDestino)) File.Copy(openFile.FileName, rutaDestino);

                    GlyphTypeface glyph = new GlyphTypeface(new Uri(rutaDestino));
                    string nombreInterno = "Arial";
                    foreach (var name in glyph.FamilyNames.Values)
                    {
                        nombreInterno = name;
                        break;
                    }

                    cajaTextoDestino.Text = $"Local:{nombreArchivo}#{nombreInterno}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show("No se pudo importar la fuente: " + ex.Message, "Error");
                }
            }
        }

        private void BtnBuscarFuentePrin_Click(object sender, RoutedEventArgs e)
        {
            ImportarFuente(TxtFuentePrin);
        }

        private void BtnBuscarFuenteLetra_Click(object sender, RoutedEventArgs e)
        {
            ImportarFuente(TxtFuenteLetra);
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtNombreSkin.Text))
            {
                MessageBox.Show("Por favor, ponle un nombre a tu Skin.", "Aviso");
                return;
            }

            try
            {
                // Construimos la estructura exacta que lee tu GestorSkins
                string contenido = $@"ColorBarraSuperior={TxtBarraSup.Text.Trim()}
ColorFondoPrincipal={TxtFondoPrin.Text.Trim()}
ColorFondoSecundario={TxtFondoSec.Text.Trim()}
ColorControles={TxtControles.Text.Trim()}
ColorAcentoTexto={TxtAcento.Text.Trim()}
ColorBtnCerrar=DarkRed
ColorBtnMinimizar=#444444
ColorLetras={TxtLetras.Text.Trim()}
ColorKaraoke={TxtKaraoke.Text.Trim()}
FuentePrincipal={TxtFuentePrin.Text.Trim()}
FuenteLetra={TxtFuenteLetra.Text.Trim()}
Barras={TxtBarras.Text.Trim()}
ColorOscilo={TxtOscilo.Text.Trim()}";

                string rutaCarpeta = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Skins");
                string rutaArchivo = Path.Combine(rutaCarpeta, TxtNombreSkin.Text.Trim() + ".skin");

                File.WriteAllText(rutaArchivo, contenido);

                MessageBox.Show("¡Skin creada y guardada con éxito!", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close(); // Cerramos la ventana al terminar
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al guardar la skin: " + ex.Message, "Error");
            }
        }
    }
}