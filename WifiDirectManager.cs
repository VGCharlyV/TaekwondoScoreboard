using System;
using Windows.Devices.WiFiDirect;
using Windows.Security.Credentials;

namespace TaekwondoScoreboard
{
    public sealed class WifiDirectManager
    {
        private WiFiDirectAdvertisementPublisher publisher;

        public bool EstaActivo { get; private set; }

        public string UltimoEstado { get; private set; }

        public event EventHandler EstadoCambiado;


        public WifiDirectManager()
        {
            UltimoEstado = "DETENIDO";
        }


        public void Iniciar(
            string nombreRed,
            string contrasena)
        {
            if (publisher != null)
            {
                return;
            }

            publisher =
                new WiFiDirectAdvertisementPublisher();


            publisher.StatusChanged +=
                Publisher_StatusChanged;


            // La PC será propietaria del grupo Wi-Fi Direct.
            publisher.Advertisement
                .IsAutonomousGroupOwnerEnabled = true;


            // Modo Legacy:
            // permite que celulares normales vean la red
            // como una red Wi-Fi convencional.
            WiFiDirectLegacySettings legacy =
                publisher.Advertisement.LegacySettings;


            legacy.IsEnabled = true;

            legacy.Ssid = nombreRed;


            PasswordCredential credencial =
                new PasswordCredential();

            credencial.Password =
                contrasena;


            legacy.Passphrase =
                credencial;


            UltimoEstado =
                "INICIANDO";


            publisher.Start();
        }


        public void Detener()
        {
            try
            {
                if (publisher != null)
                {
                    publisher.Stop();

                    publisher.StatusChanged -=
                        Publisher_StatusChanged;

                    publisher = null;
                }
            }
            catch
            {
            }


            EstaActivo = false;

            UltimoEstado =
                "DETENIDO";


            OnEstadoCambiado();
        }


        private void Publisher_StatusChanged(
            WiFiDirectAdvertisementPublisher sender,
            WiFiDirectAdvertisementPublisherStatusChangedEventArgs args)
        {
            if (args.Status ==
                WiFiDirectAdvertisementPublisherStatus.Started)
            {
                EstaActivo = true;

                UltimoEstado =
                    "ACTIVO";
            }
            else if (
                args.Status ==
                WiFiDirectAdvertisementPublisherStatus.Aborted)
            {
                EstaActivo = false;

                UltimoEstado =
                    "ERROR: " +
                    args.Error.ToString();
            }
            else if (
                args.Status ==
                WiFiDirectAdvertisementPublisherStatus.Stopped)
            {
                EstaActivo = false;

                UltimoEstado =
                    "DETENIDO";
            }
            else
            {
                UltimoEstado =
                    args.Status.ToString();
            }


            OnEstadoCambiado();
        }


        private void OnEstadoCambiado()
        {
            EventHandler handler =
                EstadoCambiado;

            if (handler != null)
            {
                handler(
                    this,
                    EventArgs.Empty
                );
            }
        }
    }
}