using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Reproductor
{
    public partial class CreadorLRC : Window
    {
        private MediaPlayer mediaPlayerLRC;
        private List<string> rutasCanciones;
        private string rutaActual = "";
        private DispatcherTimer timer;
        private bool isPlaying = false;

        public ObservableCollection<LineaLRC> ColeccionLRC { get; set; }

        public CreadorLRC(List<string> cancionesDisponibles)
        {
            InitializeComponent();

            mediaPlayerLRC = new MediaPlayer();
            mediaPlayerLRC.MediaEnded += (s, e) => { isPlaying = false; BtnPlay.Content = "▶ Play"; };

            ColeccionLRC = new ObservableCollection<LineaLRC>();
            ListaLRC.ItemsSource = ColeccionLRC;

            rutasCanciones = cancionesDisponibles;
            foreach (string ruta in rutasCanciones) ComboCanciones.Items.Add(Path.GetFileNameWithoutExtension(ruta));

            timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
            timer.Tick += (s, e) => { if (isPlaying) TxtTiempo.Text = mediaPlayerLRC.Position.ToString(@"mm\:ss\.ff"); };
            timer.Start();
        }

        private void ComboCanciones_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (ComboCanciones.SelectedIndex != -1)
            {
                rutaActual = rutasCanciones[ComboCanciones.SelectedIndex];
                mediaPlayerLRC.Open(new Uri(rutaActual));
                mediaPlayerLRC.Stop();
                isPlaying = false;
                BtnPlay.Content = "▶ Play";
                TxtTiempo.Text = "00:00.00";
            }
        }

        private void BtnCargarLetras_Click(object sender, RoutedEventArgs e)
        {
            ColeccionLRC.Clear();
            string[] lineasCrudas = TxtLetrasCrudas.Text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            bool isEnhanced = ToggleEnhanced.IsChecked == true;

            foreach (string linea in lineasCrudas)
            {
                var nuevaLinea = new LineaLRC
                {
                    TiempoStr = "[--:--.--]",
                    TextoOriginal = linea.Trim(),
                    TextoSincronizado = linea.Trim(),
                    Palabras = new List<string>(),
                    TiemposPalabras = new List<string>()
                };

                // Si está en modo Karaoke, preparamos la lista de palabras
                if (isEnhanced)
                {
                    string[] palabrasCortadas = linea.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    nuevaLinea.Palabras.AddRange(palabrasCortadas);
                    for (int i = 0; i < nuevaLinea.Palabras.Count; i++) nuevaLinea.TiemposPalabras.Add("");
                }

                ColeccionLRC.Add(nuevaLinea);
            }

            if (ColeccionLRC.Count > 0)
            {
                ListaLRC.SelectedIndex = 0;
                ListaLRC.Focus();
            }
            ActualizarSiguientePalabra();
        }

        private void ToggleEnhanced_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(TxtLetrasCrudas.Text) && ColeccionLRC.Count > 0)
            {
                // Si cambiamos de modo, recargamos la estructura interna de la letra
                BtnCargarLetras_Click(null, null);
            }
        }

        private void BtnPlay_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(rutaActual)) return;

            if (isPlaying)
            {
                mediaPlayerLRC.Pause();
                isPlaying = false;
                BtnPlay.Content = "▶ Play";
            }
            else
            {
                mediaPlayerLRC.SpeedRatio = SliderVelocidad.Value;
                mediaPlayerLRC.Play();
                isPlaying = true;
                BtnPlay.Content = "|| Pausa";
                ListaLRC.Focus();
            }
        }

        private void SliderVelocidad_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (TxtVelocidad != null)
            {
                TxtVelocidad.Text = $"{SliderVelocidad.Value:F1}x";
                if (mediaPlayerLRC != null) mediaPlayerLRC.SpeedRatio = SliderVelocidad.Value;
            }
        }

        private void ListaLRC_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            ActualizarSiguientePalabra();
        }

        private void ActualizarSiguientePalabra()
        {
            if (ListaLRC.SelectedIndex != -1)
            {
                if (ToggleEnhanced.IsChecked == true)
                {
                    var linea = ColeccionLRC[ListaLRC.SelectedIndex];
                    int wordIdx = linea.TiemposPalabras.FindIndex(t => t == "");

                    if (wordIdx != -1) TxtSiguientePalabra.Text = linea.Palabras[wordIdx];
                    else TxtSiguientePalabra.Text = "Línea lista. Espacio para reset.";
                }
                else
                {
                    TxtSiguientePalabra.Text = "Modo Línea (Normal)";
                }
            }
            else TxtSiguientePalabra.Text = "-";
        }
        private void ActualizarLineaVisual(int index, LineaLRC linea)
        {
            if (ToggleEnhanced.IsChecked == true)
            {
                string textoSync = "";
                for (int i = 0; i < linea.Palabras.Count; i++)
                {
                    if (linea.TiemposPalabras[i] != "") textoSync += linea.TiemposPalabras[i] + linea.Palabras[i] + " ";
                    else textoSync += linea.Palabras[i] + " ";
                }
                linea.TextoSincronizado = textoSync.TrimEnd();
            }
            else
            {
                linea.TextoSincronizado = linea.TextoOriginal;
            }

            // Al crear un nuevo objeto, obligamos a WPF a redibujar la celda en pantalla
            ColeccionLRC[index] = new LineaLRC
            {
                TiempoStr = linea.TiempoStr,
                TextoOriginal = linea.TextoOriginal,
                TextoSincronizado = linea.TextoSincronizado,
                Palabras = linea.Palabras,
                TiemposPalabras = linea.TiemposPalabras
            };
        }
        // --- SISTEMA DE ATAJOS PROFESIONAL ---
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (TxtLetrasCrudas.IsFocused) return;

            if (e.Key == Key.Space && isPlaying)
            {
                if (ListaLRC.SelectedIndex != -1 && ListaLRC.SelectedIndex < ColeccionLRC.Count)
                {
                    int currentIndex = ListaLRC.SelectedIndex;
                    var lineaActiva = ColeccionLRC[currentIndex];
                    string timeNow = mediaPlayerLRC.Position.ToString(@"mm\:ss\.ff");

                    if (ToggleEnhanced.IsChecked == true)
                    {
                        int wordIdx = lineaActiva.TiemposPalabras.FindIndex(t => t == "");

                        // Si la línea estaba llena y el usuario presiona espacio, la reiniciamos
                        if (wordIdx == -1)
                        {
                            for (int i = 0; i < lineaActiva.TiemposPalabras.Count; i++) lineaActiva.TiemposPalabras[i] = "";
                            wordIdx = 0;
                        }

                        if (wordIdx == 0) lineaActiva.TiempoStr = $"[{timeNow}]";
                        lineaActiva.TiemposPalabras[wordIdx] = $"<{timeNow}>";

                        ActualizarLineaVisual(currentIndex, lineaActiva);
                        ListaLRC.SelectedIndex = currentIndex;

                        // Salto automático si fue la última palabra
                        if (wordIdx == lineaActiva.Palabras.Count - 1 && currentIndex + 1 < ColeccionLRC.Count)
                        {
                            ListaLRC.SelectedIndex = currentIndex + 1;
                            ListaLRC.ScrollIntoView(ListaLRC.SelectedItem);
                        }
                    }
                    else // Modo Normal
                    {
                        lineaActiva.TiempoStr = $"[{timeNow}]";
                        ActualizarLineaVisual(currentIndex, lineaActiva);
                        ListaLRC.SelectedIndex = currentIndex;

                        if (currentIndex + 1 < ColeccionLRC.Count)
                        {
                            ListaLRC.SelectedIndex = currentIndex + 1;
                            ListaLRC.ScrollIntoView(ListaLRC.SelectedItem);
                        }
                    }
                    ActualizarSiguientePalabra();
                }
                e.Handled = true;
            }
            // NUEVO: Botón DESHACER (Retroceso / Backspace)
            else if (e.Key == Key.Back)
            {
                if (ListaLRC.SelectedIndex != -1)
                {
                    int currentIndex = ListaLRC.SelectedIndex;
                    var lineaActiva = ColeccionLRC[currentIndex];

                    if (ToggleEnhanced.IsChecked == true)
                    {
                        int lastWordIdx = lineaActiva.TiemposPalabras.FindLastIndex(t => t != "");

                        if (lastWordIdx != -1)
                        {
                            // Borramos la última palabra marcada de la línea actual
                            lineaActiva.TiemposPalabras[lastWordIdx] = "";
                            if (lastWordIdx == 0) lineaActiva.TiempoStr = "[--:--.--]";

                            ActualizarLineaVisual(currentIndex, lineaActiva);
                            ListaLRC.SelectedIndex = currentIndex;
                        }
                        else if (currentIndex > 0)
                        {
                            // Si la línea está vacía, saltamos a la anterior y borramos su última palabra
                            int prevIndex = currentIndex - 1;
                            var lineaAnt = ColeccionLRC[prevIndex];
                            int prevLastWordIdx = lineaAnt.TiemposPalabras.FindLastIndex(t => t != "");

                            if (prevLastWordIdx != -1)
                            {
                                lineaAnt.TiemposPalabras[prevLastWordIdx] = "";
                                if (prevLastWordIdx == 0) lineaAnt.TiempoStr = "[--:--.--]";
                                ActualizarLineaVisual(prevIndex, lineaAnt);
                            }

                            ListaLRC.SelectedIndex = prevIndex;
                            ListaLRC.ScrollIntoView(ListaLRC.SelectedItem);
                        }
                    }
                    else // Modo Normal Deshacer
                    {
                        if (lineaActiva.TiempoStr != "[--:--.--]")
                        {
                            lineaActiva.TiempoStr = "[--:--.--]";
                            ActualizarLineaVisual(currentIndex, lineaActiva);
                            ListaLRC.SelectedIndex = currentIndex;
                        }
                        else if (currentIndex > 0)
                        {
                            int prevIndex = currentIndex - 1;
                            var lineaAnt = ColeccionLRC[prevIndex];
                            lineaAnt.TiempoStr = "[--:--.--]";
                            ActualizarLineaVisual(prevIndex, lineaAnt);

                            ListaLRC.SelectedIndex = prevIndex;
                            ListaLRC.ScrollIntoView(ListaLRC.SelectedItem);
                        }
                    }
                    ActualizarSiguientePalabra();
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Left)
            {
                mediaPlayerLRC.Position -= TimeSpan.FromSeconds(5);
                TxtTiempo.Text = mediaPlayerLRC.Position.ToString(@"mm\:ss\.ff");
                e.Handled = true;
            }
            else if (e.Key == Key.Right)
            {
                mediaPlayerLRC.Position += TimeSpan.FromSeconds(5);
                TxtTiempo.Text = mediaPlayerLRC.Position.ToString(@"mm\:ss\.ff");
                e.Handled = true;
            }
        }

        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(rutaActual) || ColeccionLRC.Count == 0) return;

            List<string> lineasGuardar = new List<string>();
            foreach (var item in ColeccionLRC)
            {
                if (item.TiempoStr != "[--:--.--]")
                {
                    if (ToggleEnhanced.IsChecked == true) lineasGuardar.Add($"{item.TiempoStr}{item.TextoSincronizado}");
                    else lineasGuardar.Add($"{item.TiempoStr}{item.TextoOriginal}");
                }
            }

            string rutaLrc = Path.ChangeExtension(rutaActual, ".lrc");
            File.WriteAllLines(rutaLrc, lineasGuardar);
            MessageBox.Show("Archivo .LRC guardado correctamente.", "Éxito");
        }

        protected override void OnClosed(EventArgs e)
        {
            if (mediaPlayerLRC != null) { mediaPlayerLRC.Stop(); mediaPlayerLRC.Close(); }
            timer.Stop();
            base.OnClosed(e);
        }
    }

    public class LineaLRC
    {
        public string TiempoStr { get; set; }
        public string TextoOriginal { get; set; }
        public string TextoSincronizado { get; set; }
        public List<string> Palabras { get; set; }
        public List<string> TiemposPalabras { get; set; }
    }
}