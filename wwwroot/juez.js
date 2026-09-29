let socket = null;

let conectado = false;
let combateActivo = false;
let pausado = false;
let descansando = false;
let puntoDeOro = false;


const parametros =
    new URLSearchParams(
        window.location.search
    );


const token =
    parametros.get("token");


const elementoConexion =
    document.getElementById(
        "conexion"
    );


const elementoJuez =
    document.getElementById(
        "juez"
    );


const elementoMensaje =
    document.getElementById(
        "mensaje"
    );


const elementoAzul =
    document.getElementById(
        "scoreAzul"
    );


const elementoRojo =
    document.getElementById(
        "scoreRojo"
    );


const elementoRonda =
    document.getElementById(
        "ronda"
    );


const elementoTiempo =
    document.getElementById(
        "tiempo"
    );


// =========================================================
// MENSAJES
// =========================================================

function mostrarMensaje(texto) {

    elementoMensaje.textContent =
        texto;
}


// =========================================================
// CONEXIÓN
// =========================================================

function actualizarConexion(
    estado
) {

    conectado = estado;


    if (estado) {

        elementoConexion.textContent =
            "● CONECTADO";

        elementoConexion.className =
            "conectado";

    }
    else {

        elementoConexion.textContent =
            "● DESCONECTADO";

        elementoConexion.className =
            "desconectado";

    }


    actualizarBotones();
}


// =========================================================
// TIEMPO
// =========================================================

function formatearTiempo(
    segundos
) {

    segundos =
        Math.max(
            0,
            Number(segundos) || 0
        );


    const minutos =
        Math.floor(
            segundos / 60
        );


    const resto =
        segundos % 60;


    return (
        minutos +
        ":" +
        resto
            .toString()
            .padStart(2, "0")
    );
}


// =========================================================
// ESTADO
// =========================================================

function actualizarEstado(
    estado
) {

    if (!estado) {
        return;
    }


    if (
        estado.blueScore !==
        undefined
    ) {

        elementoAzul.textContent =
            estado.blueScore;
    }


    if (
        estado.redScore !==
        undefined
    ) {

        elementoRojo.textContent =
            estado.redScore;
    }


    if (
        estado.round !==
        undefined
    ) {

        if (
            estado.goldenPoint ===
            true
        ) {

            elementoRonda.textContent =
                "ORO";
        }
        else {

            elementoRonda.textContent =
                estado.round;
        }
    }


    if (
        estado.secondsRemaining !==
        undefined
    ) {

        elementoTiempo.textContent =
            formatearTiempo(
                estado.secondsRemaining
            );
    }


    pausado =
        estado.paused === true;


    descansando =
        estado.resting === true;


    combateActivo =
        estado.matchActive === true;


    puntoDeOro =
        estado.goldenPoint === true;


    if (pausado) {

        if (descansando) {

            mostrarMensaje(
                "DESCANSO EN PAUSA"
            );
        }
        else {

            mostrarMensaje(
                "COMBATE EN PAUSA"
            );
        }

    }
    else if (descansando) {

        mostrarMensaje(
            "DESCANSO"
        );

    }
    else if (puntoDeOro) {

        mostrarMensaje(
            "PUNTO DE ORO - PRIMER PUNTO GANA"
        );

    }
    else if (!combateActivo) {

        mostrarMensaje(
            estado.status ||
            "ESPERANDO INICIO"
        );

    }
    else {

        mostrarMensaje(
            estado.status ||
            "COMBATE EN CURSO"
        );
    }


    actualizarBotones();
}


// =========================================================
// BLOQUEAR BOTONES
// =========================================================

function actualizarBotones() {

    const botones =
        document.querySelectorAll(
            ".acciones button"
        );


    const bloquear =
        !conectado ||
        !combateActivo ||
        pausado ||
        descansando;


    botones.forEach(
        function (boton) {

            boton.disabled =
                bloquear;
        }
    );
}


// =========================================================
// WEBSOCKET
// =========================================================

function conectar() {

    if (!token) {

        actualizarConexion(false);

        mostrarMensaje(
            "CÓDIGO DE JUEZ NO VÁLIDO"
        );

        return;
    }


    const protocolo =
        window.location.protocol ===
            "https:"
            ? "wss:"
            : "ws:";


    const direccion =
        protocolo +
        "//" +
        window.location.host +
        "/ws";


    try {

        socket =
            new WebSocket(
                direccion
            );


        socket.onopen =
            function () {

                socket.send(
                    JSON.stringify(
                        {
                            type:
                                "hello",

                            token:
                                token
                        }
                    )
                );


                mostrarMensaje(
                    "CONECTANDO CON EL MARCADOR..."
                );
            };


        socket.onmessage =
            function (event) {

                try {

                    const mensaje =
                        JSON.parse(
                            event.data
                        );


                    if (
                        mensaje.type ===
                        "hello"
                    ) {

                        conectado = true;

                        actualizarConexion(
                            true
                        );


                        if (
                            mensaje.judge
                        ) {

                            elementoJuez.textContent =
                                "JUEZ " +
                                mensaje.judge;
                        }


                        if (
                            mensaje.state
                        ) {

                            actualizarEstado(
                                mensaje.state
                            );
                        }

                        return;
                    }


                    if (
                        mensaje.type ===
                        "state"
                    ) {

                        actualizarEstado(
                            mensaje
                        );

                        return;
                    }


                    if (
                        mensaje.type ===
                        "message"
                    ) {

                        mostrarMensaje(
                            mensaje.text ||
                            ""
                        );
                    }

                }
                catch (error) {

                    console.error(
                        error
                    );
                }
            };


        socket.onclose =
            function () {

                conectado = false;

                actualizarConexion(
                    false
                );


                mostrarMensaje(
                    "RECONECTANDO..."
                );


                setTimeout(
                    conectar,
                    1500
                );
            };


        socket.onerror =
            function () {

                conectado = false;

                actualizarConexion(
                    false
                );
            };

    }
    catch (error) {

        conectado = false;

        actualizarConexion(
            false
        );


        setTimeout(
            conectar,
            1500
        );
    }
}


// =========================================================
// ENVIAR PUNTO
// =========================================================

function enviarAccion(
    lado,
    accion
) {

    // Kyong-go permanece reservado,
    // pero no es funcional.
    if (
        accion ===
        "KyongGo"
    ) {

        return;
    }


    if (
        !socket ||
        socket.readyState !==
        WebSocket.OPEN
    ) {

        mostrarMensaje(
            "SIN CONEXIÓN"
        );

        return;
    }


    if (!combateActivo) {

        mostrarMensaje(
            "EL COMBATE NO HA INICIADO"
        );

        return;
    }


    if (pausado) {

        mostrarMensaje(
            "COMBATE EN PAUSA"
        );

        return;
    }


    if (descansando) {

        mostrarMensaje(
            "DESCANSO"
        );

        return;
    }


    socket.send(
        JSON.stringify(
            {
                type:
                    "action",

                side:
                    lado,

                action:
                    accion
            }
        )
    );


    mostrarMensaje(
        "ACCIÓN REGISTRADA"
    );
}


// =========================================================
// INICIO
// =========================================================

window.addEventListener(
    "load",
    function () {

        actualizarConexion(
            false
        );

        actualizarBotones();

        conectar();
    }
);