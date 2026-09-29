using System;
using System.Drawing;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Windows.Forms;
using QRCoder;

namespace TaekwondoScoreboard
{
    public class MainForm : Form
    {
        private const int Puerto = 8080;

        private ScoreServer server;
        private Timer reloj;
        private Timer esperaRed;
        private WifiDirectManager wifiDirect;

        private TableLayoutPanel tablaPrincipal;

        private Panel panelAzul;
        private Panel panelRojo;
        private Panel panelConexion;

        private Label lblPuntosAzul;
        private Label lblPuntosRojo;
        private Label lblRondasAzul;
        private Label lblRondasRojo;

        private Label lblTiempo;
        private Label lblRonda;
        private Label lblEstado;

        private Label lblConexionJuez1;
        private Label lblConexionJuez2;

        private Label lblUrlJuez1;
        private Label lblUrlJuez2;

        private PictureBox qrJuez1;
        private PictureBox qrJuez2;

        private Button btnServidor;
        private Button btnIniciar;
        private Button btnPausa;
        private Button btnNuevo;
        private Button btnConexion;

        private bool pantallaCompleta = false;
        private bool servidorIniciado = false;
        private int intentosRed = 0;

        private FormBorderStyle bordeAnterior;
        private FormWindowState estadoAnterior;

        private string ipLocal = "";


        public MainForm()
        {
            InicializarVentana();
            CrearInterfaz();

            server = new ScoreServer();

            server.JudgeConnectionsChanged +=
                Server_JudgeConnectionsChanged;

            wifiDirect = new WifiDirectManager();

            wifiDirect.EstadoCambiado +=
                WifiDirect_EstadoCambiado;

            reloj = new Timer();
            reloj.Interval = 1000;
            reloj.Tick += Reloj_Tick;

            esperaRed = new Timer();
            esperaRed.Interval = 500;
            esperaRed.Tick += EsperaRed_Tick;

            KeyDown += MainForm_KeyDown;

            ActualizarPantalla();
        }


        // =========================================================
        // VENTANA
        // =========================================================

        private void InicializarVentana()
        {
            Text = "Taekwondo Scoreboard";

            StartPosition =
                FormStartPosition.CenterScreen;

            Size =
                new Size(1400, 850);

            MinimumSize =
                new Size(1100, 700);

            BackColor =
                Color.Black;

            ForeColor =
                Color.White;

            Font =
                new Font("Segoe UI", 10);

            KeyPreview = true;
        }


        // =========================================================
        // INTERFAZ
        // =========================================================

        private void CrearInterfaz()
        {
            tablaPrincipal =
                new TableLayoutPanel();

            tablaPrincipal.Dock =
                DockStyle.Fill;

            tablaPrincipal.BackColor =
                Color.Black;

            tablaPrincipal.Padding =
                new Padding(10);

            tablaPrincipal.ColumnCount = 2;
            tablaPrincipal.RowCount = 5;


            tablaPrincipal.ColumnStyles.Add(
                new ColumnStyle(
                    SizeType.Percent,
                    50
                )
            );

            tablaPrincipal.ColumnStyles.Add(
                new ColumnStyle(
                    SizeType.Percent,
                    50
                )
            );


            tablaPrincipal.RowStyles.Add(
                new RowStyle(
                    SizeType.Percent,
                    9
                )
            );

            tablaPrincipal.RowStyles.Add(
                new RowStyle(
                    SizeType.Percent,
                    46
                )
            );

            tablaPrincipal.RowStyles.Add(
                new RowStyle(
                    SizeType.Percent,
                    22
                )
            );

            tablaPrincipal.RowStyles.Add(
                new RowStyle(
                    SizeType.Percent,
                    8
                )
            );

            tablaPrincipal.RowStyles.Add(
                new RowStyle(
                    SizeType.Percent,
                    15
                )
            );


            Controls.Add(tablaPrincipal);

            CrearEncabezado();
            CrearMarcadores();
            CrearCronometro();
            CrearEstado();
            CrearControles();
            CrearPanelConexion();
        }


        // =========================================================
        // ENCABEZADO
        // =========================================================

        private void CrearEncabezado()
        {
            Panel encabezado =
                new Panel();

            encabezado.Dock =
                DockStyle.Fill;

            encabezado.BackColor =
                Color.FromArgb(18, 18, 18);

            encabezado.Margin =
                new Padding(4);


            TableLayoutPanel tabla =
                new TableLayoutPanel();

            tabla.Dock =
                DockStyle.Fill;

            tabla.ColumnCount = 3;

            tabla.ColumnStyles.Add(
                new ColumnStyle(
                    SizeType.Percent,
                    30
                )
            );

            tabla.ColumnStyles.Add(
                new ColumnStyle(
                    SizeType.Percent,
                    40
                )
            );

            tabla.ColumnStyles.Add(
                new ColumnStyle(
                    SizeType.Percent,
                    30
                )
            );


            lblConexionJuez1 =
                CrearLabel(
                    "JUEZ 1  ● DESCONECTADO",
                    14,
                    Color.OrangeRed
                );

            Label titulo =
                CrearLabel(
                    "TAEKWONDO",
                    28,
                    Color.White
                );

            lblConexionJuez2 =
                CrearLabel(
                    "JUEZ 2  ● DESCONECTADO",
                    14,
                    Color.OrangeRed
                );


            tabla.Controls.Add(
                lblConexionJuez1,
                0,
                0
            );

            tabla.Controls.Add(
                titulo,
                1,
                0
            );

            tabla.Controls.Add(
                lblConexionJuez2,
                2,
                0
            );


            encabezado.Controls.Add(tabla);


            tablaPrincipal.Controls.Add(
                encabezado,
                0,
                0
            );

            tablaPrincipal.SetColumnSpan(
                encabezado,
                2
            );
        }


        // =========================================================
        // MARCADORES
        // =========================================================

        private void CrearMarcadores()
        {
            panelAzul =
                CrearPanelCompetidor(
                    "AZUL",
                    Color.FromArgb(0, 70, 170),
                    out lblPuntosAzul,
                    out lblRondasAzul
                );

            panelRojo =
                CrearPanelCompetidor(
                    "ROJO",
                    Color.FromArgb(190, 20, 35),
                    out lblPuntosRojo,
                    out lblRondasRojo
                );


            tablaPrincipal.Controls.Add(
                panelAzul,
                0,
                1
            );

            tablaPrincipal.Controls.Add(
                panelRojo,
                1,
                1
            );
        }


        private Panel CrearPanelCompetidor(
            string nombre,
            Color color,
            out Label puntos,
            out Label rondas)
        {
            Panel panel =
                new Panel();

            panel.Dock =
                DockStyle.Fill;

            panel.BackColor =
                color;

            panel.Margin =
                new Padding(4);


            TableLayoutPanel tabla =
                new TableLayoutPanel();

            tabla.Dock =
                DockStyle.Fill;

            tabla.ColumnCount = 1;
            tabla.RowCount = 3;


            tabla.RowStyles.Add(
                new RowStyle(
                    SizeType.Percent,
                    18
                )
            );

            tabla.RowStyles.Add(
                new RowStyle(
                    SizeType.Percent,
                    65
                )
            );

            tabla.RowStyles.Add(
                new RowStyle(
                    SizeType.Percent,
                    17
                )
            );


            Label lblNombre =
                CrearLabel(
                    nombre,
                    30,
                    Color.White
                );


            puntos =
                CrearLabel(
                    "0",
                    120,
                    Color.White
                );


            rondas =
                CrearLabel(
                    "RONDAS GANADAS: 0",
                    20,
                    Color.White
                );


            tabla.Controls.Add(
                lblNombre,
                0,
                0
            );

            tabla.Controls.Add(
                puntos,
                0,
                1
            );

            tabla.Controls.Add(
                rondas,
                0,
                2
            );


            panel.Controls.Add(tabla);

            return panel;
        }


        // =========================================================
        // CRONÓMETRO
        // =========================================================

        private void CrearCronometro()
        {
            Panel panel =
                new Panel();

            panel.Dock =
                DockStyle.Fill;

            panel.BackColor =
                Color.FromArgb(8, 8, 8);

            panel.Margin =
                new Padding(4);


            TableLayoutPanel tabla =
                new TableLayoutPanel();

            tabla.Dock =
                DockStyle.Fill;

            tabla.ColumnCount = 3;


            tabla.ColumnStyles.Add(
                new ColumnStyle(
                    SizeType.Percent,
                    25
                )
            );

            tabla.ColumnStyles.Add(
                new ColumnStyle(
                    SizeType.Percent,
                    50
                )
            );

            tabla.ColumnStyles.Add(
                new ColumnStyle(
                    SizeType.Percent,
                    25
                )
            );


            lblRonda =
                CrearLabel(
                    "RONDA 1",
                    28,
                    Color.Gold
                );


            lblTiempo =
                CrearLabel(
                    "2:00",
                    78,
                    Color.White
                );


            Label ayuda =
                CrearLabel(
                    "F11\nPANTALLA COMPLETA",
                    11,
                    Color.Gray
                );


            tabla.Controls.Add(
                lblRonda,
                0,
                0
            );

            tabla.Controls.Add(
                lblTiempo,
                1,
                0
            );

            tabla.Controls.Add(
                ayuda,
                2,
                0
            );


            panel.Controls.Add(tabla);


            tablaPrincipal.Controls.Add(
                panel,
                0,
                2
            );

            tablaPrincipal.SetColumnSpan(
                panel,
                2
            );
        }


        // =========================================================
        // ESTADO
        // =========================================================

        private void CrearEstado()
        {
            lblEstado =
                CrearLabel(
                    "INICIA LA RED LOCAL",
                    19,
                    Color.White
                );

            lblEstado.Dock =
                DockStyle.Fill;

            lblEstado.BackColor =
                Color.FromArgb(45, 45, 45);

            lblEstado.Margin =
                new Padding(4);


            tablaPrincipal.Controls.Add(
                lblEstado,
                0,
                3
            );

            tablaPrincipal.SetColumnSpan(
                lblEstado,
                2
            );
        }


        // =========================================================
        // CONTROLES
        // =========================================================

        private void CrearControles()
        {
            TableLayoutPanel controles =
                new TableLayoutPanel();

            controles.Dock =
                DockStyle.Fill;

            controles.ColumnCount = 4;

            controles.Margin =
                new Padding(4);


            for (int i = 0; i < 4; i++)
            {
                controles.ColumnStyles.Add(
                    new ColumnStyle(
                        SizeType.Percent,
                        25
                    )
                );
            }


            btnIniciar =
                CrearBoton(
                    "INICIAR COMBATE",
                    Color.FromArgb(0, 105, 70)
                );


            btnPausa =
                CrearBoton(
                    "PAUSA",
                    Color.FromArgb(190, 105, 0)
                );


            btnNuevo =
                CrearBoton(
                    "NUEVO COMBATE",
                    Color.FromArgb(90, 90, 90)
                );


            btnConexion =
                CrearBoton(
                    "CONEXIÓN / QR",
                    Color.FromArgb(45, 45, 45)
                );


            btnIniciar.Click +=
                BtnIniciar_Click;

            btnPausa.Click +=
                BtnPausa_Click;

            btnNuevo.Click +=
                BtnNuevo_Click;

            btnConexion.Click +=
                BtnConexion_Click;


            controles.Controls.Add(
                btnIniciar,
                0,
                0
            );

            controles.Controls.Add(
                btnPausa,
                1,
                0
            );

            controles.Controls.Add(
                btnNuevo,
                2,
                0
            );

            controles.Controls.Add(
                btnConexion,
                3,
                0
            );


            tablaPrincipal.Controls.Add(
                controles,
                0,
                4
            );

            tablaPrincipal.SetColumnSpan(
                controles,
                2
            );
        }


        // =========================================================
        // PANEL DE CONEXIÓN
        // =========================================================

        private void CrearPanelConexion()
        {
            panelConexion =
                new Panel();

            panelConexion.Size =
                new Size(760, 570);

            panelConexion.BackColor =
                Color.FromArgb(25, 25, 25);

            panelConexion.BorderStyle =
                BorderStyle.FixedSingle;


            TableLayoutPanel tabla =
                new TableLayoutPanel();

            tabla.Dock =
                DockStyle.Fill;

            tabla.Padding =
                new Padding(15);

            tabla.ColumnCount = 2;
            tabla.RowCount = 5;


            tabla.ColumnStyles.Add(
                new ColumnStyle(
                    SizeType.Percent,
                    50
                )
            );

            tabla.ColumnStyles.Add(
                new ColumnStyle(
                    SizeType.Percent,
                    50
                )
            );


            tabla.RowStyles.Add(
                new RowStyle(
                    SizeType.Absolute,
                    60
                )
            );

            tabla.RowStyles.Add(
                new RowStyle(
                    SizeType.Percent,
                    65
                )
            );

            tabla.RowStyles.Add(
                new RowStyle(
                    SizeType.Absolute,
                    55
                )
            );

            tabla.RowStyles.Add(
                new RowStyle(
                    SizeType.Absolute,
                    60
                )
            );

            tabla.RowStyles.Add(
                new RowStyle(
                    SizeType.Absolute,
                    70
                )
            );


            Label titulo =
                CrearLabel(
                    "CONEXIÓN DE JUECES",
                    24,
                    Color.White
                );


            tabla.Controls.Add(
                titulo,
                0,
                0
            );

            tabla.SetColumnSpan(
                titulo,
                2
            );


            qrJuez1 =
                CrearPictureBox();

            qrJuez2 =
                CrearPictureBox();


            tabla.Controls.Add(
                qrJuez1,
                0,
                1
            );

            tabla.Controls.Add(
                qrJuez2,
                1,
                1
            );


            Label j1 =
                CrearLabel(
                    "JUEZ 1",
                    18,
                    Color.White
                );

            Label j2 =
                CrearLabel(
                    "JUEZ 2",
                    18,
                    Color.White
                );


            tabla.Controls.Add(
                j1,
                0,
                2
            );

            tabla.Controls.Add(
                j2,
                1,
                2
            );


            lblUrlJuez1 =
                CrearLabel(
                    "",
                    9,
                    Color.LightGray
                );

            lblUrlJuez2 =
                CrearLabel(
                    "",
                    9,
                    Color.LightGray
                );


            tabla.Controls.Add(
                lblUrlJuez1,
                0,
                3
            );

            tabla.Controls.Add(
                lblUrlJuez2,
                1,
                3
            );


            btnServidor =
                CrearBoton(
                    "INICIAR RED LOCAL",
                    Color.FromArgb(0, 105, 70)
                );


            btnServidor.Click +=
                BtnServidor_Click;


            tabla.Controls.Add(
                btnServidor,
                0,
                4
            );

            tabla.SetColumnSpan(
                btnServidor,
                2
            );


            panelConexion.Controls.Add(tabla);

            Controls.Add(panelConexion);

            panelConexion.BringToFront();


            Resize += delegate
            {
                CentrarPanelConexion();
            };


            CentrarPanelConexion();
        }


        private void CentrarPanelConexion()
        {
            if (panelConexion == null)
            {
                return;
            }

            panelConexion.Left =
                (ClientSize.Width -
                 panelConexion.Width) / 2;

            panelConexion.Top =
                (ClientSize.Height -
                 panelConexion.Height) / 2;
        }


        // =========================================================
        // INICIAR RED LOCAL
        // =========================================================

        private void BtnServidor_Click(
            object sender,
            EventArgs e)
        {
            if (servidorIniciado)
            {
                return;
            }

            try
            {
                btnServidor.Enabled = false;

                btnServidor.Text =
                    "CREANDO RED TORNEO...";

                lblEstado.Text =
                    "CREANDO RED WI-FI DIRECT...";

                lblEstado.BackColor =
                    Color.FromArgb(190, 105, 0);


                wifiDirect.Iniciar(
                    "TORNEO",
                    "Taekwondo2026"
                );
            }
            catch (Exception ex)
            {
                btnServidor.Enabled = true;

                btnServidor.Text =
                    "INICIAR RED LOCAL";

                lblEstado.Text =
                    "ERROR AL CREAR LA RED";

                lblEstado.BackColor =
                    Color.FromArgb(150, 30, 30);


                MessageBox.Show(
                    "No se pudo iniciar Wi-Fi Direct.\n\n" +
                    ex.Message,
                    "Wi-Fi Direct",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }


        // =========================================================
        // ESTADO WI-FI DIRECT
        // =========================================================

        private void WifiDirect_EstadoCambiado(
            object sender,
            EventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(
                    new Action(
                        () =>
                        {
                            WifiDirect_EstadoCambiado(
                                sender,
                                e
                            );
                        }
                    )
                );

                return;
            }


            if (wifiDirect.EstaActivo)
            {
                lblEstado.Text =
                    "RED TORNEO ACTIVA - PREPARANDO SERVIDOR...";

                lblEstado.BackColor =
                    Color.FromArgb(0, 100, 65);

                btnServidor.Text =
                    "PREPARANDO SERVIDOR...";

                intentosRed = 0;

                esperaRed.Start();

                return;
            }


            if (wifiDirect.UltimoEstado.StartsWith("ERROR"))
            {
                esperaRed.Stop();

                btnServidor.Enabled = true;

                btnServidor.Text =
                    "INICIAR RED LOCAL";

                btnServidor.BackColor =
                    Color.FromArgb(180, 40, 40);

                lblEstado.Text =
                    wifiDirect.UltimoEstado;

                lblEstado.BackColor =
                    Color.FromArgb(150, 30, 30);


                MessageBox.Show(
                    "Windows no pudo iniciar Wi-Fi Direct.\n\n" +
                    wifiDirect.UltimoEstado,
                    "Wi-Fi Direct",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }


        // =========================================================
        // ESPERAR IP WI-FI DIRECT
        // =========================================================

        private void EsperaRed_Tick(
            object sender,
            EventArgs e)
        {
            intentosRed++;


            string ip =
                ObtenerIpWifiDirect();


            if (!string.IsNullOrWhiteSpace(ip))
            {
                esperaRed.Stop();

                IniciarServidorLocal(ip);

                return;
            }


            // 20 intentos x 500 ms = aproximadamente 10 segundos.
            if (intentosRed >= 20)
            {
                esperaRed.Stop();

                btnServidor.Enabled = true;

                btnServidor.Text =
                    "REINTENTAR SERVIDOR";

                lblEstado.Text =
                    "TORNEO ACTIVA, PERO NO SE ENCONTRÓ LA IP LOCAL";

                lblEstado.BackColor =
                    Color.FromArgb(150, 80, 0);


                MessageBox.Show(
                    "La red TORNEO se inició, pero Windows todavía " +
                    "no mostró la dirección IPv4 del adaptador.\n\n" +
                    "Comprueba que TORNEO esté visible y vuelve a intentar.",
                    "Red local",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
            }
        }


        // =========================================================
        // SERVIDOR + QR
        // =========================================================

        private void IniciarServidorLocal(
            string ip)
        {
            try
            {
                ipLocal = ip;


                server.Start(Puerto);


                string url1 =
                    "http://" +
                    ipLocal +
                    ":" +
                    Puerto +
                    "/juez.html?token=" +
                    server.Judge1Token;


                string url2 =
                    "http://" +
                    ipLocal +
                    ":" +
                    Puerto +
                    "/juez.html?token=" +
                    server.Judge2Token;


                if (qrJuez1.Image != null)
                {
                    Image anterior =
                        qrJuez1.Image;

                    qrJuez1.Image = null;

                    anterior.Dispose();
                }


                if (qrJuez2.Image != null)
                {
                    Image anterior =
                        qrJuez2.Image;

                    qrJuez2.Image = null;

                    anterior.Dispose();
                }


                qrJuez1.Image =
                    CrearQr(url1);

                qrJuez2.Image =
                    CrearQr(url2);


                lblUrlJuez1.Text =
                    "http://" +
                    ipLocal +
                    ":" +
                    Puerto +
                    "\nJUEZ 1";


                lblUrlJuez2.Text =
                    "http://" +
                    ipLocal +
                    ":" +
                    Puerto +
                    "\nJUEZ 2";


                servidorIniciado = true;


                btnServidor.Text =
                    "RED TORNEO ACTIVA - " +
                    ipLocal;

                btnServidor.BackColor =
                    Color.FromArgb(0, 125, 70);

                btnServidor.Enabled =
                    false;


                lblEstado.Text =
                    "RED LOCAL LISTA - CONECTA LOS DOS JUECES";

                lblEstado.BackColor =
                    Color.FromArgb(0, 100, 65);


                reloj.Start();

                MostrarConexion();

                ActualizarJueces();
            }
            catch (Exception ex)
            {
                servidorIniciado = false;

                ipLocal = "";

                btnServidor.Enabled = true;

                btnServidor.Text =
                    "REINTENTAR SERVIDOR";

                lblEstado.Text =
                    "ERROR AL INICIAR EL SERVIDOR";

                lblEstado.BackColor =
                    Color.FromArgb(150, 30, 30);


                MessageBox.Show(
                    "TORNEO está activa, pero no se pudo iniciar " +
                    "el servidor local.\n\n" +
                    ex.Message,
                    "Servidor local",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }


        // =========================================================
        // COMBATE
        // =========================================================

        private void BtnIniciar_Click(
            object sender,
            EventArgs e)
        {
            if (!servidorIniciado ||
                string.IsNullOrWhiteSpace(ipLocal))
            {
                MostrarConexion();

                MessageBox.Show(
                    "Primero inicia la red local.",
                    "Taekwondo",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );

                return;
            }


            if (!server.Judge1Connected ||
                !server.Judge2Connected)
            {
                MessageBox.Show(
                    "Deben estar conectados los dos jueces " +
                    "antes de iniciar el combate.",
                    "Jueces",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );

                MostrarConexion();

                return;
            }


            server.StartMatch();

            ActualizarPantalla();
        }


        private void BtnPausa_Click(
            object sender,
            EventArgs e)
        {
            if (!server.CurrentActive)
            {
                return;
            }

            server.TogglePause();

            ActualizarPantalla();
        }


        private void BtnNuevo_Click(
            object sender,
            EventArgs e)
        {
            DialogResult resultado =
                MessageBox.Show(
                    "¿Iniciar un nuevo combate?\n\n" +
                    "Se borrarán puntos y rondas.",
                    "Nuevo combate",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );


            if (resultado != DialogResult.Yes)
            {
                return;
            }


            server.NewMatch();

            ActualizarPantalla();
        }


        private void BtnConexion_Click(
            object sender,
            EventArgs e)
        {
            if (panelConexion.Visible)
            {
                if (server.Judge1Connected &&
                    server.Judge2Connected)
                {
                    panelConexion.Visible =
                        false;
                }
            }
            else
            {
                MostrarConexion();
            }
        }


        // =========================================================
        // TIMER
        // =========================================================

        private void Reloj_Tick(
            object sender,
            EventArgs e)
        {
            server.TickSecond();

            server.CheckDifference();

            ActualizarPantalla();
        }


        // =========================================================
        // ACTUALIZAR PANTALLA
        // =========================================================

        private void ActualizarPantalla()
        {
            if (InvokeRequired)
            {
                BeginInvoke(
                    new Action(
                        ActualizarPantalla
                    )
                );

                return;
            }


            lblPuntosAzul.Text =
                FormatearPuntos(
                    server.CurrentBlue
                );


            lblPuntosRojo.Text =
                FormatearPuntos(
                    server.CurrentRed
                );


            lblRondasAzul.Text =
                "RONDAS GANADAS: " +
                server.CurrentBlueRounds;


            lblRondasRojo.Text =
                "RONDAS GANADAS: " +
                server.CurrentRedRounds;


            lblTiempo.Text =
                FormatearTiempo(
                    server.CurrentSeconds
                );


            if (server.IsGoldenPoint)
            {
                lblRonda.Text =
                    "PUNTO DE ORO";
            }
            else
            {
                lblRonda.Text =
                    "RONDA " +
                    server.CurrentRound;
            }


            if (server.CurrentActive)
            {
                lblEstado.Text =
                    server.CurrentStatus;


                if (server.IsPaused)
                {
                    lblEstado.BackColor =
                        Color.FromArgb(190, 105, 0);
                }
                else if (server.IsResting)
                {
                    lblEstado.BackColor =
                        Color.FromArgb(90, 75, 15);
                }
                else if (server.IsGoldenPoint)
                {
                    lblEstado.BackColor =
                        Color.FromArgb(150, 110, 0);
                }
                else
                {
                    lblEstado.BackColor =
                        Color.FromArgb(0, 100, 65);
                }
            }
            else if (servidorIniciado)
            {
                lblEstado.Text =
                    "RED LOCAL LISTA - CONECTA LOS DOS JUECES";

                lblEstado.BackColor =
                    Color.FromArgb(0, 100, 65);
            }


            btnPausa.Text =
                server.IsPaused
                ? "REANUDAR"
                : "PAUSA";


            btnPausa.BackColor =
                server.IsPaused
                ? Color.FromArgb(0, 125, 70)
                : Color.FromArgb(190, 105, 0);


            btnPausa.Enabled =
                server.CurrentActive;


            ActualizarJueces();
        }


        // =========================================================
        // ESTADO DE JUECES
        // =========================================================

        private void ActualizarJueces()
        {
            bool juez1 =
                server.Judge1Connected;

            bool juez2 =
                server.Judge2Connected;


            if (juez1)
            {
                lblConexionJuez1.Text =
                    "JUEZ 1  ● CONECTADO";

                lblConexionJuez1.ForeColor =
                    Color.LimeGreen;

                qrJuez1.Visible =
                    false;

                lblUrlJuez1.Visible =
                    false;
            }
            else
            {
                lblConexionJuez1.Text =
                    "JUEZ 1  ● DESCONECTADO";

                lblConexionJuez1.ForeColor =
                    Color.OrangeRed;

                qrJuez1.Visible =
                    true;

                lblUrlJuez1.Visible =
                    true;
            }


            if (juez2)
            {
                lblConexionJuez2.Text =
                    "JUEZ 2  ● CONECTADO";

                lblConexionJuez2.ForeColor =
                    Color.LimeGreen;

                qrJuez2.Visible =
                    false;

                lblUrlJuez2.Visible =
                    false;
            }
            else
            {
                lblConexionJuez2.Text =
                    "JUEZ 2  ● DESCONECTADO";

                lblConexionJuez2.ForeColor =
                    Color.OrangeRed;

                qrJuez2.Visible =
                    true;

                lblUrlJuez2.Visible =
                    true;
            }


            if (juez1 && juez2)
            {
                panelConexion.Visible =
                    false;
            }
            else if (servidorIniciado)
            {
                panelConexion.Visible =
                    true;

                panelConexion.BringToFront();

                CentrarPanelConexion();
            }
        }


        private void MostrarConexion()
        {
            panelConexion.Visible =
                true;

            panelConexion.BringToFront();

            CentrarPanelConexion();
        }


        private void Server_JudgeConnectionsChanged(
            object sender,
            EventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(
                    new Action(
                        ActualizarJueces
                    )
                );

                return;
            }

            ActualizarJueces();
        }


        // =========================================================
        // F11
        // =========================================================

        private void MainForm_KeyDown(
            object sender,
            KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F11)
            {
                CambiarPantallaCompleta();

                e.Handled = true;
            }


            if (e.KeyCode == Keys.Escape &&
                pantallaCompleta)
            {
                CambiarPantallaCompleta();

                e.Handled = true;
            }
        }


        private void CambiarPantallaCompleta()
        {
            if (!pantallaCompleta)
            {
                bordeAnterior =
                    FormBorderStyle;

                estadoAnterior =
                    WindowState;


                FormBorderStyle =
                    FormBorderStyle.None;

                WindowState =
                    FormWindowState.Maximized;

                TopMost = true;

                pantallaCompleta = true;
            }
            else
            {
                TopMost = false;

                FormBorderStyle =
                    bordeAnterior;

                WindowState =
                    estadoAnterior;

                pantallaCompleta = false;
            }
        }


        // =========================================================
        // CONTROLES AUXILIARES
        // =========================================================

        private Label CrearLabel(
            string texto,
            float tamano,
            Color color)
        {
            Label label =
                new Label();

            label.Text =
                texto;

            label.Dock =
                DockStyle.Fill;

            label.TextAlign =
                ContentAlignment.MiddleCenter;

            label.ForeColor =
                color;

            label.BackColor =
                Color.Transparent;

            label.Font =
                new Font(
                    "Segoe UI",
                    tamano,
                    FontStyle.Bold
                );

            return label;
        }


        private Button CrearBoton(
            string texto,
            Color color)
        {
            Button boton =
                new Button();

            boton.Text =
                texto;

            boton.Dock =
                DockStyle.Fill;

            boton.Margin =
                new Padding(6);

            boton.FlatStyle =
                FlatStyle.Flat;

            boton.FlatAppearance.BorderSize =
                0;

            boton.BackColor =
                color;

            boton.ForeColor =
                Color.White;

            boton.Font =
                new Font(
                    "Segoe UI",
                    15,
                    FontStyle.Bold
                );

            boton.Cursor =
                Cursors.Hand;

            return boton;
        }


        private PictureBox CrearPictureBox()
        {
            PictureBox picture =
                new PictureBox();

            picture.Dock =
                DockStyle.Fill;

            picture.SizeMode =
                PictureBoxSizeMode.Zoom;

            picture.BackColor =
                Color.White;

            picture.Margin =
                new Padding(10);

            return picture;
        }


        // =========================================================
        // QR
        // =========================================================

        private Bitmap CrearQr(
            string texto)
        {
            using (
                QRCodeGenerator generator =
                    new QRCodeGenerator()
            )
            {
                using (
                    QRCodeData data =
                        generator.CreateQrCode(
                            texto,
                            QRCodeGenerator.ECCLevel.Q
                        )
                )
                {
                    using (
                        QRCode qr =
                            new QRCode(data)
                    )
                    {
                        return qr.GetGraphic(8);
                    }
                }
            }
        }


        // =========================================================
        // IP WI-FI DIRECT
        // =========================================================

        private string ObtenerIpWifiDirect()
        {
            try
            {
                // Nuestra red TORNEO comprobada usa
                // 192.168.137.1 en el adaptador Wi-Fi Direct.
                foreach (
                    NetworkInterface adaptador
                    in NetworkInterface.GetAllNetworkInterfaces()
                )
                {
                    if (adaptador.OperationalStatus !=
                        OperationalStatus.Up)
                    {
                        continue;
                    }


                    foreach (
                        var direccion
                        in adaptador
                            .GetIPProperties()
                            .UnicastAddresses
                    )
                    {
                        if (direccion.Address.AddressFamily !=
                            AddressFamily.InterNetwork)
                        {
                            continue;
                        }


                        string ip =
                            direccion.Address.ToString();


                        if (ip ==
                            "192.168.137.1")
                        {
                            return ip;
                        }
                    }
                }


                return "";
            }
            catch
            {
                return "";
            }
        }


        // =========================================================
        // FORMATOS
        // =========================================================

        private string FormatearTiempo(
            int segundos)
        {
            if (segundos < 0)
            {
                segundos = 0;
            }


            return
                (segundos / 60) +
                ":" +
                (segundos % 60)
                    .ToString("00");
        }


        private string FormatearPuntos(
            double puntos)
        {
            if (Math.Abs(
                    puntos -
                    Math.Round(puntos)
                ) < 0.0001)
            {
                return
                    ((int)Math.Round(puntos))
                    .ToString();
            }


            return puntos.ToString("0.0");
        }


        // =========================================================
        // CIERRE
        // =========================================================

        protected override void OnFormClosing(
            FormClosingEventArgs e)
        {
            try
            {
                reloj.Stop();
                esperaRed.Stop();

                server.Stop();

                if (wifiDirect != null)
                {
                    wifiDirect.Detener();
                }


                if (qrJuez1 != null &&
                    qrJuez1.Image != null)
                {
                    qrJuez1.Image.Dispose();
                    qrJuez1.Image = null;
                }


                if (qrJuez2 != null &&
                    qrJuez2.Image != null)
                {
                    qrJuez2.Image.Dispose();
                    qrJuez2.Image = null;
                }
            }
            catch
            {
            }

            base.OnFormClosing(e);
        }
    }
}