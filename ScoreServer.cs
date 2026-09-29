using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace TaekwondoScoreboard
{
    public sealed class ScoreServer
    {
        private HttpListener listener;

        private readonly Dictionary<int, WebSocket> judges =
            new Dictionary<int, WebSocket>();

        private readonly List<JudgeAction> pending =
            new List<JudgeAction>();

        private readonly object sync =
            new object();

        private int port;

        private const int MatchWindowMilliseconds = 1000;
        private const int RoundSeconds = 120;
        private const int RestSeconds = 60;

        private int secondsRemaining = RoundSeconds;

        private int currentRound = 1;

        private int blueRounds = 0;
        private int redRounds = 0;

        private double blueScore = 0;
        private double redScore = 0;

        private bool matchActive = false;
        private bool resting = false;
        private bool goldenPoint = false;
        private bool paused = false;

        private string status =
            "Servidor detenido";

        private readonly string judge1Token =
            "JUEZ1-TKD-2026";

        private readonly string judge2Token =
            "JUEZ2-TKD-2026";


        public event EventHandler JudgeConnectionsChanged;


        // =========================================================
        // PROPIEDADES
        // =========================================================

        public double CurrentBlue
        {
            get
            {
                lock (sync)
                {
                    return blueScore;
                }
            }
        }


        public double CurrentRed
        {
            get
            {
                lock (sync)
                {
                    return redScore;
                }
            }
        }


        public int CurrentRound
        {
            get
            {
                lock (sync)
                {
                    return currentRound;
                }
            }
        }


        public int CurrentBlueRounds
        {
            get
            {
                lock (sync)
                {
                    return blueRounds;
                }
            }
        }


        public int CurrentRedRounds
        {
            get
            {
                lock (sync)
                {
                    return redRounds;
                }
            }
        }


        public int CurrentSeconds
        {
            get
            {
                lock (sync)
                {
                    return secondsRemaining;
                }
            }
        }


        public bool CurrentActive
        {
            get
            {
                lock (sync)
                {
                    return matchActive;
                }
            }
        }


        public bool IsResting
        {
            get
            {
                lock (sync)
                {
                    return resting;
                }
            }
        }


        public bool IsGoldenPoint
        {
            get
            {
                lock (sync)
                {
                    return goldenPoint;
                }
            }
        }


        public bool IsPaused
        {
            get
            {
                lock (sync)
                {
                    return paused;
                }
            }
        }


        public string CurrentStatus
        {
            get
            {
                lock (sync)
                {
                    return status;
                }
            }
        }


        public string Judge1Token
        {
            get
            {
                return judge1Token;
            }
        }


        public string Judge2Token
        {
            get
            {
                return judge2Token;
            }
        }


        public bool Judge1Connected
        {
            get
            {
                lock (sync)
                {
                    return judges.ContainsKey(1);
                }
            }
        }


        public bool Judge2Connected
        {
            get
            {
                lock (sync)
                {
                    return judges.ContainsKey(2);
                }
            }
        }


        // =========================================================
        // SERVIDOR
        // =========================================================

        public void Start(int serverPort)
        {
            port = serverPort;

            if (listener != null &&
                listener.IsListening)
            {
                return;
            }

            listener =
                new HttpListener();

            // IMPORTANTE:
            // usamos + porque es el prefijo que autorizamos
            // anteriormente con netsh.
            listener.Prefixes.Add(
                "http://+:" + port + "/"
            );

            listener.Start();

            lock (sync)
            {
                status =
                    "Servidor activo";
            }

            Task.Run(
                () => EscucharClientes()
            );
        }


        public void Stop()
        {
            try
            {
                if (listener != null)
                {
                    listener.Stop();
                    listener.Close();
                    listener = null;
                }
            }
            catch
            {
            }


            lock (sync)
            {
                matchActive = false;
                resting = false;
                goldenPoint = false;
                paused = false;

                status =
                    "Servidor detenido";

                foreach (
                    WebSocket socket
                    in judges.Values)
                {
                    try
                    {
                        socket.Abort();
                    }
                    catch
                    {
                    }
                }

                judges.Clear();
                pending.Clear();
            }

            NotificarConexiones();
        }


        private async Task EscucharClientes()
        {
            while (
                listener != null &&
                listener.IsListening)
            {
                HttpListenerContext context =
                    null;

                try
                {
                    context =
                        await listener
                            .GetContextAsync();
                }
                catch
                {
                    break;
                }


                if (context == null)
                {
                    continue;
                }


                if (
                    context.Request
                        .IsWebSocketRequest &&
                    context.Request.Url
                        .AbsolutePath
                        .Equals(
                            "/ws",
                            StringComparison
                                .OrdinalIgnoreCase))
                {
                    _ = Task.Run(
                        () =>
                            ProcesarWebSocket(
                                context)
                    );

                    continue;
                }


                ServirArchivo(context);
            }
        }


        // =========================================================
        // ARCHIVOS WEB
        // =========================================================

        private void ServirArchivo(
            HttpListenerContext context)
        {
            try
            {
                string path =
                    context.Request.Url
                        .AbsolutePath;


                if (
                    string.IsNullOrWhiteSpace(path) ||
                    path == "/")
                {
                    path =
                        "/juez.html";
                }


                path =
                    path
                        .TrimStart('/')
                        .Replace(
                            "/",
                            Path.DirectorySeparatorChar
                                .ToString()
                        );


                string root =
                    Path.Combine(
                        AppDomain
                            .CurrentDomain
                            .BaseDirectory,
                        "wwwroot"
                    );


                string archivo =
                    Path.Combine(
                        root,
                        path
                    );


                if (!File.Exists(archivo))
                {
                    context.Response
                        .StatusCode = 404;

                    context.Response
                        .Close();

                    return;
                }


                byte[] datos =
                    File.ReadAllBytes(
                        archivo
                    );


                context.Response.ContentType =
                    ObtenerContentType(
                        archivo
                    );


                context.Response.ContentLength64 =
                    datos.Length;


                context.Response.OutputStream
                    .Write(
                        datos,
                        0,
                        datos.Length
                    );


                context.Response.OutputStream
                    .Close();
            }
            catch
            {
                try
                {
                    context.Response
                        .StatusCode = 500;

                    context.Response
                        .Close();
                }
                catch
                {
                }
            }
        }


        private string ObtenerContentType(
            string archivo)
        {
            string extension =
                Path.GetExtension(
                    archivo
                ).ToLowerInvariant();


            switch (extension)
            {
                case ".html":
                    return
                        "text/html; charset=utf-8";

                case ".css":
                    return
                        "text/css; charset=utf-8";

                case ".js":
                    return
                        "application/javascript; charset=utf-8";

                case ".json":
                    return
                        "application/json; charset=utf-8";

                case ".png":
                    return
                        "image/png";

                case ".jpg":
                case ".jpeg":
                    return
                        "image/jpeg";

                case ".svg":
                    return
                        "image/svg+xml";

                default:
                    return
                        "application/octet-stream";
            }
        }


        // =========================================================
        // WEBSOCKET
        // =========================================================

        private async Task ProcesarWebSocket(
            HttpListenerContext context)
        {
            WebSocket socket = null;

            int judgeNumber = 0;


            try
            {
                HttpListenerWebSocketContext
                    wsContext =
                        await context
                            .AcceptWebSocketAsync(
                                null
                            );


                socket =
                    wsContext.WebSocket;


                byte[] buffer =
                    new byte[8192];


                StringBuilder recibido =
                    new StringBuilder();


                while (
                    socket.State ==
                    WebSocketState.Open)
                {
                    WebSocketReceiveResult
                        result =
                            await socket
                                .ReceiveAsync(
                                    new ArraySegment<byte>(
                                        buffer
                                    ),
                                    CancellationToken.None
                                );


                    if (
                        result.MessageType ==
                        WebSocketMessageType.Close)
                    {
                        break;
                    }


                    string texto =
                        Encoding.UTF8
                            .GetString(
                                buffer,
                                0,
                                result.Count
                            );


                    recibido.Append(texto);


                    if (!result.EndOfMessage)
                    {
                        continue;
                    }


                    string mensaje =
                        recibido.ToString();


                    recibido.Clear();


                    string tipo =
                        ObtenerString(
                            mensaje,
                            "type"
                        );


                    // =============================================
                    // IDENTIFICACIÓN DEL JUEZ
                    // =============================================

                    if (tipo == "hello")
                    {
                        string token =
                            ObtenerString(
                                mensaje,
                                "token"
                            );


                        judgeNumber =
                            ObtenerJuezPorToken(
                                token
                            );


                        if (judgeNumber == 0)
                        {
                            await EnviarTexto(
                                socket,
                                CrearMensaje(
                                    "message",
                                    "Código de juez no válido."
                                )
                            );


                            await CerrarSocket(
                                socket
                            );

                            return;
                        }


                        lock (sync)
                        {
                            if (
                                judges.ContainsKey(
                                    judgeNumber))
                            {
                                try
                                {
                                    judges[
                                        judgeNumber
                                    ].Abort();
                                }
                                catch
                                {
                                }
                            }


                            judges[judgeNumber] =
                                socket;
                        }


                        NotificarConexiones();


                        await EnviarTexto(
                            socket,
                            CrearMensajeHello(
                                judgeNumber
                            )
                        );


                        await EnviarEstado(
                            socket
                        );


                        continue;
                    }


                    // =============================================
                    // ACCIÓN DEL JUEZ
                    // =============================================

                    if (tipo == "action")
                    {
                        if (judgeNumber == 0)
                        {
                            continue;
                        }


                        string side =
                            ObtenerString(
                                mensaje,
                                "side"
                            );


                        string action =
                            ObtenerString(
                                mensaje,
                                "action"
                            );


                        RegistrarAction(
                            judgeNumber,
                            side,
                            action
                        );
                    }
                }
            }
            catch
            {
            }
            finally
            {
                if (socket != null)
                {
                    lock (sync)
                    {
                        if (
                            judgeNumber != 0 &&
                            judges.ContainsKey(
                                judgeNumber) &&
                            judges[judgeNumber] ==
                                socket)
                        {
                            judges.Remove(
                                judgeNumber
                            );
                        }
                    }


                    try
                    {
                        socket.Dispose();
                    }
                    catch
                    {
                    }


                    NotificarConexiones();
                }
            }
        }


        private int ObtenerJuezPorToken(
            string token)
        {
            if (
                string.Equals(
                    token,
                    judge1Token,
                    StringComparison.Ordinal))
            {
                return 1;
            }


            if (
                string.Equals(
                    token,
                    judge2Token,
                    StringComparison.Ordinal))
            {
                return 2;
            }


            return 0;
        }


        // =========================================================
        // ACCIONES DE LOS JUECES
        // =========================================================

        private void RegistrarAction(
            int judge,
            string side,
            string action)
        {
            ActionType actionType;


            if (
                !TryParseAction(
                    action,
                    out actionType))
            {
                return;
            }


            if (
                side != "blue" &&
                side != "red")
            {
                return;
            }


            JudgeAction nueva =
                new JudgeAction
                {
                    Judge = judge,
                    Side = side,
                    Action = actionType,
                    TimeUtc =
                        DateTime.UtcNow
                };


            lock (sync)
            {
                // No se permiten puntos:
                // - antes del combate
                // - durante descanso
                // - durante pausa
                if (
                    !matchActive ||
                    resting ||
                    paused)
                {
                    return;
                }


                LimpiarAccionesAntiguas();


                JudgeAction encontrada =
                    null;


                foreach (
                    JudgeAction pendiente
                    in pending)
                {
                    if (
                        pendiente.Judge != judge &&
                        pendiente.Side ==
                            nueva.Side &&
                        pendiente.Action ==
                            nueva.Action)
                    {
                        double diferencia =
                            Math.Abs(
                                (
                                    nueva.TimeUtc -
                                    pendiente.TimeUtc
                                )
                                .TotalMilliseconds
                            );


                        if (
                            diferencia <=
                            MatchWindowMilliseconds)
                        {
                            encontrada =
                                pendiente;

                            break;
                        }
                    }
                }


                if (encontrada != null)
                {
                    pending.Remove(
                        encontrada
                    );


                    AplicarPunto(
                        nueva.Side,
                        nueva.Action
                    );


                    return;
                }


                pending.Add(nueva);
            }
        }


        private void LimpiarAccionesAntiguas()
        {
            DateTime limite =
                DateTime.UtcNow
                    .AddMilliseconds(
                        -MatchWindowMilliseconds
                    );


            pending.RemoveAll(
                a =>
                    a.TimeUtc < limite
            );
        }


        private bool TryParseAction(
            string texto,
            out ActionType action)
        {
            switch (texto)
            {
                case "Punch1":
                    action =
                        ActionType.Punch1;
                    return true;

                case "Body2":
                    action =
                        ActionType.Body2;
                    return true;

                case "Head3":
                    action =
                        ActionType.Head3;
                    return true;

                case "SpinHead4":
                    action =
                        ActionType.SpinHead4;
                    return true;

                // Lo conservamos internamente.
                // Después lo ocultaremos del celular.
                case "KyongGo":
                    action =
                        ActionType.KyongGo;
                    return true;

                case "GamJeom":
                    action =
                        ActionType.GamJeom;
                    return true;

                default:
                    action =
                        ActionType.Punch1;
                    return false;
            }
        }


        private void AplicarPunto(
            string side,
            ActionType action)
        {
            switch (action)
            {
                case ActionType.Punch1:

                    Sumar(
                        side,
                        1
                    );

                    break;


                case ActionType.Body2:

                    Sumar(
                        side,
                        2
                    );

                    break;


                case ActionType.Head3:

                    Sumar(
                        side,
                        3
                    );

                    break;


                case ActionType.SpinHead4:

                    Sumar(
                        side,
                        4
                    );

                    break;


                case ActionType.KyongGo:

                    // Se conserva internamente,
                    // pero después no tendrá botón.
                    Sumar(
                        OtroLado(side),
                        0.5
                    );

                    break;


                case ActionType.GamJeom:

                    // Penalización al competidor seleccionado:
                    // +1 para el oponente.
                    Sumar(
                        OtroLado(side),
                        1
                    );

                    break;
            }


            EnviarEstadoATodos();


            // En punto de oro,
            // la primera acción coincidente
            // termina el combate.
            if (goldenPoint)
            {
                string ganador =
                    ObtenerGanadorPorAccion(
                        side,
                        action
                    );

                WinRound(
                    ganador
                );

                return;
            }


            CheckDifferenceInterno();
        }


        private string ObtenerGanadorPorAccion(
            string side,
            ActionType action)
        {
            if (
                action ==
                    ActionType.KyongGo ||
                action ==
                    ActionType.GamJeom)
            {
                return
                    OtroLado(side);
            }


            return side;
        }


        private void Sumar(
            string side,
            double puntos)
        {
            if (side == "blue")
            {
                blueScore += puntos;
            }
            else
            {
                redScore += puntos;
            }
        }


        private string OtroLado(
            string side)
        {
            return
                side == "blue"
                    ? "red"
                    : "blue";
        }


        // =========================================================
        // INICIO / NUEVO COMBATE
        // =========================================================

        public void StartMatch()
        {
            lock (sync)
            {
                blueScore = 0;
                redScore = 0;

                blueRounds = 0;
                redRounds = 0;

                currentRound = 1;

                secondsRemaining =
                    RoundSeconds;

                resting = false;
                goldenPoint = false;
                paused = false;

                matchActive = true;

                pending.Clear();

                status =
                    "RONDA 1 - COMBATE EN CURSO";
            }


            EnviarEstadoATodos();
        }


        public void NewMatch()
        {
            lock (sync)
            {
                blueScore = 0;
                redScore = 0;

                blueRounds = 0;
                redRounds = 0;

                currentRound = 1;

                secondsRemaining =
                    RoundSeconds;

                resting = false;
                goldenPoint = false;
                paused = false;

                matchActive = false;

                pending.Clear();

                status =
                    "Listo para nuevo combate";
            }


            EnviarEstadoATodos();
        }


        // =========================================================
        // PAUSA / REANUDAR
        // =========================================================

        public void TogglePause()
        {
            lock (sync)
            {
                if (!matchActive)
                {
                    return;
                }


                paused =
                    !paused;


                // Borramos coincidencias pendientes
                // para que una pulsación anterior
                // a la pausa no pueda contar después.
                pending.Clear();


                if (paused)
                {
                    if (resting)
                    {
                        status =
                            "DESCANSO EN PAUSA";
                    }
                    else
                    {
                        status =
                            "COMBATE EN PAUSA";
                    }
                }
                else
                {
                    if (resting)
                    {
                        if (currentRound >= 4)
                        {
                            status =
                                "DESCANSO - SIGUIENTE: PUNTO DE ORO";
                        }
                        else
                        {
                            status =
                                "DESCANSO - 1 MINUTO";
                        }
                    }
                    else if (goldenPoint)
                    {
                        status =
                            "PUNTO DE ORO - PRIMER PUNTO GANA";
                    }
                    else
                    {
                        status =
                            "RONDA " +
                            currentRound +
                            " - COMBATE EN CURSO";
                    }
                }
            }


            EnviarEstadoATodos();
        }


        // =========================================================
        // RELOJ
        // =========================================================

        public void TickSecond()
        {
            bool enviarEstado =
                false;


            lock (sync)
            {
                if (!matchActive)
                {
                    return;
                }


                // Pausa completa:
                // no avanza ronda ni descanso.
                if (paused)
                {
                    return;
                }


                // =============================================
                // DESCANSO
                // =============================================

                if (resting)
                {
                    if (secondsRemaining > 0)
                    {
                        secondsRemaining--;
                    }


                    if (secondsRemaining <= 0)
                    {
                        TerminarDescansoInterno();
                    }


                    enviarEstado =
                        true;
                }


                // =============================================
                // PUNTO DE ORO
                // =============================================

                else if (goldenPoint)
                {
                    // No descontamos tiempo.
                    // Esperamos el primer punto
                    // confirmado por ambos jueces.

                    enviarEstado =
                        true;
                }


                // =============================================
                // RONDA NORMAL
                // =============================================

                else
                {
                    if (secondsRemaining > 0)
                    {
                        secondsRemaining--;
                    }


                    if (secondsRemaining <= 0)
                    {
                        TerminarRondaPorTiempo();
                    }


                    enviarEstado =
                        true;
                }
            }


            if (enviarEstado)
            {
                EnviarEstadoATodos();
            }
        }


        // =========================================================
        // DIFERENCIA DE 12
        // =========================================================

        public void CheckDifference()
        {
            lock (sync)
            {
                if (
                    !matchActive ||
                    resting ||
                    goldenPoint ||
                    paused)
                {
                    return;
                }


                CheckDifferenceInterno();
            }
        }


        private void CheckDifferenceInterno()
        {
            if (
                Math.Abs(
                    blueScore -
                    redScore
                ) >= 12)
            {
                if (
                    blueScore >
                    redScore)
                {
                    WinRound(
                        "blue"
                    );
                }
                else
                {
                    WinRound(
                        "red"
                    );
                }
            }
        }


        // =========================================================
        // FIN DE RONDA
        // =========================================================

        private void TerminarRondaPorTiempo()
        {
            if (
                blueScore >
                redScore)
            {
                WinRound(
                    "blue"
                );

                return;
            }


            if (
                redScore >
                blueScore)
            {
                WinRound(
                    "red"
                );

                return;
            }


            // Si termina empatada una ronda normal,
            // por ahora la dejamos marcada para revisión.
            //
            // Si es la tercera ronda y sigue empatado
            // el combate, preparamos punto de oro.

            if (currentRound >= 3)
            {
                IniciarDescansoInterno(
                    true
                );

                return;
            }


            status =
                "RONDA EMPATADA - REVISAR";

            resting =
                true;

            secondsRemaining =
                RestSeconds;

            pending.Clear();
        }


        // =========================================================
        // GANADOR DE RONDA
        // =========================================================

        private void WinRound(
            string winner)
        {
            lock (sync)
            {
                if (
                    winner == "blue")
                {
                    blueRounds++;
                }
                else if (
                    winner == "red")
                {
                    redRounds++;
                }
                else
                {
                    return;
                }


                pending.Clear();


                // =============================================
                // PUNTO DE ORO
                // =============================================

                if (goldenPoint)
                {
                    status =
                        winner == "blue"
                            ? "AZUL GANA POR PUNTO DE ORO"
                            : "ROJO GANA POR PUNTO DE ORO";


                    matchActive =
                        false;

                    resting =
                        false;

                    goldenPoint =
                        false;

                    paused =
                        false;


                    EnviarEstadoATodos();

                    return;
                }


                // =============================================
                // 2 RONDAS GANADAS
                // =============================================

                if (blueRounds >= 2)
                {
                    status =
                        "AZUL GANA EL COMBATE";

                    matchActive =
                        false;

                    resting =
                        false;

                    goldenPoint =
                        false;

                    paused =
                        false;


                    EnviarEstadoATodos();

                    return;
                }


                if (redRounds >= 2)
                {
                    status =
                        "ROJO GANA EL COMBATE";

                    matchActive =
                        false;

                    resting =
                        false;

                    goldenPoint =
                        false;

                    paused =
                        false;


                    EnviarEstadoATodos();

                    return;
                }


                // =============================================
                // PREPARAR SIGUIENTE RONDA
                // =============================================

                blueScore = 0;
                redScore = 0;


                status =
                    winner == "blue"
                        ? "AZUL GANA LA RONDA"
                        : "ROJO GANA LA RONDA";


                IniciarDescansoInterno(
                    false
                );
            }
        }


        // =========================================================
        // DESCANSO
        // =========================================================

        private void IniciarDescansoInterno(
            bool prepararGoldenPoint)
        {
            resting =
                true;

            paused =
                false;

            pending.Clear();


            // Ahora el descanso usa el mismo
            // contador visible de la aplicación.
            secondsRemaining =
                RestSeconds;


            if (prepararGoldenPoint)
            {
                currentRound = 4;

                goldenPoint =
                    false;

                status =
                    "DESCANSO - SIGUIENTE: PUNTO DE ORO";
            }
            else
            {
                currentRound++;

                goldenPoint =
                    false;

                status =
                    "DESCANSO - 1 MINUTO";
            }
        }


        private void TerminarDescansoInterno()
        {
            resting =
                false;

            paused =
                false;

            blueScore = 0;
            redScore = 0;

            pending.Clear();


            if (currentRound >= 4)
            {
                goldenPoint =
                    true;

                // Mostramos 0:00 porque en nuestro
                // punto de oro no usamos cuenta regresiva.
                secondsRemaining =
                    0;

                status =
                    "PUNTO DE ORO - PRIMER PUNTO GANA";
            }
            else
            {
                goldenPoint =
                    false;

                secondsRemaining =
                    RoundSeconds;

                status =
                    "RONDA " +
                    currentRound +
                    " - COMBATE EN CURSO";
            }
        }


        // =========================================================
        // ENVÍO DE ESTADO
        // =========================================================

        private async Task EnviarEstado(
            WebSocket socket)
        {
            if (
                socket == null ||
                socket.State !=
                    WebSocketState.Open)
            {
                return;
            }


            string mensaje =
                CrearEstadoJson();


            await EnviarTexto(
                socket,
                mensaje
            );
        }


        private void EnviarEstadoATodos()
        {
            List<WebSocket> sockets;


            lock (sync)
            {
                sockets =
                    new List<WebSocket>(
                        judges.Values
                    );
            }


            foreach (
                WebSocket socket
                in sockets)
            {
                _ = EnviarEstado(
                    socket
                );
            }
        }


        private async Task EnviarTexto(
            WebSocket socket,
            string texto)
        {
            if (
                socket == null ||
                socket.State !=
                    WebSocketState.Open)
            {
                return;
            }


            byte[] datos =
                Encoding.UTF8
                    .GetBytes(
                        texto
                    );


            try
            {
                await socket.SendAsync(
                    new ArraySegment<byte>(
                        datos
                    ),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None
                );
            }
            catch
            {
            }
        }


        // =========================================================
        // JSON
        // =========================================================

        private string CrearMensajeHello(
            int judge)
        {
            return
                "{"
                + "\"type\":\"hello\","
                + "\"judge\":"
                + judge
                + ","
                + "\"state\":"
                + CrearEstadoJson()
                + "}";
        }


        private string CrearMensaje(
            string tipo,
            string texto)
        {
            return
                "{"
                + "\"type\":\""
                + EscaparJson(tipo)
                + "\","
                + "\"text\":\""
                + EscaparJson(texto)
                + "\""
                + "}";
        }


        private string CrearEstadoJson()
        {
            lock (sync)
            {
                return
                    "{"
                    + "\"type\":\"state\","

                    + "\"round\":"
                    + currentRound
                    + ","

                    + "\"blueScore\":"
                    + NumeroJson(
                        blueScore
                    )
                    + ","

                    + "\"redScore\":"
                    + NumeroJson(
                        redScore
                    )
                    + ","

                    + "\"blueRounds\":"
                    + blueRounds
                    + ","

                    + "\"redRounds\":"
                    + redRounds
                    + ","

                    + "\"secondsRemaining\":"
                    + secondsRemaining
                    + ","

                    + "\"status\":\""
                    + EscaparJson(
                        status
                    )
                    + "\","

                    + "\"matchActive\":"
                    + (
                        matchActive
                            ? "true"
                            : "false"
                    )
                    + ","

                    + "\"resting\":"
                    + (
                        resting
                            ? "true"
                            : "false"
                    )
                    + ","

                    + "\"goldenPoint\":"
                    + (
                        goldenPoint
                            ? "true"
                            : "false"
                    )
                    + ","

                    + "\"paused\":"
                    + (
                        paused
                            ? "true"
                            : "false"
                    )

                    + "}";
            }
        }


        private string NumeroJson(
            double numero)
        {
            if (
                Math.Abs(
                    numero -
                    Math.Round(numero)
                ) < 0.0001)
            {
                return
                    ((int)Math.Round(
                        numero
                    ))
                    .ToString();
            }


            return
                numero
                    .ToString("0.0")
                    .Replace(
                        ",",
                        "."
                    );
        }


        private string EscaparJson(
            string texto)
        {
            if (texto == null)
            {
                return "";
            }


            return
                texto
                    .Replace(
                        "\\",
                        "\\\\"
                    )
                    .Replace(
                        "\"",
                        "\\\""
                    )
                    .Replace(
                        "\r",
                        "\\r"
                    )
                    .Replace(
                        "\n",
                        "\\n"
                    );
        }


        private string ObtenerString(
            string json,
            string propiedad)
        {
            string patron =
                "\"" +
                Regex.Escape(
                    propiedad
                ) +
                "\"\\s*:\\s*\"([^\"]*)\"";


            Match match =
                Regex.Match(
                    json,
                    patron,
                    RegexOptions.IgnoreCase
                );


            if (!match.Success)
            {
                return "";
            }


            return
                match.Groups[1]
                    .Value;
        }


        // =========================================================
        // CONEXIÓN DE JUECES
        // =========================================================

        private void NotificarConexiones()
        {
            try
            {
                JudgeConnectionsChanged
                    ?.Invoke(
                        this,
                        EventArgs.Empty
                    );
            }
            catch
            {
            }
        }


        private async Task CerrarSocket(
            WebSocket socket)
        {
            try
            {
                if (
                    socket.State ==
                    WebSocketState.Open)
                {
                    await socket
                        .CloseAsync(
                            WebSocketCloseStatus
                                .PolicyViolation,
                            "Código no válido",
                            CancellationToken.None
                        );
                }
            }
            catch
            {
            }
        }
    }
}