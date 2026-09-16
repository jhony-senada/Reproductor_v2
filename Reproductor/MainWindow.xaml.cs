using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace Reproductor
{
    public partial class MainWindow : Window
    {
        private GestorSkins gestorSkins;
        private GestorPlaylists gestorPlaylists;
        private MotorAudio motorAudio;
        private MotorVisualizador motorVisualizador;

        private bool isResizing = false;
        private Point startPoint;
        private bool isDraggingSlider = false;

        private DispatcherTimer relojUI;
        private Dictionary<TimeSpan, string> diccionarioLetras = new Dictionary<TimeSpan, string>();
        private List<TimeSpan> tiemposLetras = new List<TimeSpan>();

        public MainWindow()
        {
            InitializeComponent();

            gestorSkins = new GestorSkins();
            gestorPlaylists = new GestorPlaylists();
            motorAudio = new MotorAudio();
            motorVisualizador = new MotorVisualizador(CanvasVisualizador);

            motorAudio.CancionTerminada += (s, args) =>
            {
                Dispatcher.Invoke(() => AvanzarCancion(true));
            };

            CargarListaSkins();
            ActualizarComboPlaylists();
            CargarEqualizador();

            relojUI = new DispatcherTimer();
            relojUI.Interval = TimeSpan.FromMilliseconds(30);
            relojUI.Tick += RelojUI_Tick;
            relojUI.Start();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { if (e.LeftButton == MouseButtonState.Pressed) this.DragMove(); }
        private void BtnClose_Click(object sender, RoutedEventArgs e) => this.Close();
        private void BtnMii_Click(object sender, RoutedEventArgs e) => this.WindowState = WindowState.Minimized;

        private void ResizeGrip_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            isResizing = true; startPoint = e.GetPosition(this); ((UIElement)sender).CaptureMouse(); e.Handled = true;
        }

        private void ResizeGrip_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            isResizing = false; ((UIElement)sender).ReleaseMouseCapture();
        }

        private void ResizeGrip_MouseMove(object sender, MouseEventArgs e)
        {
            if (isResizing)
            {
                Point currentPoint = e.GetPosition(this);
                double newWidth = this.ActualWidth + (currentPoint.X - startPoint.X);
                double newHeight = this.ActualHeight + (currentPoint.Y - startPoint.Y);

                if (newWidth > 300) this.Width = newWidth;
                if (newHeight > 200) this.Height = newHeight;
                startPoint = currentPoint;
            }
        }

        private void CargarListaSkins()
        {
            ListaSkins.Items.Clear();
            foreach (string skin in gestorSkins.ObtenerListaSkins()) ListaSkins.Items.Add(skin);
            if (ListaSkins.Items.Count > 0)
            {
                ListaSkins.SelectedIndex = 0;
                gestorSkins.AplicarSkin(ListaSkins.SelectedItem.ToString());
            }
        }

        private void BtnAplicarSkin_Click(object sender, RoutedEventArgs e)
        {
            if (ListaSkins.SelectedItem != null) gestorSkins.AplicarSkin(ListaSkins.SelectedItem.ToString());
        }

        private void ActualizarComboPlaylists()
        {
            ComboPlaylists.Items.Clear();
            foreach (string nombre in gestorPlaylists.ObtenerNombresPlaylists()) ComboPlaylists.Items.Add(nombre);
            if (ComboPlaylists.Items.Count > 0) ComboPlaylists.SelectedIndex = 0;
        }

        private void ComboPlaylists_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboPlaylists.SelectedItem != null)
            {
                string nombrePlaylist = ComboPlaylists.SelectedItem.ToString();
                gestorPlaylists.CargarPlaylist(nombrePlaylist);

                ListaVisualCanciones.Items.Clear();
                foreach (string ruta in gestorPlaylists.RutasInternas) ListaVisualCanciones.Items.Add(System.IO.Path.GetFileNameWithoutExtension(ruta));
            }
        }

        private void BtnAgregarCancion_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog abrirArchivos = new OpenFileDialog { Multiselect = true, Filter = "Audio|*.mp3;*.m4a;*.wav" };
            if (abrirArchivos.ShowDialog() == true)
            {
                foreach (string archivo in abrirArchivos.FileNames)
                {
                    gestorPlaylists.RutasInternas.Add(archivo);
                    ListaVisualCanciones.Items.Add(System.IO.Path.GetFileNameWithoutExtension(archivo));
                }
                if (ComboPlaylists.SelectedItem != null) gestorPlaylists.GuardarPlaylist(ComboPlaylists.SelectedItem.ToString());
            }
        }

        private void BtnEliminarCancion_Click(object sender, RoutedEventArgs e)
        {
            int indice = ListaVisualCanciones.SelectedIndex;
            if (indice != -1)
            {
                ListaVisualCanciones.Items.RemoveAt(indice);
                gestorPlaylists.RutasInternas.RemoveAt(indice);

                if (ComboPlaylists.SelectedItem != null) gestorPlaylists.GuardarPlaylist(ComboPlaylists.SelectedItem.ToString());

                if (gestorPlaylists.RutasInternas.Count == 0)
                {
                    motorAudio.DetenerTotalmente();
                    Reproduciendo.Text = "Sin reproducir...";
                    TextoLetras.Text = "";
                    BtnPlayPause.Content = "▶";
                }
            }
            else { MessageBox.Show("Selecciona una canción primero.", "Aviso"); }
        }

        private void BtnGuardarComoPlaylist_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog guardarDialog = new SaveFileDialog { Filter = "Playlist (*.txt)|*.txt", Title = "Guardar Playlist" };
            if (guardarDialog.ShowDialog() == true)
            {
                string nombreSinExtension = System.IO.Path.GetFileNameWithoutExtension(guardarDialog.FileName);
                System.IO.File.WriteAllLines(guardarDialog.FileName, gestorPlaylists.RutasInternas);
                ActualizarComboPlaylists();
                ComboPlaylists.SelectedItem = nombreSinExtension;
            }
        }

        private void ListaVisualCanciones_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListaVisualCanciones.SelectedIndex != -1) ReproducirCancionActual(ListaVisualCanciones.SelectedIndex);
        }

        private void ReproducirCancionActual(int indice)
        {
            try
            {
                string ruta = gestorPlaylists.RutasInternas[indice];
                motorAudio.Reproducir(ruta, (float)SliderVolumen.Value, (float)EQ_Pre.Value);

                Reproduciendo.Text = System.IO.Path.GetFileNameWithoutExtension(ruta);
                CargarLetras(ruta);
                motorVisualizador.ActualizarCancionActual(ruta);

                BtnPlayPause.Content = "||";
            }
            catch (Exception ex) { MessageBox.Show("Error al reproducir: " + ex.Message); }
        }

        private void BtnPlayPause_Click(object sender, RoutedEventArgs e)
        {
            if (gestorPlaylists.RutasInternas.Count == 0) return;
            if (!motorAudio.IsPlaying && motorAudio.TiempoTotal.TotalSeconds == 0) ListaVisualCanciones.SelectedIndex = 0;
            else
            {
                motorAudio.AlternarPlayPause();
                BtnPlayPause.Content = motorAudio.IsPlaying ? "||" : "▶";
            }
        }

        private void BtnAnterior_Click(object sender, RoutedEventArgs e)
        {
            int nuevoIndice = gestorPlaylists.CalcularIndiceAnterior(ListaVisualCanciones.SelectedIndex);
            if (nuevoIndice != -1) ListaVisualCanciones.SelectedIndex = nuevoIndice;
        }

        private void BtnSiguiente_Click(object sender, RoutedEventArgs e) => AvanzarCancion(false);

        private void AvanzarCancion(bool automatico)
        {
            int nuevoIndice = gestorPlaylists.CalcularSiguienteIndice(ListaVisualCanciones.SelectedIndex, automatico);
            if (nuevoIndice != -1) ListaVisualCanciones.SelectedIndex = nuevoIndice;
            else motorAudio.DetenerTotalmente();
        }

        private void BtnShuffle_Click(object sender, RoutedEventArgs e)
        {
            gestorPlaylists.ModoShuffle = !gestorPlaylists.ModoShuffle;
            IconoShuffle.Opacity = gestorPlaylists.ModoShuffle ? 1.0 : 0.4;
            BtnShuffle.ToolTip = gestorPlaylists.ModoShuffle ? "Shuffle: Activado" : "Shuffle: Desactivado";
        }

        private void BtnLoop_Click(object sender, RoutedEventArgs e)
        {
            gestorPlaylists.ModoLoop++;
            if (gestorPlaylists.ModoLoop > 2) gestorPlaylists.ModoLoop = 0;

            switch (gestorPlaylists.ModoLoop)
            {
                case 0:
                    IconoLoop.Opacity = 0.4;
                    BrushLoop.ImageSource = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/assets/imagenes/loop.png"));
                    BtnLoop.ToolTip = "Loop: Desactivado";
                    break;
                case 1:
                    IconoLoop.Opacity = 1.0;
                    BrushLoop.ImageSource = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/assets/imagenes/loop.png"));
                    BtnLoop.ToolTip = "Loop: Todo";
                    break;
                case 2:
                    IconoLoop.Opacity = 1.0;
                    BrushLoop.ImageSource = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/assets/imagenes/repeat.png"));
                    BtnLoop.ToolTip = "Loop: 1";
                    break;
            }
        }

        private void RelojUI_Tick(object sender, EventArgs e)
        {
            if (motorAudio != null && motorAudio.IsPlaying)
            {
                TimeSpan posActual = motorAudio.TiempoActual;
                TimeSpan total = motorAudio.TiempoTotal;

                if (!isDraggingSlider)
                {
                    SliderProgreso.Maximum = total.TotalSeconds;
                    SliderProgreso.Value = posActual.TotalSeconds;
                }

                TextoTiempoActual.Text = posActual.ToString(@"mm\:ss");
                TextoTiempoTotal.Text = total.ToString(@"mm\:ss");

                string letraActiva = "";
                foreach (TimeSpan tiempo in tiemposLetras)
                {
                    if (posActual >= tiempo) letraActiva = diccionarioLetras[tiempo];
                    else break;
                }
                if (!string.IsNullOrEmpty(letraActiva)) TextoLetras.Text = letraActiva;

                double[] gananciasEcualizador = new double[]
                {
                    EQ_31.Value, EQ_62.Value, EQ_125.Value, EQ_250.Value, EQ_500.Value,
                    EQ_1k.Value, EQ_2k.Value, EQ_4k.Value, EQ_8k.Value, EQ_16k.Value
                };
                motorVisualizador.DibujarFrame(motorAudio.CurrentPeak, motorAudio.FftMagnitudes, gananciasEcualizador);
            }
        }

        private void CanvasVisualizador_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => motorVisualizador.CambiarModo();
        private void SliderProgreso_MouseDown(object sender, MouseButtonEventArgs e) => isDraggingSlider = true;

        private void SliderProgreso_MouseUp(object sender, MouseButtonEventArgs e)
        {
            motorAudio.CambiarPosicion(SliderProgreso.Value);
            isDraggingSlider = false;
        }

        private void SliderProgreso_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (isDraggingSlider) TextoTiempoActual.Text = TimeSpan.FromSeconds(SliderProgreso.Value).ToString(@"mm\:ss");
        }

        private void CargarLetras(string rutaAudio)
        {
            diccionarioLetras.Clear(); tiemposLetras.Clear(); TextoLetras.Text = "";
            string rutaLrc = System.IO.Path.ChangeExtension(rutaAudio, ".lrc");
            if (System.IO.File.Exists(rutaLrc))
            {
                string[] lineas = System.IO.File.ReadAllLines(rutaLrc);
                foreach (string linea in lineas)
                {
                    if (linea.StartsWith("[") && linea.Contains("]"))
                    {
                        int indexCierre = linea.IndexOf("]");
                        string tiempoStr = linea.Substring(1, indexCierre - 1);
                        string texto = linea.Substring(indexCierre + 1).Trim();
                        string[] partes = tiempoStr.Split(':');
                        if (partes.Length >= 2 && double.TryParse(partes[0], out double minutos) && double.TryParse(partes[1], NumberStyles.Any, CultureInfo.InvariantCulture, out double segundos))
                        {
                            TimeSpan ts = TimeSpan.FromSeconds((minutos * 60) + segundos);
                            diccionarioLetras[ts] = texto; tiemposLetras.Add(ts);
                        }
                    }
                }
                tiemposLetras.Sort();
            }
        }

        private void SliderVolumen_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => ActualizarVolumenTotal();
        private void EQ_Pre_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => ActualizarVolumenTotal();

        private void ActualizarVolumenTotal()
        {
            if (SliderVolumen != null && EQ_Pre != null && motorAudio != null) motorAudio.ActualizarVolumen((float)SliderVolumen.Value, (float)EQ_Pre.Value);
        }

        private void EqBand_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider slider = sender as Slider;
            if (slider != null && motorAudio != null)
            {
                int bandIndex = -1;
                switch (slider.Name)
                {
                    case "EQ_31": bandIndex = 0; break;
                    case "EQ_62": bandIndex = 1; break;
                    case "EQ_125": bandIndex = 2; break;
                    case "EQ_250": bandIndex = 3; break;
                    case "EQ_500": bandIndex = 4; break;
                    case "EQ_1k": bandIndex = 5; break;
                    case "EQ_2k": bandIndex = 6; break;
                    case "EQ_4k": bandIndex = 7; break;
                    case "EQ_8k": bandIndex = 8; break;
                    case "EQ_16k": bandIndex = 9; break;
                }
                if (bandIndex != -1) motorAudio.ActualizarBandaEcualizador(bandIndex, (float)slider.Value);
            }
        }

        private void AplicarPreset(double[] valores)
        {
            if (valores.Length != 10) return;
            EQ_31.Value = valores[0]; EQ_62.Value = valores[1]; EQ_125.Value = valores[2]; EQ_250.Value = valores[3]; EQ_500.Value = valores[4];
            EQ_1k.Value = valores[5]; EQ_2k.Value = valores[6]; EQ_4k.Value = valores[7]; EQ_8k.Value = valores[8]; EQ_16k.Value = valores[9];
        }

        private void BtnEqRock_Click(object sender, RoutedEventArgs e) => AplicarPreset(new double[] { 6, 4, 0, -2, -4, -2, 0, 3, 5, 6 });
        private void BtnEqPop_Click(object sender, RoutedEventArgs e) => AplicarPreset(new double[] { -2, -1, 1, 3, 4, 4, 2, 0, -1, -2 });
        private void BtnEqFlat_Click(object sender, RoutedEventArgs e) { AplicarPreset(new double[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }); EQ_Pre.Value = 0; }

        private void BtnEqGuardar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string rutaEq = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ConfigEq.txt");
                string[] valores = new string[] {
                    EQ_31.Value.ToString(), EQ_62.Value.ToString(), EQ_125.Value.ToString(), EQ_250.Value.ToString(), EQ_500.Value.ToString(),
                    EQ_1k.Value.ToString(), EQ_2k.Value.ToString(), EQ_4k.Value.ToString(), EQ_8k.Value.ToString(), EQ_16k.Value.ToString(), EQ_Pre.Value.ToString()
                };
                System.IO.File.WriteAllLines(rutaEq, valores);
                MessageBox.Show("Configuración guardada.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { MessageBox.Show("Error al guardar: " + ex.Message); }
        }

        private void CargarEqualizador()
        {
            try
            {
                string rutaEQ = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ConfigEq.txt");
                if (System.IO.File.Exists(rutaEQ))
                {
                    string[] lineas = System.IO.File.ReadAllLines(rutaEQ);
                    if (lineas.Length >= 11)
                    {
                        EQ_31.Value = double.Parse(lineas[0]); EQ_62.Value = double.Parse(lineas[1]); EQ_125.Value = double.Parse(lineas[2]); EQ_250.Value = double.Parse(lineas[3]); EQ_500.Value = double.Parse(lineas[4]);
                        EQ_1k.Value = double.Parse(lineas[5]); EQ_2k.Value = double.Parse(lineas[6]); EQ_4k.Value = double.Parse(lineas[7]); EQ_8k.Value = double.Parse(lineas[8]); EQ_16k.Value = double.Parse(lineas[9]); EQ_Pre.Value = double.Parse(lineas[10]);
                    }
                }
            }
            catch { }
        }
    }
}