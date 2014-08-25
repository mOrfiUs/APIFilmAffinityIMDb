using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using System.Net;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Windows.Forms;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Web;
using System.ComponentModel;
using IMDb_Scraper;
using System.Threading;
//using MediaPortal.GUI.Library;//add ref Core & Utils
using System.Threading.Tasks;

namespace APIFilmAffinityIMDb
{
    #region Interfaces
    /// <summary>
    /// el interface permite que la función subsecuente implementada sea accesible desde COM
    /// </summary>
    public interface IFunctions
    {
        [Description("Versión de este componentee")]
        string getDLLversion();
        [Description("URL completa a partir del id único de cada Película")]
        string urlFromIdFA(string idFA);
        [Description("Devuelve los datos partir del id único de cada Película (obj.idFA debe ser válido antes de llamar a la función).\n obj debe ser un objeto.\nSi mostrarAvisos es verdadero, se notificarán las incidencias y errores.\nPara búsquedas masivas, mejor establecer falso para omitir demasiados avisos.\n fIMDb indica si se deben recuperar datos adicionales desde IMDb\n refineSearchIMDb determina si se refina la búsqueda en IMDb o se devuelve el primer resultado encontrado")]
        bool getObjFA(object obj, bool bIMDb = false, bool refineSearchIMDb = false, bool getDownLinks = false, bool mostrarAvisos = false);
        [Description("Busqueda por título (año e intervalo opcionales). Devuelve idFA (identificador único), buscado (cadena de búsqueda).\nEl valor retornado es True si encontró al menos una única Película.\nFalse puede indicar a)idFA = -1 Error Forbidden, sin conexión. b) nº de resultados, si éstos son más de 40 c)idFA Empty sin resultados")]
        bool findMovieFA(string peli, ref string idFA, ref string buscado, int año = 0, int precisión = 0, bool mostrarAvisos = false, object pelisMP = null, int iErrorforbidden = 0, object sender = null);
        [Description("Devuelve el id único de cada Película a partir de la URL completa")]
        string idFromUrlFA(string urlFA);
        [Description("Busca el año en un string")]
        int buscarAño(string s);
        [Description("Elimina publicidad, calidad, etc. del nombre de una película")]
        string normalize(string s);
    }

    /// <summary>
    /// el interface permite que la función subsecuente implementada sea accesible desde COM
    /// </summary>
    public interface IParamPeli
    {
        #region definitions
        int ID { get; set; }
        int idGénero { get; set; }
        string Géneros { get; set; }
        int Duración { get; set; }
        string Directores { get; set; }
        string Director { get; set; }           //05
        string Género { get; set; }
        string Carátula { get; set; }
        string Título { get; set; }
        string TítuloOriginal { get; set; }
        string Argumento { get; set; }          //10
        string Intérpretes { get; set; }
        string Buscado { get; set; }
        string idFA { get; set; }
        int Año { get; set; }
        string País { get; set; }               //15
        string Guión { get; set; }
        string Música { get; set; }
        string Fotografía { get; set; }
        string Productora { get; set; }
        string Premios { get; set; }            //20
        string Críticas { get; set; }
        int Votos { get; set; }
        float Calificación { get; set; }
        string WebOficial { get; set; }
        string TagGéneros { get; set; }         //25
        string Lema { get; set; }
        string CalificaciónMPAA { get; set; }
        string idIMDb { get; set; }
        string CarátulaIMDb { get; set; }
        string ArgumentoIMDb { get; set; }      //30
        string MediaImages { get; set; }
        string IntérpretPrincipal { get; set; }
        string DownLinks { get; set; }        
        string Tipo { get; set; }
        string AKA { get; set; }                //35
        string Productores { get; set; }
        string Cinematographers { get; set; }
        string Editors { get; set; }
        string Oscars { get; set; }
        string Awards { get; set; }             //40
        string Nominations { get; set; }
        int Top250 { get; set; }
        ArrayList MediaVideos { get; set; }
        ArrayList IntérpretesIMDb { get; set; }
        string GetAll {
            [Description("Contiene todos los datos devueltos. Es util para llamarla desde JavaScript, etc.")]
            get;
            set;
        }
#endregion
    }
    #endregion Interfaces

    /// <summary>
    /// La clase implementan el interface, de tal forma que COM sólo mostrará estas clases
    /// Esta clase aglutina las funciones utilizadas
    /// </summary>
    [ClassInterface(ClassInterfaceType.None)]
    [Description("Funciones y utilidades para la gestión de la búsqueda y extracción de información desde IMDb y FilmAffinity")]
    public class Functions : IFunctions
    {
        #region private const
        internal const string wwwFA = "http://www.filmaffinity.com";
        internal const string wwwIMDb = "http://www.imdb.com";
        internal const string wIMDbTitle = "http://www.imdb.com/title/";
        internal const string wFAFilm = "http://www.filmaffinity.com/es/film";
        #endregion

        /// <summary>
        /// función que se puede emplear como chequeo del funcionamiento
        /// </summary>
        /// <returns>la versión de la librería</returns>
        public string getDLLversion()
        {
            return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }

        /// <summary>
        /// elimina publicidad, calidad, etc. del nombre de una película. Devuelve el mismo nombre si tras la limpieza se queda en empty
        /// </summary>
        /// <param name="sNormalize">ej. Titanic HDRip www.hazclick.com (1997).avi</param>
        /// <returns>nombre normalizado, ej. Titanic</returns>
        public string normalize(string s)
        {
            string sNormalize = s;
            sNormalize = Regex.Replace(sNormalize, @"(19\d{2}|20\d{2})(.*?|$)", string.Empty);//año entre puntos
            sNormalize = Regex.Replace(sNormalize, @"\.(19\d{2}|20\d{2})\.", string.Empty);//año entre puntos
            sNormalize = Regex.Replace(sNormalize, @"\.", " ");//puntos por espacios
            sNormalize = Regex.Replace(sNormalize, @"(\(|\[|\{).*?(\)|\]|\})", string.Empty);//paréntesis, etc.
            sNormalize = Regex.Replace(sNormalize, @"(720p|1080p|dual|bluray|dts|spanish|bdrip|subt|hdrip|dvdrip).*?", string.Empty, RegexOptions.IgnoreCase);
            sNormalize = Regex.Replace(sNormalize, @"\s+", " ").Trim();
            if (!string.IsNullOrEmpty(sNormalize))
                return sNormalize;
            return s;
        }

        /// <summary>
        /// busca el año en un string
        /// </summary>
        /// <param name="s">ej. Titanic (1997)</param>
        /// <returns>Cero o el año encontrado</returns>
        public int buscarAño(string s)
        {
            int iAño = 0;
            string sAño = match(@"(\.|\[|\()(19\d{2}|20\d{2})(\)|\]|\.)", s, 2);
            if (Int32.TryParse(sAño, out iAño))
                return iAño;
            string sNormalize = normalize(s);
            sAño = match(@".*?(19\d{2}|20\d{2})(.*?|$)", sNormalize, 1);
            if (Int32.TryParse(sAño, out iAño))
                return iAño;
            sAño = match(@".*?(19\d{2}|20\d{2})(.*?|$)", s, 1);
            Int32.TryParse(sAño, out iAño);
            return iAño;
        }

        public string urlFromIdFA(string idFA)
        {
            return Regex.IsMatch(idFA, @"[1-9]\d{5}") ? wFAFilm + idFA + ".html" : string.Empty;
        }

        public string urlFromIdIMDb(string idIMDb)
        {
            return Regex.IsMatch(idIMDb, @"\d{7}") ? wIMDbTitle + idIMDb + "/" : string.Empty;
        }

        public string idFromUrlFA(string urlFA)
        {
            return match(wFAFilm + @"([1-9]\d{5}).html", urlFA);            
        }

        public string idFromUrlIMDb(string urlIMDb)
        {
            return match(wIMDbTitle + @"(tt\d{7})/", urlIMDb);
        }

        public bool RunCMD(string cmd, bool mostrarAvisos)
        {
            System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo(cmd);
            psi.UseShellExecute = true;
            try
            {
                System.Diagnostics.Process.Start(psi);
                return true;
            }
            catch (Exception eSendLog)
            {
                if (mostrarAvisos)
                    MessageBox.Show("no se pudo ejecutar la aplicación " + cmd + Environment.NewLine + eSendLog.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
        }

        internal string match(string regex, string html, int i = 1)
        {
            return new Regex(regex, RegexOptions.Multiline).Match(html).Groups[i].Value.Trim();
        }

        /// <summary>
        /// Busca todas las coincidencias de una cadena en otra
        /// </summary>
        /// <param name="regex">Patrón de búsqueda</param>
        /// <param name="html">Cadena en la que se realiza la busqueda</param>
        /// <param name="i">Grupo de búsqueda</param>
        /// <param name="iMaxReturns">Máximo número de resultados a devolver</param>
        /// <returns>Lista con los resultados</returns>
        internal ArrayList matchAll(string regex, string html, int i = 1, int iMaxReturns = 1000)
        {
            ArrayList list = new ArrayList();
            int iCount = 0;
            foreach (Match m in new Regex(regex, RegexOptions.Multiline).Matches(html))
            {
                if (++iCount > iMaxReturns)
                    break;
                list.Add(m.Groups[i].Value.Trim());
            }
            return list;
        }

        private string RemoveLastCrLf(string s)
        {
            return Regex.Replace(s, Environment.NewLine + "$", "");
        }

        // help avaible in Interface
        public bool getObjFA(object obj, bool bIMDb = false, bool refineSearchIMDb = false, bool getDownLinks = false, bool mostrarAvisos = false)
        {
            paramPeli movie;
            try
            {
                movie = (paramPeli)obj;
            }
            catch (Exception ex)
            {
                if (mostrarAvisos)
                    MessageBox.Show("No se pudo convertir el objeto a la clase paramPeli. Error: " + ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            if (!getMovieFA(ref movie, bIMDb, refineSearchIMDb, getDownLinks, mostrarAvisos))
                return false;
            obj = (object)movie;
            return true;
        }

        public bool getMovieFA(ref paramPeli peli, bool bIMDb = false, bool refineSearchIMDb = false, bool getDownLinks = false, bool mostrarAvisos = false, bool bMP = false, string fileLocal = "", int iErrorforbidden = 0, object sender = null)
        {
            frmSplash fSplash = new frmSplash(new System.Drawing.Font("Mistral", 24F));
            fSplash.Text = "extrayendo de FilmAffinity";
            fSplash.Show();
            ProgressBar pbSplash = (ProgressBar)fSplash.Controls[0];
            frmDrawString(fSplash, fSplash.Text);
            pbSplash.Width = fSplash.Width;
            pbSplash.Visible = true;
            pbSplash.Increment(5);

            bool bIdFA = Regex.IsMatch(peli.idFA, @"[1-9]\d{5}");
            if (!bIdFA && (string.IsNullOrEmpty(fileLocal)))
            {//aquí no debiera entrar
                fSplash.Dispose();
                fSplash = null;
                MessageBox.Show("El parámetro idFA debe ser un valor entre 100000 y 999999. Valor pasado a la función: " + peli.idFA, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            
            string html = string.IsNullOrEmpty(fileLocal) ? getDataWebR(urlFromIdFA(peli.idFA)) : getDataWebR(fileLocal);
            if (html != null)
                if (html == "-1")//si devuelve -1 anulo
                {
                    fSplash.Dispose();
                    fSplash = null;
                    return false;
                }
            if (string.IsNullOrEmpty(html))
            {
                if (iErrorforbidden > 2)
                {
                    fSplash.Dispose();
                    fSplash = null;
                    return false;
                }
                //Log.Warn("ERROR document null getMovieFA " + peli.idFA + " iErrorforbidden=" + iErrorforbidden);
                System.Threading.Thread.Sleep(2000);
                paramPeli peliError = new paramPeli();
                peliError.idFA = peli.idFA;
                if (getMovieFA(ref peliError, bIMDb, refineSearchIMDb, getDownLinks, mostrarAvisos, bMP, fileLocal, ++iErrorforbidden))
                {
                    //Log.Info("ARREGLADO document null getMovieFA " + peliError.idFA + " iErrorforbidden=" + iErrorforbidden);
                    fSplash.Dispose();
                    fSplash = null;
                    peli = peliError;
                    return true;
                }
                else
                {
                    fSplash.Dispose();
                    fSplash = null;
                    return false;
                }

            }                
            pbSplash.Increment(30);
            peli.Título = match(@"<span itemprop=""name"">(.*?)</span></a></h1>", html, 1);
            peli.Título = Regex.Replace(peli.Título, @"\s+", " ").Trim();//dobles espacios

            List<string> lTipos = new List<string>() { " (C)", " (Serie de TV)", " (TV)" };
            foreach (string Tipo in lTipos)
                if (peli.Título.IndexOf(Tipo) > 0)
                {
                    peli.Título = peli.Título.Replace(Tipo, string.Empty);
                    peli.Tipo += Tipo.Replace(" (", string.Empty).Replace(")", string.Empty) + Environment.NewLine;
                }
            peli.Tipo = RemoveLastCrLf(peli.Tipo.Replace("C", "Cortometraje"));
            string sCalificación = match(@"<div id=""movie-rat-avg"" itemprop=""ratingValue"">(.*?)</div>", html).Trim();
            string sVotos = match(@"<span itemprop=""ratingCount"">(.*?)</span>", html).Trim().Replace(".", string.Empty);
            string sCarátula = match(@"<a class=""lightbox"" href=""(.*?)"" title", html).Trim();
            if (string.IsNullOrEmpty(sCarátula))
                sCarátula = match(@"<div id=""movie-main-image-container"">(.*?)"">", html).Trim().Replace(@"<img src=""", string.Empty).Trim();//días azules 2005
            if ((!string.IsNullOrEmpty(sCarátula)) && (sCarátula != wwwFA + "/imgs/movies/noimgfull.jpg"))
                peli.Carátula = sCarátula;
            peli.Premios = RemoveLastCrLf(string.Join(Environment.NewLine, (string[])(matchAll(@"<div  class=""margin-bottom"">.*?"">(.*?)</div>", match(@"<dd class=""award"">(.*?)</dd>", html))).ToArray(Type.GetType("System.String"))).Replace("</a>", string.Empty));
            peli.Críticas = getCríticas(html);
            processMovieInfo(ref peli, matchAll(@"<dt>(.*?)</dd>", match(@"<dl class=""movie-info"">(.*?)</dl>", html)), mostrarAvisos);

            int nCheck = peli.Votos;
            if (System.Int32.TryParse(sVotos, out nCheck))
                peli.Votos = nCheck;

            double dCheck = peli.Calificación;
            if (System.Double.TryParse(sCalificación, out dCheck))
                peli.Calificación = (float)dCheck;

            getAKA(ref peli);
            if (bIMDb)
            {
                fSplash.Text = "buscando en IMDb";
                frmDrawString(fSplash, fSplash.Text);
                getIMDbInfo(pbSplash, ref peli, refineSearchIMDb, bMP, mostrarAvisos);
            }
            pbSplash.Increment(30);

            string sIntérpretesIMDb = string.Empty;
            if (peli.IntérpretesIMDb != null)
                foreach (string[] Intérprete in peli.IntérpretesIMDb)
                    sIntérpretesIMDb += string.Join("_-_", Intérprete) + Environment.NewLine;
            sIntérpretesIMDb = RemoveLastCrLf(sIntérpretesIMDb).Replace(Environment.NewLine, "_~_");

            string MediaVideos = string.Empty;
            if (peli.MediaVideos != null)
                foreach (string[] MediaVideo in peli.MediaVideos)
                    MediaVideos += string.Join("_-_", MediaVideo) + Environment.NewLine;
            MediaVideos = RemoveLastCrLf(MediaVideos).Replace(Environment.NewLine, "_~_");
            peli.DownLinks = "https://mega.co.nz/SeNecesitaCuentaGratuitaEnAlgúnPortalDeDescargas#F!dYfCfJJg!yflCsfwqGz_BfTlaL21OJXg";
            PropertyInfo[] pi = ((object)peli).GetType().GetProperties();
            string sGetAll = string.Empty;
            foreach (var p in pi)
                if ((p.Name != "GetAll") && (p.Name != "IntérpretesIMDb") && (p.Name != "MediaVideos"))
                    sGetAll += p.Name + " = " + p.GetValue(peli, null).ToString().Replace(Environment.NewLine, "_~_") + Environment.NewLine;
            sGetAll += "MediaVideos = " + MediaVideos + Environment.NewLine;
            sGetAll += "IntérpretesIMDb = " + sIntérpretesIMDb;//sin Environment.NewLine final
            peli.GetAll = sGetAll;
            checkMovieValues(ref peli, mostrarAvisos);//se puede usar como filtro de valores correctos
            fSplash.Dispose();
            fSplash = null;
            return true;
        }

        private void getIMDbInfo(ProgressBar pbSplash, ref paramPeli peli, bool refineSearchIMDb, bool bMP, bool mostrarAvisos = false)
        {
            string IntérpretPrincipal = normalize(peli.IntérpretPrincipal);
            string sTONormalizado = Regex.Replace(peli.TítuloOriginal, @"(/.*?)$", string.Empty);//cambiado para nymphomaniac por "/"
            if (!peli.TítuloOriginal.StartsWith("("))
                sTONormalizado = Regex.Replace(sTONormalizado, @" \(.*?\)", string.Empty);
            string sBuscaIMDB = "\"" + string.Join("\" \"", new string[] { sTONormalizado, peli.Año.ToString() }) + "\"";
            string sSearchIMDb = string.Empty;
            if (refineSearchIMDb)
                sSearchIMDb = peli.Título;
            peli.Buscado = sBuscaIMDB;
            IMDb PeliIMDb = new IMDb(pbSplash, ref sBuscaIMDB, sSearchIMDb, bMP, mostrarAvisos);
            /*activando este bloque sólo busca ImdbURL en google, sin parsear la página en IMDb
            if (bMP)//para MP sólo necesitaríamos el idIMDb
            {
                peli.idIMDb = idFromUrlIMDb(sBuscaIMDB);
                return;
            }*/
            if (!string.IsNullOrEmpty(PeliIMDb.Id))
            {
                checkCoherencia(peli, PeliIMDb, mostrarAvisos, bMP);
                peli.Lema = PeliIMDb.Tagline;
                peli.CarátulaIMDb = PeliIMDb.Poster;
                peli.ArgumentoIMDb = PeliIMDb.Plot;
                peli.idIMDb = PeliIMDb.Id;
                peli.CalificaciónMPAA = getMpaaRating(PeliIMDb.MpaaRating);
                peli.MediaImages = PeliIMDb.MediaImages;
                peli.IntérpretesIMDb = PeliIMDb.FullCast;
                peli.MediaVideos = PeliIMDb.MediaVideos;
                peli.Productores = string.Join(Environment.NewLine, (string[])PeliIMDb.Producers.ToArray(Type.GetType("System.String")));
                peli.Oscars = PeliIMDb.Oscars;
                peli.Awards = PeliIMDb.Awards;
                peli.Nominations = PeliIMDb.Nominations;
                int nCheck = peli.Top250;
                if (System.Int32.TryParse(PeliIMDb.Top250, out nCheck))
                    peli.Top250 = nCheck;
            }
        }

        private void processMovieInfo(ref paramPeli peli, ArrayList movieinfo, bool mostrarAvisos = false)
        {
            List<string> ListCheck = new List<string> { "Título original", "Año", "Duración", "País", "Director", "Reparto", "Género", "Sinopsis", "Guión", "Música", "Fotografía", "Productora", "Web oficial" };
            List<string[]> ListOk = new List<string[]>();
            foreach (string sCheck in movieinfo)
            {
                string[] sNameValue = Regex.Split(sCheck, @"</dt>(.*?)<dd?.", RegexOptions.None);
                ListOk.Add(new string[] { sNameValue[0], sNameValue[2] });
            }
            foreach (string item in ListCheck)
            {
                string sValue = string.Empty;
                foreach (string[] sCheck in ListOk)
                    if (item == sCheck[0])
                    {
                        sValue = sCheck[1];
                        break;
                    }
                if (string.IsNullOrEmpty(sValue))
                    if (mostrarAvisos)
                        MessageBox.Show("No hay valor para el campo " + item + " en la película " + peli.Título, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                if (!string.IsNullOrEmpty(sValue))
                {
                    switch (item)
                    {
                        case "Título original":
                            List<string> lATipos = new List<string>() { " (S)", " (TV Series)", " (TV)" };
                            foreach (string Tipo in lATipos)
                                sValue = sValue.Replace(Tipo, string.Empty);
                            peli.TítuloOriginal = Regex.Replace(sValue, @"\s+", " ").Trim();//dobles espacios
                            break;
                        case "Año":
                            string sAño = sValue;
                            int nCheck = peli.Año;
                            if (System.Int32.TryParse(sAño, out nCheck))
                                peli.Año = nCheck;
                            break;
                        case "Duración":
                            string sDuración = sValue.Replace(" min.", string.Empty);
                            nCheck = peli.Duración;
                            if (System.Int32.TryParse(sDuración, out nCheck))
                                peli.Duración = nCheck;
                            break;
                        case "País":
                            peli.País = match(@"title=""(.*)""></span>", sValue);
                            break;
                        case "Sinopsis":
                            peli.Argumento = Regex.Replace(sValue, @"<br.*?>", Environment.NewLine);
                            break;//
                        case "Guión":
                            peli.Guión = sValue.Trim();
                            break;
                        case "Música":
                            peli.Música = sValue.Trim();
                            break;
                        case "Fotografía":
                            peli.Fotografía = sValue.Trim();
                            break;
                        case "Productora":
                            peli.Productora = sValue.Trim().Replace(" / ", Environment.NewLine).Replace("; ", Environment.NewLine);
                            break;
                        case "Web oficial":
                            peli.WebOficial = match(@""">(.*)</a>", sValue.Replace(@"class=""web-url"">", string.Empty), 1);
                            break;
                        case "Director":
                            ArrayList ad = matchAll(@""">(.*?)</a>", sValue);
                            string sDirectores = string.Join(Environment.NewLine, (string[])ad.ToArray(Type.GetType("System.String")));
                            peli.Directores = sDirectores.Replace(" (Creator)", string.Empty);
                            if (!string.IsNullOrEmpty(peli.Directores))
                                peli.Director = peli.Directores.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)[0].ToString().Trim();
                            break;
                        case "Reparto":
                            ArrayList ar = matchAll(@""">(.*?)</a>", sValue);
                            string sIntérpretes = string.Join(Environment.NewLine, (string[])ar.ToArray(Type.GetType("System.String")));
                            sIntérpretes = sIntérpretes.Replace("Documentary", string.Empty).Replace("Animation", string.Empty);
                            if (sIntérpretes.IndexOf(Environment.NewLine) == 0)
                                sIntérpretes = sIntérpretes.Substring(Environment.NewLine.Length);//eliminar Environment.NewLine, ej Cosmos 1980
                            peli.Intérpretes = RemoveLastCrLf(sIntérpretes);
                            if (!string.IsNullOrEmpty(sIntérpretes))
                                peli.IntérpretPrincipal = sIntérpretes.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)[0].ToString().Trim();
                            break;
                        case "Género":
                            string[] splitGéneros = sValue.Split("|".ToCharArray());
                            if (splitGéneros.Length > 0)
                            {
                                ArrayList ag = matchAll(@""">(.*?)</a>", splitGéneros[0]);
                                peli.Géneros = RemoveLastCrLf(string.Join(Environment.NewLine, (string[])ag.ToArray(Type.GetType("System.String"))));
                                if (!string.IsNullOrEmpty(peli.Géneros))
                                    peli.Género = RemoveLastCrLf(peli.Géneros.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)[0].ToString().Trim());
                            }
                            if (splitGéneros.Length > 1)
                            {
                                ArrayList atg = matchAll(@""">(.*?)</a>", splitGéneros[1]);
                                peli.TagGéneros = string.Join(Environment.NewLine, (string[])atg.ToArray(Type.GetType("System.String")));
                            }
                            break;
                        default:
                            //aquí no debiera llegar nunca
                            MessageBox.Show("Algún campo no está correctamente manipulado" + peli.Título + " - " + peli.Año, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            break;
                    }
                }
            }
        }

        public string getCríticas(string html)
        {
            ArrayList alCríticas = matchAll(@"<div class=""pro-review"">(.*?)</li>", match(@"<ul id=""pro-reviews"">(.*?)</ul>", html));
            if (alCríticas != null)
            {
                string sCríticas = string.Empty;
                foreach (string review in alCríticas)
                {
                    string sCríticaTexto = match(@"<div>(.*?)(<img|</div>)", review);
                    string sCríticaAutor = match(@"<div class=""pro-crit-med"">(.*?)<img", review);
                    string sCríticaLink = match(@"href=""(.*?)""", review);
                    string sCríticaTipo = match(@" title='(.*?)' src", review);
                    if (!string.IsNullOrEmpty(sCríticaTipo))
                        sCríticaTipo = " (" + sCríticaTipo.Replace("crítica ", string.Empty).Replace("positiva", "+").Replace("negativa", "-").Replace("neutral", "=") + ")";
                    sCríticas += string.Join(Environment.NewLine, new string[] { "\t" + sCríticaTexto, sCríticaAutor + sCríticaTipo, sCríticaLink, Environment.NewLine });
                }
                sCríticas = Regex.Replace(sCríticas, @"<br.*?>", Environment.NewLine);
                sCríticas = Regex.Replace(sCríticas, @"★", "*");
                sCríticas = Regex.Replace(sCríticas, Environment.NewLine + Environment.NewLine + Environment.NewLine, Environment.NewLine + Environment.NewLine);
                return RemoveLastCrLf(sCríticas);
            }
            return string.Empty;
        }

        public void getAKA(ref paramPeli movieFA)
        {
            string rx = @"\(AKA(.*?)\)";
            ArrayList al = matchAll(rx, movieFA.Título + movieFA.TítuloOriginal);
            movieFA.AKA = string.Join(Environment.NewLine, (string[])al.ToArray(Type.GetType("System.String")));
            rx = @"\(AKA.*?\)";
            movieFA.Título = Regex.Replace(movieFA.Título, rx, string.Empty).Trim();
            movieFA.TítuloOriginal = Regex.Replace(movieFA.TítuloOriginal, rx, string.Empty).Trim();
        }

        public string getMpaaRating(string MpaaRating)
        {
            //http://www.mecd.gob.es/cultura-mecd/areas-cultura/cine/conceptos-cine-y-audiovisual/calificacion-de-peliculas.html
            List<string> lEng = new List<string>() { "NC-17", "R", "PG-13", "PG", "G" };
            List<string> lSpa = new List<string>() { ">18"  , ">16", ">12"  , ">7", "TP" };
            string sMpaaRating = "NC";
            for (int i = 0; i < lEng.Count; i++)
                if (lEng.Contains(MpaaRating))
                    return lSpa[i];
            return sMpaaRating;
        }

        private bool checkCoherencia(paramPeli movieFA, IMDb movieIMDb, bool mostrarAvisos, bool bMP)
        {//TODO This regex finds last coma: (,)[^,]*$
            string[] specialChars = @"¡ ! \ / : * & ¿ ? "" < > | ( ) = ' , [ ] _".Split(' ');//eliminamos todo menos los guiones

            List<string> lTO = new List<string>() { Regex.Replace(movieFA.TítuloOriginal, @"\(.*?\)", ""), movieIMDb.Title, movieIMDb.OriginalTitle };
            List<string> l = new List<string>();
            string rx = @"\(.*?\)";
            foreach (string TO in lTO)
            {
                string s = TO;
                foreach (string specialChar in specialChars)
                    s = s.Replace(specialChar, string.Empty);
                s = Regex.Replace(s, @"-.*?$", "");//desde guión hasta final de palabra
                l.Add(Regex.Replace(s.Replace(" ", string.Empty), rx, string.Empty).Trim());
            }
            bool bTO1 = false;
            bool bTO2 = false;
            if (l.Count == 3)
            {
                bTO1 = String.Equals(l[0], l[1], StringComparison.CurrentCultureIgnoreCase);
                bTO2 = String.Equals(l[0], l[2], StringComparison.CurrentCultureIgnoreCase);
            }
            int iYear = 0;
            bool bTO = (bTO1 | bTO2);

            Int32.TryParse(movieIMDb.Year, out iYear);
            bool bDif = false;
            if ((movieFA.Año > 1900) && (iYear > 1900) )
                bDif = Math.Abs(movieFA.Año - iYear) > 2;
            if (!bDif && bTO)
                return true;
            if (!bMP)
            {//TODO Opción de no verificar coherencia
                RunCMD(urlFromIdIMDb(movieIMDb.Id) + "combined", mostrarAvisos);
                RunCMD(urlFromIdFA(movieFA.idFA), mostrarAvisos);
            }
            return false;
        }

        internal bool checkMovieValues(ref paramPeli movie, bool mostrarAvisos)
        {
            bool bRes = true;
            PropertyInfo[] pi = ((object)movie).GetType().GetProperties();
            foreach (var p in pi)
            {
                string name = p.Name;
                var value = p.GetValue(movie, null);
                if (p.PropertyType.FullName == "System.String")
                {
                    //bloque deshabilitado, solo cambia bRes a False
                    if ((string.IsNullOrEmpty((string)value)))
                        if ("ArgumentoIntérpretesTítuloDirectorGénero".IndexOf(p.Name) > -1)//campos que no pueden ser empty
                            bRes = false;//p.SetValue(movie, "SinDatos", null);

                    string s = (string)value;
                    //sólo se permiten ciertos campos con más de 255 caractéres
                    if ((s.Length > 254) && ("ArgumentoIntérpretesCríticasPremiosGetAllIntérpretesIMDbMediaVideosMediaImagesDownLinks".IndexOf(p.Name) < 0))
                    {//TODO  REGEX
                        s = s.Remove(254);
                        int iLastComma = s.LastIndexOf(",");
                        int iLastNewLine = s.LastIndexOf(Environment.NewLine);
                        int iLast = iLastNewLine + iLastComma + 1;
                        if (iLast > -1)
                            s = s.Remove(iLast);
                        bRes = false;
                        p.SetValue(movie, s, null);
                    }
                }
            }
            return bRes;
        }

        public bool findMovieFA(string peli, ref string idFA, ref string buscado, int año = 0, int precisión = 0, bool mostrarAvisos = false, object pelisMP = null, int iErrorforbidden = 0, object sender = null)
        {
            frmSplash fSplash = new frmSplash(new System.Drawing.Font("Mistral", 24F));
            fSplash.Text = "buscando " + peli;
            fSplash.Show();
            ProgressBar pbSplash = (ProgressBar)fSplash.Controls[0];
            frmDrawString(fSplash, fSplash.Text);
            pbSplash.Width = fSplash.Width;
            pbSplash.Visible = true;
            pbSplash.Increment(5);
            idFA = string.Empty;
            buscado = peli;
            int AñoMax = año + precisión;
            int AñoMin = año - precisión;
            if (AñoMin < precisión)
            {
                AñoMax = 0;
                AñoMin = 0;
            }
            string urlSearchMovie = wwwFA + "/es/advsearch.php?stext=" + HttpUtility.UrlEncode(peli, Encoding.UTF8) + "&stype=title&fromyear=" + AñoMin + "&toyear=" + AñoMax;
            string document = getDataWebR(urlSearchMovie);
            pbSplash.Increment(30);
            if (string.IsNullOrEmpty(document))
            {
                if  (iErrorforbidden > 2)
                {
                    idFA = "-1";
                    fSplash.Dispose();
                    fSplash = null;
                    return false;
                }
                //Log.Warn("ERROR document null buscando " + peli + " iErrorforbidden=" + iErrorforbidden);
                System.Threading.Thread.Sleep(2000);
                string idFAError = string.Empty;
                ArrayList pelisMPError = null;
                if (pelisMP != null)
                    pelisMPError = new ArrayList();
                if (findMovieFA(peli, ref idFAError, ref buscado, año, precisión, mostrarAvisos, pelisMPError, ++iErrorforbidden))
                {
                    //Log.Info("ARREGLADO document null buscando " + peli + " iErrorforbidden=" + iErrorforbidden);
                    idFA = idFAError;
                    fSplash.Dispose();
                    fSplash = null;
                    if (pelisMP != null)
                        if (pelisMPError.Count > 0)
                            foreach (string[] item in pelisMPError)
                                ((ArrayList)pelisMP).Add(item);
                    return true;
                }
                else
                {
                    idFA = "-1";
                    fSplash.Dispose();
                    fSplash = null;
                    return false;
                }
            }
            bool bNotFound = false;
            if (document.IndexOf(@"<div id=""adv-search-no-results"">") > -1)
                // sin resultado 'No hay resultados exactos'                //<b>No se han encontrado coincidencias.</b></div>
                bNotFound = true;
            if (document.IndexOf(@"<div id=""adv-search-pager-info"">") < -1)
                // por algún motivo, no se encuentra 'adv-search-pager-info'
                bNotFound = true;
            if (bNotFound)
            {
                if (mostrarAvisos)
                    MessageBox.Show("No se encontró resultado válido para " + peli + " - " + año, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                fSplash.Dispose();
                fSplash = null;
                return false;
            }
            string resultados = string.Empty;
            resultados = match(@"<div style=""float: right;""><b>(.*?)</b>", document);
            int nResultados = 0;
            Int32.TryParse(resultados, out nResultados);
            if ((nResultados > 50) || (nResultados == 0))
            {
                //se devuelve el nº de resultados encontrados o string.empty
                if (nResultados > 0)
                    idFA = nResultados.ToString();
                fSplash.Dispose();
                fSplash = null;
                return false;
            }

            List<string[]> lPelis = new List<string[]>();
            recursiveSearch(pbSplash, lPelis, document);

            fSplash.Dispose();
            fSplash = null;
            if (pelisMP != null)
            {
                if (lPelis.Count > 0)
                    idFA = lPelis[0][1];//asigna idFA al primer valor de la Lista, así se puede usar desde COM, si sólo se quiere un valor y no una lista
                foreach (string[] miniPeli in lPelis)
                {
                    string sParam = string.Empty;
                    for (int i = 2; i < 5; i++)
                        if (!string.IsNullOrEmpty(miniPeli[i]))
                            sParam += "-" + miniPeli[i];
                    ((ArrayList)pelisMP).Add(new string[2] { urlFromIdFA(miniPeli[1]), miniPeli[0] + " " + sParam });
                }
                return true;
            }
            string sAño = año > 0 ? "(" + año + ") " : string.Empty;
            dlgSelector dlgSelector = new dlgSelector("\"" + peli + "\" " + sAño + " [" + resultados + "] posibles.", lPelis, buscado, typeof(APIFilmAffinityIMDb.Functions), mostrarAvisos);
            idFA = dlgSelector.idFA;
            dlgSelector.Dispose();
            dlgSelector = null;
            return !string.IsNullOrEmpty(idFA);
        }

        private void processMiniInfo(List<string[]> lPelis, string document)
        {
            ArrayList cTitles = matchAll(@"<div class=""movie-card movie-card-1"" data-movie-id(.*?)</div></div></div>", document);//"//div[@class='mc-title']") amplio a las imágenes
            if (cTitles == null)//si llegó aquí es página no encontrada Forbidden, Acceso denegado, etc
                return;
            foreach (string title in cTitles)//cada título de la página de resultados se añade a una lista
            {
                string mcinfocontainer = match(@"<div class=""mc-info-container"">(.*?)</div></div>", title);
                string mctitle = match(@"<div class=""mc-title"">(.*?)</div>", title);
                string buscado = match(@".html"">(.*?)</a>", mctitle).Trim();
                string País = match(@"title=""(.*?)"">", mctitle).Trim();
                string Año = match(@"</a>(.*?)<img src", mctitle).Trim();
                string lsidFA = match(@"<a href=""/es/film(.*?).html"">", mctitle).Trim();
                string urlImg = match(@" src=""http://pics.filmaffinity.com/(.*?).jpg""", title).Trim();
                if (!string.IsNullOrEmpty( urlImg))
                    urlImg = "http://pics.filmaffinity.com/" + urlImg + ".jpg";

                string mcdirector = match(@"<div class=""mc-director"">(.*?)</div>", title);
                string Director = match(@""">(.*?)</a>", mcdirector).Trim();
                string mcratings = match(@"/imgs/ratings(.*?)"" >", title);
                string Estrellas = match(@"/(.*?).png""", mcratings).Trim();
                string Calificación = match(@"alt=""(.*?)$", mcratings).Trim();
                string mccast = match(@"<div class=""mc-cast"">(.*?)</div>", title);
                string Intérprete = match(@""">(.*?)</a>", mccast).Trim();
                lPelis.Add(new string[8] { buscado, lsidFA, buscarAño(Año).ToString(), Director, Intérprete, Calificación, Estrellas, urlImg });
            }
        }

        private void recursiveSearch(ProgressBar pbSplash, List<string[]> lPelis, string document)
        {
            string pager = match(@"<div class=""pager"">(.*?)</div>", document);
            ArrayList cPager = matchAll(@"<a href=""(.*?)"">", pager);
            if (cPager.Count > 0)
            {//actualiza el nodo de la página recién cargada
                if (pager.Contains(">>>"))// corresponde a "&gt;&gt;" OR CONTADOR > 20
                {
                    processMiniInfo(lPelis, document);//si no lo agrego aqui se pone en orden inverso
                    string urlSearchMovie = wwwFA + "/es/" + cPager[cPager.Count - 1].ToString();//el último ítem tiene el enlace a la página siguiente
                    APIFilmAffinityIMDb.Functions f = new APIFilmAffinityIMDb.Functions();
                    document = f.getDataWebR(urlSearchMovie);
                    pbSplash.Increment(10);
                    recursiveSearch(pbSplash, lPelis, document);
                    return;
                }
            }
            processMiniInfo(lPelis, document);
            return;
        }

        public string getDataWebR(string url, bool fIMDb = false)
        {
            HttpWebRequest wReq = (HttpWebRequest)WebRequest.Create(url);
            List<string[]> lValues = new List<string[]>();
            setHeadersValues(lValues, fIMDb);
            int iHeaders = 0;
            for (iHeaders = 0; iHeaders < (lValues.Count - 2); iHeaders++)
                wReq.Headers[lValues[iHeaders][0]] = lValues[iHeaders][1];
            wReq.Referer = lValues[iHeaders++][1];
            wReq.UserAgent = lValues[iHeaders][1];
            wReq.Timeout = 10000;
            wReq.Proxy.Credentials = CredentialCache.DefaultCredentials;
            wReq.ReadWriteTimeout = 10000;
            try
            {
                WebResponse wResponse = wReq.GetResponse();
                Stream st = wResponse.GetResponseStream();
                string sRet = getStream(st, fIMDb);
                return string.IsNullOrEmpty( sRet) ? "-1" : sRet;//páginas en blanco sin error -> "-1", así no lo intenta más
            }
            catch (System.Net.WebException eWebException)
            {
                //throw eWebException;
                string s = string.Empty;
                //using (var stream = eWebException.Response.GetResponseStream())                using (var reader = new StreamReader(stream))                    s += reader.ReadToEnd();
                //if (!url.Contains("google"))                    //Log.Info(" eWebException " + url + " " + eWebException.Message);//google Error en el servidor remoto: (503) Servidor no disponible//Se excedió el tiempo de espera de la operación
                //if (!string.IsNullOrEmpty(s))                    //Log.Warn(" " + s);
            }
            catch (System.Threading.ThreadAbortException exThreadAbortException)
            {
                StackTrace st = new StackTrace(true);
                for (int iError = 0; iError < st.FrameCount; iError++)
                {
                    // Note that high up the call stack, there is only one stack frame.
                    StackFrame sf = st.GetFrame(iError);
                    if (sf.GetFileLineNumber() > 0)
                    {
                        ////Log.Error(" exThreadAbortException Method: {0}", sf.GetMethod());
                        //Log.Error(" exThreadAbortException Line Number: {0}", sf.GetFileLineNumber().ToString());
                    }
                }
                //Log.Error(" exThreadAbortException inesperado en getDataWebR con: " + url + " - " + exThreadAbortException.Message + " - " + exThreadAbortException.Source);
            }
            catch (Exception exOther)
            {
                StackTrace st = new StackTrace(true);
                for (int iError = 0; iError < st.FrameCount; iError++)
                {
                    // Note that high up the call stack, there is only one stack frame.
                    StackFrame sf = st.GetFrame(iError);
                    if (sf.GetFileLineNumber() > 0)
                    {
                        //Log.Error(" eWebExceptionOther Method: {0}", sf.GetMethod());
                        //Log.Error(" eWebExceptionOther Line Number: {0}", sf.GetFileLineNumber().ToString());
                    }
                }
                //Log.Error(" eWebExceptionOther inesperado en getDataWebR con: " + url + " - " + exOther.Message + " - " + exOther.Source);
            }
            return string.Empty;
        }

        public string getDataWebC(string url, bool fIMDb = false)
        {//idéntica utilizando la clase WebClient NO USADA, salvo para fileLocal
            WebClient wClient = new WebClient();
            List<string[]> lHeadersValues = new List<string[]>();
            setHeadersValues(lHeadersValues, fIMDb);
            foreach (string[] sHeadersValue in lHeadersValues)
                wClient.Headers[sHeadersValue[0]] = sHeadersValue[1];
            Stream st = wClient.OpenRead(url);
            return getStream(st, fIMDb);
        }

        public void setHeadersValues(List<string[]> lHeadersValues, bool fIMDb)
        {
            Random r = new Random(DateTime.Now.Millisecond);
            lHeadersValues.Add(new string[] { "X-Forwarded-For", r.Next(0, 255) + "." + r.Next(0, 255) + "." + r.Next(0, 255) + "." + r.Next(0, 255) });
            //lHeadersValues.Add(new string[] { "X-Forwarded-For", "172.10" + "." + r.Next(0, 255) + "." + r.Next(0, 255) });
            if (fIMDb)
                lHeadersValues.Add(new string[] { "Accept-Language", "es" });//MP usa "en-US,en;q=0.5", sin resultados aparentemente distintos
            lHeadersValues.Add(new string[] { "Referer", wwwFA });
            lHeadersValues.Add(new string[] { "User-Agent", "Mozilla/" + r.Next(3, 5) + ".0 (Windows NT " + r.Next(3, 5) + "." + r.Next(0, 2) + "; rv:2.0.1) Gecko/20100101 Firefox/" + r.Next(3, 5) + "." + r.Next(0, 5) + "." + r.Next(0, 5) });
        }

        internal string getStream(Stream st, bool fIMDb)
        {
            StreamReader sr = null;
            string sDecode = string.Empty;
            using (sr = new StreamReader(st))
            {
                StringBuilder sb = new StringBuilder();
                while (!sr.EndOfStream)
                    sb.Append(sr.ReadLine());
                sDecode = sb.ToString();
            }
            sDecode = HttpUtility.HtmlDecode(sDecode);
            return sDecode;
        }

        private void frmDrawString(Form frm, string drawString)
        {//TODO CAMBIAR A USING
            frm.Refresh();
            System.Drawing.Graphics formGraphics = frm.CreateGraphics();
            System.Drawing.SolidBrush drawBrush = new System.Drawing.SolidBrush(System.Drawing.Color.Brown);
            float x = 0.0F;
            float y = 50.0F;
            System.Drawing.StringFormat drawFormat = new System.Drawing.StringFormat();
            formGraphics.DrawString(" " + drawString, new System.Drawing.Font("Mistral", 26F, System.Drawing.FontStyle.Regular), drawBrush, x, y, drawFormat);
            drawBrush.Dispose();
            formGraphics.Dispose();
        }

        /// Minor adjust to the code above
        /// <summary>
        /// Flashes a window until the window comes to the foreground
        /// Receives the form that will flash
        /// </summary>
        /// <param name="hWnd">The handle to the window to flash</param>
        /// <returns>whether or not the window needed flashing</returns>
        public bool FlashWindowEx(Form frm)
        {
            IntPtr hWnd = frm.Handle;
            FLASHWINFO fInfo = new FLASHWINFO();

            fInfo.cbSize = Convert.ToUInt32(Marshal.SizeOf(fInfo));
            fInfo.hwnd = hWnd;
            fInfo.dwFlags = (uint)(FlashWindow.FLASHW_ALL | FlashWindow.FLASHW_TIMERNOFG);
            fInfo.uCount = UInt32.MaxValue;
            fInfo.dwTimeout = 0;

            return FlashWindowEx(ref fInfo);
        }

        public void ForceForegroundWindow(IntPtr hWnd)
        {
            uint foreThread = GetWindowThreadProcessId(GetForegroundWindow(),
                IntPtr.Zero);
            uint appThread = GetCurrentThreadId();
            const uint SW_SHOW = 5;

            if (foreThread != appThread)
            {
                AttachThreadInput(foreThread, appThread, true);
                BringWindowToTop(hWnd);
                ShowWindow(hWnd, SW_SHOW);
                AttachThreadInput(foreThread, appThread, false);
            }
            else
            {
                BringWindowToTop(hWnd);
                ShowWindow(hWnd, SW_SHOW);
            }
        }

        #region NativeWindows
        [StructLayout(LayoutKind.Sequential)]
        public struct FLASHWINFO
        {
            public UInt32 cbSize;
            public IntPtr hwnd;
            public UInt32 dwFlags;
            public UInt32 uCount;
            public UInt32 dwTimeout;
        }

        public enum FlashWindow : int
        {
            /// <summary>
            /// Stop flashing. The system restores the window to its original state. 
            /// </summary>    
            FLASHW_STOP = 0,

            /// <summary>
            /// Flash the window caption 
            /// </summary>
            FLASHW_CAPTION = 1,

            /// <summary>
            /// Flash the taskbar button. 
            /// </summary>
            FLASHW_TRAY = 2,

            /// <summary>
            /// Flash both the window caption and taskbar button.
            /// This is equivalent to setting the FLASHW_CAPTION | FLASHW_TRAY flags. 
            /// </summary>
            FLASHW_ALL = 3,

            /// <summary>
            /// Flash continuously, until the FLASHW_STOP flag is set.
            /// </summary>
            FLASHW_TIMER = 4,

            /// <summary>
            /// Flash continuously until the window comes to the foreground. 
            /// </summary>
            FLASHW_TIMERNOFG = 12
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        protected static extern bool FlashWindowEx(ref FLASHWINFO pwfi);

        [DllImport("user32.dll")]
        protected static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        protected static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        // When you don't want the ProcessId, use this overload and pass 
        // IntPtr.Zero for the second parameter
        [DllImport("user32.dll")]
        protected static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr ProcessId);

        [DllImport("kernel32.dll")]
        protected static extern uint GetCurrentThreadId();

        /// The GetForegroundWindow function returns a handle to the 
        /// foreground window.
        [DllImport("user32.dll")]
        protected static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        protected static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("user32.dll", SetLastError = true)]
        protected static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        protected static extern bool BringWindowToTop(HandleRef hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        protected static extern bool SetWindowPos(IntPtr hWnd, Int32 hWndInsertAfter, Int32 X, Int32 Y, Int32 cx, Int32 cy, uint uFlags);

        [DllImport("user32.dll")]
        protected static extern bool ShowWindow(IntPtr hWnd, uint nCmdShow);
        #endregion NativeWindows

    }

    /// <summary>
    /// La clase implementan el interface, de tal forma que COM sólo mostrará estas clases
    /// </summary>
    [Description("Clase con los detalles en FilmAffinity de una película. El campo o propiedad GetAll contiene todos los nombres y valores de dichas propiedades")]
    [ClassInterface(ClassInterfaceType.None)]
    //[DefaultProperty("Título")]//TODO don't work
    public class paramPeli : IParamPeli
    {
        #region private def
        private int _mID = -1;
        private string _Directores = string.Empty;
        private string _Director = string.Empty;
        private string _Género = string.Empty;
        private string _Argumento = string.Empty;
        private string _Carátula = string.Empty;
        private string _sTítuloOriginal = string.Empty;
        private string _sTítulo = string.Empty;
        private string _idFA = string.Empty;
        private string _Intérpretes = string.Empty;
        private string _Buscado = string.Empty;
        private int _Año = 1900;
        private int _Duración = 0;
        private int _idGénero = -1;
        private string _Géneros = string.Empty;
        private string _País = string.Empty;
        private string _Guión = string.Empty;
        private string _Música = string.Empty;
        private string _Fotografía = string.Empty;
        private string _Productora = string.Empty;
        private string _Premios = string.Empty;
        private string _Críticas = string.Empty;
        private int _Votos = 0;
        private float _Calificación = -1;
        private string _WebOficial = string.Empty;
        private string _TagGéneros = string.Empty;
        private string _Lema = string.Empty;
        private string _CalificaciónMPAA = string.Empty;
        private string _idIMDb = string.Empty;
        private string _CarátulaIMDb = string.Empty;
        private string _ArgumentoIMDb = string.Empty;
        private string _MediaImages = string.Empty;
        private string _IntérpretPrincipal = string.Empty;
        private string _DownLinks = string.Empty;
        private string _Tipo = string.Empty;
        private string _AKA = string.Empty;
        private string _Productores = string.Empty;
        private string _Cinematographers = string.Empty;
        private string _Editors = string.Empty;
        private string _Oscars = string.Empty;
        private string _Awards = string.Empty;
        private string _Nominations = string.Empty;
        private int _Top250 = 0;
        private string _GetAll = string.Empty;

        #endregion

        #region public def
        public int ID
        {
            get { return _mID; }
            set { _mID = value; }
        }

        public int idGénero
        {
            get { return _idGénero; }
            set { _idGénero = value; }
        }

        public string Géneros
        {
            get { return _Géneros; }
            set { _Géneros = value; }
        }

        public int Duración
        {
            get { return _Duración; }
            set { _Duración = value; }
        }

        public string Directores
        {
            get { return _Directores; }
            set { _Directores = value; }
        }

        public string Director
        {
            get { return _Director; }
            set { _Director = value; }
        }

        public string Género
        {
            get { return _Género; }
            set { _Género = value; }
        }

        public string Carátula
        {
            get { return _Carátula; }
            set { _Carátula = value; }
        }

        public string Título
        {
            get { return _sTítuloOriginal; }
            set { _sTítuloOriginal = value; }
        }

        public string TítuloOriginal
        {
            get { return _sTítulo; }
            set { _sTítulo = value; }
        }

        public string Argumento
        {
            get { return _Argumento; }
            set { _Argumento = value; }
        }

        public string Intérpretes
        {
            get { return _Intérpretes; }
            set { _Intérpretes = value; }
        }

        public string Buscado
        {
            get { return _Buscado; }
            set { _Buscado = value; }
        }

        public string idFA
        {
            get { return _idFA; }
            set { _idFA = value; }
        }

        public int Año
        {
            get { return _Año; }
            set { _Año = value; }
        }

        public string País
        {
            get { return _País; }
            set { _País = value; }
        }

        public string Guión
        {
            get { return _Guión; }
            set { _Guión = value; }
        }

        public string Música
        {
            get { return _Música; }
            set { _Música = value; }
        }

        public string Fotografía
        {
            get { return _Fotografía; }
            set { _Fotografía = value; }
        }

        public string Productora
        {
            get { return _Productora; }
            set { _Productora = value; }
        }

        public string Premios
        {
            get { return _Premios; }
            set { _Premios = value; }
        }

        public string Críticas
        {
            get { return _Críticas; }
            set { _Críticas = value; }
        }

        public float Calificación
        {
            get { return _Calificación; }
            set { _Calificación = value; }
        }

        public int Votos
        {
            get { return _Votos; }
            set { _Votos = value; }
        }

        public string WebOficial
        {
            get { return _WebOficial; }
            set { _WebOficial = value; }
        }

        public string TagGéneros
        {
            get { return _TagGéneros; }
            set { _TagGéneros = value; }
        }

        public string Lema
        {
            get { return _Lema; }
            set { _Lema = value; }
        }

        public string CalificaciónMPAA
        {
            get { return _CalificaciónMPAA; }
            set { _CalificaciónMPAA = value; }
        }
        
        public string idIMDb
        {
            get { return _idIMDb; }
            set { _idIMDb = value; }
        }
        
        public string CarátulaIMDb
        {
            get { return _CarátulaIMDb; }
            set { _CarátulaIMDb = value; }
        }
        
        public string ArgumentoIMDb
        {
            get { return _ArgumentoIMDb; }
            set { _ArgumentoIMDb = value; }
        }
        
        public string MediaImages
        {
            get { return _MediaImages; }
            set { _MediaImages = value; }
        }

        public string IntérpretPrincipal
        {
            get { return _IntérpretPrincipal; }
            set { _IntérpretPrincipal = value; }
        }

        public string DownLinks
        {
            get { return _DownLinks; }
            set { _DownLinks = value; }
        }

        public string Tipo
        {
            get { return _Tipo; }
            set { _Tipo = value; }
        }

        public string AKA
        {
            get { return _AKA; }
            set { _AKA = value; }
        }

        public string Productores
        {
            get { return _Productores; }
            set { _Productores = value; }
        }

        public string Cinematographers
        {
            get { return _Cinematographers; }
            set { _Cinematographers = value; }
        }

        public string Editors
        {
            get { return _Editors; }
            set { _Editors = value; }
        }

        public string Oscars
        {
            get { return _Oscars; }
            set { _Oscars = value; }
        }

        public string Awards
        {
            get { return _Awards; }
            set { _Awards = value; }
        }

        public string Nominations
        {
            get { return _Nominations; }
            set { _Nominations = value; }
        }

        public int Top250
        {
            get { return _Top250; }
            set { _Top250 = value; }
        }

        public ArrayList MediaVideos { get; set; }

        public ArrayList IntérpretesIMDb { get; set; }
        
        public string GetAll
        {
            get { return _GetAll; }
            set { _GetAll = value; }
        }
       #endregion
    }

}