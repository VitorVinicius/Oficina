﻿using System;
using System.Collections.Generic;
using System.Text;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Gms.Maps.Model;
using Newtonsoft.Json.Linq;
using Android.Gms.Maps;
using System.Net;
using Newtonsoft.Json;
using Android.Graphics;
using Android.Util;
using Android.Locations;


namespace ParkingApp
{
    public class Marcador
    {
        public Marcador(JObject estacionamento, Marker marker)
        {
            Estacionamento = estacionamento;
            Marker = marker;
        }
        public long Id { get; set; }
        public List<long> Conexoes = new List<long>();
        public JObject Estacionamento { get; set; }
        public Marker Marker { get; set; }
        private List<Polyline> _Linhas = new List<Polyline>();
        public List<Polyline> Linhas { get { return _Linhas; } set { _Linhas = value; } }
    }

    public class ControladorMapa : Java.Lang.Object, ILocationListener
    {
        Activity AtividadePai { get; set; }
        //private Resources Resources { get { return AtividadePai.Resources; } }
        public string UltimoTermoDeBusca { get; private set; }
        public GoogleMap Mapa { get; private set; }
        public JObject EstacionamentoSelecionado { get; private set; }
        public List<Marcador> MarcadoresColocados { get; private set; }
        public enum StatusGUI { Normal, DesenhandoCaminho, ModoDirecao }
        public Action ControleIniciadoEvent { get; set; }
        public Action MarcadorColocadoEvent { get; set; }
        public LatLng PosicaoGeograficaPadrao { get; set; }
        public readonly string ChaveAPIGoogleDirections = "AIzaSyAxeYnVCVNeUuE5W_l6xzgPMcJM1tW-kY4";
        Marker _UltimoPontoInteracao = null;


        public Color CorLinhaEstrada { get; set; }
        public Color CorLinhaCaminhoEstacionamento { get; set; }


        public bool MostrarEstacionamentosAoAtualizar { get; set; }
        public ControladorMapa(Activity atividadePai, GoogleMap gmap)
        {
            MarcadoresColocados = new List<Marcador>();
            MostrarEstacionamentosAoAtualizar = true;
            this.AtividadePai = atividadePai;
            this.Mapa = gmap;
            CorLinhaEstrada = Color.DarkBlue;
            CorLinhaCaminhoEstacionamento = Color.LightGray;
        }


        Marker UltimoPontoInteracao
        {
            get { return _UltimoPontoInteracao; }
            set
            {

                _UltimoPontoInteracao = value;

            }
        }


        public void DarZoom(double latitude, double longitude, float nivelZoom)
        {
            CameraUpdate camera = CameraUpdateFactory.NewLatLngZoom(new LatLng(latitude, longitude), nivelZoom);

            
            Mapa.MoveCamera(camera);
        }
        public void DarZoom(LatLngBounds bounds, int margem)
        {
            CameraUpdate camera = CameraUpdateFactory.NewLatLngBounds(bounds,margem);


            Mapa.MoveCamera(camera);
        }

        public void DarZoom(long nivelZoom)
        {
            CameraUpdate camera = CameraUpdateFactory.NewLatLngZoom(Mapa.CameraPosition.Target, nivelZoom);
            Mapa.MoveCamera(camera);
        }

        public void ToggleMostrarMarcadosCaminho()
        {
            foreach (Marcador marcador in MarcadoresColocados)
            {
                marcador.Marker.Visible = !marcador.Marker.Visible;
            }
        }

        private void MarkerClickEvent(object sender, GoogleMap.MarkerClickEventArgs args)
        {
            Marcador marcador = this.MarcadoresColocados.Find(m => m.Marker.Id == args.Marker.Id);
            if (marcador != null)
                UltimoPontoInteracao = args.Marker;
            else
            {

            }
        }


        private void ColocarNovoPontoMapa(LatLng latlngOrigem, LatLng latlngDest, JObject estacionamento)
        {
            MarkerOptions options = new MarkerOptions().SetPosition(latlngOrigem).SetTitle("").SetIcon(BitmapDescriptorFactory.FromResource(Resource.Drawable.placeholder_point));

            Marker ponto = Mapa.AddMarker(options);

            Marcador marcador = new Marcador(estacionamento, ponto);
            if (latlngDest != null)
            {
                var _latitude = latlngDest.Latitude;
                var _longitude = latlngDest.Longitude;
                //var _altitude = (ponto["Localizacao"])["Altitude"].Value<double>();
                PolylineOptions opt = new PolylineOptions();
                opt = opt.Add(latlngOrigem, new LatLng(_latitude, _longitude));
                opt = opt.InvokeWidth(5);
                opt = opt.InvokeColor(this.CorLinhaCaminhoEstacionamento);

                Polyline line = Mapa.AddPolyline(opt);
                marcador.Linhas.Add(line);
            }
            if (UltimoPontoInteracao != null)
            {
                marcador.Conexoes.Add(long.Parse(UltimoPontoInteracao.Title));
            }
            marcador = SalvarPontoInserido(marcador);
            UltimoPontoInteracao = marcador.Marker;
            this.MarcadoresColocados.Add(marcador);
            if ((Action)this.MarcadorColocadoEvent != null)
            {
                MarcadorColocadoEvent.Invoke();
            }
        }

        internal void Limpar()
        {
            this.Mapa.Clear();
            MarcadoresColocados.Clear();
            VagasColocadas.Clear();
            Polylines.Clear();
            PolylinesCaminhoParaVaga.Clear();

    }

        public JObject ObterDirecoes(string origem, string destino, bool mostrarNoMapa)
        {

            string nullResponse = "{\"status\": \"ZERO_RESULTS\",\"routes\": []}";

            if (origem == null || destino == null) return (JObject)JsonConvert.DeserializeObject(nullResponse);
            string lingua = Java.Util.Locale.Default.Language + "-" + Java.Util.Locale.Default.Country;
            string url = string.Format("https://maps.googleapis.com/maps/api/directions/{0}?{1}", "json", "origin=" + origem + "&destination=" + destino + "&key=" + this.ChaveAPIGoogleDirections + "&language=" + lingua + "&mode=driving");
            using (WebClient wc = new WebClient())
            {



                string rotasJsonText = wc.DownloadString(url);
                JObject lista = (JObject)JsonConvert.DeserializeObject(rotasJsonText);
                if (mostrarNoMapa)
                {
                    this.DesenharRota((JObject)(lista["routes"]).First);
                }
                return lista;

            }


        }
        public JObject RotaAtual { get; private set; }
        public void DesenharRota(JObject rotaEscolhida)
        {

            foreach (Polyline linha in this.PolylinesCaminhoParaVaga)
            {
                try
                {
                    linha.Remove();
                }
                catch { }
            }


            RotaAtual = rotaEscolhida;
            string polylineString = (rotaEscolhida["overview_polyline"])["points"].ToString();

            var polyline = GooglePoints.Decode(polylineString);
            PolylineOptions opt = new PolylineOptions();
            foreach (var point in polyline)
            {
                opt = opt.Add(point);
            }
            ;
            opt = opt.InvokeWidth(20);
            opt = opt.InvokeColor(this.CorLinhaEstrada);

            this.PolylinesCaminhoParaVaga.Add(Mapa.AddPolyline(opt));


        }

        internal JObject ObterEstacionamento(long idEstacionamento)
        {

            JObject estacionamento = null;
            using (WebClient wc = new WebClient())
            {
                wc.Headers.Add(HttpRequestHeader.ContentType, "application/json");
                string url = ParkingManagerServerURL + "api/EstacionamentoModels/?id=" + idEstacionamento;

                string vagasJsonText = wc.DownloadString(url);
                estacionamento = (JObject)JsonConvert.DeserializeObject(vagasJsonText);

                return estacionamento;

            }
        }

        public JArray ObterEstacionamentos( string termoBusca)
        {

            UltimoTermoDeBusca = termoBusca;

            JArray lista = null;
            using (WebClient wc = new WebClient())
            {
                wc.Headers.Add(HttpRequestHeader.ContentType, "application/json");
                string url = ParkingManagerServerURL + "api/EstacionamentoModels/busca/" + termoBusca;

                string vagasJsonText = wc.DownloadString(url);
                lista = (JArray)JsonConvert.DeserializeObject(vagasJsonText);
                
                return lista;

            }
        }


        public void MostrarEstacionamentosNoMap(JArray lista)
        {

            foreach (Marcador marcador in MarcadoresColocados)
            {
                marcador.Marker.Remove();
                foreach (Polyline linha in marcador.Linhas)
                {
                    linha.Remove();
                }

            }
            MarcadoresColocados.Clear();

            foreach (Polyline linha in Polylines)
            {
                linha.Remove();
            }
            Polylines.Clear();
            foreach (var estacionamento in lista)
            {
                var latitude = (estacionamento["Localizacao"])["Latitude"].Value<double>();
                var longitude = (estacionamento["Localizacao"])["Longitude"].Value<double>();
                var altitude = (estacionamento["Localizacao"])["Altitude"].Value<float>();



                LatLng latlng = new LatLng(Convert.ToDouble(latitude), Convert.ToDouble(longitude));


                var imagemMarcador = BitmapDescriptorFactory.FromResource(Resource.Drawable.parking);
                MarkerOptions options = new MarkerOptions().SetPosition(latlng).SetTitle("").SetIcon(imagemMarcador);

                Marker ponto = Mapa.AddMarker(options);
                this.Markers.Add(ponto);
                Marcador marcador = new Marcador((JObject)estacionamento, ponto);
                MarcadoresColocados.Add(marcador);
                var pontos = (JArray)estacionamento["Pontos"];
                MostrarPontosNoMapa((JObject)estacionamento, pontos);
                if (!string.IsNullOrEmpty(estacionamento["ImagemBase64"].Value<string>()))
                {
                    try
                    {
                        var swbounds = new LatLng(estacionamento["SWBoundImagem"].Value<double>("Latitude"),
                        estacionamento["SWBoundImagem"].Value<double>("Longitude"));
                        var nebounds = new LatLng(estacionamento["NEBoundImagem"].Value<double>("Latitude"),
                        estacionamento["NEBoundImagem"].Value<double>("Longitude"));


                        LatLngBounds bounds = new LatLngBounds(swbounds, nebounds);






                        byte[] decodedString = Base64.Decode(estacionamento["ImagemBase64"].Value<string>(), Base64Flags.Default);

                        Bitmap decodedByte = BitmapFactory.DecodeByteArray(decodedString, 0, decodedString.Length);

                        var bitmapDescriptor = BitmapDescriptorFactory.FromBitmap(decodedByte);

                        GroundOverlayOptions newarkMap = new GroundOverlayOptions()
            .InvokeImage(bitmapDescriptor).PositionFromBounds(bounds);

                        var overlay= Mapa.AddGroundOverlay(newarkMap);
                        overlay.Clickable = true;
                        Mapa.GroundOverlayClick += (obj, args) =>
                        {
                            if (args.GroundOverlay.Id == overlay.Id)
                            {
                                if (STATUS_CONTROLE == StatusGUI.Normal)
                                {
                                    this.EstacionamentoSelecionado = (JObject)estacionamento;
                                    if (this.EstacionamentoSelecionadoEvent != null)
                                        (this.EstacionamentoSelecionadoEvent).DynamicInvoke(EstacionamentoSelecionado);
                                }
                            }
                            else
                            {
                                if ((Action)this.CliqueNoChaoEvent != null)
                                {
                                    CliqueNoChaoEvent.DynamicInvoke(args.GroundOverlay);
                                }
                            }
                        };
                    }
                    catch (Exception ex)
                    {

                    }
                }
            }
        }

        public List<Vaga> VagasColocadas = new List<Vaga>();
        private void MostrarVagasNoMapa(long idEstacionamento,  JObject ponto, JArray lista)
        {
            if (lista != null)
            {
                foreach (var vaga in lista)
                {
                    Vaga _vaga = new Vaga();
                    var latitude = (vaga["Localizacao"])["Latitude"].Value<double>();
                    var longitude = (vaga["Localizacao"])["Longitude"].Value<double>();
                    var altitude = (vaga["Localizacao"])["Altitude"].Value<double>();
                    LatLng latlng = new LatLng(Convert.ToDouble(latitude), Convert.ToDouble(longitude));

                    int icone = 0;
                    switch (vaga["Tipo"].Value<int>())
                    {
                        default:
                            icone = Resource.Drawable.parking_sign;//vaga normal;
                            break;
                        case 1:
                            icone = Resource.Drawable.parking_sign_special_1;//vaga idoso;
                            break;
                        case 2:
                            icone = Resource.Drawable.parking_sign_special_2;//vaga especial;
                            break;
                    }


                    var ocupacao = vaga["Ocupacao"];


                    MarkerOptions options = new MarkerOptions().SetPosition(latlng).SetTitle(vaga["Numero"].Value<long>().ToString()).SetIcon(BitmapDescriptorFactory.FromResource(icone));

                    _vaga.Marker = Mapa.AddMarker(options);
                    _vaga.Ponto = ponto;
                    _vaga.Dados = vaga;
                    _vaga.IdEstacionamento = idEstacionamento;
                    VagasColocadas.Add(_vaga);
                    ChecarVisibilidadeVaga(_vaga);

                    var _latitude = (ponto["Localizacao"])["Latitude"].Value<double>();
                    var _longitude = (ponto["Localizacao"])["Longitude"].Value<double>();
                    var _altitude = (ponto["Localizacao"])["Altitude"].Value<double>();
                    PolylineOptions opt = new PolylineOptions();
                    opt = opt.Add(new LatLng(latitude, longitude), new LatLng(_latitude, _longitude));
                    opt = opt.InvokeWidth(5);
                    opt = opt.InvokeColor(this.CorLinhaCaminhoEstacionamento);

                    Polyline line = Mapa.AddPolyline(opt);

                }

            }
        }

        public void ChecarVisibilidadeVaga(Vaga vaga)
        {
            
            if ((vaga.Dados["Reserva"].Type != JTokenType.Null) && ((vaga.Dados["Reserva"])["Usuario"].Value<long>("Id") == MainActivity.Usuario.Value<long>("Id")))
            {
                vaga.Marker.Visible = true;
            }
            else
            {
                vaga.Marker.Visible = (vaga.Dados["Ocupacao"].Type == JTokenType.Null) && (vaga.Dados["Reserva"].Type == JTokenType.Null);
            }
        }
        private void AssociarPontos(long id, List<long> conexoes)
        {
            foreach (long conexao in conexoes)
            {
                string url = ParkingManagerServerURL + string.Format("api/PontoModels/ConectarPontos/{0}/{1}", id, conexao);
                Uri uri = new Uri(url);
                using (WebClient wc = new WebClient())
                {
                    wc.DownloadStringAsync(uri);
                }
            }
        }


      

        private void MostrarPontosNoMapa(JObject estacionamento, JArray lista)
        {
            foreach (var ponto in lista)
            {

                var latitude = (ponto["Localizacao"])["Latitude"].Value<double>();
                var longitude = (ponto["Localizacao"])["Longitude"].Value<double>();
                var altitude = (ponto["Localizacao"])["Altitude"].Value<double>();
                LatLng latlng = new LatLng(Convert.ToDouble(latitude), Convert.ToDouble(longitude));
                MarkerOptions options = new MarkerOptions().SetPosition(latlng).SetTitle(ponto["Id"].Value<long>().ToString()).SetIcon(BitmapDescriptorFactory.FromResource(Resource.Drawable.placeholder_point));
                Marker marker = Mapa.AddMarker(options);
                marker.Visible = false;
                var vagas = (JArray)ponto["VagasConectadas"];
                Marcador marcador = new Marcador(estacionamento, marker)
                {
                    Id = ponto["Id"].Value<long>()
                };

                foreach (var conexao in ponto["Conexoes"])
                {

                    foreach (var _ponto in lista)
                    {
                        if (_ponto["Id"].Value<long>() == conexao.Value<long>())
                        {
                            var _latitude = (_ponto["Localizacao"])["Latitude"].Value<double>();
                            var _longitude = (_ponto["Localizacao"])["Longitude"].Value<double>();
                            var _altitude = (_ponto["Localizacao"])["Altitude"].Value<double>();
                            PolylineOptions opt = new PolylineOptions();
                            opt = opt.Add(latlng, new LatLng(_latitude, _longitude));
                            opt = opt.InvokeWidth(5);
                            opt = opt.InvokeColor(this.CorLinhaCaminhoEstacionamento);

                            Polyline line = Mapa.AddPolyline(opt);
                            marcador.Linhas.Add(line);
                        }

                    }
                }
                MarcadoresColocados.Add(marcador);


                MostrarVagasNoMapa(estacionamento.Value<long>("Id"),(JObject)ponto, vagas);



            }
        }


        private void GMapLongClickEvent(object sender, GoogleMap.MapLongClickEventArgs e)
        {
            if (STATUS_CONTROLE == StatusGUI.DesenhandoCaminho)
            {
                if (UltimoPontoInteracao == null)
                {
                    //Colocar novo ponto
                    ColocarNovoPontoMapa(e.Point, (LatLng)null, EstacionamentoSelecionado);
                }
                else
                {
                    //colocar novo ponto a partir do ultimo interagido
                    ColocarNovoPontoMapa(e.Point, UltimoPontoInteracao.Position, EstacionamentoSelecionado);
                }
            }


        }


        private Marcador SalvarPontoInserido(Marcador marcador)
        {


            JArray jPontos = new JArray();


            {
                string url = ParkingManagerServerURL + string.Format("api/EstacionamentoModel/{0}/AdicionarPonto", EstacionamentoSelecionado["Id"].Value<long>());
                JObject jPonto = new JObject();
                jPonto.Add("VagasConectadas", null);
                JObject jLocalizacao = new JObject();
                jLocalizacao.Add("Latitude", marcador.Marker.Position.Latitude);
                jLocalizacao.Add("Longitude", marcador.Marker.Position.Longitude);
                jLocalizacao.Add("Altitude", 0);
                jPonto.Add("Localizacao", jLocalizacao);
                jPonto.Add("Entrada", false);
                jPonto.Add("Saida", false);

                using (WebClient wc = new WebClient())
                {
                    wc.Headers.Add(HttpRequestHeader.ContentType, "application/json");
                    string result = wc.UploadString(url, jPonto.ToString());
                    long id = ((JObject)JsonConvert.DeserializeObject(result))["Id"].Value<long>();
                    marcador.Id = id;
                    marcador.Marker.Title = id.ToString();
                    if (marcador.Conexoes != null && marcador.Conexoes.Count > 0)
                        AssociarPontos(id, marcador.Conexoes);

                    return marcador;
                }
            }




        }


        public void IniciarControle()
        {
            this.Mapa.MapLongClick += GMapLongClickEvent;
            this.Mapa.MarkerClick += MarkerClickEvent;
            if ((Action)this.ControleIniciadoEvent != null)
            {
                ControleIniciadoEvent.Invoke();
            }
            InitializeLocationManager();


        }



        private void Atualizar(object sender, EventArgs e)
        {
            try
            {
                MostrarEstacionamentosNoMap(ObterEstacionamentos(UltimoTermoDeBusca));
            }
            catch { }
        }

        private StatusGUI _STATUS_GUI = StatusGUI.Normal;
        public StatusGUI STATUS_CONTROLE
        {
            get { return _STATUS_GUI; }
            set
            {
                StatusGUI _STATUS_GUI_ANT = _STATUS_GUI;
                _STATUS_GUI = value;

                MudancaDeEstado(_STATUS_GUI_ANT, _STATUS_GUI);
            }
        }

        public Action EstacionamentoSelecionadoEvent { get; set; }
        public Action CliqueNoChaoEvent { get; set; }
        public Action LocalizacaoAtualAlteradaEvent { get;  set; }

        IntPtr intPtr = new IntPtr(new System.Random().Next());




        private void MudancaDeEstado(StatusGUI _STATUS_GUI_ANT, StatusGUI _STATUS_GUI)
        {
            if (_STATUS_GUI_ANT == StatusGUI.DesenhandoCaminho && _STATUS_GUI == StatusGUI.Normal)
            {
                UltimoPontoInteracao = null;
            }
        }

        public static readonly string ParkingManagerServerURL = "http://parkingmanagerserver.azurewebsites.net/";

        private readonly string ObterVagasURL = "api/VagaModels";



        public Location LocalizacaoAtual { get; private set; }
        private List<Marker> Markers = new List<Marker>();

        public List<Polyline> Polylines = new List<Polyline>();
        public List<Polyline> PolylinesCaminhoParaVaga = new List<Polyline>();

        public readonly LocationManager GerenciadorDeLocalizacao = Application.Context.GetSystemService(Context.LocationService) as LocationManager;

        string _locationProvider;
        void InitializeLocationManager()
        {

            Criteria locationCriteria = new Criteria();

            locationCriteria.Accuracy = Accuracy.Fine;
            locationCriteria.PowerRequirement = Power.Low;

            _locationProvider = GerenciadorDeLocalizacao.GetBestProvider(locationCriteria, true);

            if (_locationProvider != null)
            {
                GerenciadorDeLocalizacao.RequestLocationUpdates(_locationProvider, 1000, 0.3f, this);
            }
            else
            {
                Log.Info("Localização", "No location providers available");
            }

            /* try
             {
                 GerenciadorDeLocalizacao = (LocationManager)Application.Context.GetSystemService(Context.LocationService);

              Criteria criteriaForLocationService = new Criteria
             {
                 Accuracy = Accuracy.Fine
             };
             IList<string> acceptableLocationProviders = GerenciadorDeLocalizacao.GetProviders(criteriaForLocationService, true);

             if (acceptableLocationProviders.Any())
             {
                 _locationProvider = acceptableLocationProviders.First();
             }
             else
             {
                 _locationProvider = LocationManager.GpsProvider;
             }

                 GerenciadorDeLocalizacao.RequestLocationUpdates(_locationProvider, 0, 0, this);
             }
             catch(Exception ex)
             {

             }
             */
        }

        public void OnLocationChanged(Location location)
        {
            LocalizacaoAtual = location;
            if (this.LocalizacaoAtualAlteradaEvent != null)
                this.LocalizacaoAtualAlteradaEvent.Invoke();
        }

        public void OnProviderDisabled(string provider)
        {

        }

        public void OnProviderEnabled(string provider)
        {

        }

        public void OnStatusChanged(string provider, [GeneratedEnum] Availability status, Bundle extras)
        {

        }




    }

    public class Vaga
    {
        public JToken Dados { get; internal set; }
        public long IdEstacionamento { get; internal set; }
        public Marker Marker { get; internal set; }
        public JObject Ponto { get; internal set; }
    }

    /// <summary>
    /// See https://developers.google.com/maps/documentation/utilities/polylinealgorithm
    /// </summary>
    public static class GooglePoints
    {
        /// <summary>
        /// Decode google style polyline coordinates.
        /// </summary>
        /// <param name="encodedPoints"></param>
        /// <returns></returns>
        public static IEnumerable<LatLng> Decode(string encodedPoints)
        {
            if (string.IsNullOrEmpty(encodedPoints))
                throw new ArgumentNullException("encodedPoints");

            char[] polylineChars = encodedPoints.ToCharArray();
            int index = 0;

            int currentLat = 0;
            int currentLng = 0;
            int next5bits;
            int sum;
            int shifter;

            while (index < polylineChars.Length)
            {
                // calculate next latitude
                sum = 0;
                shifter = 0;
                do
                {
                    next5bits = (int)polylineChars[index++] - 63;
                    sum |= (next5bits & 31) << shifter;
                    shifter += 5;
                } while (next5bits >= 32 && index < polylineChars.Length);

                if (index >= polylineChars.Length)
                    break;

                currentLat += (sum & 1) == 1 ? ~(sum >> 1) : (sum >> 1);

                //calculate next longitude
                sum = 0;
                shifter = 0;
                do
                {
                    next5bits = (int)polylineChars[index++] - 63;
                    sum |= (next5bits & 31) << shifter;
                    shifter += 5;
                } while (next5bits >= 32 && index < polylineChars.Length);

                if (index >= polylineChars.Length && next5bits >= 32)
                    break;

                currentLng += (sum & 1) == 1 ? ~(sum >> 1) : (sum >> 1);

                yield return new LatLng(Convert.ToDouble(currentLat) / 1E5, Convert.ToDouble(currentLng) / 1E5);
            }
        }

        /// <summary>
        /// Encode it
        /// </summary>
        /// <param name="points"></param>
        /// <returns></returns>
        public static string Encode(IEnumerable<LatLng> points)
        {
            var str = new StringBuilder();

            var encodeDiff = (Action<int>)(diff =>
            {
                int shifted = diff << 1;
                if (diff < 0)
                    shifted = ~shifted;

                int rem = shifted;

                while (rem >= 0x20)
                {
                    str.Append((char)((0x20 | (rem & 0x1f)) + 63));

                    rem >>= 5;
                }

                str.Append((char)(rem + 63));
            });

            int lastLat = 0;
            int lastLng = 0;

            foreach (var point in points)
            {
                int lat = (int)Math.Round(point.Latitude * 1E5);
                int lng = (int)Math.Round(point.Longitude * 1E5);

                encodeDiff(lat - lastLat);
                encodeDiff(lng - lastLng);

                lastLat = lat;
                lastLng = lng;
            }

            return str.ToString();
        }
    }
    //Fonte: https://github.com/mburst/dijkstras-algorithm/blob/master/dijkstras.cs
    class Graph
    {
       public Dictionary<string, Dictionary<string, int>> vertices = new Dictionary<string, Dictionary<string, int>>();

        public void add_vertex(string name, Dictionary<string, int> edges)
        {
            vertices[name] = edges;
        }

        public List<string> shortest_path(string start, string finish)
        {
            var previous = new Dictionary<string, string>();
            var distances = new Dictionary<string, int>();
            var nodes = new List<string>();

            List<string> path = null;

            foreach (var vertex in vertices)
            {
                if (vertex.Key == start)
                {
                    distances[vertex.Key] = 0;
                }
                else
                {
                    distances[vertex.Key] = int.MaxValue;
                }

                nodes.Add(vertex.Key);
            }

            while (nodes.Count != 0)
            {
                nodes.Sort((x, y) => distances[x] - distances[y]);

                var smallest = nodes[0];
                nodes.Remove(smallest);

                if (smallest == finish)
                {
                    path = new List<string>();
                    while (previous.ContainsKey(smallest))
                    {
                        path.Add(smallest);
                        smallest = previous[smallest];
                    }

                    break;
                }

                if (distances[smallest] == int.MaxValue)
                {
                    break;
                }

                foreach (var neighbor in vertices[smallest])
                {
                    var alt = distances[smallest] + neighbor.Value;
                    if (alt < distances[neighbor.Key])
                    {
                        distances[neighbor.Key] = alt;
                        previous[neighbor.Key] = smallest;
                    }
                }
            }

            return path;
        }
    }

    public static class NumericExtensions
    {
        public static double ToRadians(this double val)
        {
            return (Math.PI / 180) * val;
        }
    }


}