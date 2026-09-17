using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Reproductor
{
    public class MotorVisualizador
    {
        private Canvas lienzo;
        private int modoVisualizador = 0;

        private double anguloEspiral = 0;
        private double tiempoColor = 0;
        private Random rndFuego = new Random();

        private List<Particula> particulasActivas = new List<Particula>();
        private List<Rectangle> barrasFuego = new List<Rectangle>();
        private Polyline lineaOnda = new Polyline();

        private Image imgPortada = new Image();
        private BitmapImage portadaActual = null;
        private double anguloDisco = 0;
        private Ellipse agujeroDisco = new Ellipse();

        public MotorVisualizador(Canvas canvasDestino)
        {
            lienzo = canvasDestino;
            imgPortada.Stretch = Stretch.UniformToFill;
            imgPortada.Effect = new DropShadowEffect { Color = Colors.Black, BlurRadius = 20, ShadowDepth = 0 };
        }

        public void CambiarModo()
        {
            modoVisualizador++;
            if (modoVisualizador > 4) modoVisualizador = 0;
            LimpiarLienzo();
        }

        private void LimpiarLienzo()
        {
            lienzo.Children.Clear();
            particulasActivas.Clear();
            barrasFuego.Clear();
            lineaOnda.Points.Clear();
            imgPortada.Clip = null;
            imgPortada.RenderTransform = null;
        }

        public void ActualizarCancionActual(string rutaCancion)
        {
            portadaActual = ExtraerPortadaDesdeMetadata(rutaCancion);
            imgPortada.Source = portadaActual;
        }

        public void DibujarFrame(float pico, float[] magnitudesFft, double[] gananciasEcualizador)
        {
            if (lienzo.ActualWidth == 0) return;
            if (pico > 1.2f) pico = 1.2f;

            switch (modoVisualizador)
            {
                case 0: RenderizarBarrasFuego(pico); break;
                case 1: RenderizarOsciloscopio(pico, magnitudesFft, gananciasEcualizador); break;
                case 2: RenderizarFractal(pico); break;
                case 3: RenderizarPortadaReactiva(pico); break;
                case 4: RenderizarDisco(); break; 
            }
        }

        private void RenderizarPortadaReactiva(float pico)
        {
            if (portadaActual == null) return;
            if (!lienzo.Children.Contains(imgPortada)) lienzo.Children.Add(imgPortada);

            double centroX = lienzo.ActualWidth / 2;
            double centroY = lienzo.ActualHeight / 2;

            double tamanoBase = Math.Min(lienzo.ActualWidth, lienzo.ActualHeight) * 0.55;
            if (tamanoBase < 100) tamanoBase = 100;

            double tamanoReactivo = tamanoBase + (pico * 45);

            imgPortada.Width = tamanoReactivo;
            imgPortada.Height = tamanoReactivo;

            Canvas.SetLeft(imgPortada, centroX - (tamanoReactivo / 2));
            Canvas.SetTop(imgPortada, centroY - (tamanoReactivo / 2));

            if (imgPortada.Effect is DropShadowEffect shadow)
            {
                shadow.BlurRadius = 15 + (pico * 30);
            }
        }

        private BitmapImage ExtraerPortadaDesdeMetadata(string ruta)
        {
            try
            {
                using (var file = TagLib.File.Create(ruta))
                {
                    if (file.Tag.Pictures != null && file.Tag.Pictures.Length > 0)
                    {
                        var ilustracion = file.Tag.Pictures[0];
                        byte[] datosImagen = ilustracion.Data.Data;

                        using (MemoryStream ms = new MemoryStream(datosImagen))
                        {
                            BitmapImage bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.StreamSource = ms;
                            bitmap.EndInit();
                            bitmap.Freeze();
                            return bitmap;
                        }
                    }
                }
            }
            catch { }
            return null;
        }

        private void RenderizarBarrasFuego(float pico)
        {
            int numBarras = 30;
            double anchoBarra = lienzo.ActualWidth / numBarras;
            SolidColorBrush colorBrush = (SolidColorBrush)Application.Current.FindResource("Barras");

            if (barrasFuego.Count == 0)
            {
                for (int i = 0; i < numBarras; i++)
                {
                    var barra = new Rectangle { Width = anchoBarra - 2, Fill = colorBrush, VerticalAlignment = VerticalAlignment.Bottom };
                    barrasFuego.Add(barra);
                    lienzo.Children.Add(barra);
                }
            }

            for (int i = 0; i < numBarras; i++)
            {
                double ruido = rndFuego.NextDouble() * 0.5 + 0.5;
                double alturaMeta = 10 + (pico * 150 * ruido);

                if (i > 10 && i < 20) alturaMeta *= 1.5;
                barrasFuego[i].Height = alturaMeta;

                double factorBrillo = 0.4 + (alturaMeta / 150.0);
                byte rc = (byte)Math.Max(0, Math.Min(255, colorBrush.Color.R * factorBrillo));
                byte gc = (byte)Math.Max(0, Math.Min(255, colorBrush.Color.G * factorBrillo));
                byte bc = (byte)Math.Max(0, Math.Min(255, colorBrush.Color.B * factorBrillo));

                barrasFuego[i].Fill = new SolidColorBrush(Color.FromRgb(rc, gc, bc));
                Canvas.SetLeft(barrasFuego[i], i * anchoBarra);
                Canvas.SetBottom(barrasFuego[i], 0);
            }
        }

        private void RenderizarOsciloscopio(float pico, float[] magnitudes, double[] eqBandsGains)
        {
            if (magnitudes == null) return;
            if (!lienzo.Children.Contains(lineaOnda))
            {
                SolidColorBrush colorOciloscopio = (SolidColorBrush)Application.Current.FindResource("ColorOscilo");
                lineaOnda.Stroke = new SolidColorBrush(colorOciloscopio.Color);
                lineaOnda.StrokeThickness = 2;
                lineaOnda.Effect = new DropShadowEffect { Color = colorOciloscopio.Color, BlurRadius = 15, ShadowDepth = 0 };
                lienzo.Children.Add(lineaOnda);
            }

            lineaOnda.Points.Clear();

            double centroX = lienzo.ActualWidth / 2;
            double centroY = lienzo.ActualHeight / 2;
            double amplitudGlobal = (Math.Min(centroX, centroY) * 0.5) + (pico * 50);
            tiempoColor += 0.05 + (pico * 0.1);

            double[] audioEnVivo = new double[10];
            double sensibilidad = 150.0;

            for (int i = 0; i < 10; i++)
            {
                int index = (i == 0) ? 2 : (i == 1) ? 5 : (i == 2) ? 10 : (i == 3) ? 20 : (i == 4) ? 40 : (i == 5) ? 80 : (i == 6) ? 120 : (i == 7) ? 160 : (i == 8) ? 250 : 400;
                audioEnVivo[i] = magnitudes[index] * sensibilidad;
            }

            int resolucionLazo = 200;
            for (int i = 0; i <= resolucionLazo; i++)
            {
                double t = (i / (double)resolucionLazo) * Math.PI * 2;
                double coordX = 0;
                double coordY = 0;

                for (int k = 0; k < 5; k++) coordX += Math.Sin((k + 1) * t) * (((eqBandsGains[k] + 12) / 12.0) + audioEnVivo[k]);
                for (int k = 5; k < 10; k++) coordY += Math.Sin((k - 4) * t) * (((eqBandsGains[k] + 12) / 12.0) + audioEnVivo[k]);

                lineaOnda.Points.Add(new Point(centroX + (coordX / 5.0) * amplitudGlobal, centroY + (coordY / 5.0) * amplitudGlobal));
            }
        }
        private void RenderizarDisco()
        {
            if (portadaActual == null) return;

            // Asegurarnos de que la imagen y el agujero están en el lienzo
            if (!lienzo.Children.Contains(imgPortada)) lienzo.Children.Add(imgPortada);
            if (!lienzo.Children.Contains(agujeroDisco)) lienzo.Children.Add(agujeroDisco);

            double tamano = Math.Min(lienzo.ActualWidth, lienzo.ActualHeight) * 0.8;
            if (tamano <= 0) return;

            // 1. Reciclar imgPortada y hacerla circular
            imgPortada.Width = tamano;
            imgPortada.Height = tamano;
            imgPortada.Clip = new EllipseGeometry(new Point(tamano / 2, tamano / 2), tamano / 2, tamano / 2);

            // Centrar la imagen en el canvas
            Canvas.SetLeft(imgPortada, (lienzo.ActualWidth - tamano) / 2);
            Canvas.SetTop(imgPortada, (lienzo.ActualHeight - tamano) / 2);

            // Rotar la imagen sobre su propio centro
            anguloDisco += 1.0;
            if (anguloDisco >= 360) anguloDisco = 0;
            imgPortada.RenderTransform = new RotateTransform(anguloDisco, tamano / 2, tamano / 2);

            // 2. Dibujar y centrar el agujero del vinilo
            double tamanoAgujero = tamano * 0.15;
            agujeroDisco.Width = tamanoAgujero;
            agujeroDisco.Height = tamanoAgujero;

            // Usamos el color de fondo dinámico de la skin para que el agujero se "fusione"
            agujeroDisco.Fill = (SolidColorBrush)Application.Current.FindResource("ColorFondoPrincipal");

            Canvas.SetLeft(agujeroDisco, (lienzo.ActualWidth - tamanoAgujero) / 2);
            Canvas.SetTop(agujeroDisco, (lienzo.ActualHeight - tamanoAgujero) / 2);

            // Asegurar que el agujero siempre esté encima de la portada
            Panel.SetZIndex(agujeroDisco, 10);
        }
        private void RenderizarFractal(float pico)
        {
            anguloEspiral += 0.05 + (pico * 0.2);
            tiempoColor += 0.1 + pico;

            byte r = (byte)(Math.Sin(tiempoColor) * 127 + 128);
            byte g = (byte)(Math.Sin(tiempoColor + 2) * 127 + 128);
            byte b = (byte)(Math.Sin(tiempoColor + 4) * 127 + 128);
            SolidColorBrush colorActual = new SolidColorBrush(Color.FromRgb(r, g, b));

            if (pico > 0.02f)
            {
                int numBrazos = 4;
                for (int i = 0; i < numBrazos; i++)
                {
                    double anguloBrazo = anguloEspiral + (i * (Math.PI * 2 / numBrazos));
                    double velocidadExplosion = 2 + (pico * 15);

                    var nuevaForma = new Ellipse
                    {
                        Width = 5 + (pico * 25),
                        Height = 5 + (pico * 25),
                        Fill = colorActual,
                        Opacity = 1.0,
                        Effect = new DropShadowEffect { Color = colorActual.Color, BlurRadius = 15, ShadowDepth = 0 }
                    };

                    Particula p = new Particula { Forma = nuevaForma, X = lienzo.ActualWidth / 2, Y = lienzo.ActualHeight / 2, VelX = Math.Cos(anguloBrazo) * velocidadExplosion, VelY = Math.Sin(anguloBrazo) * velocidadExplosion, Vida = 1.0 };
                    Canvas.SetLeft(p.Forma, p.X); Canvas.SetTop(p.Forma, p.Y);
                    lienzo.Children.Add(p.Forma); particulasActivas.Add(p);
                }
            }

            for (int i = particulasActivas.Count - 1; i >= 0; i--)
            {
                var p = particulasActivas[i];
                p.X += p.VelX; p.Y += p.VelY; p.VelX += Math.Cos(anguloEspiral) * 0.2; p.VelY += Math.Sin(anguloEspiral) * 0.2; p.Vida -= 0.03;

                if (p.Vida <= 0 || p.X < -50 || p.X > lienzo.ActualWidth + 50 || p.Y < -50 || p.Y > lienzo.ActualHeight + 50)
                {
                    lienzo.Children.Remove(p.Forma); particulasActivas.RemoveAt(i);
                }
                else
                {
                    p.Forma.Opacity = p.Vida; p.Forma.Width = Math.Max(1, p.Forma.Width * 0.95); p.Forma.Height = Math.Max(1, p.Forma.Height * 0.95);
                    Canvas.SetLeft(p.Forma, p.X); Canvas.SetTop(p.Forma, p.Y);
                }
            }
        }
        private class Particula { public Shape Forma { get; set; } public double X { get; set; } public double Y { get; set; } public double VelX { get; set; } public double VelY { get; set; } public double Vida { get; set; } }
    }
}